namespace Game.App.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game.App.Services;

public partial class LoginViewModel(IAuthService authService, INavigationService navigation)
    : ViewModelBase(navigation)
{
    [ObservableProperty]
    public partial string? Username { get; set; }

    [ObservableProperty]
    public partial string? Password { get; set; }

    [RelayCommand]
    // REQ13
    private Task LoginAsync() => RunAsync(async () =>
    {
        await authService.LoginAsync(Username ?? "", Password ?? "");
        await Navigation.GoToAsync(Routes.Menu);
    });
}
