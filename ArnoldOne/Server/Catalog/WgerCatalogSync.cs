using System.Net.Http.Json;
using ArnoldOne.Shared.Catalog;
using DbArnold.DbArnold.models;
using Microsoft.EntityFrameworkCore;

namespace ArnoldOne.Server.Catalog;

/// <summary>Hämtar hela övningskatalogen från wger och uppdaterar ExerciseCatalog. Se docs/wger-ovningskatalog-plan.md.</summary>
public sealed class WgerCatalogSync(HttpClient http, ArnoldContext context, CatalogSeed seed)
{
    private const int EnglishLanguageId = 2;

    // shortcut: låset gäller en process, räcker så länge appen körs i en instans och synken startas från admin.
    private static readonly SemaphoreSlim Lock = new(1, 1);

    public async Task<CatalogSyncResultDto> SyncAsync(CancellationToken cancellationToken = default)
    {
        if (!await Lock.WaitAsync(TimeSpan.Zero, cancellationToken))
            throw new InvalidOperationException("En synk pågår redan.");

        try
        {
            var fetched = await FetchAllAsync(cancellationToken);
            var existing = await context.ExerciseCatalog.ToDictionaryAsync(row => row.WgerUuid, cancellationToken);

            // Ett trasigt eller halvt svar från wger får inte inaktivera halva katalogen.
            var activeCount = existing.Values.Count(row => row.IsActive);
            if (fetched.Count == 0 || fetched.Count * 2 < activeCount)
                throw new InvalidOperationException($"wger returnerade {fetched.Count} övningar mot {activeCount} aktiva hos oss. Synken avbröts.");

            var result = new CatalogSyncResultDto { Fetched = fetched.Count };
            var now = DateTimeOffset.UtcNow;
            var seen = new HashSet<Guid>();

            foreach (var exercise in fetched)
            {
                var english = exercise.Translations.FirstOrDefault(t => t.Language == EnglishLanguageId);
                if (english is null || string.IsNullOrWhiteSpace(english.Name) || !seen.Add(exercise.Uuid))
                    continue;

                if (!existing.TryGetValue(exercise.Uuid, out var row))
                {
                    row = new ExerciseCatalog { WgerUuid = exercise.Uuid };
                    context.ExerciseCatalog.Add(row);
                    result.Added++;
                }
                else if (!row.IsActive)
                {
                    result.Reactivated++;
                }

                ApplyText(row, english, result);
                ApplyMetadata(row, exercise, english);
                row.IsActive = true;
                row.LastSyncedUtc = now;
            }

            foreach (var row in existing.Values.Where(row => row.IsActive && !seen.Contains(row.WgerUuid)))
            {
                row.IsActive = false;
                result.Deactivated++;
            }

            await context.SaveChangesAsync(cancellationToken);
            return result;
        }
        finally
        {
            Lock.Release();
        }
    }

    private void ApplyText(ExerciseCatalog row, WgerTranslation english, CatalogSyncResultDto result)
    {
        var hash = CatalogText.SourceHash(english.Name, english.Description);
        var isNew = row.NameEn.Length == 0;
        var changed = !isNew && CatalogText.SourceHash(row.NameEn, row.DescriptionEn) != hash;

        row.NameEn = english.Name.Trim();
        row.DescriptionEn = english.Description ?? string.Empty;

        if (changed)
        {
            result.TextChanged++;
            var status = (CatalogTranslationStatus)row.TranslationStatus;
            if (status is CatalogTranslationStatus.Granskad or CatalogTranslationStatus.BehoverGranskning)
            {
                // Granskad svenska skrivs aldrig över. SourceHash ligger kvar på texten granskaren såg.
                if (status == CatalogTranslationStatus.Granskad)
                    result.NeedsReview++;
                row.TranslationStatus = (byte)CatalogTranslationStatus.BehoverGranskning;
            }
            else
            {
                row.NameSv = null;
                row.DescriptionSv = null;
                row.TranslationStatus = (byte)CatalogTranslationStatus.Saknas;
            }
        }

        if (row.TranslationStatus != (byte)CatalogTranslationStatus.Saknas)
        {
            if (isNew)
                row.SourceHash = hash;
            return;
        }

        row.SourceHash = hash;
        if (seed.Find(row.WgerUuid, hash) is { } translation)
        {
            row.NameSv = translation.NameSv;
            row.DescriptionSv = translation.DescriptionSv;
            row.TranslationStatus = (byte)CatalogTranslationStatus.Auto;
            result.TranslatedFromSeed++;
        }
    }

    private static void ApplyMetadata(ExerciseCatalog row, WgerExercise exercise, WgerTranslation english)
    {
        row.Category = exercise.Category.Name;
        static string MuscleKey(WgerMuscle muscle) => string.IsNullOrWhiteSpace(muscle.NameEn) ? muscle.Name : muscle.NameEn;
        var primary = exercise.Muscles.Select(MuscleKey).Distinct().ToList();
        row.Muscles = string.Join(", ", primary);
        row.MusclesSecondary = string.Join(", ", exercise.MusclesSecondary.Select(MuscleKey).Distinct().Except(primary));
        row.Equipment = string.Join(", ", exercise.Equipment.Select(item => item.Name).Distinct());
        row.Aliases = string.Join("\n", english.Aliases.Select(alias => alias.Alias.Trim()).Where(alias => alias.Length > 0));
        row.ImageUrl = (exercise.Images.FirstOrDefault(image => image.IsMain) ?? exercise.Images.FirstOrDefault())?.Image;
        row.License = exercise.License?.ShortName ?? string.Empty;
        row.LicenseAuthor = english.LicenseAuthor ?? exercise.LicenseAuthor ?? string.Empty;
    }

    private async Task<List<WgerExercise>> FetchAllAsync(CancellationToken cancellationToken)
    {
        var all = new List<WgerExercise>();
        string? url = "api/v2/exerciseinfo/?limit=1000";
        while (url is not null)
        {
            var page = await http.GetFromJsonAsync<WgerPage>(url, cancellationToken)
                ?? throw new InvalidOperationException("Tomt svar från wger.");
            all.AddRange(page.Results);
            url = page.Next;
        }

        return all;
    }
}
