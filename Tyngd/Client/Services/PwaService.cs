using System.Text.Json.Serialization;
using Microsoft.JSInterop;

namespace Tyngd.Client.Services;

/// <summary>
/// Håller installation- och uppdateringsstatus för PWA-funktionerna.
/// </summary>
public sealed class PwaService
{
    private readonly IJSRuntime _jsRuntime;
    private DotNetObjectReference<PwaService>? _objectReference;
    private bool _initialized;

    public PwaService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public bool CanInstall { get; private set; }
    public bool IsUpdateAvailable { get; private set; }

    public event Action? OnChange;

    public async Task InitializeAsync()
    {
        if (_initialized)
            return;

        _initialized = true;
        _objectReference = DotNetObjectReference.Create(this);

        try
        {
            var state = await _jsRuntime.InvokeAsync<PwaState>(
                "tyngd.pwa.initialize",
                _objectReference);
            ApplyState(state);
        }
        catch (JSException)
        {
            // Appen fungerar fortfarande som en vanlig webbapp utan PWA-stöd.
        }
    }

    public async Task<bool> InstallAsync()
    {
        if (!CanInstall)
            return false;

        return await _jsRuntime.InvokeAsync<bool>("tyngd.pwa.install");
    }

    public ValueTask ReloadAsync()
        => _jsRuntime.InvokeVoidAsync("tyngd.pwa.reload");

    [JSInvokable]
    public Task OnPwaStateChanged(PwaState state)
    {
        ApplyState(state);
        return Task.CompletedTask;
    }

    private void ApplyState(PwaState state)
    {
        if (CanInstall == state.CanInstall
            && IsUpdateAvailable == state.UpdateAvailable)
        {
            return;
        }

        CanInstall = state.CanInstall;
        IsUpdateAvailable = state.UpdateAvailable;
        OnChange?.Invoke();
    }

    public sealed class PwaState
    {
        [JsonPropertyName("canInstall")]
        public bool CanInstall { get; init; }

        [JsonPropertyName("updateAvailable")]
        public bool UpdateAvailable { get; init; }
    }
}
