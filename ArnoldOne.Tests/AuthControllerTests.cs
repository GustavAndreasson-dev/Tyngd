using ArnoldOne.Server.Controllers;
using ArnoldOne.Shared.DTOs;
using DbArnold.DbArnold.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ArnoldOne.Tests;

public class AuthControllerTests
{
    [Theory]
    [InlineData("bob", "bob@example.com", "")]
    [InlineData("bob", "bob@example.com", "12345")]
    [InlineData("ab", "bob@example.com", "secret1")]
    [InlineData("bob@example.com", "bob@example.com", "secret1")]
    [InlineData("bob", "not-an-email", "secret1")]
    public async Task Register_rejects_invalid_input(string userName, string email, string password)
    {
        var (controller, context) = Create();

        var result = await controller.Register(new RegisterRequest { UserName = userName, Email = email, Password = password });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(0, await context.UserInfo.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Register_rejects_values_longer_than_the_columns()
    {
        var (controller, _) = Create();
        var tooLong = new string('a', 51);

        var byName = await controller.Register(new RegisterRequest { UserName = tooLong, Email = "a@b.se", Password = "secret1" });
        var byEmail = await controller.Register(new RegisterRequest { UserName = "bob", Email = tooLong + "@b.se", Password = "secret1" });

        Assert.IsType<BadRequestObjectResult>(byName.Result);
        Assert.IsType<BadRequestObjectResult>(byEmail.Result);
    }

    [Fact]
    public async Task Register_accepts_valid_input()
    {
        var (controller, context) = Create();

        var result = await controller.Register(new RegisterRequest { UserName = "bob", Email = "bob@example.com", Password = "secret1" });

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(1, await context.UserInfo.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Login_works_after_register_and_rejects_wrong_password()
    {
        var (controller, _) = Create();
        await controller.Register(new RegisterRequest { UserName = "bob", Email = "bob@example.com", Password = "secret1" });

        var ok = await controller.Login(new LoginRequest { UserName = "bob", Password = "secret1" });
        var bad = await controller.Login(new LoginRequest { UserName = "bob", Password = "wrong" });

        Assert.IsType<OkObjectResult>(ok.Result);
        Assert.IsType<UnauthorizedObjectResult>(bad.Result);
    }

    [Fact]
    public async Task Legacy_hash_still_logs_in_and_is_upgraded()
    {
        var (controller, context) = Create();
        // 100k-round hash of "secret1" in the old "salt:hash" format.
        var salt = new byte[16];
        var legacy = $"{Convert.ToBase64String(salt)}:" + Convert.ToBase64String(
            Microsoft.AspNetCore.Cryptography.KeyDerivation.KeyDerivation.Pbkdf2(
                "secret1", salt, Microsoft.AspNetCore.Cryptography.KeyDerivation.KeyDerivationPrf.HMACSHA256, 100_000, 32));
        context.UserInfo.Add(new UserInfo
        {
            UserId = Guid.NewGuid(), UserName = "old", Email = "old@example.com",
            CreatedAt = DateTime.UtcNow, Roles = "User", PasswordHash = legacy,
        });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await controller.Login(new LoginRequest { UserName = "old", Password = "secret1" });

        Assert.IsType<OkObjectResult>(result.Result);
        var upgraded = (await context.UserInfo.SingleAsync(TestContext.Current.CancellationToken)).PasswordHash;
        Assert.StartsWith("600000:", upgraded);
        Assert.IsType<OkObjectResult>((await controller.Login(new LoginRequest { UserName = "old", Password = "secret1" })).Result);
    }

    private static (AuthController, ArnoldContext) Create()
    {
        var context = new ArnoldContext(new DbContextOptionsBuilder<ArnoldContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = new string('k', 48),
            ["Jwt:Issuer"] = "test",
            ["Jwt:Audience"] = "test",
            ["Jwt:ExpiryMinutes"] = "5",
        }).Build();
        return (new AuthController(context, config), context);
    }
}
