using ArnoldOne.Client.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Reflection;

namespace ArnoldOne.Client.Pages;

public partial class Admin : ComponentBase, IDisposable
{
    [Inject]
    private LoginState LoginState { get; set; } = default!;

    private List<TableInfo> _tables = new();

    protected override void OnInitialized()
    {
        LoginState.OnChange += StateHasChanged;
        DiscoverDbModels();
    }

    private void DiscoverDbModels()
    {
        _tables.Clear();

        var assembly = typeof(ArnoldOne.Shared.TrainingPrograms.Exercise).Assembly;
        var modelTypes = assembly.GetTypes()
            .Where(t => (t.Namespace == "ArnoldOne.Shared.TrainingPrograms" || t.Namespace == "ArnoldOne.Shared.Users" || t.Namespace == "ArnoldOne.Shared.DTOs") && t.IsClass && !t.IsAbstract)
            .OrderBy(t => t.Name)
            .ToList();

        foreach (var type in modelTypes)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType.IsPrimitive || p.PropertyType == typeof(string) || p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(Guid) || p.PropertyType == typeof(decimal) || Nullable.GetUnderlyingType(p.PropertyType) != null)
                .OrderBy(p => p.Name)
                .ToList();

            _tables.Add(new TableInfo
            {
                ClassName  = type.Name,
                Type       = type,
                Properties = properties,
                Items      = new List<object>(),
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
