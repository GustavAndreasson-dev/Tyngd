namespace DbArnold.DbArnold.models;

/// <summary>Övning importerad från wger.de. Se docs/wger-ovningskatalog-plan.md.</summary>
public class ExerciseCatalog
{
    public int CatalogExerciseId { get; set; }

    public Guid WgerUuid { get; set; }

    public string NameEn { get; set; } = string.Empty;

    public string DescriptionEn { get; set; } = string.Empty;

    public string? NameSv { get; set; }

    public string? DescriptionSv { get; set; }

    /// <summary>ArnoldOne.Shared.Catalog.CatalogTranslationStatus som byte.</summary>
    public byte TranslationStatus { get; set; }

    /// <summary>SHA-256 (hex) av wgers engelska namn + "\n" + beskrivning som den svenska texten översattes från.</summary>
    public string SourceHash { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    /// <summary>Kommaseparerade engelska muskelnamn, primära först.</summary>
    public string Muscles { get; set; } = string.Empty;

    /// <summary>Kommaseparerade engelska utrustningsnamn.</summary>
    public string Equipment { get; set; } = string.Empty;

    /// <summary>wgers engelska alias, en per rad. Används i sökningen.</summary>
    public string Aliases { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public string License { get; set; } = string.Empty;

    public string LicenseAuthor { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTimeOffset LastSyncedUtc { get; set; }
}

public partial class Exercise
{
    public int? CatalogExerciseId { get; set; }

    public virtual ExerciseCatalog? CatalogExercise { get; set; }
}

public partial class ExerciseTemplate
{
    public int? CatalogExerciseId { get; set; }

    public virtual ExerciseCatalog? CatalogExercise { get; set; }
}
