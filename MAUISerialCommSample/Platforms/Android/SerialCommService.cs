/*
 * .NET MAUI port of the Zebra EMDK SerialCommSample1 (Xamarin).
 * The EMDK SerialCommMgr logic is Android-only and lives here.
 */
using System.Text;
using Symbol.XamarinEMDK;
using Symbol.XamarinEMDK.SerialComm;
using MAUISerialCommSample.Services;
using Application = Android.App.Application;

namespace MAUISerialCommSample.Platforms.Android;

/// <summary>
/// Android EMDK implementation of <see cref="ISerialCommService"/>. Mirrors the original
/// sample: enables a serial comm channel and reads/writes byte data on it.
/// </summary>
public class SerialCommService : Java.Lang.Object, ISerialCommService, EMDKManager.IEMDKListener
{
    private EMDKManager? _emdkManager;
    private SerialCommMgr? _serialComm;

    public event EventHandler<string>? StatusChanged;
    public event EventHandler<bool>? ReadyStateChanged;

    private void SetStatus(string m) => StatusChanged?.Invoke(this, m);
    private void SetReady(bool r) => ReadyStateChanged?.Invoke(this, r);

    public void Initialize()
    {
        EMDKResults results = EMDKManager.GetEMDKManager(Application.Context, this);
        SetStatus(results.StatusCode == EMDKResults.STATUS_CODE.Success ? "Opening EMDK..." : "Failed to open EMDK");
        SetReady(false);
    }

    public void OnOpened(EMDKManager emdkManager)
    {
        _emdkManager = emdkManager;
        try
        {
            _serialComm = (SerialCommMgr?)_emdkManager.GetInstance(EMDKManager.FEATURE_TYPE.Serialcomm);
            if (_serialComm != null)
            {
                try
                {
                    _serialComm.Enable();
                    SetStatus("Serial comm channel enabled");
                    SetReady(true);
                }
                catch (SerialCommException e)
                {
                    SetStatus(e.Message ?? "Enable failed");
                    SetReady(false);
                }
            }
            else
            {
                SetStatus("SerialComm feature not supported or initialization error.");
                SetReady(false);
            }
        }
        catch (Java.Lang.Exception e)
        {
            SetStatus(e.Message ?? "Error");
        }
    }

    public void OnClosed()
    {
        _emdkManager?.Release();
        SetStatus("EMDK closed abruptly.");
        SetReady(false);
    }

    public void Write(string data)
    {
        if (_serialComm == null) { SetStatus("Serial channel not ready"); return; }
        try
        {
            byte[] bytes = Encoding.ASCII.GetBytes(data ?? string.Empty);
            int written = _serialComm.Write(bytes, bytes.Length);
            SetStatus("Bytes written: " + written);
        }
        catch (SerialCommException ex) { SetStatus("write: " + ex.Result.Description); }
        catch (Java.Lang.Exception exp) { SetStatus("write: " + exp.Message); }
    }

    public void Read()
    {
        if (_serialComm == null) { SetStatus("Serial channel not ready"); return; }
        try
        {
            byte[] readBuffer = _serialComm.Read(10000); // 10s timeout
            if (readBuffer != null)
            {
                SetStatus("Data Read:\n" + Encoding.UTF8.GetString(readBuffer));
            }
            else
            {
                SetStatus("No Data Available");
            }
        }
        catch (SerialCommException ex) { SetStatus("read: " + ex.Result.Description); }
        catch (Java.Lang.Exception exp) { SetStatus("read: " + exp.Message); }
    }

    public void Release()
    {
        _serialComm = null;
        if (_emdkManager != null) { _emdkManager.Release(); _emdkManager = null; }
    }
}
