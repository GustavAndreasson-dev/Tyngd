namespace Tyngd.Client.Services;

/// <summary>
/// Singleton-tjänst för vilotimern: inställningar och pågående vila. Delas av webbappen och Uno-appen.
/// Vilan räknas mot klockan (<see cref="EndsAtUtc"/>), så den stämmer även när appen pausats eller legat i bakgrunden,
/// och sluttiden sparas lokalt så att den överlever omladdning, omstart och byte av sida.
/// Larmet när vilan är slut sköts av respektive app via <see cref="Finished"/>.
/// </summary>
/// <remarks>Händelserna kan utlösas från en bakgrundstråd.</remarks>
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
    public int? SplitId { get; private set; }

    /// <inheritdoc cref="SplitId"/>
    public int? PassId { get; private set; }

    public bool IsResting => RemainingSeconds > 0;

    public int RemainingSeconds =>
        EndsAtUtc is { } end ? Math.Max(0, (int)Math.Ceiling((end - DateTimeOffset.UtcNow).TotalSeconds)) : 0;

    public int PercentLeft => DurationSeconds <= 0 ? 0 : Math.Min(100, RemainingSeconds * 100 / DurationSeconds);

    /// <summary>Utlöses när inställningarna ändras.</summary>
    public event Action? OnChange;

    /// <summary>Utlöses ungefär varje sekund medan vilan pågår, och när den startar, avbryts eller tar slut.</summary>
    public event Action? OnTick;

    /// <summary>Utlöses när en vila startar (även när den återställs), t.ex. för att schemalägga en notis.</summary>
    public event Action? Started;

    /// <summary>Utlöses när en vila avbryts med <see cref="CancelAsync"/>.</summary>
    public event Action? Cancelled;

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

    /// <summary>Startar en ny vila med inställd längd från passet <paramref name="splitId"/>/<paramref name="passId"/>.</summary>
    public async Task StartAsync(int splitId, int passId)
    {
        EndsAtUtc = DateTimeOffset.UtcNow.AddSeconds(Seconds);
        DurationSeconds = Seconds;
        SplitId = splitId;
        PassId = passId;
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
        Cancelled?.Invoke();
    }

    /// <summary>Läser en vila som pågick när appen stängdes. En vila som redan tagit slut tas bort utan larm.</summary>
    public async Task RestoreAsync()
    {
        StoredRest? stored;
        try
        {
            stored = await _storage.GetItemAsync<StoredRest>(StorageKey);
        }
        catch (Exception)
        {
            // Trasig eller blockerad lagring: ingen vila att återställa.
            return;
        }

        if (stored is null || stored.EndsAtUtc <= DateTimeOffset.UtcNow)
        {
            if (stored is not null)
                await RemoveStoredAsync();
            return;
        }

        EndsAtUtc = stored.EndsAtUtc;
        DurationSeconds = stored.DurationSeconds;
        SplitId = stored.SplitId;
        PassId = stored.PassId;
        StartTicker();
    }

    private void StartTicker()
    {
        StopTicker();
        var cts = _ticker = new CancellationTokenSource();
        OnTick?.Invoke();
        Started?.Invoke();
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
            // Bakgrundsflikar och appar kan sakta ner fördröjningen; tiden räknas ändå mot klockan.
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
        {
            await RemoveStoredAsync();
            return;
        }

        try
        {
            await _storage.SetItemAsync(StorageKey, new StoredRest(EndsAtUtc.Value, DurationSeconds, SplitId, PassId));
        }
        catch (Exception)
        {
            // Vilan fungerar ändå i minnet; den överlever bara inte en omstart.
        }
    }

    private async Task RemoveStoredAsync()
    {
        try
        {
            await _storage.RemoveItemAsync(StorageKey);
        }
        catch (Exception) { }
    }

    public sealed record StoredRest(DateTimeOffset EndsAtUtc, int DurationSeconds, int? SplitId, int? PassId);
}
