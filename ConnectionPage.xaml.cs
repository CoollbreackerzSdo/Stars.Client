using Stars.Client.Features.Discovery;

namespace Stars.Client;

public partial class ConnectionPage : ContentPage
{
	private readonly MainViewModel _viewModel;

	public ConnectionPage(MainViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = viewModel;
	}

	private void OnServerSelected(object? sender, DiscoveredServer server)
		=> _viewModel.UseServer(server);
}
