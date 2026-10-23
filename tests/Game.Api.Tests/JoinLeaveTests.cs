namespace Game.Api.Tests;

using System.Net;
using Game.Api.Tests.Infrastructure;

public class JoinLeaveTests(ApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Join_OpenLobby_AddsPlayer()
    {
        var gameMaster = await CreateUserAsync("master");
        var gameId = await CreateGameAsync(gameMaster);
        var player = await CreateUserAsync("player");

        var response = await PostAsync(player, $"/games/{gameId}/join");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var game = await GetGameAsync(player, gameId);
        var participant = Assert.Single(game.Participants);
        Assert.Equal(player.Id, participant.UserId);
        Assert.Equal("player", participant.Username);
    }

    [Fact]
    public async Task Join_WhenUnknown_Returns404()
    {
        var player = await CreateUserAsync("player");

        var response = await PostAsync(player, $"/games/{Guid.NewGuid()}/join");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Join_WhenLobbyIsFull_Returns409()
    {
        var gameId = await CreateGameAsync(await CreateUserAsync("master"), maxPlayers: 1);
        await PostAsync(await CreateUserAsync("first"), $"/games/{gameId}/join");

        var response = await PostAsync(await CreateUserAsync("second"), $"/games/{gameId}/join");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Join_WhenAlreadyJoined_Returns409()
    {
        var gameId = await CreateGameAsync(await CreateUserAsync("master"));
        var player = await CreateUserAsync("player");
        await PostAsync(player, $"/games/{gameId}/join");

        var response = await PostAsync(player, $"/games/{gameId}/join");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Join_AsGameMaster_Returns409()
    {
        var gameMaster = await CreateUserAsync("master");
        var gameId = await CreateGameAsync(gameMaster);

        var response = await PostAsync(gameMaster, $"/games/{gameId}/join");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Join_WithoutUsername_Returns409()
    {
        var gameId = await CreateGameAsync(await CreateUserAsync("master"));
        var client = Factory.CreateClientFor(Guid.NewGuid());

        var response = await client.PostAsync($"/games/{gameId}/join", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Join_WhenRunning_Returns409()
    {
        var gameMaster = await CreateUserAsync("master");
        var gameId = await CreateGameAsync(gameMaster);
        await PostAsync(await CreateUserAsync("player"), $"/games/{gameId}/join");
        await PostAsync(gameMaster, $"/games/{gameId}/start");

        var response = await PostAsync(await CreateUserAsync("late"), $"/games/{gameId}/join");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Join_ParallelForLastSlot_OnlyOneSucceeds()
    {
        var gameMaster = await CreateUserAsync("master");
        var gameId = await CreateGameAsync(gameMaster, maxPlayers: 1);
        var players = await Task.WhenAll(Enumerable.Range(1, 8).Select(i => CreateUserAsync($"player{i}")));

        var responses = await Task.WhenAll(players.Select(p => PostAsync(p, $"/games/{gameId}/join")));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.NoContent);
        Assert.All(responses.Where(r => r.StatusCode != HttpStatusCode.NoContent),
            r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode));
        Assert.Single((await GetGameAsync(gameMaster, gameId)).Participants);
    }

    [Fact]
    public async Task Join_ParallelBySameUser_AddsPlayerOnce()
    {
        var gameMaster = await CreateUserAsync("master");
        var gameId = await CreateGameAsync(gameMaster, maxPlayers: 10);
        var player = await CreateUserAsync("player");

        await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => PostAsync(player, $"/games/{gameId}/join")));

        Assert.Single((await GetGameAsync(gameMaster, gameId)).Participants);
    }

    [Fact]
    public async Task Leave_InLobby_RemovesPlayerAndAllowsJoiningAgain()
    {
        var gameId = await CreateGameAsync(await CreateUserAsync("master"));
        var player = await CreateUserAsync("player");
        await PostAsync(player, $"/games/{gameId}/join");

        var leave = await PostAsync(player, $"/games/{gameId}/leave");
        var rejoin = await PostAsync(player, $"/games/{gameId}/join");

        Assert.Equal(HttpStatusCode.NoContent, leave.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, rejoin.StatusCode);
    }

    [Fact]
    public async Task Leave_WhenRunning_RemovesPlayerAndForbidsJoiningAgain()
    {
        var gameMaster = await CreateUserAsync("master");
        var gameId = await CreateGameAsync(gameMaster);
        var player = await CreateUserAsync("player");
        await PostAsync(player, $"/games/{gameId}/join");
        await PostAsync(gameMaster, $"/games/{gameId}/start");

        var leave = await PostAsync(player, $"/games/{gameId}/leave");
        var rejoin = await PostAsync(player, $"/games/{gameId}/join");

        Assert.Equal(HttpStatusCode.NoContent, leave.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, rejoin.StatusCode);
        Assert.Empty((await GetGameAsync(gameMaster, gameId)).Participants);
    }

    [Fact]
    public async Task Leave_AsGameMaster_Returns409()
    {
        var gameMaster = await CreateUserAsync("master");
        var gameId = await CreateGameAsync(gameMaster);

        var response = await PostAsync(gameMaster, $"/games/{gameId}/leave");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Leave_AsOutsider_Returns409()
    {
        var gameId = await CreateGameAsync(await CreateUserAsync("master"));

        var response = await PostAsync(await CreateUserAsync("outsider"), $"/games/{gameId}/leave");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Leave_AfterLeaving_GameIsNoLongerVisible()
    {
        var gameId = await CreateGameAsync(await CreateUserAsync("master"));
        var player = await CreateUserAsync("player");
        await PostAsync(player, $"/games/{gameId}/join");
        await PostAsync(player, $"/games/{gameId}/leave");

        var response = await player.Client.GetAsync($"/games/{gameId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
