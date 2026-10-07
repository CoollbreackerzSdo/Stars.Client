using Stars.Shared;

namespace Stars.Client;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
		BindingContext = AppState.Main;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		var intro = AppState.ConsumeIntro();
		await Hero.AnimateOnceAsync(intro, 30);
		await NowCard.AnimateOnceAsync(intro, 110);
		await TipCard.AnimateOnceAsync(intro, 190);
	}
}
