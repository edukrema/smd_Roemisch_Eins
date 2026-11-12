namespace Game.Api.Tests.Infrastructure;

using Game.Api.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

/// <summary>Starts the Api once per test run against a real PostgreSQL container.</summary>
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Same TLS setup as docker-compose.yml, so the tests also cover the encrypted connection.
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17")
        .WithCommand(
            "-c", "ssl=on",
            "-c", "ssl_cert_file=/etc/ssl/certs/ssl-cert-snakeoil.pem",
            "-c", "ssl_key_file=/etc/ssl/private/ssl-cert-snakeoil.key")
        .Build();

    public HttpClient CreateClientFor(Guid userId)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, userId.ToString());
        return client;
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
        await db.Database.ExecuteSqlRawAsync("""TRUNCATE "Games", "UserProfiles", "UserAccounts" CASCADE""");
    }

    public Task InitializeAsync() => _postgres.StartAsync();

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Game", _postgres.GetConnectionString());
        builder.ConfigureTestServices(services =>
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, null));
    }
}
