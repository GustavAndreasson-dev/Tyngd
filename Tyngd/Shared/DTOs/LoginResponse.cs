using Tyngd.Shared.Users;

namespace Tyngd.Shared.DTOs;

public class LoginResponse
{
    public UserInfo User  { get; set; } = default!;
    public string   Token { get; set; } = string.Empty;
}
