using Game.App.Views;

namespace Game.App;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		Routing.RegisterRoute(Routes.Username, typeof(UsernamePage));
		Routing.RegisterRoute(Routes.Lobbies, typeof(LobbyListPage));
		Routing.RegisterRoute(Routes.Game, typeof(GamePage));
	}
}
