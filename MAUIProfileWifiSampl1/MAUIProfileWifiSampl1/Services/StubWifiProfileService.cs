namespace MAUIProfileWifiSampl1.Services;

/// <summary>
/// No-op implementation used on platforms without EMDK (Windows, iOS, MacCatalyst).
/// Lets the shared UI build and run for layout/design work off-device.
/// </summary>
public sealed class StubWifiProfileService : IWifiProfileService
{
    public bool IsSupported => false;

    public Task<string> InitializeAsync() =>
        Task.FromResult("EMDK is only available on Zebra Android devices. Running in stub mode.");

    public Task<string> ApplyWifiProfileAsync(bool enableWifi, NetworkAction action, string ssid) =>
        Task.FromResult("Not supported on this platform. Deploy to a Zebra Android device to apply Wi-Fi profiles.");
}
