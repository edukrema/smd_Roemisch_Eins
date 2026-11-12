using Game.App.Services;
using Game.App.ViewModels;
using Game.App.Views;

namespace Game.App;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder.UseMauiApp<App>();

		builder.Services.AddSingleton<IAuthService, AuthService>();
		builder.Services.AddSingleton(new HttpClient(DevCertificate.CreateHandler()) { BaseAddress = new Uri(AppSettings.ApiBaseUrl) });
		builder.Services.AddSingleton<IGameApiClient, GameApiClient>();
		builder.Services.AddSingleton<IGameHubClient, GameHubClient>();
		builder.Services.AddSingleton<INavigationService, ShellNavigationService>();

		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<LoginViewModel>();
		builder.Services.AddTransient<UsernamePage>();
		builder.Services.AddTransient<UsernameViewModel>();
		builder.Services.AddTransient<MenuPage>();
		builder.Services.AddTransient<MenuViewModel>();
		builder.Services.AddTransient<LobbyListPage>();
		builder.Services.AddTransient<LobbyListViewModel>();
		builder.Services.AddTransient<GamePage>();
		builder.Services.AddTransient<GameViewModel>();

		return builder.Build();
	}
}
