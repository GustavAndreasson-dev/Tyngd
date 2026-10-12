namespace Tyngd.Server.Catalog;

/// <summary>Svenska visningsnamn för wgers kategorier och utrustning; muskler kommer från Shared.Catalog.Muscles. Okända namn visas på engelska.</summary>
public static class CatalogNames
{
    private static readonly Dictionary<string, string> Swedish = new(StringComparer.OrdinalIgnoreCase)
    {
        // Kategorier
        ["Legs"] = "Ben", ["Back"] = "Rygg", ["Arms"] = "Armar", ["Shoulders"] = "Axlar", ["Abs"] = "Mage",
        ["Chest"] = "Bröst", ["Cardio"] = "Kondition", ["Calves"] = "Vader",
        // Utrustning
        ["Barbell"] = "Skivstång", ["Bench"] = "Bänk", ["Cable machine"] = "Kabelmaskin", ["Dumbbell"] = "Hantlar",
        ["Gym mat"] = "Träningsmatta", ["Incline bench"] = "Lutande bänk", ["Kettlebell"] = "Kettlebell",
        ["Pull-up bar"] = "Räcke", ["Resistance band"] = "Gummiband", ["SZ-Bar"] = "SZ-stång", ["Swiss Ball"] = "Pilatesboll",
        ["none (bodyweight exercise)"] = "Kroppsvikt",
    };

    public static string Sv(string english) =>
        Swedish.GetValueOrDefault(english) ?? Tyngd.Shared.Catalog.Muscles.Find(english)?.NameSv ?? english;

    public static List<string> SvList(string commaSeparated) =>
        commaSeparated.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(Sv).ToList();
}
