namespace MAUIMultiBarcodeSample1.Services;

/// <summary>Event payload carrying the enumerated scanner list and the default index.</summary>
public class ScannerListEventArgs : EventArgs
{
    public ScannerListEventArgs(IReadOnlyList<string> names, int defaultIndex)
    {
        Names = names;
        DefaultIndex = defaultIndex;
    }

    public IReadOnlyList<string> Names { get; }
    public int DefaultIndex { get; }
}

/// <summary>
/// Cross-platform contract for the Zebra EMDK multi-barcode scanner. The shared UI talks
/// to this interface; the Android EMDK implementation lives in Platforms/Android.
/// </summary>
public interface IBarcodeScannerService
{
    /// <summary>Raised with a human-readable status message.</summary>
    event EventHandler<string>? StatusChanged;

    /// <summary>Raised once the supported scanners have been enumerated.</summary>
    event EventHandler<ScannerListEventArgs>? ScannersEnumerated;

    /// <summary>Raised when a (multi-)barcode read completes with results.</summary>
    event EventHandler<IReadOnlyList<ScannedBarcode>>? BarcodesScanned;

    /// <summary>Creates the EMDKManager and BarcodeManager.</summary>
    void Initialize();

    /// <summary>Selects the scanner at the given index in the enumerated list.</summary>
    void SelectScanner(int index);

    /// <summary>Sets the trigger type: 0 = Hard, 1 = Soft.</summary>
    void SetTriggerType(int triggerIndex);

    /// <summary>Sets the multi-barcode count (2..10).</summary>
    void SetBarcodeCount(int count);

    /// <summary>Submits a read on the active scanner.</summary>
    void StartScan();

    /// <summary>Releases scanner and EMDK resources.</summary>
    void Release();
}
