using ArnoldOne.Shared.DTOs;
using ArnoldOne.Shared.Users;
using DbArnold.DbArnold.models;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using SharedUserInfo = ArnoldOne.Shared.Users.UserInfo;

namespace ArnoldOne.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ArnoldContext _context;
    private readonly IConfiguration _config;

    public AuthController(ArnoldContext context, IConfiguration config)
    {
        _context = context;
        _config  = config;
    }

    [HttpPost("register")]
    public async Task<ActionResult<LoginResponse>> Register(RegisterRequest request)
    {
        if (await _context.UserInfo.AnyAsync(u => u.UserName == request.UserName))
            return BadRequest("Användarnamnet är redan taget.");

        if (await _context.UserInfo.AnyAsync(u => u.Email == request.Email))
            return BadRequest("E-postadressen är redan registrerad.");

        var user = new DbArnold.DbArnold.models.UserInfo
        {
            UserId       = Guid.NewGuid(),
            UserName     = request.UserName.Trim(),
            Email        = request.Email.Trim(),
            CreatedAt    = DateTime.UtcNow,
            Roles        = AppRoles.User,
            PasswordHash = HashPassword(request.Password),
        };

        _context.UserInfo.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new LoginResponse { User = MapToDto(user), Token = GenerateToken(user) });
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var user = await _context.UserInfo.FirstOrDefaultAsync(u =>
            u.UserName == request.UserName || u.Email == request.UserName);

        if (user is null || !VerifyPassword(request.Password, user.PasswordHash))
            return Unauthorized("Felaktigt användarnamn eller lösenord.");

        return Ok(new LoginResponse { User = MapToDto(user), Token = GenerateToken(user) });
    }

    private string GenerateToken(DbArnold.DbArnold.models.UserInfo user)
    {
        var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds   = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry  = DateTime.UtcNow.AddMinutes(double.Parse(_config["Jwt:ExpiryMinutes"]!));
        var roles   = user.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName),
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r.Trim())));

        var token = new JwtSecurityToken(
            issuer:   _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims:   claims,
            expires:  expiry,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static SharedUserInfo MapToDto(DbArnold.DbArnold.models.UserInfo user) =>
        new()
        {
            UserId    = user.UserId.ToString(),
            UserName  = user.UserName,
            Email     = user.Email,
            CreatedAt = user.CreatedAt,
            Roles     = user.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(r => r.Trim()).ToList(),
        };

    private static string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] hash = KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA256, 100_000, 32);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string stored)
    {
        var parts = stored.Split(':');
        if (parts.Length != 2) return false;
        byte[] salt = Convert.FromBase64String(parts[0]);
        byte[] hash = KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA256, 100_000, 32);
        return Convert.ToBase64String(hash) == parts[1];
    }
}
