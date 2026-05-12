using System.Text.Json.Serialization;

namespace ArnoldOne.Shared.Users;

/// <summary>
/// DTO för att skicka användarinformation från server till klient.
/// Exponerar bara det klienten behöver – aldrig lösenordshash eller känslig data.
/// Mappas från <see cref="ArnoldOne.Server.Models.AppUser"/> i API-lagret.
/// </summary>
public class UserInfo
{
    /// <summary>Användarens GUID-id (ärvs från IdentityUser).</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Användarnamn.</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>E-postadress.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Datum och tid när kontot skapades (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Användarens roller, t.ex. "Admin" eller "User".
    /// Mappas från ASP.NET Core Identity-roller i API-lagret.
    /// </summary>
    public List<string> Roles { get; set; } = new();

    /// <summary>Anger om användaren har rollen "Admin".</summary>
    [JsonIgnore]
    public bool IsAdmin => Roles.Contains(AppRoles.Admin);
}
