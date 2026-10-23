using Game.App.ViewModels;

namespace Game.App.Views;

public partial class MenuPage : ContentPage
{
	private readonly MenuViewModel _viewModel;

	public MenuPage(MenuViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = _viewModel = viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _viewModel.OnAppearingAsync();
	}
}
