namespace ArnoldOne.Client.Services;

/// <summary>
/// Singleton-tjänst som håller applikationens temainställningar i minnet
/// och notifierar prenumeranter vid ändringar.
/// </summary>
public class ThemeState
{
    /// <summary>Anger om mörkt läge är aktivt.</summary>
    public bool IsDarkMode { get; private set; }

    /// <summary>Utlöses när <see cref="IsDarkMode"/> ändras.</summary>
    public event Action? OnChange;

    /// <summary>Sätter mörkt läge och notifierar alla prenumeranter.</summary>
    public void SetDarkMode(bool value)
    {
        IsDarkMode = value;
        OnChange?.Invoke();
    }
}
