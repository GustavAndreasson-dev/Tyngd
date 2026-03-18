using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Blazored.LocalStorage;
using MudBlazor.Services;
using ArnoldOne.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Registrera namngiven HttpClient som pekar mot server-API:t
builder.Services.AddHttpClient("ArnoldOne.ServerAPI", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));

// Supply HttpClient instances that include access tokens when making requests to the server project
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("ArnoldOne.ServerAPI"));

// MudBlazor UI-komponenter
builder.Services.AddMudServices();

// LocalStorage för att spara vikter och reps mellan sessioner
builder.Services.AddBlazoredLocalStorage();

// Singleton för att dela temainställningar (dark mode) globalt
builder.Services.AddSingleton<ThemeState>();

await builder.Build().RunAsync();
