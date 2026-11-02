/*
 * .NET MAUI port of the Zebra EMDK MultiBarcodeSample1 (Xamarin).
 * The EMDK BarcodeManager/Scanner logic is Android-only and lives here.
 */
using System.Collections.Generic;
using Symbol.XamarinEMDK;
using Symbol.XamarinEMDK.Barcode;
using MAUIMultiBarcodeSample1.Services;
using Application = Android.App.Application;

namespace MAUIMultiBarcodeSample1.Platforms.Android;

/// <summary>
/// Android EMDK implementation of <see cref="IBarcodeScannerService"/>. Mirrors the
/// original sample: enumerates scanners, configures Multi-Barcode scan mode with a barcode
/// count, applies a trigger type, and reports decoded barcodes back to the shared UI.
/// </summary>
public class BarcodeScannerService : Java.Lang.Object, IBarcodeScannerService, EMDKManager.IEMDKListener
{
    private EMDKManager? _emdkManager;
    private BarcodeManager? _barcodeManager;
    private Scanner? _scanner;

    private IList<ScannerInfo>? _deviceList;
    private int _scannerIndex;
    private int _defaultIndex;
    private int _triggerIndex = 1; // default: Soft
    private int _barcodeCount = 5;
    private readonly List<ScannedBarcode> _pendingResults = new();
    private bool _isScanInProgress;

    public event EventHandler<string>? StatusChanged;
    public event EventHandler<ScannerListEventArgs>? ScannersEnumerated;
    public event EventHandler<IReadOnlyList<ScannedBarcode>>? BarcodesScanned;

    private void SetStatus(string message) => StatusChanged?.Invoke(this, message);

    public void Initialize()
    {
        EMDKResults results = EMDKManager.GetEMDKManager(Application.Context, this);
        SetStatus(results.StatusCode == EMDKResults.STATUS_CODE.Success
            ? "Status: EMDKManager object creation succeeded."
            : "Status: EMDKManager object creation failed.");
    }

    public void OnOpened(EMDKManager emdkManagerInstance)
    {
        SetStatus("Status: EMDK open success.");
        _emdkManager = emdkManagerInstance;

        try
        {
            _barcodeManager = (BarcodeManager)_emdkManager.GetInstance(EMDKManager.FEATURE_TYPE.Barcode);

            if (_barcodeManager != null)
            {
                _barcodeManager.Connection += BarcodeManager_Connection;
            }

            EnumerateScanners();

            // Initialize the default scanner.
            SelectScanner(_defaultIndex);
        }
        catch (System.Exception e)
        {
            SetStatus("Status: BarcodeManager object creation failed.");
            Console.WriteLine("Exception:" + e.StackTrace);
        }
    }

    public void OnClosed()
    {
        Release();
        SetStatus("Status: EMDK closed unexpectedly! Please close and restart the application.");
    }

    private void EnumerateScanners()
    {
        if (_barcodeManager == null)
        {
            return;
        }

        var names = new List<string>();
        _deviceList = _barcodeManager.SupportedDevicesInfo;

        if (_deviceList != null && _deviceList.Count > 0)
        {
            int index = 0;
            foreach (ScannerInfo info in _deviceList)
            {
                names.Add(info.FriendlyName);
                if (info.IsDefaultScanner)
                {
                    _defaultIndex = index;
                }
                ++index;
            }
        }
        else
        {
            SetStatus("Status: Failed to get the list of supported scanner devices! Please close and restart the application.");
        }

        ScannersEnumerated?.Invoke(this, new ScannerListEventArgs(names, _defaultIndex));
    }

    public void SelectScanner(int index)
    {
        if (index == _scannerIndex && _scanner != null)
        {
            return;
        }

        _scannerIndex = index;
        DeInitScanner();
        InitScanner();
        SetTrigger();
        SetConfig();
    }

    public void SetTriggerType(int triggerIndex)
    {
        _triggerIndex = triggerIndex;
        SetTrigger();
    }

    public void SetBarcodeCount(int count)
    {
        _barcodeCount = count;
        SetConfig();
    }

    public void StartScan()
    {
        if (_scanner == null)
        {
            InitScanner();
        }

        if (_scanner == null)
        {
            return;
        }

        try
        {
            if (_scanner.IsEnabled)
            {
                _pendingResults.Clear();
                _isScanInProgress = true;
                _scanner.Read();
            }
            else
            {
                SetStatus("Status: Scanner is not enabled");
            }
        }
        catch (ScannerException ex)
        {
            _isScanInProgress = false;
            SetStatus("Status: " + ex.Message);
            Console.WriteLine(ex.StackTrace);
        }
    }

    private void BarcodeManager_Connection(object? sender, BarcodeManager.ScannerConnectionEventArgs e)
    {
        ScannerInfo scannerInfo = e.P0;
        BarcodeManager.ConnectionState connectionState = e.P1;

        string statusBT = connectionState.ToString();
        string scannerNameBT = scannerInfo.FriendlyName;

        string scannerName = "";
        if (_deviceList != null && _deviceList.Count != 0)
        {
            scannerName = _deviceList[_scannerIndex].FriendlyName;
        }

        if (scannerName.ToLower().Equals(scannerNameBT.ToLower()))
        {
            SetStatus("Status: " + scannerNameBT + ":" + statusBT);

            if (connectionState == BarcodeManager.ConnectionState.Connected)
            {
                DeInitScanner();
                InitScanner();
                SetTrigger();
                SetConfig();
            }
            else if (connectionState == BarcodeManager.ConnectionState.Disconnected)
            {
                DeInitScanner();
            }

            SetStatus("Status: " + scannerNameBT + ":" + statusBT);
        }
        else
        {
            SetStatus("Status: " + scannerNameBT + ":" + statusBT);
        }
    }

