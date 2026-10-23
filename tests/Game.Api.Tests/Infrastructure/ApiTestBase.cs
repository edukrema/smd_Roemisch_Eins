namespace Game.Api.Tests.Infrastructure;

using System.Net.Http.Json;
using Game.Contracts;

[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "Api";
}

public record TestUser(Guid Id, HttpClient Client);

/// <summary>All Api tests share one container; the data is deleted before each test.</summary>
[Collection(ApiCollection.Name)]
public abstract class ApiTestBase(ApiFactory factory) : IAsyncLifetime
{
    protected ApiFactory Factory { get; } = factory;

    public Task InitializeAsync() => Factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    protected async Task<TestUser> CreateUserAsync(string username)
    {
        var id = Guid.NewGuid();
        var client = Factory.CreateClientFor(id);
        var response = await client.PutAsJsonAsync("/me/username", new ChangeUsernameRequest(username));
        response.EnsureSuccessStatusCode();
        return new TestUser(id, client);
    }

    protected static async Task<Guid> CreateGameAsync(TestUser gameMaster, int maxPlayers = 4)
    {
        var response = await PostAsync(gameMaster, "/games");
        response.EnsureSuccessStatusCode();
        var game = (await response.Content.ReadFromJsonAsync<GameDto>())!;

        if (maxPlayers != game.MaxPlayers)
        {
            var settings = await gameMaster.Client.PutAsJsonAsync(
                $"/games/{game.Id}/settings", new UpdateSettingsRequest(maxPlayers));
            settings.EnsureSuccessStatusCode();
        }

        return game.Id;
    }

    protected static async Task<GameDto> GetGameAsync(TestUser user, Guid gameId) =>
        (await user.Client.GetFromJsonAsync<GameDto>($"/games/{gameId}"))!;

    protected static async Task<HttpResponseMessage> PostAsync(TestUser user, string path) =>
        await user.Client.PostAsync(path, content: null);
}
