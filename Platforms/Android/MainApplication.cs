using System;
using Android.App;
using Android.Runtime;

namespace Stars.Client;

[Application]
public class MainApplication : MauiApplication
{
	public MainApplication(IntPtr handle, JniHandleOwnership ownership)
		: base(handle, ownership)
	{
		// Last-resort safety nets: never let an unhandled background exception kill the app.
		AppDomain.CurrentDomain.UnhandledException += (_, e) =>
			Android.Util.Log.Error("Stars", $"Unhandled: {e.ExceptionObject}");

		TaskScheduler.UnobservedTaskException += (_, e) =>
		{
			Android.Util.Log.Error("Stars", $"Unobserved task: {e.Exception}");
			e.SetObserved();
		};

		AndroidEnvironment.UnhandledExceptionRaiser += (_, e) =>
		{
			Android.Util.Log.Error("Stars", $"Android unhandled: {e.Exception}");
			e.Handled = true;
		};
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
