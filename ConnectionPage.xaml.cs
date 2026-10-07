using Stars.Client.Features.Discovery;
using Stars.Shared;

namespace Stars.Client;

public partial class ConnectionPage : ContentPage
{
	public ConnectionPage()
	{
		InitializeComponent();
		BindingContext = AppState.Main;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		var intro = AppState.ConsumeIntro();
		await LinkCard.AnimateOnceAsync(intro, 30);
		await ServersCard.AnimateOnceAsync(intro, 110);
		await OutputCard.AnimateOnceAsync(intro, 190);
	}

	private void OnServerSelected(object? sender, DiscoveredServer server)
		=> AppState.Main.UseServer(server);
}
