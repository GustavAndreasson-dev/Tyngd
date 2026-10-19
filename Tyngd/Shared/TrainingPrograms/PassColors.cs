namespace Tyngd.Shared.TrainingPrograms;

/// <summary>Färgerna ett pass kan få. Bara dessa godtas av servern.</summary>
public static class PassColors
{
    public static readonly (string Hex, string Name)[] Options =
    [
        ("#23CE6B", "Grön"), ("#E0A458", "Bärnsten"), ("#4EA8DE", "Blå"), ("#B57EDC", "Lila"),
        ("#F25F5C", "Röd"), ("#F2D34E", "Gul"), ("#3CC8C0", "Turkos"), ("#F28DB2", "Rosa"),
    ];

    public static bool IsValid(string color) => Options.Any(o => o.Hex == color);
}
