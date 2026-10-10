namespace ArnoldOne.Shared.Users;

/// <summary>
/// Konstanter för applikationens roller.
/// Används med ASP.NET Core Identity som lagrar roller som strängar.
/// </summary>
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string User  = "User";

    /// <summary>Delar upp den kommaseparerade rollkolumnen i unika, trimmade roller.</summary>
    public static List<string> Parse(string? roles) =>
        roles?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()
        ?? new List<string>();
}
