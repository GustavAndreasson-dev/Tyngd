namespace Tyngd.Shared.TrainingPrograms;

/// <summary>
/// Kopplingstabell mellan en användare och en splitmall som användaren har aktiverat.
/// Skapas när användaren väljer en split på SplitTemplates-sidan.
/// En rad per användare och splitmall.
/// </summary>
public class UserSplit
{
    /// <summary>Unikt id för raden.</summary>
    public int Id { get; set; }

    /// <summary>FK till användaren (IdentityUser GUID).</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>FK till splitmallen.</summary>
    public int SplitId { get; set; }

    /// <summary>Navigeringsegenskap till splitmallen.</summary>
    public TrainingSplitTemplate Split { get; set; } = new();

    /// <summary>Datum och tid när splitten aktiverades (UTC).</summary>
    public DateTime ActivatedAt { get; set; } = DateTime.UtcNow;
}
