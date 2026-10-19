namespace Tyngd.Client.Services;

/// <summary>Lokal lagringsnyckel för ett pågående pass: "pass-{splitId}-{passId}".</summary>
public static class PassKey
{
    public static string Format(int splitId, int passId) => $"pass-{splitId}-{passId}";

    public static bool TryParse(string key, out int splitId, out int passId)
    {
        splitId = 0;
        passId = 0;
        var parts = key.Split('-', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 3
            && parts[0] == "pass"
            && int.TryParse(parts[1], out splitId)
            && int.TryParse(parts[2], out passId);
    }
}
