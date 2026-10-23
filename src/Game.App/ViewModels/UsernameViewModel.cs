namespace Game.App.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game.App.Services;

public partial class UsernameViewModel(IGameApiClient api, INavigationService navigation) : ViewModelBase(navigation)
{
    [ObservableProperty]
    public partial string? Username { get; set; }

    public Task OnAppearingAsync() => RunAsync(async () => Username = (await api.GetMeAsync()).Username);

    [RelayCommand]
    private Task SaveAsync() => RunAsync(async () =>
    {
        await api.ChangeUsernameAsync(Username ?? "");
        await Navigation.GoToAsync(Routes.Menu);
    });
}
