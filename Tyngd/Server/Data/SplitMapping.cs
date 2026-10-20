using Tyngd.Shared.TrainingPrograms;
using Tyngd.Data.Models;

namespace Tyngd.Server;

public static class SplitMapping
{
    /// <summary>Maps a split and its loaded passes to the client DTO. Filter and order the passes when loading them.</summary>
    public static TrainingSplit ToTrainingSplit(this Split split) =>
        new()
        {
            SplitId   = split.SplitId,
            SplitName = split.SplitName,
            UserId    = split.UserId.ToString(),
            SplitTemplateId = split.SplitTemplateId,
            Passes    = split.Pass.Select(pass => new TrainingPass
            {
                PassId   = pass.PassId,
                SplitId  = split.SplitId,
                PassName = pass.PassName,
                Color    = pass.Color,
            }).ToList(),
        };
}
