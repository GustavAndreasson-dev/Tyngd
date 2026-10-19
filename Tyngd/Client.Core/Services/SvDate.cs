namespace Tyngd.Client.Services;

/// <summary>Korta svenska datum. WebAssembly-appen laddar inte svenska kulturdata, så månaderna skrivs ut här.</summary>
public static class SvDate
{
    private static readonly string[] Months = ["jan", "feb", "mar", "apr", "maj", "jun", "jul", "aug", "sep", "okt", "nov", "dec"];

    /// <summary>T.ex. "11 okt".</summary>
    public static string Short(int day, int month) => $"{day} {Months[month - 1]}";

    public static string Short(DateOnly date) => Short(date.Day, date.Month);

    private static readonly string[] DayNames = ["söndag", "måndag", "tisdag", "onsdag", "torsdag", "fredag", "lördag"];
    private static readonly string[] MonthNames = ["januari", "februari", "mars", "april", "maj", "juni", "juli", "augusti", "september", "oktober", "november", "december"];

    /// <summary>T.ex. "måndag 19 oktober".</summary>
    public static string Long(DateTime date) => $"{DayNames[(int)date.DayOfWeek]} {date.Day} {MonthNames[date.Month - 1]}";
}
