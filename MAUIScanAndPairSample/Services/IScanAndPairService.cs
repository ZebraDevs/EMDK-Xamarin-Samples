namespace MAUIScanAndPairSample1.Services;

/// <summary>Type of data encoded in the scanned pairing barcode.</summary>
public enum ScanDataTypeOption
{
    MacAddress,
    DeviceName,
    Unspecified
}

/// <summary>User-supplied options for a Pair/Unpair operation, gathered from the UI.</summary>
public class ScanPairRequest
{
    public bool AlwaysScan { get; set; } = true;
    public bool HardTrigger { get; set; } = true;
    public ScanDataTypeOption ScanDataType { get; set; } = ScanDataTypeOption.MacAddress;
    public string DeviceName { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
}

/// <summary>A Bluetooth device discovered during scanning (name + MAC address).</summary>
public record DiscoveredDevice(string DeviceName, string MacAddress);

/// <summary>
/// Cross-platform contract for the Zebra EMDK Scan-and-Pair feature. The shared UI talks
/// to this interface; the Android EMDK implementation lives in Platforms/Android.
/// </summary>
public interface IScanAndPairService
{
    /// <summary>Raised with a human-readable status message.</summary>
    event EventHandler<string>? StatusChanged;

    /// <summary>Raised when a device is discovered (used to fill in name/address).</summary>
    event EventHandler<DiscoveredDevice>? DeviceDiscovered;

    /// <summary>Creates the EMDKManager.</summary>
    void Initialize();

    /// <summary>Scans a barcode and pairs the encoded Bluetooth device.</summary>
    void Pair(ScanPairRequest request);

    /// <summary>Scans a barcode and unpairs the encoded Bluetooth device.</summary>
    void Unpair(ScanPairRequest request);

    /// <summary>Releases EMDK resources.</summary>
    void Release();
}
