using Microsoft.Extensions.Logging;
using MAUIScanAndPairSample1.Services;

namespace MAUIScanAndPairSample1;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// EMDK Scan-and-Pair service — Android-only implementation.
#if ANDROID
		builder.Services.AddSingleton<IScanAndPairService, Platforms.Android.ScanAndPairService>();
#endif

		// Pages
		builder.Services.AddTransient<MainPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
