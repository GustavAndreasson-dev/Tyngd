using Tyngd.Shared.TrainingPrograms;

namespace Tyngd.Shared.DTOs;

public sealed class UserDashboardDto
{
    public List<TrainingSplit> ActiveSplits { get; set; } = new();
    public int CompletedSessionsLast7Days { get; set; }
    public int CompletedSessionsLast30Days { get; set; }
    public int TotalCompletedSessions { get; set; }
    /// <summary>Slutförandetid för pass de senaste 7 dagarna (veckovyn).</summary>
    public List<DateTimeOffset> RecentCompletedAtUtc { get; set; } = new();
    public DashboardSessionDto? LastCompletedSession { get; set; }
    /// <summary>De senaste lösa passen (dold split), så att ett pågående löst pass kan återupptas från översikten.</summary>
    public TrainingSplit? RecentFreeSessions { get; set; }
}

/// <summary>En punkt i viktutvecklingen för en övning.</summary>
public sealed record ExerciseProgressPointDto(DateOnly Date, decimal Weight);

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

/// <summary>Ett genomfört pass där en katalogövning ingick, oavsett vilket pass eller löst pass.</summary>
public sealed class ExerciseHistoryEntryDto
{
    public DateOnly Date { get; set; }
    public string PassName { get; set; } = string.Empty;
    public decimal MaxWeight { get; set; }
    public int Sets { get; set; }
    public int TotalReps { get; set; }
}
