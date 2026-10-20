using Microsoft.AspNetCore.ResponseCompression;
using Tyngd.Server;
using Tyngd.Server.Catalog;
using Tyngd.Server.Middleware;
using Tyngd.Server.Services;
using Tyngd.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Registrera TyngdContext mot SQL Server
builder.Services.AddDbContext<TyngdContext>(options =>
    options.UseSqlServer(DbConnection.Resolve(builder.Configuration)));

builder.Services.Configure<ErrorNotificationOptions>(
    builder.Configuration.GetSection(ErrorNotificationOptions.SectionName));
builder.Services.AddSingleton<IErrorNotificationService, SmtpErrorNotificationService>();

builder.Services.AddSingleton(CatalogSeed.LoadEmbedded());
builder.Services.AddHttpClient<WgerCatalogSync>(client =>
{
    client.BaseAddress = new Uri("https://wger.de/");
    client.Timeout = TimeSpan.FromMinutes(2);
});

// JWT-autentisering
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException("Missing configuration value: Jwt:Key. In Azure App Service, set Jwt__Key.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        };
        // En raderad (avidentifierad) användare har inga roller kvar: underkänn token som utfärdades före raderingen.
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var sub = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.Principal?.FindFirstValue("sub");
                var db = context.HttpContext.RequestServices.GetRequiredService<TyngdContext>();
                if (!Guid.TryParse(sub, out var userId)
                    || !await db.UserInfo.AnyAsync(u => u.UserId == userId && u.Roles != ""))
                    context.Fail("Kontot finns inte längre.");
            },
        };
    });
builder.Services.AddAuthorization();

// Login and register are CPU-heavy (password hashing) and open to guessing: cap them per client IP.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
    // Client error reports are anonymous and send email.
    options.AddPolicy("client-errors", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});

// Registrera MVC, Razor Pages och Blazor WebAssembly-komponenter
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    // Inte i Development: Uno-appen på telefonen når servern via "adb reverse" på http://localhost:5123
    // och kan inte följa en omdirigering till https-porten.
    app.UseHttpsRedirection();
}

app.UseMiddleware<ErrorNotificationMiddleware>();

// Okänd sidadress: rendera appen så att routern visar "Sidan finns inte" i stället för en tom 404.
// Inte för API:t, där klienterna läser statuskoden.
app.UseWhen(ctx => !ctx.Request.Path.StartsWithSegments("/api"),
    branch => branch.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.UseStaticFiles();
app.MapStaticAssets();
app.MapRazorPages();
app.MapControllers();

// Registrera Blazor-komponenterna och lägg till Client-assembly för routing
app.MapRazorComponents<App>()
.AddInteractiveWebAssemblyRenderMode()
.AddAdditionalAssemblies(typeof(Tyngd.Client.Routes).Assembly);

app.Run();
