namespace ArnoldOne.Client.Services;

/// <summary>Korta svenska datum. WebAssembly-appen laddar inte svenska kulturdata, så månaderna skrivs ut här.</summary>
public static class SvDate
{
    private static readonly string[] Months = ["jan", "feb", "mar", "apr", "maj", "jun", "jul", "aug", "sep", "okt", "nov", "dec"];

    /// <summary>T.ex. "11 okt".</summary>
    public static string Short(int day, int month) => $"{day} {Months[month - 1]}";

    public static string Short(DateOnly date) => Short(date.Day, date.Month);
}
