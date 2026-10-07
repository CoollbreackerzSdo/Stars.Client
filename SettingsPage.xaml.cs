using Stars.Shared;

namespace Stars.Client;

public partial class SettingsPage : ContentPage
{
	public SettingsPage()
	{
		InitializeComponent();
		BindingContext = AppState.Main;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		var intro = AppState.ConsumeIntro();
		await AppearanceCard.AnimateOnceAsync(intro, 30);
		await ActivityCard.AnimateOnceAsync(intro, 110);
		await AboutCard.AnimateOnceAsync(intro, 190);
	}
}
