using SchedulerApp.Views;

namespace SchedulerApp;

public partial class AppShell : Shell
{
	public const string SettingsRoute = "SettingsPage";

	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute(SettingsRoute, typeof(SettingsPage));
	}
}
