using ArnoldOne.Shared.DTOs;
using ArnoldOne.Shared.Users;
using DbArnold.DbArnold.models;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using SharedUserInfo = ArnoldOne.Shared.Users.UserInfo;

namespace ArnoldOne.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("auth")]
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
        var userName = request.UserName.Trim();
        var email = request.Email.Trim();

        if (ValidateRegistration(userName, email, request.Password) is { } validationError)
            return BadRequest(validationError);

        if (await _context.UserInfo.AnyAsync(u => u.UserName == userName))
            return BadRequest("Användarnamnet är redan taget.");

        if (await _context.UserInfo.AnyAsync(u => u.Email == email))
            return BadRequest("E-postadressen är redan registrerad.");

        var user = new DbArnold.DbArnold.models.UserInfo
        {
            UserId       = Guid.NewGuid(),
            UserName     = userName,
            Email        = email,
            CreatedAt    = DateTime.UtcNow,
            Roles        = AppRoles.User,
            PasswordHash = HashPassword(request.Password),
        };

        _context.UserInfo.Add(user);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex, "UQ_UserInfo_UserName"))
        {
            return BadRequest("Användarnamnet är redan taget.");
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex, "UQ_UserInfo_Email"))
        {
            return BadRequest("E-postadressen är redan registrerad.");
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            return BadRequest("Användarnamnet eller e-postadressen är redan registrerad.");
        }

        return Ok(new LoginResponse { User = MapToDto(user), Token = GenerateToken(user) });
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var user = await _context.UserInfo.FirstOrDefaultAsync(u =>
            u.UserName == request.UserName || u.Email == request.UserName);

        if (user is null || !VerifyPassword(request.Password, user.PasswordHash))
            return Unauthorized("Felaktigt användarnamn eller lösenord.");

        // Old hashes (100k rounds) are upgraded the first time their owner logs in.
        if (NeedsRehash(user.PasswordHash))
        {
            user.PasswordHash = HashPassword(request.Password);
            await _context.SaveChangesAsync();
        }

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

    // Matches the VARCHAR(50) columns. '@' is banned in usernames so a username can never
    // equal someone's email, which Login also accepts as an identifier.
    private static string? ValidateRegistration(string userName, string email, string password)
    {
        if (userName.Length is < 3 or > 50 || userName.Contains('@'))
            return "Användarnamnet måste vara 3–50 tecken och får inte innehålla @.";

        if (email.Length > 50 || !MailAddress.TryCreate(email, out var address) || address.Address != email)
            return "Ange en giltig e-postadress på högst 50 tecken.";

        if (password.Length < 6)
            return "Lösenordet måste vara minst 6 tecken.";

        return null;
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception, string? constraintName = null)
    {
        if (exception.InnerException is not SqlException sqlException)
            return false;

        var isUniqueViolation = sqlException.Number is 2601 or 2627;
        if (!isUniqueViolation)
            return false;

        return constraintName is null || sqlException.Message.Contains(constraintName, StringComparison.OrdinalIgnoreCase);
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

    private const int LegacyIterations = 100_000;
    private const int Iterations = 600_000;

    // Format: "iterations:salt:hash". Legacy hashes are "salt:hash" and used LegacyIterations.
    private static string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] hash = KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA256, Iterations, 32);
        return $"{Iterations}:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    private static bool NeedsRehash(string stored) => stored.Split(':').Length == 2;

    private static bool VerifyPassword(string password, string stored)
    {
        var parts = stored.Split(':');
        int iterations;
        if (parts.Length == 2)
            iterations = LegacyIterations;
        else if (parts.Length == 3 && int.TryParse(parts[0], out iterations) && iterations > 0)
            parts = parts[1..];
        else
            return false;

        try
        {
            byte[] salt = Convert.FromBase64String(parts[0]);
            byte[] expected = Convert.FromBase64String(parts[1]);
            byte[] actual = KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA256, iterations, 32);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
