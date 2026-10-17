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

    public Task<CatalogExerciseDetailDto?> GetAsync(int catalogExerciseId) =>
        _http.GetFromJsonAsync<CatalogExerciseDetailDto>($"api/catalog/{catalogExerciseId}");

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

    private async Task<List<CatalogExerciseDto>> LoadAsync() =>
        await _http.GetFromJsonAsync<List<CatalogExerciseDto>>("api/catalog") ?? new List<CatalogExerciseDto>();
}
