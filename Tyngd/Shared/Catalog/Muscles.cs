namespace Tyngd.Shared.Catalog;

/// <summary>wgers 15 muskelgrupper. Nyckeln är wgers engelska namn, som lagras i katalogen.</summary>
public static class Muscles
{
    public sealed record Muscle(string Key, int WgerId, bool IsFront, string NameSv);

    public static readonly IReadOnlyList<Muscle> All =
    [
        new("Shoulders", 2, true, "Axlar"),
        new("Biceps", 1, true, "Biceps"),
        new("Brachialis", 13, true, "Brachialis"),
        new("Chest", 4, true, "Bröst"),
        new("Abs", 6, true, "Mage"),
        new("Obliquus externus abdominis", 14, true, "Sneda magmuskler"),
        new("Serratus anterior", 3, true, "Serratus"),
        new("Quads", 10, true, "Framsida lår"),
        new("Trapezius", 9, false, "Trapezius"),
        new("Lats", 12, false, "Latissimus"),
        new("Triceps", 5, false, "Triceps"),
        new("Glutes", 8, false, "Säte"),
        new("Hamstrings", 11, false, "Baksida lår"),
        new("Calves", 7, false, "Vader"),
        new("Soleus", 15, false, "Soleus"),
    ];

    private static readonly Dictionary<string, Muscle> ByKey = All.ToDictionary(m => m.Key, StringComparer.OrdinalIgnoreCase);

    public static Muscle? Find(string key) => ByKey.GetValueOrDefault(key);

    /// <summary>Delar katalogens kommaseparerade muskelfält i nycklar.</summary>
    public static List<string> ParseKeys(string? commaSeparated) =>
        (commaSeparated ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}

/// <summary>Summerad belastning per muskel. Primär muskel ger 1 per övning, sekundär 0,5.</summary>
public static class MuscleLoad
{
    public const double Primary = 1.0;
    public const double Secondary = 0.5;

    public static Dictionary<string, double> Sum(IEnumerable<(IEnumerable<string> Primary, IEnumerable<string> Secondary)> exercises)
    {
        var load = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var (primary, secondary) in exercises)
        {
            foreach (var key in primary.Distinct(StringComparer.OrdinalIgnoreCase))
                load[key] = load.GetValueOrDefault(key) + Primary;
            foreach (var key in secondary.Distinct(StringComparer.OrdinalIgnoreCase).Except(primary, StringComparer.OrdinalIgnoreCase))
                load[key] = load.GetValueOrDefault(key) + Secondary;
        }
        return load;
    }
}
