namespace Game.App.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game.App.Services;

public partial class MenuViewModel(IGameApiClient api, IAuthService authService, INavigationService navigation)
    : ViewModelBase(navigation)
{
    [ObservableProperty]
    public partial string? Username { get; set; }

    public Task OnAppearingAsync() => RunAsync(async () =>
    {
        Username = (await api.GetMeAsync()).Username;
    });

    [RelayCommand]
    private Task CreateGameAsync() => RunAsync(async () =>
    {
        var game = await api.CreateGameAsync();
        await GoToGameAsync(game.Id);
    });

    [RelayCommand]
    private Task OpenLobbiesAsync() => Navigation.GoToAsync(Routes.Lobbies);

    [RelayCommand]
    private Task ChangeUsernameAsync() => Navigation.GoToAsync(Routes.Username);

    [RelayCommand]
    private Task LogoutAsync() => RunAsync(async () =>
    {
        try
        {
            await authService.LogoutAsync();
        }
        finally
        {
            await Navigation.GoToAsync(Routes.Login);
        }
    });
}
