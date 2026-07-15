using ArnoldOne.Client.Services;
using ArnoldOne.Client.Shared;
using ArnoldOne.Shared.DTOs;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace ArnoldOne.Client.Pages;

public partial class Admin : PageComponentBase, IDisposable
{
    [Inject] private LoginState LoginState { get; set; } = default!;
    [Inject] private AdminService AdminService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;

    private AdminOverviewDto? overview;
    private PagedResultDto<AdminUserDto>? users;
    private List<AdminSplitTemplateDto> templates = new();
    private string? userSearch;
    private int TotalUserPages => Math.Max(1, (int)Math.Ceiling((users?.TotalCount ?? 0) / (double)(users?.PageSize ?? 25)));

    protected override async Task OnInitializedAsync()
    {
        LoginState.OnChange += OnLoginChanged;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (!LoginState.IsLoggedIn || LoginState.CurrentUser?.IsAdmin != true)
            return;

        await TryAsync(async () =>
        {
            var overviewTask = AdminService.GetOverviewAsync();
            var usersTask = AdminService.GetUsersAsync(userSearch, 1);
            var templatesTask = AdminService.GetTemplatesAsync();
            await Task.WhenAll(overviewTask, usersTask, templatesTask);
            overview = await overviewTask;
            users = await usersTask;
            templates = await templatesTask;
        }, "Kunde inte ladda adminöversikten");
    }

    private async Task SearchUsersAsync() => await LoadUsersPageAsync(1);
    private async Task PreviousUsersPageAsync() => await LoadUsersPageAsync(Math.Max(1, (users?.Page ?? 1) - 1));
    private async Task NextUsersPageAsync() => await LoadUsersPageAsync(Math.Min(TotalUserPages, (users?.Page ?? 1) + 1));

    private async Task LoadUsersPageAsync(int page) =>
        await TryAsync(async () => users = await AdminService.GetUsersAsync(userSearch, page), "Kunde inte ladda användare");

    private async Task ChangeAdminRoleAsync(AdminUserDto user, bool isAdmin)
    {
        var action = isAdmin ? "ge adminbehörighet till" : "ta bort adminbehörighet från";
        var confirmed = await DialogService.ShowMessageBoxAsync("Bekräfta rolländring", $"Vill du {action} {user.UserName}?", yesText: "Ja", cancelText: "Avbryt");
        if (confirmed != true)
            return;

        await TryAsync(async () =>
        {
            await AdminService.SetAdminRoleAsync(user.UserId, isAdmin);
            await LoadUsersPageAsync(users?.Page ?? 1);
            overview = await AdminService.GetOverviewAsync();
        }, "Kunde inte ändra användarrollen");
    }

    private async Task ToggleTemplateVisibilityAsync(AdminSplitTemplateDto template)
    {
        var verb = template.IsVisible ? "arkivera" : "återställa";
        var confirmed = await DialogService.ShowMessageBoxAsync("Bekräfta", $"Vill du {verb} mallen {template.SplitTemplateName}?", yesText: "Ja", cancelText: "Avbryt");
        if (confirmed != true)
            return;

        await TryAsync(async () =>
        {
            await AdminService.SetTemplateVisibilityAsync(template.SplitTemplateId, !template.IsVisible);
            templates = await AdminService.GetTemplatesAsync();
            overview = await AdminService.GetOverviewAsync();
        }, "Kunde inte ändra mallens status");
    }

    private async void OnLoginChanged()
    {
        try
        {
            await LoadAsync();
            await InvokeAsync(StateHasChanged);
        }
        catch { }
    }

    public void Dispose() => LoginState.OnChange -= OnLoginChanged;
}
