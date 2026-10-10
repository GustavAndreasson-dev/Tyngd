using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ArnoldOne.Server.Catalog;

public static partial class CatalogText
{
    /// <summary>
    /// Hash av källtexten som en översättning gjordes från. Räknas på wgers råa namn och beskrivning,
    /// så samma hash kan räknas utanför appen (seed-filen) utan att sanering måste matcha exakt.
    /// </summary>
    public static string SourceHash(string nameEn, string? descriptionEn) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(nameEn.Trim() + "\n" + (descriptionEn ?? string.Empty))));

    /// <summary>
    /// Gör om wgers HTML-beskrivning till ren text med radbrytningar. Klienten visar den som text,
    /// aldrig som markup, så ingen HTML från wger eller översättningar kan köras i appen.
    /// </summary>
    public static string ToPlainText(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var text = LineBreakTags().Replace(html, "\n");
        text = ListItemTags().Replace(text, "\n• ");
        text = AnyTag().Replace(text, string.Empty);
        text = WebUtility.HtmlDecode(text).Replace(' ', ' ');
        text = SpacesAroundNewline().Replace(text, "\n");
        text = ManyNewlines().Replace(text, "\n\n");
        return text.Trim();
    }

    [GeneratedRegex(@"<\s*(br|/p|/div|/h[1-6]|/ul|/ol)\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex LineBreakTags();

    [GeneratedRegex(@"<\s*li\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex ListItemTags();

    [GeneratedRegex(@"<[^>]*>")]
    private static partial Regex AnyTag();

    [GeneratedRegex(@"[ \t\r]*\n[ \t\r]*")]
    private static partial Regex SpacesAroundNewline();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex ManyNewlines();
}
