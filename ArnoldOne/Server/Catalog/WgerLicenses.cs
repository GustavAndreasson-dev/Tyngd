namespace ArnoldOne.Server.Catalog;

/// <summary>
/// Namn på wgers licenser, per licens-id i wgers licenstabell (<c>/api/v2/license/</c>).
/// Texten och varje bild har en egen licens, så den som visas ska komma från rätt objekt.
/// </summary>
public static class WgerLicenses
{
    public static string? Name(int? id) => id switch
    {
        1 => "CC-BY-SA 3",
        2 => "CC-BY-SA 4",
        3 => "CC0",
        4 => "CC-BY 4",
        5 => "ODbL",
        _ => null,
    };
}
