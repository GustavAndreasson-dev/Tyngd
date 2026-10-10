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
