namespace ArnoldOne.Client.Services;

/// <summary>
/// Singleton-tjänst som håller inställningarna för vilotimern i minnet
/// och notifierar prenumeranter vid ändringar.
/// </summary>
public class RestTimerState
{
    /// <summary>Anger om vilotimern är aktiverad.</summary>
    public bool IsEnabled { get; private set; } = true;

    /// <summary>Vilotid i sekunder. Default är 120 (2 minuter).</summary>
    public int Seconds { get; private set; } = 120;

    /// <summary>Utlöses när inställningarna ändras.</summary>
    public event Action? OnChange;

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
}
