using Microsoft.AspNetCore.ResponseCompression;
using ArnoldOne.Server;

var builder = WebApplication.CreateBuilder(args);

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

app.UseRouting();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorPages();
app.MapControllers();

// Registrera Blazor-komponenterna och lägg till Client-assembly för routing
app.MapRazorComponents<App>()
.AddInteractiveWebAssemblyRenderMode()
.AddAdditionalAssemblies(typeof(ArnoldOne.Client.Routes).Assembly);

app.Run();
