using Microsoft.JSInterop;

namespace Tyngd.Client.Services;

/// <summary>
/// Singleton-tjänst för vilotimern: inställningar och pågående vila.
/// Vilan räknas mot klockan (<see cref="EndsAtUtc"/>), så den stämmer även när webbläsaren pausat appen,
/// och sluttiden sparas lokalt så att den överlever omladdning och byte av sida.
/// </summary>
public class RestTimerState
{
    private const string StorageKey = "rest-timer";
    private readonly IJSRuntime _js;
    private CancellationTokenSource? _ticker;

    public RestTimerState(IJSRuntime js) => _js = js;

    /// <summary>Anger om vilotimern är aktiverad.</summary>
    public bool IsEnabled { get; private set; } = true;

    /// <summary>Vilotid i sekunder. Default är 120 (2 minuter).</summary>
    public int Seconds { get; private set; } = 120;

    /// <summary>När pågående vila tar slut, eller null om ingen vila pågår.</summary>
    public DateTimeOffset? EndsAtUtc { get; private set; }

    /// <summary>Längden på pågående vila i sekunder (för förloppsindikatorn).</summary>
    public int DurationSeconds { get; private set; }

    /// <summary>Passidan vilan startades från, för länken tillbaka.</summary>
    public string? PassUrl { get; private set; }

    public bool IsResting => RemainingSeconds > 0;

    public int RemainingSeconds =>
        EndsAtUtc is { } end ? Math.Max(0, (int)Math.Ceiling((end - DateTimeOffset.UtcNow).TotalSeconds)) : 0;

    public int PercentLeft => DurationSeconds <= 0 ? 0 : Math.Min(100, RemainingSeconds * 100 / DurationSeconds);

    /// <summary>Utlöses när inställningarna ändras.</summary>
    public event Action? OnChange;

    /// <summary>Utlöses ungefär varje sekund medan vilan pågår, och när den startar, avbryts eller tar slut.</summary>
    public event Action? OnTick;

    /// <summary>Sätter om timern är aktiverad och notifierar prenumeranter.</summary>
    public void SetEnabled(bool value)
    {
        IsEnabled = value;
        OnChange?.Invoke();
    }

    /// <summary>Sätter vilotiden i sekunder och notifierar prenumeranter.</summary>
    public void SetSeconds(int value)
    {
        Seconds = value;
        OnChange?.Invoke();
    }

    /// <summary>Startar en ny vila med inställd längd.</summary>
    public async Task StartAsync(string passUrl)
    {
        EndsAtUtc = DateTimeOffset.UtcNow.AddSeconds(Seconds);
        DurationSeconds = Seconds;
        PassUrl = passUrl;
        await SaveAsync();
        StartTicker();
    }

    /// <summary>Avbryter pågående vila utan larm.</summary>
    public async Task CancelAsync()
    {
        if (EndsAtUtc is null)
            return;

        StopTicker();
        EndsAtUtc = null;
        await SaveAsync();
        OnTick?.Invoke();
    }

    /// <summary>Läser en vila som pågick när appen stängdes. En vila som redan tagit slut tas bort utan larm.</summary>
    public async Task RestoreAsync()
    {
        try
        {
            var stored = await _js.InvokeAsync<StoredRest?>("tyngd.restTimer.load", StorageKey);
            if (stored is null || stored.EndsAtUtc <= DateTimeOffset.UtcNow)
            {
                if (stored is not null)
                    await _js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
                return;
            }

            EndsAtUtc = stored.EndsAtUtc;
            DurationSeconds = stored.DurationSeconds;
            PassUrl = stored.PassUrl;
            StartTicker();
        }
        catch (JSException)
        {
            // Trasig eller blockerad lagring: ingen vila att återställa.
        }
    }

    private void StartTicker()
    {
        StopTicker();
        var cts = _ticker = new CancellationTokenSource();
        OnTick?.Invoke();
        _ = TickAsync(cts.Token);
    }

    private void StopTicker()
    {
        _ticker?.Cancel();
        _ticker?.Dispose();
        _ticker = null;
    }

    private async Task TickAsync(CancellationToken token)
    {
        try
        {
            // Bakgrundsflikar kan sakta ner fördröjningen; tiden räknas ändå mot klockan.
            while (RemainingSeconds > 0)
            {
                await Task.Delay(500, token);
                OnTick?.Invoke();
            }

            EndsAtUtc = null;
            await SaveAsync();
            OnTick?.Invoke();
            await _js.InvokeVoidAsync("tyngd.restTimer.finished", token);
        }
        catch (OperationCanceledException) { }
        catch (JSException) { }
    }

    private async Task SaveAsync()
    {
        try
        {
            if (EndsAtUtc is null)
                await _js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
            else
                await _js.InvokeVoidAsync("tyngd.restTimer.save", StorageKey,
                    new StoredRest(EndsAtUtc.Value, DurationSeconds, PassUrl));
        }
        catch (JSException) { }
    }

    private sealed record StoredRest(DateTimeOffset EndsAtUtc, int DurationSeconds, string? PassUrl);
}
