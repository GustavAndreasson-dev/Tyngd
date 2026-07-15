using System.Text.Json.Serialization;

namespace ArnoldOne.Client.Models;

/// <summary>Pågående passdata som sparas lokalt innan den synkas till API:t.</summary>
public class PassData
{
    public Guid SessionKey { get; set; }
    public DateTimeOffset? StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public bool IsSynced { get; set; } = true;

    [JsonPropertyName("IsSaved")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? LegacyIsSaved { get; set; }

    public Dictionary<int, bool> SavedExercises { get; set; } = new();
    public Dictionary<int, decimal> Weights { get; set; } = new();
    public Dictionary<int, double[]> Reps { get; set; } = new();
    public Dictionary<int, string> Notes { get; set; } = new();
    public Dictionary<int, bool[]> CompletedSets { get; set; } = new();

    public void NormalizeSyncState()
    {
        if (!LegacyIsSaved.HasValue)
            return;

        IsSynced = LegacyIsSaved.Value;
        LegacyIsSaved = null;
    }

    public bool NormalizeSessionIdentity(DateTimeOffset nowUtc)
    {
        var changed = false;
        if (SessionKey == Guid.Empty)
        {
            SessionKey = Guid.NewGuid();
            changed = true;
        }

        if (!StartedAtUtc.HasValue)
        {
            StartedAtUtc = nowUtc.ToUniversalTime();
            changed = true;
        }

        return changed;
    }
}
