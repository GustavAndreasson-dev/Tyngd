using ArnoldOne.Shared.TrainingPrograms;

namespace ArnoldOne.Shared.DTOs;

public sealed class UserDashboardDto
{
    public List<TrainingSplit> ActiveSplits { get; set; } = new();
    public int CompletedSessionsLast7Days { get; set; }
    public int CompletedSessionsLast30Days { get; set; }
    public int TotalCompletedSessions { get; set; }
    public DashboardSessionDto? LastCompletedSession { get; set; }
}

public sealed class DashboardSessionDto
{
    public Guid SessionKey { get; set; }
    public int SplitId { get; set; }
    public string SplitName { get; set; } = string.Empty;
    public int PassId { get; set; }
    public string PassName { get; set; } = string.Empty;
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset CompletedAtUtc { get; set; }
}
