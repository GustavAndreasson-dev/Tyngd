namespace ArnoldOne.Shared.Catalog;

/// <summary>Resultat av en synk mot wger.</summary>
public sealed class CatalogSyncResultDto
{
    public int Fetched { get; set; }
    public int Added { get; set; }
    public int TextChanged { get; set; }
    public int NeedsReview { get; set; }
    public int Deactivated { get; set; }
    public int Reactivated { get; set; }
    public int TranslatedFromSeed { get; set; }
}

/// <summary>En katalogövning i listan som skickas till klienten. Utan beskrivning för att hålla listan liten.</summary>
public sealed class CatalogExerciseDto
{
    public int CatalogExerciseId { get; set; }
    /// <summary>Svenskt namn om det finns, annars engelskt.</summary>
    public string Name { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    /// <summary>Engelska alias från wger, för sökning.</summary>
    public List<string> Aliases { get; set; } = new();
    public string Category { get; set; } = string.Empty;
    /// <summary>Svenska namn, primära först.</summary>
    public List<string> Muscles { get; set; } = new();
    /// <summary>Muskelnycklar (Muscles.All) för muskelfiguren.</summary>
    public List<string> PrimaryMuscles { get; set; } = new();
    public List<string> SecondaryMuscles { get; set; } = new();
    public List<string> Equipment { get; set; } = new();
    public string? ImageUrl { get; set; }
}

/// <summary>En katalogövning med beskrivning (ren text) och licens.</summary>
public sealed class CatalogExerciseDetailDto
{
    public int CatalogExerciseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<string> Muscles { get; set; } = new();
    public List<string> PrimaryMuscles { get; set; } = new();
    public List<string> SecondaryMuscles { get; set; } = new();
    public List<string> Equipment { get; set; } = new();
    public string? ImageUrl { get; set; }
    public string? ImageAuthor { get; set; }
    public string? ImageLicense { get; set; }
    public string License { get; set; } = string.Empty;
    public string LicenseAuthor { get; set; } = string.Empty;
    public Guid WgerUuid { get; set; }
}

/// <summary>Rad i admins granskningslista.</summary>
public sealed class AdminCatalogExerciseDto
{
    public int CatalogExerciseId { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string? NameSv { get; set; }
    public string DescriptionEn { get; set; } = string.Empty;
    public string? DescriptionSv { get; set; }
    public CatalogTranslationStatus TranslationStatus { get; set; }
    public string Category { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

/// <summary>Admin sparar svensk text. Sparad text räknas som granskad.</summary>
public sealed class UpdateCatalogTranslationRequest
{
    public string NameSv { get; set; } = string.Empty;
    public string DescriptionSv { get; set; } = string.Empty;
}
