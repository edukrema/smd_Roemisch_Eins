namespace Game.Api.Tests;

using Game.Api.Data;
using Game.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public class DataSecurityTests(ApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task DatabaseConnection_UsesTls()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();

        var ssl = await db.Database
            .SqlQueryRaw<bool>("""SELECT ssl AS "Value" FROM pg_stat_ssl WHERE pid = pg_backend_pid()""")
            .SingleAsync();

        Assert.True(ssl);
    }
}
