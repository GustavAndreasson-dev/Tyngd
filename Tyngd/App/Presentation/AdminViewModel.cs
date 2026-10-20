using System.Collections.ObjectModel;
using System.Net;
using Tyngd.Shared.DTOs;

namespace Tyngd.App.Presentation;

/// <summary>Administration (som /admin i webbappen): statistik, användare, mallar och övningskatalogen.</summary>
public partial class AdminViewModel : ObservableObject
{
    private readonly AdminService _adminService;
    private readonly LoginState _loginState;
    private readonly INavigator _navigator;
    private PagedResultDto<AdminUserDto>? _users;
    private bool _loading;

    public AdminViewModel(AdminService adminService, AdminCatalogService adminCatalogService, LoginState loginState, INavigator navigator)
    {
        _adminService = adminService;
        _loginState = loginState;
        _navigator = navigator;
        Catalog = new AdminCatalogViewModel(adminCatalogService);
    }

    public AdminCatalogViewModel Catalog { get; }

    public string[] Sections { get; } = ["Översikt", "Användare", "Mallar", "Övningskatalog"];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOverviewSection), nameof(IsUsersSection), nameof(IsTemplatesSection), nameof(IsCatalogSection))]
    private string? _section = "Översikt";

    public bool IsOverviewSection => Section == Sections[0];
    public bool IsUsersSection => Section == Sections[1];
    public bool IsTemplatesSection => Section == Sections[2];
    public bool IsCatalogSection => Section == Sections[3];

    // Katalogen är stor; hämta den först när fliken öppnas.
    partial void OnSectionChanged(string? value)
    {
        if (IsCatalogSection)
            _ = Catalog.LoadOnceAsync();
    }

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private AdminOverviewDto? _overview;

    public ObservableCollection<AdminListRow> RecentUsers { get; } = [];
    public ObservableCollection<AdminListRow> RecentSessions { get; } = [];

    [ObservableProperty]
    private bool _hasNoRecentSessions;

    [ObservableProperty]
    private string? _userSearch;

    public ObservableCollection<AdminUserRow> Users { get; } = [];

    [ObservableProperty]
    private bool _hasNoUsers;

    [ObservableProperty]
    private string _pageText = "Sida 1 av 1";

    [ObservableProperty]
    private bool _canPreviousPage;

    [ObservableProperty]
    private bool _canNextPage;

    public ObservableCollection<AdminTemplateRow> Templates { get; } = [];

    private int TotalUserPages => Math.Max(1, (int)Math.Ceiling((_users?.TotalCount ?? 0) / (double)(_users?.PageSize ?? 25)));

    /// <summary>Laddar översikt, användare och mallar. Körs också när sidan visas igen efter mallredigeraren.</summary>
    public async Task LoadAsync()
    {
        if (_loading)
            return;
        _loading = true;
        IsLoading = true;
        try
        {
            var overviewTask = _adminService.GetOverviewAsync();
            var usersTask = _adminService.GetUsersAsync(UserSearch, 1);
            var templatesTask = _adminService.GetTemplatesAsync();
            await Task.WhenAll(overviewTask, usersTask, templatesTask);
            ShowOverview(await overviewTask);
            ShowUsers(await usersTask);
            ShowTemplates(await templatesTask);
            ErrorMessage = null;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            ErrorMessage = ErrorText(ex, "Kunde inte ladda adminöversikten.");
        }
        finally
        {
            _loading = false;
            IsLoading = false;
        }
    }

    /// <summary>403 betyder att adminrollen saknas (t.ex. borttagen sedan inloggningen); övrigt är oftast nätverket.</summary>
    internal static string ErrorText(Exception ex, string fallback) =>
        ex is HttpRequestException { StatusCode: HttpStatusCode.Forbidden } ? "Du saknar adminbehörighet." : fallback;

    private void ShowOverview(AdminOverviewDto overview)
    {
        Overview = overview;
        RecentUsers.Clear();
        foreach (var user in overview.RecentUsers)
            RecentUsers.Add(new AdminListRow(user.UserName, user.Email, user.IsAdmin ? "Admin" : "User",
                user.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd")));
        RecentSessions.Clear();
        foreach (var session in overview.RecentSessions)
            RecentSessions.Add(new AdminListRow(session.PassName, $"{session.SplitName} · {session.UserName}",
                $"{session.DurationMinutes} min", session.CompletedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm")));
        HasNoRecentSessions = RecentSessions.Count == 0;
    }

    private void ShowUsers(PagedResultDto<AdminUserDto> users)
    {
        _users = users;
        Users.Clear();
        foreach (var user in users.Items)
            Users.Add(new AdminUserRow(user, user.UserId.ToString() != _loginState.CurrentUser?.UserId));
        HasNoUsers = Users.Count == 0;
        PageText = $"Sida {users.Page} av {TotalUserPages}";
        CanPreviousPage = users.Page > 1;
        CanNextPage = users.Page < TotalUserPages;
    }

    private void ShowTemplates(List<AdminSplitTemplateDto> templates)
    {
        Templates.Clear();
        foreach (var template in templates)
            Templates.Add(new AdminTemplateRow(template));
    }

    [RelayCommand]
    private Task SearchUsersAsync() => LoadUsersPageAsync(1);

    [RelayCommand]
    private Task PreviousUsersPageAsync() => LoadUsersPageAsync(Math.Max(1, (_users?.Page ?? 1) - 1));

    [RelayCommand]
    private Task NextUsersPageAsync() => LoadUsersPageAsync(Math.Min(TotalUserPages, (_users?.Page ?? 1) + 1));

    private async Task LoadUsersPageAsync(int page)
    {
        try
        {
            ShowUsers(await _adminService.GetUsersAsync(UserSearch, page));
            ErrorMessage = null;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            ErrorMessage = ErrorText(ex, "Kunde inte ladda användare.");
        }
    }

    /// <summary>Sidan har redan bekräftat ändringen. Returnerar false om den inte gick igenom.</summary>
    public async Task<bool> SetAdminRoleAsync(AdminUserRow user, bool isAdmin)
    {
        try
        {
            await _adminService.SetAdminRoleAsync(user.User.UserId, isAdmin);
            ShowUsers(await _adminService.GetUsersAsync(UserSearch, _users?.Page ?? 1));
            ShowOverview(await _adminService.GetOverviewAsync());
            ErrorMessage = null;
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            ErrorMessage = ErrorText(ex, "Kunde inte ändra användarrollen.");
            return false;
        }
    }

    /// <summary>Sidan har redan bekräftat ändringen.</summary>
    public async Task ToggleTemplateVisibilityAsync(AdminTemplateRow row)
    {
        try
        {
            await _adminService.SetTemplateVisibilityAsync(row.Template.SplitTemplateId, !row.Template.IsVisible);
            ShowTemplates(await _adminService.GetTemplatesAsync());
            ShowOverview(await _adminService.GetOverviewAsync());
            ErrorMessage = null;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            ErrorMessage = ErrorText(ex, "Kunde inte ändra mallens status.");
        }
    }

    [RelayCommand]
    private Task NewTemplateAsync() =>
        _navigator.NavigateViewModelAsync<AdminTemplateViewModel>(this, data: new AdminTemplateArgs(null));

    public Task EditTemplateAsync(AdminTemplateRow row) =>
        _navigator.NavigateViewModelAsync<AdminTemplateViewModel>(this, data: new AdminTemplateArgs(row.Template.SplitTemplateId));

    [RelayCommand]
    private Task BackAsync() => _navigator.NavigateBackAsync(this);
}

/// <summary>En rad i "Senaste användarna" eller "Senaste träningspassen".</summary>
public record AdminListRow(string Title, string Detail, string Meta, string MetaDetail);

public record AdminUserRow(AdminUserDto User, bool CanToggle)
{
    public string UserName => User.UserName;
    public string Email => User.Email;
    public bool IsAdmin => User.IsAdmin;
    public string Created => $"Skapad {User.CreatedAt.ToLocalTime():yyyy-MM-dd}";
}

public record AdminTemplateRow(AdminSplitTemplateDto Template)
{
    public string Name => Template.SplitTemplateName;
    public string Summary => $"{Template.Passes.Count} pass · {Template.Passes.Sum(pass => pass.Exercises.Count)} övningar";
    public string StatusText => Template.IsVisible ? "Aktiv" : "Arkiverad";
    public string ToggleText => Template.IsVisible ? "Arkivera" : "Återställ";
    public double CardOpacity => Template.IsVisible ? 1 : 0.6;
}
