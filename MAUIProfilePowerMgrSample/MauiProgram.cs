using Microsoft.Extensions.Logging;
using MAUIProfilePowerMgrSample1.Services;

namespace MAUIProfilePowerMgrSample1;

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

		// EMDK PowerMgr service — Android-only implementation.
#if ANDROID
		builder.Services.AddSingleton<IPowerMgrService, Platforms.Android.PowerMgrService>();
#endif

		// Pages
		builder.Services.AddTransient<MainPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
