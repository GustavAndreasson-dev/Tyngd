using ArnoldOne.Shared.Users;

namespace ArnoldOne.Shared.DTOs;

public class LoginResponse
{
    public UserInfo User  { get; set; } = default!;
    public string   Token { get; set; } = string.Empty;
}
