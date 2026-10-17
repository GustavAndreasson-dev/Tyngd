using System.Net.Http.Json;

namespace Tyngd.Client.Services;

/// <summary>Inloggad användares favoritövningar (katalog-id:n), hämtade en gång och uppdaterade lokalt.</summary>
public sealed class FavoritesService
{
    private readonly HttpClient _http;
    private readonly LoginState _loginState;
    private Task<HashSet<int>>? _favorites;
    private string? _loadedFor;

    public FavoritesService(HttpClient http, LoginState loginState)
    {
        _http = http;
        _loginState = loginState;
    }

    public event Action? OnChange;

    /// <summary>Favoriterna, tomt för den som inte är inloggad. Laddas om när användaren byts.</summary>
    public Task<HashSet<int>> GetAsync()
    {
        var userId = _loginState.IsLoggedIn ? _loginState.CurrentUser?.UserId : null;
        if (userId is null)
            return Task.FromResult(new HashSet<int>());

        if (_favorites is null || _favorites.IsFaulted || _loadedFor != userId)
        {
            _loadedFor = userId;
            _favorites = LoadAsync();
        }
        return _favorites;
    }

    /// <summary>Växlar favorit. Returnerar true om övningen nu är favorit.</summary>
    public async Task<bool> ToggleAsync(int catalogExerciseId)
    {
        var favorites = await GetAsync();
        var add = !favorites.Contains(catalogExerciseId);
        var response = add
            ? await _http.PutAsync($"api/favorites/{catalogExerciseId}", null)
            : await _http.DeleteAsync($"api/favorites/{catalogExerciseId}");
        response.EnsureSuccessStatusCode();

        if (add)
            favorites.Add(catalogExerciseId);
        else
            favorites.Remove(catalogExerciseId);
        OnChange?.Invoke();
        return add;
    }

    private async Task<HashSet<int>> LoadAsync() =>
        (await _http.GetFromJsonAsync<List<int>>("api/favorites") ?? new()).ToHashSet();
}