    private void SetConfig()
    {
        if (_scanner == null)
        {
            InitScanner();
        }

        if (_scanner is { IsEnabled: true })
        {
            try
            {
                ScannerConfig config = _scanner.GetConfig();

                // Scan Mode set to Multi Barcode.
                config.ReaderParams.ReaderSpecific.ImagerSpecific.ScanMode = ScannerConfig.ScanMode.MultiBarcode;

                // Set the barcode count.
                config.MultiBarcodeParams.BarcodeCount = _barcodeCount;

                _scanner.SetConfig(config);

                SetStatus("Status: Configuration changed!");
            }
            catch (ScannerException e)
            {
                SetStatus("Status: " + e.Message);
            }
        }
    }

    private void SetTrigger()
    {
        if (_scanner == null)
        {
            InitScanner();
        }

        if (_scanner == null)
        {
            return;
        }

        _scanner.TriggerType = _triggerIndex switch
        {
            0 => Scanner.TriggerTypes.Hard,
            1 => Scanner.TriggerTypes.SoftAlways,
            _ => _scanner.TriggerType
        };
    }

    private void InitScanner()
    {
        if (_scanner != null)
        {
            return;
        }

        if (_deviceList == null || _deviceList.Count == 0)
        {
            SetStatus("Status: Failed to get the specified scanner device! Please close and restart the application.");
            return;
        }

        _scanner = _barcodeManager?.GetDevice(_deviceList[_scannerIndex]);

        if (_scanner == null)
        {
            SetStatus("Status: Failed to initialize the scanner device.");
            return;
        }

        _scanner.Data += Scanner_Data;
        _scanner.Status += Scanner_Status;

        try
        {
            _scanner.Enable();
            CheckMultiBarcodeSupport();
        }
        catch (ScannerException e)
        {
            SetStatus("Status: " + e.Message);
            Console.WriteLine(e.StackTrace);
        }
    }

    private void CheckMultiBarcodeSupport()
    {
        if (_scanner == null)
        {
            return;
        }

        try
        {
            ScannerConfig config = _scanner.GetConfig();
            if (!config.IsParamSupported("config.multiBarcodeParams.barcodeCount"))
            {
                SetStatus("NOTE: Multibarcode feature is unsupported in the selected scanner");
            }
        }
        catch (System.Exception e)
        {
            SetStatus("Status: " + e.Message);
        }
    }

    private void Scanner_Status(object? sender, Scanner.StatusEventArgs e)
    {
        StatusData.ScannerStates state = e.P0.State;
        SetStatus("Status: " + state);
    }

    private void Scanner_Data(object? sender, Scanner.DataEventArgs e)
    {
        if (!_isScanInProgress)
        {
            return;
        }

        ScanDataCollection scanDataCollection = e.P0;

        if (scanDataCollection == null || scanDataCollection.Result != ScannerResults.Success)
        {
            return;
        }

        IList<ScanDataCollection.ScanData>? scanData = scanDataCollection.GetScanData();
        if (scanData == null)
        {
            return;
        }

        foreach (ScanDataCollection.ScanData data in scanData)
        {
            if (_pendingResults.Count >= _barcodeCount)
            {
                break;
            }

            _pendingResults.Add(new ScannedBarcode(data.LabelType.ToString() ?? "", data.Data ?? ""));
        }

        BarcodesScanned?.Invoke(this, new List<ScannedBarcode>(_pendingResults));

        if (_pendingResults.Count >= _barcodeCount)
        {
            _isScanInProgress = false;
            return;
        }

        if (_scanner is { IsEnabled: true })
        {
            try
            {
                _scanner.Read();
            }
            catch (ScannerException ex)
            {
                _isScanInProgress = false;
                SetStatus("Status: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }
    }

    private void DeInitScanner()
    {
        _isScanInProgress = false;
        _pendingResults.Clear();

        if (_scanner == null)
        {
            return;
        }

        try
        {
            _scanner.CancelRead();
            _scanner.Disable();
        }
        catch (ScannerException e)
        {
            SetStatus("Status: " + e.Message);
            Console.WriteLine(e.StackTrace);
        }

        _scanner.Data -= Scanner_Data;
        _scanner.Status -= Scanner_Status;

        try
        {
            _scanner.Release();
        }
        catch (ScannerException e)
        {
            SetStatus("Status: " + e.Message);
            Console.WriteLine(e.StackTrace);
        }

        _scanner = null;
    }

    public void Release()
    {
        DeInitScanner();

        if (_barcodeManager != null)
        {
            _barcodeManager.Connection -= BarcodeManager_Connection;
            _barcodeManager = null;
        }

        if (_emdkManager != null)
        {
            _emdkManager.Release();
            _emdkManager = null;
        }
    }
}
