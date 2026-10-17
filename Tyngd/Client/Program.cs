using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Blazored.LocalStorage;
using MudBlazor.Services;
using Tyngd.Client.Services;
using Tyngd.Shared.TrainingPrograms;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Registrera namngiven HttpClient som pekar mot server-API:t
builder.Services.AddTransient<AuthTokenHandler>();
builder.Services.AddHttpClient("Tyngd.ServerAPI", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
    .AddHttpMessageHandler<AuthTokenHandler>();

// Supply HttpClient instances that include access tokens when making requests to the server project
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Tyngd.ServerAPI"));

// MudBlazor UI-komponenter
builder.Services.AddMudServices();

// LocalStorage för att spara vikter och reps mellan sessioner. Singleton så att singleton-tjänster
// som LoginState kan använda den direkt; i WebAssembly är scoped och singleton ändå samma instans.
builder.Services.AddBlazoredLocalStorageAsSingleton();
builder.Services.AddSingleton<IAppStorage, BlazoredAppStorage>();

// Singleton för att dela temainställningar (dark mode) globalt
builder.Services.AddSingleton<ThemeState>();

// Singleton för vilotimer-inställningar (aktiverad/sekunder)
builder.Services.AddSingleton<RestTimerState>();

// Singleton för att hålla pågående pass-session vid navigering
builder.Services.AddSingleton<SessionState>();

builder.Services.AddScoped<PendingSyncState>();
builder.Services.AddScoped<PwaService>();

// Singleton för inloggningsstatus
builder.Services.AddSingleton<LoginState>();

// Service för användarens aktiverade splits
builder.Services.AddScoped<UserSplitsService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<AdminService>();

// Service för autentisering mot API
builder.Services.AddScoped<AuthService>();

// Service för splitmallar och pass från API
builder.Services.AddScoped<SplitTemplateService>();
builder.Services.AddScoped<CatalogService>();
builder.Services.AddScoped<FavoritesService>();
builder.Services.AddScoped<FreeSessionService>();
builder.Services.AddScoped<AdminCatalogService>();

// Service för att spara och läsa pass-logg från API
builder.Services.AddScoped<PassLogService>();

// Service för användarinställningar från API
builder.Services.AddScoped<UserSettingsService>();

// Scoped för localStorage-migrering
builder.Services.AddScoped<LocalStorageMigrationService>();

builder.Services.AddScoped<ClientErrorService>();

await builder.Build().RunAsync();
