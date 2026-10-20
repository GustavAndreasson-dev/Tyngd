namespace Tyngd.Shared.DTOs;

/// <summary>Radera kontot. Lösenordet krävs så att en kvarglömd inloggning inte räcker.</summary>
public class DeleteAccountRequest
{
    public string Password { get; set; } = string.Empty;
}
