using Tyngd.Shared.Catalog;
using System.Net.Http.Json;

namespace Tyngd.Client.Services;

public class CatalogService
{
    private readonly HttpClient _http;
    private Task<List<CatalogExerciseDto>>? _all;

    public CatalogService(HttpClient http)
    {
        _http = http;
    }

    // shortcut: listan hämtas en gång per sidladdning; en synk syns först efter omladdning.
    public Task<List<CatalogExerciseDto>> GetAllAsync()
    {
        if (_all is null || _all.IsFaulted)
            _all = LoadAsync();
        return _all;
    }

    public async Task<CatalogExerciseDetailDto?> GetAsync(int catalogExerciseId)
    {
        var exercise = await _http.GetFromJsonAsync<CatalogExerciseDetailDto>($"api/catalog/{catalogExerciseId}");
        if (exercise is not null)
            exercise.ImageUrl = AbsoluteUrl(exercise.ImageUrl);
        return exercise;
    }

    // Servern skickar bildens adress relativt sig själv; appen behöver en hel adress för att visa bilden.
    private string? AbsoluteUrl(string? url) =>
        url is null || _http.BaseAddress is null ? url : new Uri(_http.BaseAddress, url).ToString();

    /// <summary>Söker i svenskt och engelskt namn och alias. Alla ord i söktexten måste finnas; namn som börjar med söktexten kommer först.</summary>
    public static IEnumerable<CatalogExerciseDto> Search(IEnumerable<CatalogExerciseDto> all, string? text)
    {
        var words = (text ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (words.Length == 0)
            return all;

        return all
            .Select(exercise => (exercise, haystack: string.Join(" | ", [exercise.Name, exercise.NameEn, .. exercise.Aliases])))
            .Where(item => words.All(word => item.haystack.Contains(word, StringComparison.CurrentCultureIgnoreCase)))
            .OrderByDescending(item => item.exercise.Name.StartsWith(words[0], StringComparison.CurrentCultureIgnoreCase)
                || item.exercise.NameEn.StartsWith(words[0], StringComparison.CurrentCultureIgnoreCase))
            .ThenBy(item => item.exercise.Name.Length)
            .Select(item => item.exercise);
    }

    /// <summary>Katalogsidans filter och sökning; null betyder alla. Favoriter först, annars sökordningen.</summary>
    public static IEnumerable<CatalogExerciseDto> Filter(
        IEnumerable<CatalogExerciseDto> all,
        IReadOnlySet<int> favorites,
        bool onlyFavorites,
        string? category,
        string? muscle,
        string? equipment,
        string? search)
    {
        var rows = all;
        if (onlyFavorites)
            rows = rows.Where(e => favorites.Contains(e.CatalogExerciseId));
        if (category is not null)
            rows = rows.Where(e => e.Category == category);
        if (muscle is not null)
            rows = rows.Where(e => e.PrimaryMuscles.Contains(muscle) || e.SecondaryMuscles.Contains(muscle));
        if (equipment is not null)
            rows = rows.Where(e => e.Equipment.Contains(equipment));
        return Search(rows, search).OrderByDescending(e => favorites.Contains(e.CatalogExerciseId));
    }

    /// <summary>Raden under övningsnamnet: kategori · utrustning · engelskt namn (om det skiljer sig).</summary>
    public static string Meta(CatalogExerciseDto exercise)
    {
        var parts = new List<string> { exercise.Category };
        if (exercise.Equipment.Count > 0)
            parts.Add(string.Join(", ", exercise.Equipment));
        if (!string.Equals(exercise.Name, exercise.NameEn, StringComparison.OrdinalIgnoreCase))
            parts.Add(exercise.NameEn);
        return string.Join(" · ", parts);
    }

    /// <summary>Muskelbelastning för de övningar som är kopplade till katalogen; okopplade räknas inte.</summary>
    public static Dictionary<string, double> MuscleLoadFor(IEnumerable<CatalogExerciseDto> catalog, IEnumerable<int?> catalogExerciseIds)
    {
        var byId = catalog.ToDictionary(exercise => exercise.CatalogExerciseId);
        return MuscleLoad.Sum(catalogExerciseIds
            .Select(id => id is { } value ? byId.GetValueOrDefault(value) : null)
            .OfType<CatalogExerciseDto>()
            .Select(exercise => ((IEnumerable<string>)exercise.PrimaryMuscles, (IEnumerable<string>)exercise.SecondaryMuscles)));
    }

    public async Task<Dictionary<string, double>> MuscleLoadForAsync(IEnumerable<int?> catalogExerciseIds) =>
        MuscleLoadFor(await GetAllAsync(), catalogExerciseIds);

    private async Task<List<CatalogExerciseDto>> LoadAsync()
    {
        var all = await _http.GetFromJsonAsync<List<CatalogExerciseDto>>("api/catalog") ?? new List<CatalogExerciseDto>();
        foreach (var exercise in all)
            exercise.ImageUrl = AbsoluteUrl(exercise.ImageUrl);
        return all;
    }
}
