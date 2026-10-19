using Tyngd.Shared;

namespace Tyngd.Tests;

public class AppVersionTests
{
    [Theory]
    [InlineData("2026.10.17.1605+267123a1c66749d0d587fd4e0145fcee14df76bc", "2026.10.17.1605 · 267123a")]
    [InlineData("2026.10.17.1605", "2026.10.17.1605")]
    [InlineData(null, "okänd")]
    public void Shows_build_time_and_short_commit(string? informational, string expected) =>
        Assert.Equal(expected, AppVersion.Format(informational));
}
