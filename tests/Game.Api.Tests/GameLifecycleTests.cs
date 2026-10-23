namespace Game.Api.Tests;

using System.Net;
using System.Net.Http.Json;
using Game.Api.Tests.Infrastructure;
using Game.Contracts;

public class GameLifecycleTests(ApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task CreateGame_WithoutLogin_Returns401()
    {
        var response = await Factory.CreateClient().PostAsync("/games", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateGame_ReturnsLobbyWithGameMasterAndDefaultMaxPlayers()
    {
        var gameMaster = await CreateUserAsync("master");

        var response = await PostAsync(gameMaster, "/games");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var game = await response.Content.ReadFromJsonAsync<GameDto>();
        Assert.Equal(GameStatusDto.Lobby, game!.Status);
        Assert.Equal(gameMaster.Id, game.GameMasterId);
        Assert.Equal("master", game.GameMasterName);
        Assert.Equal(Domain.Game.DefaultMaxPlayers, game.MaxPlayers);
        Assert.Empty(game.Participants);
    }

    [Fact]
    public async Task GetGame_WhenUnknown_Returns404()
    {
        var user = await CreateUserAsync("user");

        var response = await user.Client.GetAsync($"/games/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetGame_AsOutsider_Returns403()
    {
        var gameId = await CreateGameAsync(await CreateUserAsync("master"));
        var outsider = await CreateUserAsync("outsider");

        var response = await outsider.Client.GetAsync($"/games/{gameId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetOpenLobbies_ShowsOnlyForeignLobbiesTheUserHasNotJoined()
    {
        var gameMaster = await CreateUserAsync("master");
        var player = await CreateUserAsync("player");
        var openLobby = await CreateGameAsync(gameMaster);
        var joinedLobby = await CreateGameAsync(gameMaster);
        var runningGame = await CreateGameAsync(gameMaster);
        await CreateGameAsync(player);
        await PostAsync(player, $"/games/{joinedLobby}/join");
        await PostAsync(await CreateUserAsync("other"), $"/games/{runningGame}/join");
        await PostAsync(gameMaster, $"/games/{runningGame}/start");

        var lobbies = await player.Client.GetFromJsonAsync<List<GameSummaryDto>>("/games");

        var lobby = Assert.Single(lobbies!);
        Assert.Equal(openLobby, lobby.Id);
        Assert.Equal("master", lobby.GameMasterName);
    }

    [Fact]
    public async Task Start_AsGameMasterWithPlayer_SetsRunning()
    {
        var gameMaster = await CreateUserAsync("master");
        var gameId = await CreateGameAsync(gameMaster);
        await PostAsync(await CreateUserAsync("player"), $"/games/{gameId}/join");

        var response = await PostAsync(gameMaster, $"/games/{gameId}/start");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(GameStatusDto.Running, (await GetGameAsync(gameMaster, gameId)).Status);
    }

    [Fact]
    public async Task Start_AsPlayer_Returns403()
    {
        var gameId = await CreateGameAsync(await CreateUserAsync("master"));
        var player = await CreateUserAsync("player");
        await PostAsync(player, $"/games/{gameId}/join");

        var response = await PostAsync(player, $"/games/{gameId}/start");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Start_WithoutPlayers_Returns409()
    {
        var gameMaster = await CreateUserAsync("master");
        var gameId = await CreateGameAsync(gameMaster);

        var response = await PostAsync(gameMaster, $"/games/{gameId}/start");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Start_WhenUnknown_Returns404()
    {
        var gameMaster = await CreateUserAsync("master");

        var response = await PostAsync(gameMaster, $"/games/{Guid.NewGuid()}/start");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task End_AsGameMasterWhenRunning_SetsFinished()
    {
        var (gameMaster, _, gameId) = await CreateRunningGameAsync();

        var response = await PostAsync(gameMaster, $"/games/{gameId}/end");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(GameStatusDto.Finished, (await GetGameAsync(gameMaster, gameId)).Status);
    }

    [Fact]
    public async Task End_AsPlayer_Returns403()
    {
        var (_, player, gameId) = await CreateRunningGameAsync();

        var response = await PostAsync(player, $"/games/{gameId}/end");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task End_InLobby_Returns409()
    {
        var gameMaster = await CreateUserAsync("master");
        var gameId = await CreateGameAsync(gameMaster);

        var response = await PostAsync(gameMaster, $"/games/{gameId}/end");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Delete_InLobby_RemovesGame()
    {
        var gameMaster = await CreateUserAsync("master");
        var gameId = await CreateGameAsync(gameMaster);
        await PostAsync(await CreateUserAsync("player"), $"/games/{gameId}/join");

        var response = await gameMaster.Client.DeleteAsync($"/games/{gameId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await gameMaster.Client.GetAsync($"/games/{gameId}")).StatusCode);
    }

    [Fact]
    public async Task Delete_WhenFinished_RemovesGame()
    {
        var (gameMaster, player, gameId) = await CreateRunningGameAsync();
        await gameMaster.Client.PutAsJsonAsync(
            $"/games/{gameId}/participants/{player.Id}/attributes", new[] { new AttributeDto("hp", "10") });
        await PostAsync(gameMaster, $"/games/{gameId}/end");

        var response = await gameMaster.Client.DeleteAsync($"/games/{gameId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WhenRunning_Returns409()
    {
        var (gameMaster, _, gameId) = await CreateRunningGameAsync();

        var response = await gameMaster.Client.DeleteAsync($"/games/{gameId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Delete_AsPlayer_Returns403()
    {
        var gameId = await CreateGameAsync(await CreateUserAsync("master"));
        var player = await CreateUserAsync("player");
        await PostAsync(player, $"/games/{gameId}/join");

        var response = await player.Client.DeleteAsync($"/games/{gameId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateSettings_AsGameMaster_ChangesMaxPlayers()
    {
        var gameMaster = await CreateUserAsync("master");
        var gameId = await CreateGameAsync(gameMaster, maxPlayers: 4);

        var response = await gameMaster.Client.PutAsJsonAsync($"/games/{gameId}/settings", new UpdateSettingsRequest(8));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(8, (await GetGameAsync(gameMaster, gameId)).MaxPlayers);
    }

    [Fact]
    public async Task UpdateSettings_AsPlayer_Returns403()
    {
        var gameId = await CreateGameAsync(await CreateUserAsync("master"));
        var player = await CreateUserAsync("player");
        await PostAsync(player, $"/games/{gameId}/join");

        var response = await player.Client.PutAsJsonAsync($"/games/{gameId}/settings", new UpdateSettingsRequest(8));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateSettings_BelowPlayerCount_Returns409()
    {
        var gameMaster = await CreateUserAsync("master");
        var gameId = await CreateGameAsync(gameMaster, maxPlayers: 4);
        await PostAsync(await CreateUserAsync("first"), $"/games/{gameId}/join");
        await PostAsync(await CreateUserAsync("second"), $"/games/{gameId}/join");

        var response = await gameMaster.Client.PutAsJsonAsync($"/games/{gameId}/settings", new UpdateSettingsRequest(1));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private async Task<(TestUser GameMaster, TestUser Player, Guid GameId)> CreateRunningGameAsync()
    {
        var gameMaster = await CreateUserAsync("master");
        var player = await CreateUserAsync("player");
        var gameId = await CreateGameAsync(gameMaster);
        await PostAsync(player, $"/games/{gameId}/join");
        (await PostAsync(gameMaster, $"/games/{gameId}/start")).EnsureSuccessStatusCode();
        return (gameMaster, player, gameId);
    }
}
