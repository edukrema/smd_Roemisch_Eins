namespace Game.App.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Game.App.Services;
using Game.Contracts;

public partial class LobbyListViewModel(IGameApiClient api, IGameHubClient hub, INavigationService navigation)
    : ViewModelBase(navigation)
{
    public ObservableCollection<GameSummaryDto> Lobbies { get; } = [];

    public async Task OnAppearingAsync()
    {
        hub.LobbiesChanged += OnLobbiesChanged;
        await LoadAsync();
        await RunAsync(hub.WatchLobbiesAsync);
    }

    public async Task OnDisappearingAsync()
    {
        hub.LobbiesChanged -= OnLobbiesChanged;
        await RunAsync(hub.UnwatchLobbiesAsync);
    }

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync();

    [RelayCommand]
    private Task JoinAsync(GameSummaryDto lobby) => RunAsync(async () =>
    {
        await api.JoinAsync(lobby.Id);
        await GoToGameAsync(lobby.Id);
    });

    private async void OnLobbiesChanged() => await LoadAsync();

    private Task LoadAsync() => RunAsync(async () => Lobbies.ReplaceWith(await api.GetOpenLobbiesAsync()));
}
