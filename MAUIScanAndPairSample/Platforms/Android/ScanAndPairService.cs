/*
 * .NET MAUI port of the Zebra EMDK ScanAndPairSample1 (Xamarin).
 * The EMDK ScanAndPairManager logic is Android-only and lives here.
 */
using Symbol.XamarinEMDK;
using Symbol.XamarinEMDK.ScanAndPair;
using MAUIScanAndPairSample1.Services;
using EmdkScanDataType = Symbol.XamarinEMDK.ScanAndPair.ScanAndPairConfig.ScanDataType;
using EmdkTriggerType = Symbol.XamarinEMDK.ScanAndPair.ScanAndPairConfig.TriggerType;
using EmdkNotificationTypes = Symbol.XamarinEMDK.ScanAndPair.ScanAndPairConfig.NotificationTypes;

namespace MAUIScanAndPairSample1.Platforms.Android;

/// <summary>
/// Android EMDK implementation of <see cref="IScanAndPairService"/>. Mirrors the original
/// sample: it configures the ScanAndPairManager (always-scan vs. explicit device, trigger
/// type, scan-data type) and runs ScanAndPair / ScanAndUnpair, reporting status back.
/// </summary>
public class ScanAndPairService : Java.Lang.Object, IScanAndPairService, EMDKManager.IEMDKListener
{
    private EMDKManager? _emdkManager;
    private ScanAndPairManager? _scanAndPairMgr;

    public event EventHandler<string>? StatusChanged;
    public event EventHandler<DiscoveredDevice>? DeviceDiscovered;

    private void SetStatus(string message) => StatusChanged?.Invoke(this, message);

    public void Initialize()
    {
        EMDKResults results = EMDKManager.GetEMDKManager(global::Android.App.Application.Context, this);
        SetStatus(results.StatusCode == EMDKResults.STATUS_CODE.Success
            ? "Please wait, initialization in progress..."
            : "Initialization failed!");
    }

    public void OnOpened(EMDKManager emdkManager)
    {
        _emdkManager = emdkManager;
        SetStatus("Application Initialized.");
    }

    public void OnClosed() => SetStatus("Error!! Restart the application!!");

    public void Pair(ScanPairRequest request)
    {
        try
        {
            SetStatus("ScanAndPair started...\n");
            if (!EnsureManager())
            {
                return;
            }

            ApplyConfig(request);

            ScanAndPairResults resultCode = _scanAndPairMgr!.ScanAndPair("0000");
            if (!resultCode.Equals(ScanAndPairResults.Success))
            {
                SetStatus(resultCode.ToString() + "\n\n");
            }
        }
        catch (Java.Lang.Exception ex)
        {
            SetStatus("ScanAndPair Error:" + ex.Message + "\n");
        }
    }

    public void Unpair(ScanPairRequest request)
    {
        try
        {
            SetStatus("ScanAndUnpair started...\n");
            if (!EnsureManager())
            {
                return;
            }

            ApplyConfig(request);

            ScanAndPairResults resultCode = _scanAndPairMgr!.ScanAndUnpair();
            if (!resultCode.Equals(ScanAndPairResults.Success))
            {
                SetStatus(resultCode.ToString() + "\n\n");
            }
        }
        catch (Java.Lang.Exception ex)
        {
            SetStatus("ScanAndUnpair Error:" + ex.Message + "\n");
        }
    }

    /// <summary>Lazily obtains the ScanAndPairManager and wires the status listener.</summary>
    private bool EnsureManager()
    {
        if (_scanAndPairMgr != null)
        {
            return true;
        }

        _scanAndPairMgr = (ScanAndPairManager?)_emdkManager?.GetInstance(EMDKManager.FEATURE_TYPE.Scanandpair);
        if (_scanAndPairMgr == null)
        {
            SetStatus("ScanAndPairmanager intialization failed!");
            return false;
        }

        _scanAndPairMgr.StatusEvent += ScanAndPairMgr_StatusEvent;
        return true;
    }

    private void ApplyConfig(ScanPairRequest request)
    {
        if (_scanAndPairMgr == null)
        {
            return;
        }

        _scanAndPairMgr.Config.AlwaysScan = (Java.Lang.Boolean)request.AlwaysScan;
        _scanAndPairMgr.Config.NotificationType = EmdkNotificationTypes.Beeper;

        if (!request.AlwaysScan)
        {
            _scanAndPairMgr.Config.BluetoothInfo.MacAddress = request.MacAddress.Trim();
            _scanAndPairMgr.Config.BluetoothInfo.DeviceName = request.DeviceName.Trim();
        }
        else
        {
            _scanAndPairMgr.Config.ScanInfo.ScanTimeout = 5000;
            _scanAndPairMgr.Config.ScanInfo.TriggerType =
                request.HardTrigger ? EmdkTriggerType.Hard : EmdkTriggerType.Soft;
            _scanAndPairMgr.Config.ScanInfo.ScanDataType = MapScanDataType(request.ScanDataType);
        }
    }

    private static EmdkScanDataType MapScanDataType(ScanDataTypeOption option) => option switch
    {
        ScanDataTypeOption.MacAddress => EmdkScanDataType.MacAddress,
        ScanDataTypeOption.DeviceName => EmdkScanDataType.DeviceName,
        _ => EmdkScanDataType.Unspecified
    };

    private void ScanAndPairMgr_StatusEvent(object? sender, ScanAndPairManager.StatusEventArgs e) =>
        OnStatus(e.P0);

    private void OnStatus(StatusData statusData)
    {
        string text;
        bool updateAddress = false;

        switch (statusData.State.ToString().ToUpper())
        {
            case "WAITING":
                text = "Waiting for trigger press to scan the barcode";
                break;
            case "SCANNING":
                text = "Scanner Beam is on, aim at the barcode.";
                break;
            case "DISCOVERING":
                text = "Discovering for the Bluetooth device";
                updateAddress = true;
                break;
            case "PAIRED":
                text = "Bluetooth device is paired successfully";
                break;
            case "UNPAIRED":
                text = "Bluetooth device is un-paired successfully";
                break;
            default:
                text = "\n" + statusData.GetType().Name + ": " + statusData.Result;
                break;
        }

        SetStatus(text + "\n");

        if (updateAddress && _scanAndPairMgr != null)
        {
            DeviceDiscovered?.Invoke(this, new DiscoveredDevice(
                _scanAndPairMgr.Config.BluetoothInfo.DeviceName ?? "",
                _scanAndPairMgr.Config.BluetoothInfo.MacAddress ?? ""));
        }
    }

    public void Release()
    {
        if (_scanAndPairMgr != null)
        {
            _scanAndPairMgr.StatusEvent -= ScanAndPairMgr_StatusEvent;
            _scanAndPairMgr = null;
        }

        if (_emdkManager != null)
        {
            _emdkManager.Release();
            _emdkManager = null;
        }
    }
}
