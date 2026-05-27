namespace ArnoldOne.Client.Services;

/// <summary>
/// Singleton-tjänst som håller pågående pass-session i minnet.
/// Överlever navigering mellan sidor men nollställs när ett nytt pass väljs
/// eller när alla sets är genomförda.
/// </summary>
public class SessionState
{
    /// <summary>SplitId för det aktiva passet, eller -1 om inget pass är aktivt.</summary>
    public int ActiveSplitId { get; private set; } = -1;

    /// <summary>PassId för det aktiva passet, eller -1 om inget pass är aktivt.</summary>
    public int ActivePassId { get; private set; } = -1;

    /// <summary>Checkade sets per övning (ExerciseId → bool[] per set).</summary>
    public Dictionary<int, bool[]> CompletedSets { get; private set; } = new();

    /// <summary>Anger om ett pass är aktivt.</summary>
    public bool IsActive => ActivePassId != -1;

    /// <summary>
    /// Startar ett nytt pass. Nollställer CompletedSets om det är ett annat pass än det aktiva.
    /// </summary>
    public void StartPass(int splitId, int passId)
    {
        if (ActiveSplitId != splitId || ActivePassId != passId)
        {
            CompletedSets = new();
        }

        ActiveSplitId = splitId;
        ActivePassId = passId;
    }

    /// <summary>Togglar ett set och returnerar det nya värdet.</summary>
    public bool ToggleSet(int exerciseId, int setCount, int setIndex)
    {
        if (!CompletedSets.ContainsKey(exerciseId))
            CompletedSets[exerciseId] = new bool[setCount];

        CompletedSets[exerciseId][setIndex] = !CompletedSets[exerciseId][setIndex];
        return CompletedSets[exerciseId][setIndex];
    }

    /// <summary>Returnerar om ett specifikt set är markerat som klart.</summary>
    public bool IsSetDone(int exerciseId, int setIndex)
    {
        if (CompletedSets.TryGetValue(exerciseId, out var sets) && setIndex < sets.Length)
            return sets[setIndex];
        return false;
    }

    /// <summary>Återställer sets för en övning från sparad data (t.ex. efter page refresh).</summary>
    public void RestoreSets(int exerciseId, bool[] doneSets)
    {
        CompletedSets[exerciseId] = doneSets;
    }

    /// <summary>Avslutar sessionen och nollställer all data.</summary>
    public void EndSession()
    {
        ActiveSplitId = -1;
        ActivePassId = -1;
        CompletedSets = new();
    }
}
