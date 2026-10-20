namespace Tyngd.Shared;

public static class AppVersion
{
    /// <summary>"2026.10.17.1605+&lt;hash&gt;" från bygget blir "2026.10.17.1605 · 267123a".</summary>
    public static string Format(string? informational)
    {
        if (string.IsNullOrEmpty(informational)) return "okänd";
        var parts = informational.Split('+', 2);
        return parts.Length == 2 ? $"{parts[0]} · {parts[1][..Math.Min(7, parts[1].Length)]}" : parts[0];
    }
}
