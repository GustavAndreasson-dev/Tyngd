using ArnoldOne.Client.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Reflection;

namespace ArnoldOne.Client.Pages;

/// <summary>
/// Adminsida som visar en översikt av klasserna i Temp klasser-mappen.
/// Tabeller genereras dynamiskt baserat på klasserna i namespace.
/// </summary>
public partial class Admin : ComponentBase, IDisposable
{
    [Inject]
    private LoginState LoginState { get; set; } = default!;

    private List<TableInfo> _tables = new();

    protected override void OnInitialized()
    {
        LoginState.OnChange += StateHasChanged;
        DiscoverTempClasses();
    }

    private void DiscoverTempClasses()
    {
        _tables.Clear();
        
        var assembly = typeof(ArnoldOne.Shared.Temp_klasser.Split).Assembly;
        var tempClassesTypes = assembly.GetTypes()
            .Where(t => t.Namespace == "ArnoldOne.Shared.Temp_klasser" && t.IsClass && !t.IsAbstract)
            .OrderBy(t => t.Name)
            .ToList();

        foreach (var type in tempClassesTypes)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .OrderBy(p => p.Name)
                .ToList();

            _tables.Add(new TableInfo
            {
                ClassName = type.Name,
                Type = type,
                Properties = properties,
                Items = new List<object>()
            });
        }
    }

    public void Dispose() => LoginState.OnChange -= StateHasChanged;

    private class TableInfo
    {
        public string ClassName { get; set; } = string.Empty;
        public Type Type { get; set; } = default!;
        public List<PropertyInfo> Properties { get; set; } = new();
        public List<object> Items { get; set; } = new();
    }
}
