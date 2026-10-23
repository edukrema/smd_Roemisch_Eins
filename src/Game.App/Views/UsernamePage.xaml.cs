using Game.App.ViewModels;

namespace Game.App.Views;

public partial class UsernamePage : ContentPage
{
	private readonly UsernameViewModel _viewModel;

	public UsernamePage(UsernameViewModel viewModel)
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
