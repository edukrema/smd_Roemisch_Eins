namespace Game.App.Tests;

using System.Net;
using Game.App.Services;
using Game.App.ViewModels;
using Game.Contracts;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

public class MenuViewModelTests
{
    private readonly IGameApiClient _api = Substitute.For<IGameApiClient>();
    private readonly IAuthService _auth = Substitute.For<IAuthService>();
    private readonly INavigationService _navigation = Substitute.For<INavigationService>();
    private readonly MenuViewModel _viewModel;

    public MenuViewModelTests()
    {
        _viewModel = new MenuViewModel(_api, _auth, _navigation);
    }

    [Fact]
    public async Task OnAppearing_LoadsUsername()
    {
        _api.GetMeAsync().Returns(new MeDto(Guid.NewGuid(), "alice"));

        await _viewModel.OnAppearingAsync();

        Assert.Equal("alice", _viewModel.Username);
    }

    [Fact]
    public async Task CreateGame_CreatesLobbyAndOpensIt()
    {
        var gameId = Guid.NewGuid();
        _api.CreateGameAsync().Returns(new GameDto(gameId, Guid.NewGuid(), "alice", GameStatusDto.Lobby, 4, []));

        await _viewModel.CreateGameCommand.ExecuteAsync(null);

        await _navigation.Received().GoToAsync(Routes.Game, Arg.Is<IDictionary<string, object>>(
            p => (Guid)p[Routes.GameIdParameter] == gameId));
    }

    [Fact]
    public async Task CreateGame_WhenRejected_ShowsServerMessage()
    {
        _api.CreateGameAsync().ThrowsAsync(new ApiException(HttpStatusCode.Conflict, "Not possible."));

        await _viewModel.CreateGameCommand.ExecuteAsync(null);

        Assert.Equal("Not possible.", _viewModel.ErrorMessage);
    }

    [Fact]
    public async Task Logout_WhenBrowserLogoutFails_StillNavigatesToLogin()
    {
        _auth.LogoutAsync().ThrowsAsync(new InvalidOperationException("Browser geschlossen"));

        await _viewModel.LogoutCommand.ExecuteAsync(null);

        await _navigation.Received().GoToAsync(Routes.Login);
    }
}

public class LobbyListViewModelTests
{
    private readonly IGameApiClient _api = Substitute.For<IGameApiClient>();
    private readonly IGameHubClient _hub = Substitute.For<IGameHubClient>();
    private readonly INavigationService _navigation = Substitute.For<INavigationService>();
    private readonly LobbyListViewModel _viewModel;

    public LobbyListViewModelTests()
    {
        _viewModel = new LobbyListViewModel(_api, _hub, _navigation);
    }

    private static GameSummaryDto Lobby() => new(Guid.NewGuid(), "master", GameStatusDto.Lobby, 1, 4);

    [Fact]
    public async Task OnAppearing_LoadsLobbiesAndWatchesLobbyList()
    {
        var lobby = Lobby();
        _api.GetOpenLobbiesAsync().Returns([lobby]);

        await _viewModel.OnAppearingAsync();

        Assert.Equal([lobby], _viewModel.Lobbies);
        await _hub.Received().WatchLobbiesAsync();
    }

    [Fact]
    public async Task LobbiesChanged_ReloadsLobbies()
    {
        await _viewModel.OnAppearingAsync();
        var newLobby = Lobby();
        _api.GetOpenLobbiesAsync().Returns([newLobby]);

        _hub.LobbiesChanged += Raise.Event<Action>();

        Assert.Equal([newLobby], _viewModel.Lobbies);
    }

    [Fact]
    public async Task OnDisappearing_StopsWatchingAndIgnoresSignals()
    {
        await _viewModel.OnAppearingAsync();
        await _viewModel.OnDisappearingAsync();
        _api.ClearReceivedCalls();

        _hub.LobbiesChanged += Raise.Event<Action>();

        await _hub.Received().UnwatchLobbiesAsync();
        await _api.DidNotReceive().GetOpenLobbiesAsync();
    }

    [Fact]
    public async Task OnAppearing_WhenHubIsUnreachable_StillShowsLobbies()
    {
        _api.GetOpenLobbiesAsync().Returns([Lobby()]);
        _hub.WatchLobbiesAsync().ThrowsAsync(new HttpRequestException());

        await _viewModel.OnAppearingAsync();

        Assert.Single(_viewModel.Lobbies);
        Assert.NotNull(_viewModel.ErrorMessage);
    }

    [Fact]
    public async Task Join_JoinsAndOpensGame()
    {
        var lobby = Lobby();

        await _viewModel.JoinCommand.ExecuteAsync(lobby);

        await _api.Received().JoinAsync(lobby.Id);
        await _navigation.Received().GoToAsync(Routes.Game, Arg.Is<IDictionary<string, object>>(
            p => (Guid)p[Routes.GameIdParameter] == lobby.Id));
    }

    [Fact]
    public async Task Join_WhenLobbyIsFull_ShowsErrorAndStays()
    {
        var lobby = Lobby();
        _api.JoinAsync(lobby.Id).ThrowsAsync(new ApiException(HttpStatusCode.Conflict, "The lobby is full."));

        await _viewModel.JoinCommand.ExecuteAsync(lobby);

        Assert.Equal("The lobby is full.", _viewModel.ErrorMessage);
        await _navigation.DidNotReceiveWithAnyArgs().GoToAsync(default!, default);
    }
}
