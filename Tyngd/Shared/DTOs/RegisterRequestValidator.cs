namespace Tyngd.Shared.DTOs;

/// <summary>Klientens kontroll av registreringsformuläret. Servern validerar själv (AuthController) med egna, striktare regler.</summary>
public static class RegisterRequestValidator
{
    /// <summary>Första felet som text, eller null om formuläret kan skickas.</summary>
    public static string? Validate(string? userName, string? email, string? password, string? confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword))
            return "Fyll i alla fält.";

        if (userName.Trim().Length < 3)
            return "Användarnamnet måste vara minst 3 tecken.";

        if (!IsValidEmail(email.Trim()))
            return "Ange en giltig e-postadress.";

        if (password.Length < 6)
            return "Lösenordet måste vara minst 6 tecken.";

        if (password != confirmPassword)
            return "Lösenorden matchar inte.";

        return null;
    }

    private static bool IsValidEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 0) return false;
        var dotIndex = email.IndexOf('.', atIndex);
        return dotIndex > atIndex + 1 && dotIndex < email.Length - 1;
    }
}
