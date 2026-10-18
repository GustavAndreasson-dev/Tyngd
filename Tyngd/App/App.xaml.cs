using System.Diagnostics.CodeAnalysis;
using Uno.Resizetizer;

namespace Tyngd.App;

public partial class App : Application
{
    /// <summary>
    /// Initializes the singleton application object. This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();
    }

    protected Window? MainWindow { get; private set; }
    protected IHost? Host { get; private set; }

    [SuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Uno.Extensions APIs are used in a way that is safe for trimming in this template context.")]
    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args)
            // Add navigation support for toolkit controls such as TabBar and NavigationView
            .UseToolkitNavigation()
            .Configure(host => host
#if DEBUG
                // Switch to Development environment when running in DEBUG
                .UseEnvironment(Environments.Development)
#endif
                .UseLogging(configure: (context, logBuilder) =>
                {
                    // Configure log levels for different categories of logging
                    logBuilder
                        .SetMinimumLevel(
                            context.HostingEnvironment.IsDevelopment() ?
                                LogLevel.Information :
                                LogLevel.Warning)

                        // Default filters for core Uno Platform namespaces
                        .CoreLogLevel(LogLevel.Warning);

                }, enableUnoLogging: true)
                .UseConfiguration(configure: configBuilder => configBuilder.EmbeddedSource<App>())
                .ConfigureServices((context, services) =>
                {
                    // Samma klienttjänster som webbappen (Tyngd.Client/Program.cs), men med lagring i appens mapp.
                    // Singleton genomgående: navigeringen skapar ett scope per sida, så scoped tjänster
                    // (t.ex. PendingSyncState) skulle få nya instanser och tappa sitt tillstånd vid sidbyte.
                    services.AddSingleton<IAppStorage>(new FileAppStorage(
                        Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "storage")));

                    var apiUrl = context.Configuration["ApiClient:Url"]
                        ?? throw new InvalidOperationException("Saknar ApiClient:Url i appsettings.json.");
                    services.AddTransient<AuthTokenHandler>();
                    services.AddHttpClient("Tyngd.ServerAPI", client => client.BaseAddress = new Uri(apiUrl))
                        .AddHttpMessageHandler<AuthTokenHandler>();
                    services.AddSingleton(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Tyngd.ServerAPI"));

                    services.AddSingleton<ThemeState>();
                    services.AddSingleton<SessionState>();
                    services.AddSingleton<LoginState>();
                    services.AddSingleton<PendingSyncState>();
                    services.AddSingleton<AuthService>();
                    services.AddSingleton<UserSplitsService>();
                    services.AddSingleton<DashboardService>();
                    services.AddSingleton<AdminService>();
                    services.AddSingleton<SplitTemplateService>();
                    services.AddSingleton<CatalogService>();
                    services.AddSingleton<FavoritesService>();
                    services.AddSingleton<FreeSessionService>();
                    services.AddSingleton<AdminCatalogService>();
                    services.AddSingleton<PassLogService>();
                    services.AddSingleton<UserSettingsService>();
                })
                .UseNavigation(RegisterRoutes)
            );
        MainWindow = builder.Window;

        #if DEBUG
        MainWindow.UseStudio();
#endif
                MainWindow.SetWindowIcon();

        Host = await MainWindow.InitializeNavigationAsync(
            async () =>
            {
                var host = builder.Build();
                // Läs sparad inloggning innan första sidan visas, som MainLayout gör i webbappen.
                await host.Services.GetRequiredService<LoginState>().InitializeAsync();
                return host;
            },
            initialNavigate: (services, navigator) => services.GetRequiredService<LoginState>().IsLoggedIn
                ? navigator.NavigateViewModelAsync<MainViewModel>(this)
                : navigator.NavigateViewModelAsync<LoginViewModel>(this)
        );
    }

    private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
    {
        views.Register(
            new ViewMap<LoginPage, LoginViewModel>(),
            new ViewMap<MainPage, MainViewModel>()
        );

        routes.Register(
            new RouteMap("Login", View: views.FindByViewModel<LoginViewModel>()),
            new RouteMap("Main", View: views.FindByViewModel<MainViewModel>())
        );
    }
}
