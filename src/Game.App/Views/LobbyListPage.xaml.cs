using Game.App.ViewModels;

namespace Game.App.Views;

public partial class LobbyListPage : ContentPage
{
	private readonly LobbyListViewModel _viewModel;

	public LobbyListPage(LobbyListViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = _viewModel = viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _viewModel.OnAppearingAsync();
	}

	protected override async void OnDisappearing()
	{
		base.OnDisappearing();
		await _viewModel.OnDisappearingAsync();
	}
}
