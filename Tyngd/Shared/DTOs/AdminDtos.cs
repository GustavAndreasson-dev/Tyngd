namespace Tyngd.Shared.DTOs;

public sealed class AdminOverviewDto
{
    public int TotalUsers { get; set; }
    public int NewUsersLast30Days { get; set; }
    public int ActiveUsersLast30Days { get; set; }
    public int CompletedSessionsLast7Days { get; set; }
    public int CompletedSessionsLast30Days { get; set; }
    public int VisibleTemplates { get; set; }
    public List<AdminUserDto> RecentUsers { get; set; } = new();
    public List<AdminSessionDto> RecentSessions { get; set; } = new();
}

public sealed class AdminUserDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<string> Roles { get; set; } = new();
    public bool IsAdmin => Roles.Contains(Users.AppRoles.Admin);
}

public sealed class AdminSessionDto
{
    public Guid SessionKey { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string SplitName { get; set; } = string.Empty;
    public string PassName { get; set; } = string.Empty;
    public DateTimeOffset CompletedAtUtc { get; set; }
    public int DurationMinutes { get; set; }
}

public sealed class PagedResultDto<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public sealed class SetUserAdminRequest
{
    public bool IsAdmin { get; set; }
}

public sealed class SetVisibilityRequest
{
    public bool IsVisible { get; set; }
}
