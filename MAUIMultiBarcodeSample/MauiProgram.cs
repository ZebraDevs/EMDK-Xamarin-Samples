using Microsoft.Extensions.Logging;
using MAUIMultiBarcodeSample1.Services;

namespace MAUIMultiBarcodeSample1;

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

		// EMDK barcode scanner service — Android-only implementation.
#if ANDROID
		builder.Services.AddSingleton<IBarcodeScannerService, Platforms.Android.BarcodeScannerService>();
#endif

		// Pages
		builder.Services.AddTransient<MainPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
