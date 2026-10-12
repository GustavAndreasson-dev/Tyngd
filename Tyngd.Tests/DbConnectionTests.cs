using Microsoft.Extensions.Configuration;
using Tyngd.Server;

namespace Tyngd.Tests;

public class DbConnectionTests
{
    private static IConfiguration Config(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values.ToDictionary(v => v.Key, v => (string?)v.Value)).Build();

    [Fact]
    public void Default_from_appsettings_is_used_when_nothing_else_is_set() =>
        Assert.Equal("default", DbConnection.Resolve(Config(("ConnectionStrings:TyngdDb", "default"))));

    [Fact]
    public void Legacy_key_wins_so_existing_environments_keep_their_database() =>
        Assert.Equal("legacy", DbConnection.Resolve(Config(
            ("ConnectionStrings:TyngdDb", "default"),
            ("ConnectionStrings:ArnoldDb", "legacy"))));
}
