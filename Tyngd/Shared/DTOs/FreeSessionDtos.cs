namespace Tyngd.Shared.DTOs;

/// <summary>En övning i ett löst pass: ur katalogen, eller en egen med bara namn.</summary>
public sealed class FreeSessionExerciseDto
{
    public int? CatalogExerciseId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class StartFreeSessionRequest
{
    /// <summary>Passets namn, t.ex. "Löst pass 11 okt" (klienten vet lokalt datum).</summary>
    public string PassName { get; set; } = string.Empty;
    public List<FreeSessionExerciseDto> Exercises { get; set; } = new();
}

public sealed class StartFreeSessionResponse
{
    public int SplitId { get; set; }
    public int PassId { get; set; }
}

/// <summary>Spara ett avslutat löst pass som ett riktigt pass, i en befintlig egen split eller en ny.</summary>
public sealed class KeepFreeSessionRequest
{
    public string PassName { get; set; } = string.Empty;
    /// <summary>Befintlig egen split. Null betyder ny split med <see cref="NewSplitName"/>.</summary>
    public int? TargetSplitId { get; set; }
    public string? NewSplitName { get; set; }
}
