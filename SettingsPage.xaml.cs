namespace Stars.Client;

public partial class SettingsPage : ContentPage
{
	public SettingsPage(MainViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

}
