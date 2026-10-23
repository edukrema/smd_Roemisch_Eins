namespace Game.App.ViewModels;

using System.Collections.ObjectModel;
using System.Net;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game.App.Services;
using Game.Contracts;

public partial class GameViewModel(IGameApiClient api, IGameHubClient hub, INavigationService navigation)
    : ViewModelBase(navigation), IQueryAttributable
{
    private Guid _myUserId;

    public Guid GameId { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title), nameof(StatusText), nameof(IsGameMaster), nameof(CanStart), nameof(CanEnd),
        nameof(CanDelete), nameof(CanLeave), nameof(CanEditSettings))]
    public partial GameDto? Game { get; set; }

    [ObservableProperty]
    public partial int NewMaxPlayers { get; set; }

    public ObservableCollection<ParticipantViewModel> Participants { get; } = [];

    public string Title => Game is null ? "" : $"Game by {Game.GameMasterName}";

    public string StatusText => Game?.Status switch
    {
        GameStatusDto.Lobby => $"Lobby ({Game.Participants.Count} of {Game.MaxPlayers} players)",
        GameStatusDto.Running => "Running",
        GameStatusDto.Finished => "Finished",
        _ => ""
    };

    public bool IsGameMaster => Game is not null && Game.GameMasterId == _myUserId;

    public bool CanStart => IsGameMaster && Game!.Status == GameStatusDto.Lobby;

    public bool CanEnd => IsGameMaster && Game!.Status == GameStatusDto.Running;

    public bool CanDelete => IsGameMaster && Game!.Status != GameStatusDto.Running;

    public bool CanLeave => Game is not null && !IsGameMaster && Game.Status != GameStatusDto.Finished;

    public bool CanEditSettings => CanStart;

    public bool CanEditAttributes => IsGameMaster && Game!.Status != GameStatusDto.Finished;

    public void ApplyQueryAttributes(IDictionary<string, object> query) => GameId = (Guid)query[Routes.GameIdParameter];

    public async Task OnAppearingAsync()
    {
        hub.GameChanged += OnGameChanged;
        await RunAsync(async () =>
        {
            _myUserId = (await api.GetMeAsync()).Id;
            await LoadGameAsync();
        });

        if (Game is not null)
        {
            await RunAsync(() => hub.WatchGameAsync(GameId));
        }
    }

    public async Task OnDisappearingAsync()
    {
        hub.GameChanged -= OnGameChanged;
        await RunAsync(hub.UnwatchGameAsync);
    }

    [RelayCommand]
    private Task StartAsync() => ChangeGameAsync(() => api.StartAsync(GameId));

    [RelayCommand]
    private Task EndAsync() => ChangeGameAsync(() => api.EndAsync(GameId));

    [RelayCommand]
    private Task SaveSettingsAsync() => ChangeGameAsync(() => api.UpdateSettingsAsync(GameId, NewMaxPlayers));

    [RelayCommand]
    private Task DeleteAsync() => RunAsync(async () =>
    {
        await api.DeleteAsync(GameId);
        await Navigation.GoToAsync(Routes.Menu);
    });

    [RelayCommand]
    private Task LeaveAsync() => RunAsync(async () =>
    {
        await api.LeaveAsync(GameId);
        await Navigation.GoToAsync(Routes.Menu);
    });

    [RelayCommand]
    private void AddAttribute(ParticipantViewModel participant) => participant.AddEmptyRow();

    [RelayCommand]
    private Task SaveAttributesAsync(ParticipantViewModel participant) =>
        ChangeGameAsync(() => api.ReplaceAttributesAsync(GameId, participant.UserId, participant.ToAttributes()));

    /// <summary>Reloads after every own change, even if the SignalR signal does not arrive.</summary>
    private Task ChangeGameAsync(Func<Task> change) => RunAsync(async () =>
    {
        await change();
        await LoadGameAsync();
    });

    private async void OnGameChanged(Guid gameId)
    {
        if (gameId == GameId)
        {
            await RunAsync(LoadGameAsync);
        }
    }

    private async Task LoadGameAsync()
    {
        try
        {
            Game = await api.GetGameAsync(GameId);
            NewMaxPlayers = Game.MaxPlayers;
            Participants.ReplaceWith(Game.Participants.Select(p => new ParticipantViewModel(p, CanEditAttributes)));
        }
        catch (ApiException e) when (e.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
        {
            // The game was deleted or the user is no longer part of it.
            Game = null;
            await Navigation.GoToAsync(Routes.Menu);
        }
    }
}
