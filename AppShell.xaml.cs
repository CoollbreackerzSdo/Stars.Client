namespace Stars.Client;

public partial class AppShell : Shell
{
	public AppShell(MainPage mainPage, ConnectionPage connectionPage,
		QualityPage qualityPage, SettingsPage settingsPage)
	{
		InitializeComponent();
		PlayTab.Items.Add(new ShellContent { Content = mainPage });
		ConnectionTab.Items.Add(new ShellContent { Content = connectionPage });
		QualityTab.Items.Add(new ShellContent { Content = qualityPage });
		SettingsTab.Items.Add(new ShellContent { Content = settingsPage });
	}
}
