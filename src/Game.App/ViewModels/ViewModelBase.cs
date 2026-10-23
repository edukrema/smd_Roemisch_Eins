namespace Game.App.ViewModels;

using System.Net;
using CommunityToolkit.Mvvm.ComponentModel;
using Game.App.Services;

public abstract partial class ViewModelBase(INavigationService navigation) : ObservableObject
{
    protected INavigationService Navigation { get; } = navigation;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    /// <summary>Runs an action and shows errors instead of letting the app crash.</summary>
    protected async Task RunAsync(Func<Task> action)
    {
        ErrorMessage = null;
        try
        {
            await action();
        }
        catch (ApiException e) when (e.StatusCode == HttpStatusCode.Unauthorized)
        {
            await Navigation.GoToAsync(Routes.Login);
        }
        catch (ApiException e)
        {
            ErrorMessage = e.Message;
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "The server is not reachable.";
        }
        catch (Exception e)
        {
            ErrorMessage = $"Unexpected error: {e.Message}";
        }
    }

    protected Task GoToGameAsync(Guid gameId) =>
        Navigation.GoToAsync(Routes.Game, new Dictionary<string, object> { [Routes.GameIdParameter] = gameId });
}
