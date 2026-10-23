namespace Game.Api.Tests;

using System.Net;
using System.Net.Http.Json;
using Game.Api.Tests.Infrastructure;
using Game.Contracts;

public class AttributeTests(ApiFactory factory) : ApiTestBase(factory)
{
    private TestUser _gameMaster = null!;
    private TestUser _player = null!;
    private Guid _gameId;

    private string AttributesPath(Guid userId) => $"/games/{_gameId}/participants/{userId}/attributes";

    private async Task SetUpGameAsync()
    {
        _gameMaster = await CreateUserAsync("master");
        _player = await CreateUserAsync("player");
        _gameId = await CreateGameAsync(_gameMaster);
        await PostAsync(_player, $"/games/{_gameId}/join");
    }

    [Fact]
    public async Task Replace_AsGameMaster_IsVisibleToAllPlayers()
    {
        await SetUpGameAsync();
        var otherPlayer = await CreateUserAsync("other");
        await PostAsync(otherPlayer, $"/games/{_gameId}/join");

        var response = await _gameMaster.Client.PutAsJsonAsync(AttributesPath(_player.Id),
            new[] { new AttributeDto("hp", "10"), new AttributeDto("class", "Mage") });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var game = await GetGameAsync(otherPlayer, _gameId);
        var attributes = game.Participants.Single(p => p.UserId == _player.Id).Attributes;
        Assert.Equal([new AttributeDto("class", "Mage"), new AttributeDto("hp", "10")], attributes);
    }

    [Fact]
    public async Task Replace_WithNewList_ReplacesOldAttributes()
    {
        await SetUpGameAsync();
        await _gameMaster.Client.PutAsJsonAsync(AttributesPath(_player.Id),
            new[] { new AttributeDto("hp", "10"), new AttributeDto("mana", "3") });

        await _gameMaster.Client.PutAsJsonAsync(AttributesPath(_player.Id),
            new[] { new AttributeDto("HP", "8"), new AttributeDto("note", " ") });

        var attributes = (await GetGameAsync(_gameMaster, _gameId)).Participants.Single().Attributes;
        Assert.Equal([new AttributeDto("HP", "8"), new AttributeDto("note", null)], attributes);
    }

    [Fact]
    public async Task Replace_WhenRunning_Succeeds()
    {
        await SetUpGameAsync();
        await PostAsync(_gameMaster, $"/games/{_gameId}/start");

        var response = await _gameMaster.Client.PutAsJsonAsync(AttributesPath(_player.Id), new[] { new AttributeDto("hp", "1") });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Replace_AsPlayer_Returns403()
    {
        await SetUpGameAsync();

        var response = await _player.Client.PutAsJsonAsync(AttributesPath(_player.Id), new[] { new AttributeDto("hp", "99") });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Replace_WhenFinished_Returns409()
    {
        await SetUpGameAsync();
        await PostAsync(_gameMaster, $"/games/{_gameId}/start");
        await PostAsync(_gameMaster, $"/games/{_gameId}/end");

        var response = await _gameMaster.Client.PutAsJsonAsync(AttributesPath(_player.Id), new[] { new AttributeDto("hp", "1") });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Replace_ForNonParticipant_Returns409()
    {
        await SetUpGameAsync();

        var response = await _gameMaster.Client.PutAsJsonAsync(AttributesPath(Guid.NewGuid()), new[] { new AttributeDto("hp", "1") });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Replace_WithDuplicateKeys_Returns400()
    {
        await SetUpGameAsync();

        var response = await _gameMaster.Client.PutAsJsonAsync(AttributesPath(_player.Id),
            new[] { new AttributeDto("hp", "1"), new AttributeDto("Hp", "2") });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Replace_WithNullEntry_Returns400()
    {
        await SetUpGameAsync();

        var response = await _gameMaster.Client.PutAsJsonAsync(AttributesPath(_player.Id), new AttributeDto?[] { null });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Replace_WhenGameUnknown_Returns404()
    {
        await SetUpGameAsync();
        _gameId = Guid.NewGuid();

        var response = await _gameMaster.Client.PutAsJsonAsync(AttributesPath(_player.Id), new[] { new AttributeDto("hp", "1") });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
