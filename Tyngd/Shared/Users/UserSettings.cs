namespace Tyngd.Shared.Users;

/// <summary>
/// En användares applikationsinställningar.
/// Ersätter localStorage-nycklarna "darkmode", "resttimer-enabled" och "resttimer-seconds"
/// med en databasrad per användare, så att inställningarna följer med vid enhetsbyte.
/// En rad per användare.
/// </summary>
public class UserSettings
{
    /// <summary>FK till användaren (IdentityUser GUID).</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Anger om mörkt läge är aktiverat.</summary>
    public bool IsDarkMode { get; set; }

    /// <summary>Anger om vilotimern är aktiverad.</summary>
    public bool IsRestTimerEnabled { get; set; }
    
    /// <summary>Vilotimerens längd i sekunder.</summary>
    public int RestTimerSeconds { get; set; } = 120;
}
