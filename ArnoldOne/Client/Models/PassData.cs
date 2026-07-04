using System.Text.Json.Serialization;

namespace ArnoldOne.Client.Models;

/// <summary>Pågående passdata som sparas lokalt innan den synkas till API:t.</summary>
public class PassData
{
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
}
