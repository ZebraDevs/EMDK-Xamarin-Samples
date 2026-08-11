using MAUIProfileWifiSampl1.Services;
using Microsoft.Extensions.Logging;

namespace MAUIProfileWifiSampl1
{
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

            // Register the Wi-Fi profile service. EMDK is Android only, so the
            // real implementation is registered there and a stub everywhere else.
#if ANDROID
            builder.Services.AddSingleton<IWifiProfileService, Platforms.Android.Services.WifiProfileService>();
#else
            builder.Services.AddSingleton<IWifiProfileService, StubWifiProfileService>();
#endif

            builder.Services.AddTransient<MainPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
