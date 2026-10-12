using Tyngd.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Tyngd.Server.Catalog;

public static class CatalogLinks
{
    /// <summary>True om alla angivna katalog-id:n finns. Null betyder egen övning och är alltid giltigt.</summary>
    public static async Task<bool> AllExistAsync(TyngdContext context, IEnumerable<int?> catalogExerciseIds)
    {
        var ids = catalogExerciseIds.OfType<int>().Distinct().ToList();
        return ids.Count == 0
            || await context.ExerciseCatalog.CountAsync(row => ids.Contains(row.CatalogExerciseId)) == ids.Count;
    }
}
