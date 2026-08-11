namespace MAUIProfileWifiSampl1.Services;

/// <summary>
/// Network action to perform against the Wi-Fi profile. Mirrors the
/// "Network Action" spinner from the original Xamarin ProfileWifiSample1.
/// </summary>
public enum NetworkAction
{
    DoNothing,
    Add,
    Remove,
    Connect,
    Disconnect
}

/// <summary>
/// Cross-platform contract for applying an EMDK Wi-Fi profile.
/// The real implementation lives in Platforms/Android (EMDK is Android only);
/// other platforms get a stub so the UI still builds and runs.
/// </summary>
public interface IWifiProfileService
{
    /// <summary>True when running on a device/platform where EMDK is available.</summary>
    bool IsSupported { get; }

    /// <summary>
    /// Initializes the EMDKManager and ProfileManager.
    /// Returns a human-readable status message describing the result.
    /// </summary>
    Task<string> InitializeAsync();

    /// <summary>
    /// Builds the Wi-Fi profile XML from the supplied options and applies it
    /// asynchronously via the EMDK ProfileManager. Returns a status message
    /// (success, validation error, or failure reason).
    /// </summary>
    Task<string> ApplyWifiProfileAsync(bool enableWifi, NetworkAction action, string ssid);
}
