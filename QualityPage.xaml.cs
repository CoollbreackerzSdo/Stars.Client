using Stars.Shared;

namespace Stars.Client;

public partial class QualityPage : ContentPage
{
	public QualityPage()
	{
		InitializeComponent();
		BindingContext = AppState.Main;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		var intro = AppState.ConsumeIntro();
		await BitrateCard.AnimateOnceAsync(intro, 30);
		await FecCard.AnimateOnceAsync(intro, 110);
		await MetricsCard.AnimateOnceAsync(intro, 190);
	}
}
