using Microsoft.Extensions.DependencyInjection;

namespace Stars.Client;

public partial class App : Application
{
	private readonly IServiceProvider _services;

	public App(IServiceProvider services)
	{
		InitializeComponent();
		_services = services;

		// Apply the saved theme before the UI is built so it never recreates the Activity.
		var dark = Preferences.Default.Get("stars-theme-dark", false);
		UserAppTheme = dark ? AppTheme.Dark : AppTheme.Light;

		// Catch anything that escapes a background task so the app stays alive.
		TaskScheduler.UnobservedTaskException += (_, e) => e.SetObserved();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(_services.GetRequiredService<AppShell>());
	}
}
