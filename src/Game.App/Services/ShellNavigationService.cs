namespace Game.App.Services;

public class ShellNavigationService : INavigationService
{
    public Task GoToAsync(string route, IDictionary<string, object>? parameters = null) =>
        MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync(route, parameters ?? new Dictionary<string, object>()));
}
