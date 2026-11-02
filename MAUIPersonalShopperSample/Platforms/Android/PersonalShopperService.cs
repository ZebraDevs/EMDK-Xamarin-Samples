/*
 * .NET MAUI port of the Zebra EMDK PersonalShopperSample1 (Xamarin).
 * The EMDK PersonalShopperMgr (cradle) logic is Android-only and lives here.
 */
using Symbol.XamarinEMDK;
using Symbol.XamarinEMDK.PersonalShopper;
using MAUIPersonalShopperSample.Services;
using Application = Android.App.Application;

namespace MAUIPersonalShopperSample.Platforms.Android;

/// <summary>
/// Android EMDK implementation of <see cref="IPersonalShopperService"/>. Mirrors the
/// original sample: obtains the PersonalShopperMgr, enables the cradle and exposes cradle
/// info, diagnostics, fast-charge, LED flash and unlock.
/// </summary>
public class PersonalShopperService : Java.Lang.Object, IPersonalShopperService, EMDKManager.IEMDKListener
{
    private EMDKManager? _emdkManager;
    private PersonalShopperMgr? _ps;

    public event EventHandler<string>? StatusChanged;
    private void SetStatus(string m) => StatusChanged?.Invoke(this, m);

    public void Initialize()
    {
        EMDKResults results = EMDKManager.GetEMDKManager(Application.Context, this);
        SetStatus(results.StatusCode == EMDKResults.STATUS_CODE.Success
            ? "Status: getEMDKManager Success"
            : "Status: Failed in getEMDKManager::" + results.StatusCode);
    }

    public void OnOpened(EMDKManager emdkManager)
    {
        _emdkManager = emdkManager;
        try
        {
            _ps = (PersonalShopperMgr?)_emdkManager.GetInstance(EMDKManager.FEATURE_TYPE.Personalshopper);
        }
        catch (System.Exception e)
        {
            SetStatus("Status: " + e.Message);
            return;
        }

        if (_ps == null)
        {
            SetStatus("PersonalShopper feature is NOT supported");
            return;
        }
        Enable();
        SetStatus("Status: EMDK open success. Cradle ready.");
    }

    public void OnClosed()
    {
        _emdkManager?.Release();
        SetStatus("Status: EMDK closed unexpectedly!");
    }

    private void Enable()
    {
        try { _ps?.Cradle?.Enable(); }
        catch (CradleException e) { SetStatus("Status: " + e.Message); }
    }

    private void Disable()
    {
        try { _ps?.Cradle?.Disable(); }
        catch (CradleException e) { SetStatus("Status: " + e.Message); }
    }

    public void SetFastCharging(bool enabled)
    {
        var cradle = _ps?.Cradle;
        if (cradle == null) { SetStatus("Status: Cradle not available"); return; }
        try
        {
            cradle.Config.SetFastChargingState(enabled);
            bool state = cradle.Config.FastChargingState;
            SetStatus(enabled
                ? (state ? "Status: fast charge enabled" : "Status: fast charge enabling failed")
                : (!state ? "Status: fast charge disabled" : "Status: fast charge disabling failed"));
        }
        catch (CradleException ex) { SetStatus("Status: " + ex.Message); }
    }

    public void GetCradleInfo()
    {
        var cradle = _ps?.Cradle;
        if (cradle == null) { SetStatus("Status: Cradle not available"); return; }
        try
        {
            CradleInfo info = cradle.CradleInfo;
            if (info != null)
            {
                SetStatus("FirmwareVersion: " + info.FirmwareVersion +
                    "\nDateOfManufacturing: " + info.DateOfManufacture +
                    "\nHardwareID: " + info.HardwareID +
                    "\nPartnumber: " + info.PartNumber +
                    "\nSerialNumber: " + info.SerialNumber);
            }
        }
        catch (CradleException e) { SetStatus("Status: " + e.Message); }
    }

    public void GetDiagnosticData()
    {
        if (_ps?.Diagnostic == null) { SetStatus("Status: Diagnostic not available"); return; }
        try
        {
            var config = new DiagnosticConfig(200, 60);
            DiagnosticData d = _ps.Diagnostic.GetDiagnosticData(DiagnosticParamId.All, config);
            if (d != null)
            {
                SetStatus("Battery Capacity (%): " + d.BatteryStateOfCharge +
                    "\nBattery Capacity (mins): " + d.BatteryTimeToEmpty +
                    "\nBattery SOH (%): " + d.BatteryStateOfHealth +
                    "\nCharging Time Required (mins): " + d.BatteryChargingTime +
                    "\nBattery Replacement (days): " + d.TimeSinceBatteryReplaced +
                    "\nTime since Last reboot (mins): " + d.TimeSinceReboot +
                    "\nCharging Time Elapsed (mins): " + d.BatteryChargingTimeElapsed +
                    "\nManufacturing Date: " + d.BatteryDateOfManufacture);
            }
        }
        catch (DiagnosticException e) { SetStatus("Status: " + e.Message); }
    }

    public void FlashLeds(bool smooth)
    {
        var cradle = _ps?.Cradle;
        if (cradle == null) { SetStatus("Status: Cradle not available"); return; }
        try
        {
            var ledFlashInfo = new CradleLedFlashInfo(2000, 1000, smooth);
            CradleResults result = cradle.FlashLed(5, ledFlashInfo);
            SetStatus(result == CradleResults.Success
                ? "Status: Flashed LEDs"
                : "Status: Failed error " + result.Description);
        }
        catch (CradleException e) { SetStatus("Status: " + e.Message); }
    }

    public void Unlock(bool smooth)
    {
        var cradle = _ps?.Cradle;
        if (cradle == null) { SetStatus("Status: Cradle not available"); return; }
        try
        {
            var ledFlashInfo = new CradleLedFlashInfo(500, 500, smooth);
            CradleResults result = cradle.Unlock(15, ledFlashInfo);
            SetStatus(result == CradleResults.Success
                ? "Status: Unlocked"
                : "Status: Failed error " + result.Description);
        }
        catch (CradleException e) { SetStatus("Status: " + e.Message); }
    }

    public void Release()
    {
        Disable();
        _ps = null;
        if (_emdkManager != null) { _emdkManager.Release(); _emdkManager = null; }
    }
}
