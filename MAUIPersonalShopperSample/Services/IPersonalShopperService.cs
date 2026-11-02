namespace MAUIPersonalShopperSample.Services;

/// <summary>
/// Cross-platform contract for the Zebra EMDK Personal Shopper (cradle) feature.
/// The Android EMDK implementation lives in Platforms/Android.
/// </summary>
public interface IPersonalShopperService
{
    /// <summary>Raised with a human-readable status / result message.</summary>
    event EventHandler<string>? StatusChanged;

    /// <summary>Creates the EMDKManager and obtains the PersonalShopperMgr.</summary>
    void Initialize();

    /// <summary>Enables or disables fast charging on the cradle.</summary>
    void SetFastCharging(bool enabled);

    /// <summary>Reads and reports the cradle information.</summary>
    void GetCradleInfo();

    /// <summary>Reads and reports the cradle diagnostic (battery) data.</summary>
    void GetDiagnosticData();

    /// <summary>Flashes the cradle LEDs (smooth = LED smooth effect).</summary>
    void FlashLeds(bool smooth);

    /// <summary>Unlocks the cradle (smooth = LED smooth effect).</summary>
    void Unlock(bool smooth);

    /// <summary>Releases EMDK resources.</summary>
    void Release();
}
