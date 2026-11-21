using Avalonia;
using OpenCivOne.UI;

namespace OpenCivOne
{
    public static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// Initialization code. Don't use any Avalonia, third-party APIs or any
		/// SynchronizationContext-reliant code before AppMain is called: things aren't initialized
		/// yet and stuff might break.
		/// </summary>
		public static int Main(string[] args)
		{
			// A game process that did not shut down cleanly keeps the executable locked.
			// Offer to force kill it before Avalonia and the game are initialized.
			if (!RunningInstanceGuard.ConfirmAndStopOtherInstances())
			{
				return 1;
			}

			return BuildAvaloniaApp()
				.StartWithClassicDesktopLifetime(args);
		}

		/// <summary>Avalonia configuration, don't remove; also used by visual designer.</summary>
		public static AppBuilder BuildAvaloniaApp() =>
			AppBuilder.Configure<App>()
				.UsePlatformDetect()
				.WithInterFont()
				.LogToTrace();
	}
}
