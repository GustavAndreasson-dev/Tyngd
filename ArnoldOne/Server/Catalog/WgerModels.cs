using System.Text.Json.Serialization;

namespace ArnoldOne.Server.Catalog;

// Bara de fält vi använder ur https://wger.de/api/v2/exerciseinfo/.

public sealed record WgerPage(
    [property: JsonPropertyName("count")] int Count,
    [property: JsonPropertyName("next")] string? Next,
    [property: JsonPropertyName("results")] List<WgerExercise> Results);

public sealed record WgerExercise(
    [property: JsonPropertyName("uuid")] Guid Uuid,
    [property: JsonPropertyName("category")] WgerNamed Category,
    [property: JsonPropertyName("muscles")] List<WgerMuscle> Muscles,
    [property: JsonPropertyName("muscles_secondary")] List<WgerMuscle> MusclesSecondary,
    [property: JsonPropertyName("equipment")] List<WgerNamed> Equipment,
    [property: JsonPropertyName("images")] List<WgerImage> Images,
    [property: JsonPropertyName("license")] WgerLicense? License,
    [property: JsonPropertyName("license_author")] string? LicenseAuthor,
    [property: JsonPropertyName("translations")] List<WgerTranslation> Translations);

public sealed record WgerNamed(
    [property: JsonPropertyName("name")] string Name);

public sealed record WgerMuscle(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("name_en")] string? NameEn);

public sealed record WgerImage(
    [property: JsonPropertyName("image")] string Image,
    [property: JsonPropertyName("is_main")] bool IsMain,
    [property: JsonPropertyName("license")] int? License,
    [property: JsonPropertyName("license_author")] string? LicenseAuthor);

public sealed record WgerLicense(
    [property: JsonPropertyName("short_name")] string ShortName);

public sealed record WgerTranslation(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("language")] int Language,
    [property: JsonPropertyName("license")] int? License,
    [property: JsonPropertyName("license_author")] string? LicenseAuthor,
    [property: JsonPropertyName("aliases")] List<WgerAlias> Aliases);

public sealed record WgerAlias(
    [property: JsonPropertyName("alias")] string Alias);
