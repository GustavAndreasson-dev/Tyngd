using Microsoft.AspNetCore.ResponseCompression;
using ArnoldOne.Server;
using ArnoldOne.Server.Catalog;
using ArnoldOne.Server.Middleware;
using ArnoldOne.Server.Services;
using DbArnold.DbArnold.models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Registrera ArnoldContext mot SQL Server
builder.Services.AddDbContext<ArnoldContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ArnoldDb")));

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
    });
builder.Services.AddAuthorization();

// Login and register are CPU-heavy (password hashing) and open to guessing: cap them per client IP.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
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
}

app.UseHttpsRedirection();

app.UseMiddleware<ErrorNotificationMiddleware>();

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
.AddAdditionalAssemblies(typeof(ArnoldOne.Client.Routes).Assembly);

app.Run();
