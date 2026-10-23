namespace Game.App.Tests;

using System.Net;
using Game.App.Services;
using Game.App.ViewModels;
using Game.Contracts;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

public class LoginViewModelTests
{
    private readonly IAuthService _auth = Substitute.For<IAuthService>();
    private readonly INavigationService _navigation = Substitute.For<INavigationService>();
    private readonly LoginViewModel _viewModel;

    public LoginViewModelTests()
    {
        _viewModel = new LoginViewModel(_auth, _navigation);
    }

    [Fact]
    public async Task Login_WhenSuccessful_NavigatesToMenu()
    {
        _viewModel.Username = "alice";
        _viewModel.Password = "secret";

        await _viewModel.LoginCommand.ExecuteAsync(null);

        await _auth.Received().LoginAsync("alice", "secret");
        await _navigation.Received().GoToAsync(Routes.Menu);
    }

    [Fact]
    public async Task Login_WhenRejected_ShowsServerMessageAndStays()
    {
        _viewModel.Username = "alice";
        _viewModel.Password = "wrong";
        _auth.LoginAsync("alice", "wrong")
            .ThrowsAsync(new ApiException(HttpStatusCode.Conflict, "Wrong password."));

        await _viewModel.LoginCommand.ExecuteAsync(null);

        Assert.Equal("Wrong password.", _viewModel.ErrorMessage);
        await _navigation.DidNotReceiveWithAnyArgs().GoToAsync(default!);
    }
}

public class UsernameViewModelTests
{
    private readonly IGameApiClient _api = Substitute.For<IGameApiClient>();
    private readonly INavigationService _navigation = Substitute.For<INavigationService>();
    private readonly UsernameViewModel _viewModel;

    public UsernameViewModelTests()
    {
        _viewModel = new UsernameViewModel(_api, _navigation);
    }

    [Fact]
    public async Task OnAppearing_ShowsCurrentUsername()
    {
        _api.GetMeAsync().Returns(new MeDto(Guid.NewGuid(), "alice"));

        await _viewModel.OnAppearingAsync();

        Assert.Equal("alice", _viewModel.Username);
    }

    [Fact]
    public async Task Save_WhenAccepted_NavigatesToMenu()
    {
        _viewModel.Username = "alice";

        await _viewModel.SaveCommand.ExecuteAsync(null);

        await _api.Received().ChangeUsernameAsync("alice");
        await _navigation.Received().GoToAsync(Routes.Menu);
    }

    [Fact]
    public async Task Save_WhenUsernameIsTaken_ShowsServerMessageAndStays()
    {
        _viewModel.Username = "alice";
        _api.ChangeUsernameAsync("alice")
            .ThrowsAsync(new ApiException(HttpStatusCode.Conflict, "The username is already taken."));

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal("The username is already taken.", _viewModel.ErrorMessage);
        await _navigation.DidNotReceiveWithAnyArgs().GoToAsync(default!);
    }

    [Fact]
    public async Task Save_WhenServerIsUnreachable_ShowsError()
    {
        _viewModel.Username = "alice";
        _api.ChangeUsernameAsync("alice").ThrowsAsync(new HttpRequestException());

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal("The server is not reachable.", _viewModel.ErrorMessage);
    }

    [Fact]
    public async Task Save_WhenSessionExpired_NavigatesToLogin()
    {
        _viewModel.Username = "alice";
        _api.ChangeUsernameAsync("alice").ThrowsAsync(new ApiException(HttpStatusCode.Unauthorized, "Nicht angemeldet"));

        await _viewModel.SaveCommand.ExecuteAsync(null);

        await _navigation.Received().GoToAsync(Routes.Login);
    }
}
