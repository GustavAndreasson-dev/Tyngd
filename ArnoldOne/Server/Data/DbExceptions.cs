using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ArnoldOne.Server;

public static class DbExceptions
{
    /// <summary>True when SQL Server rejected the write for a unique index or constraint (2601/2627).</summary>
    public static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}
