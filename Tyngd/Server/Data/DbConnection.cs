namespace Tyngd.Server;

public static class DbConnection
{
    /// <summary>
    /// Connection string name from before the project was renamed. Environments that still set
    /// <c>ConnectionStrings__ArnoldDb</c> (or the same key in user secrets) keep working unchanged.
    /// </summary>
    public const string LegacyName = "ArnoldDb";

    public const string Name = "TyngdDb";

    /// <summary>
    /// The legacy key is checked first because <c>appsettings.json</c> only ever defines the new one (a LocalDB default),
    /// so a set legacy key is always a deliberate override.
    /// </summary>
    public static string? Resolve(IConfiguration configuration) =>
        configuration.GetConnectionString(LegacyName) ?? configuration.GetConnectionString(Name);
}
