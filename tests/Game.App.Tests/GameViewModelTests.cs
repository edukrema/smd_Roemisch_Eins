namespace Game.App.Tests;

using System.Net;
using Game.App.Services;
using Game.App.ViewModels;
using Game.Contracts;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

public class GameViewModelTests
{
    private static readonly Guid GameId = Guid.NewGuid();
    private static readonly Guid GameMasterId = Guid.NewGuid();
    private static readonly Guid PlayerId = Guid.NewGuid();

    private readonly IGameApiClient _api = Substitute.For<IGameApiClient>();
    private readonly IGameHubClient _hub = Substitute.For<IGameHubClient>();
    private readonly INavigationService _navigation = Substitute.For<INavigationService>();
    private readonly GameViewModel _viewModel;

    public GameViewModelTests()
    {
        _viewModel = new GameViewModel(_api, _hub, _navigation);
        _viewModel.ApplyQueryAttributes(new Dictionary<string, object> { [Routes.GameIdParameter] = GameId });
    }

    private static GameDto Game(GameStatusDto status, params AttributeDto[] playerAttributes) =>
        new(GameId, GameMasterId, "master", status, 4,
            [new ParticipantDto(PlayerId, "player", playerAttributes)]);

    private async Task OpenAsAsync(Guid userId, GameDto game)
    {
        _api.GetMeAsync().Returns(new MeDto(userId, "me"));
        _api.GetGameAsync(GameId).Returns(game);
        await _viewModel.OnAppearingAsync();
    }

    [Fact]
    public async Task OnAppearing_AsGameMasterInLobby_AllowsGameMasterActions()
    {
        await OpenAsAsync(GameMasterId, Game(GameStatusDto.Lobby));

        Assert.True(_viewModel.IsGameMaster);
        Assert.True(_viewModel.CanStart);
        Assert.True(_viewModel.CanDelete);
        Assert.True(_viewModel.CanEditSettings);
        Assert.False(_viewModel.CanEnd);
        Assert.False(_viewModel.CanLeave);
        Assert.True(_viewModel.Participants.Single().CanEdit);
        await _hub.Received().WatchGameAsync(GameId);
    }

    [Fact]
    public async Task OnAppearing_AsPlayer_OnlyAllowsLeavingAndShowsAttributesReadOnly()
    {
        await OpenAsAsync(PlayerId, Game(GameStatusDto.Running, new AttributeDto("hp", "10")));

        Assert.False(_viewModel.IsGameMaster);
        Assert.False(_viewModel.CanStart);
        Assert.False(_viewModel.CanEnd);
        Assert.False(_viewModel.CanDelete);
        Assert.True(_viewModel.CanLeave);
        var attribute = _viewModel.Participants.Single().Attributes.Single();
        Assert.Equal("hp", attribute.Key);
        Assert.True(attribute.IsReadOnly);
    }

    [Fact]
    public async Task OnAppearing_WhenGameIsFinished_GameMasterCanOnlyDelete()
    {
        await OpenAsAsync(GameMasterId, Game(GameStatusDto.Finished));

        Assert.True(_viewModel.CanDelete);
        Assert.False(_viewModel.CanEnd);
        Assert.False(_viewModel.Participants.Single().CanEdit);
    }

    [Fact]
    public async Task GameChanged_ForThisGame_ReloadsGame()
    {
        await OpenAsAsync(PlayerId, Game(GameStatusDto.Lobby));
        _api.GetGameAsync(GameId).Returns(Game(GameStatusDto.Running));

        _hub.GameChanged += Raise.Event<Action<Guid>>(GameId);

        Assert.Equal(GameStatusDto.Running, _viewModel.Game!.Status);
    }

    [Fact]
    public async Task GameChanged_ForOtherGame_IsIgnored()
    {
        await OpenAsAsync(PlayerId, Game(GameStatusDto.Lobby));
        _api.ClearReceivedCalls();

        _hub.GameChanged += Raise.Event<Action<Guid>>(Guid.NewGuid());

        await _api.DidNotReceive().GetGameAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task GameChanged_WhenGameWasDeleted_NavigatesToMenu()
    {
        await OpenAsAsync(PlayerId, Game(GameStatusDto.Lobby));
        _api.GetGameAsync(GameId).ThrowsAsync(new ApiException(HttpStatusCode.NotFound, "The game was not found."));

        _hub.GameChanged += Raise.Event<Action<Guid>>(GameId);

        await _navigation.Received().GoToAsync(Routes.Menu);
    }

    [Fact]
    public async Task OnAppearing_WhenGameWasDeleted_NavigatesToMenuWithoutWatching()
    {
        _api.GetMeAsync().Returns(new MeDto(PlayerId, "player"));
        _api.GetGameAsync(GameId).ThrowsAsync(new ApiException(HttpStatusCode.NotFound, "The game was not found."));

        await _viewModel.OnAppearingAsync();

        await _navigation.Received().GoToAsync(Routes.Menu);
        await _hub.DidNotReceive().WatchGameAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task OnDisappearing_StopsWatching()
    {
        await OpenAsAsync(PlayerId, Game(GameStatusDto.Lobby));

        await _viewModel.OnDisappearingAsync();

        await _hub.Received().UnwatchGameAsync();
    }

    [Fact]
    public async Task Start_StartsGameAndReloads()
    {
        await OpenAsAsync(GameMasterId, Game(GameStatusDto.Lobby));
        _api.GetGameAsync(GameId).Returns(Game(GameStatusDto.Running));

        await _viewModel.StartCommand.ExecuteAsync(null);

        await _api.Received().StartAsync(GameId);
        Assert.True(_viewModel.CanEnd);
    }

    [Fact]
    public async Task Start_WhenRejected_ShowsServerMessage()
    {
        await OpenAsAsync(GameMasterId, Game(GameStatusDto.Lobby));
        _api.StartAsync(GameId).ThrowsAsync(new ApiException(HttpStatusCode.Conflict, "At least one player."));

        await _viewModel.StartCommand.ExecuteAsync(null);

        Assert.Equal("At least one player.", _viewModel.ErrorMessage);
    }

    [Fact]
    public async Task Leave_LeavesAndNavigatesToMenu()
    {
        await OpenAsAsync(PlayerId, Game(GameStatusDto.Lobby));

        await _viewModel.LeaveCommand.ExecuteAsync(null);

        await _api.Received().LeaveAsync(GameId);
        await _navigation.Received().GoToAsync(Routes.Menu);
    }

    [Fact]
    public async Task SaveAttributes_SendsOnlyRowsWithKey()
    {
        await OpenAsAsync(GameMasterId, Game(GameStatusDto.Running, new AttributeDto("hp", "10")));
        var participant = _viewModel.Participants.Single();
        participant.Attributes[0].Value = "8";
        _viewModel.AddAttributeCommand.Execute(participant);
        _viewModel.AddAttributeCommand.Execute(participant);
        participant.Attributes[1].Key = "class";
        participant.Attributes[1].Value = "Mage";

        await _viewModel.SaveAttributesCommand.ExecuteAsync(participant);

        await _api.Received().ReplaceAttributesAsync(GameId, PlayerId, Arg.Is<IReadOnlyList<AttributeDto>>(a =>
            a.SequenceEqual(new[] { new AttributeDto("hp", "8"), new AttributeDto("class", "Mage") })));
    }
}
