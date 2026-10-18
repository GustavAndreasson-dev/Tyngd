using Tyngd.App.Presentation;

namespace Tyngd.App.Services;

/// <summary>
/// Vilotimern: inställningar och pågående vila. Samma beteende som RestTimerState i webbappen, men sparad i
/// <see cref="IAppStorage"/> i stället för via JavaScript. Vilan räknas mot klockan (<see cref="EndsAtUtc"/>),
/// så den stämmer även när appen legat i bakgrunden, och överlever omstart.
/// </summary>
/// <remarks>Händelserna kan utlösas från en bakgrundstråd; uppdatera UI via DispatcherQueue.</remarks>
// shortcut: inget larm när appen ligger i bakgrunden (kräver lokal notis per plattform) – lägg till när passidan är klar.
public class RestTimerState
{
    private const string StorageKey = "rest-timer";
    private readonly IAppStorage _storage;
    private CancellationTokenSource? _ticker;

    public RestTimerState(IAppStorage storage) => _storage = storage;

    /// <summary>Anger om vilotimern är aktiverad.</summary>
    public bool IsEnabled { get; private set; } = true;

    /// <summary>Vilotid i sekunder. Default är 120 (2 minuter).</summary>
    public int Seconds { get; private set; } = 120;

    /// <summary>När pågående vila tar slut, eller null om ingen vila pågår.</summary>
    public DateTimeOffset? EndsAtUtc { get; private set; }

    /// <summary>Längden på pågående vila i sekunder (för förloppsindikatorn).</summary>
    public int DurationSeconds { get; private set; }

    /// <summary>Passet vilan startades från, för vägen tillbaka.</summary>
    public PassArgs? Pass { get; private set; }

    public bool IsResting => RemainingSeconds > 0;

    public int RemainingSeconds =>
        EndsAtUtc is { } end ? Math.Max(0, (int)Math.Ceiling((end - DateTimeOffset.UtcNow).TotalSeconds)) : 0;

    public int PercentLeft => DurationSeconds <= 0 ? 0 : Math.Min(100, RemainingSeconds * 100 / DurationSeconds);

    /// <summary>Utlöses när inställningarna ändras.</summary>
    public event Action? OnChange;

    /// <summary>Utlöses ungefär varje sekund medan vilan pågår, och när den startar, avbryts eller tar slut.</summary>
    public event Action? OnTick;

    /// <summary>Utlöses när en vila tar slut av sig själv (inte när den avbryts) – dags för larm.</summary>
    public event Action? Finished;

    public void SetEnabled(bool value)
    {
        IsEnabled = value;
        OnChange?.Invoke();
    }

    public void SetSeconds(int value)
    {
        Seconds = value;
        OnChange?.Invoke();
    }

    /// <summary>Startar en ny vila med inställd längd.</summary>
    public async Task StartAsync(PassArgs pass)
    {
        EndsAtUtc = DateTimeOffset.UtcNow.AddSeconds(Seconds);
        DurationSeconds = Seconds;
        Pass = pass;
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
        var stored = await _storage.GetItemAsync<StoredRest>(StorageKey);
        if (stored is null || stored.EndsAtUtc <= DateTimeOffset.UtcNow)
        {
            if (stored is not null)
                await _storage.RemoveItemAsync(StorageKey);
            return;
        }

        EndsAtUtc = stored.EndsAtUtc;
        DurationSeconds = stored.DurationSeconds;
        Pass = stored.Pass;
        StartTicker();
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
            while (RemainingSeconds > 0)
            {
                await Task.Delay(500, token);
                OnTick?.Invoke();
            }

            EndsAtUtc = null;
            await SaveAsync();
            OnTick?.Invoke();
            Finished?.Invoke();
        }
        catch (OperationCanceledException) { }
    }

    private async Task SaveAsync()
    {
        if (EndsAtUtc is null)
            await _storage.RemoveItemAsync(StorageKey);
        else
            await _storage.SetItemAsync(StorageKey, new StoredRest(EndsAtUtc.Value, DurationSeconds, Pass));
    }

    public sealed record StoredRest(DateTimeOffset EndsAtUtc, int DurationSeconds, PassArgs? Pass);
}
