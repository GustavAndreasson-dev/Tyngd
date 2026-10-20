using System.Globalization;

namespace Tyngd.Client.Services;

/// <summary>Tolkar tal som användaren skriver i ett textfält, med decimalkomma (svenskt tangentbord) eller decimalpunkt.</summary>
public static class NumberInput
{
    /// <summary>Tomt fält ger 0. Ogiltig text (t.ex. "abc") ger false, så att föregående värde behålls.</summary>
    public static bool TryParse(string? text, out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text))
            return true;

        return decimal.TryParse(text.Trim().Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>Visar ett tal som det skrivs in, utan onödiga decimaler ("62.5", "60").</summary>
    public static string Format(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
