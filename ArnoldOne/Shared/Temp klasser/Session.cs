namespace ArnoldOne.Shared.Temp_klasser;

/// <summary>
/// En genomförd träningssession för ett specifikt pass.
/// Ersätter nyckeln "pass-{SplitId}-{PassId}" i LocalStorage.
/// En rad per gång användaren kör ett pass.
/// </summary>
public class Session
{
    /// <summary>Unikt id för sessionen.</summary>
    public int SessionId { get; set; }

    /// <summary>FK till passet.</summary>
    public int PassId { get; set; }

    /// <summary>Datum då passet genomfördes.</summary>
    public DateOnly Date { get; set; }
}
