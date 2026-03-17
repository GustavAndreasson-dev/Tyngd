using System;
using System.Collections.Generic;
using System.Linq;

namespace ArnoldOne.Shared.TrainingPrograms;

public class TrainingSplitRepository
{
    private readonly List<TrainingSplit> _splits = new();

    public IReadOnlyList<TrainingSplit> All => _splits;

    public TrainingSplit? GetById(string splitId) =>
        _splits.FirstOrDefault(p => string.Equals(p.SplitId, splitId, StringComparison.OrdinalIgnoreCase));

    public void Add(TrainingSplit split) => _splits.Add(split);
}
