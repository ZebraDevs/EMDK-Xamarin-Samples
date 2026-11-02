using System.Text;
using System.Xml;
using Android.Content;
using MAUIProfileWifiSampl1.Services;
using Symbol.XamarinEMDK;
using Application = Android.App.Application;

namespace MAUIProfileWifiSampl1.Platforms.Android.Services;

/// <summary>
/// Android EMDK implementation of <see cref="IWifiProfileService"/>.
///
/// This is the MAUI port of the original Xamarin <c>MainActivity</c> from
/// ProfileWifiSample1. The EMDK APIs are callback based (IEMDKListener for
/// manager open/close, and ProfileManager.Data for async profile results), so
/// here they are wrapped with TaskCompletionSource to expose a clean async API
/// that the shared MAUI UI can await.
/// </summary>
public sealed class WifiProfileService : Java.Lang.Object, IWifiProfileService, EMDKManager.IEMDKListener
{
    // Must match the 'ProfileName' defined in Platforms/Android/Assets/EMDKConfig.xml
    private const string ProfileName = "WifiProfile-1";

    private EMDKManager? _emdkManager;
    private ProfileManager? _profileManager;

    // Completes when OnOpened / OnClosed fires after GetEMDKManager.
    private TaskCompletionSource<string>? _initTcs;

    // Completes when ProfileManager.Data fires after ProcessProfileAsync.
    private TaskCompletionSource<string>? _applyTcs;

    public bool IsSupported => true;

    public Task<string> InitializeAsync()
    {
        if (_profileManager is not null)
            return Task.FromResult("EMDK already initialized.");

        _initTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Requesting the EMDKManager is asynchronous; the result arrives in OnOpened/OnClosed.
        EMDKResults results = EMDKManager.GetEMDKManager(Application.Context, this);

        if (results.StatusCode != EMDKResults.STATUS_CODE.Success)
        {
            // Include the status codes so failures can be diagnosed from the screen / logcat.
            string reason = $"status={results.StatusCode}, ext={results.ExtendedStatusCode}";
            global::Android.Util.Log.Error(nameof(WifiProfileService), "GetEMDKManager failed: " + reason);
            _initTcs.TrySetResult($"EMDKManager object creation failed ({reason})");
        }

        return _initTcs.Task;
    }

    public void OnOpened(EMDKManager emdkManagerInstance)
    {
        _emdkManager = emdkManagerInstance;

        try
        {
            // Get the ProfileManager used to process profiles.
            _profileManager = (ProfileManager)_emdkManager.GetInstance(EMDKManager.FEATURE_TYPE.Profile)!;

            // Subscribe once to receive async profile-processing results.
            _profileManager.Data += OnProfileData;

            _initTcs?.TrySetResult("EMDK open success.");
        }
        catch (Exception e)
        {
            _initTcs?.TrySetResult("Error loading profile manager: " + e.Message);
        }
    }

    public void OnClosed()
    {
        // EMDK closed unexpectedly (e.g. EMDK service updated). Release and reset.
        Release();
        _initTcs?.TrySetResult("EMDK closed unexpectedly! Please restart the application.");
        _applyTcs?.TrySetResult("EMDK closed unexpectedly! Please restart the application.");
    }

    public Task<string> ApplyWifiProfileAsync(bool enableWifi, NetworkAction action, string ssid)
    {
        if (_profileManager is null)
            return Task.FromResult("Profile manager not ready. Initialize EMDK first.");

        string modifyData = BuildProfileXml(enableWifi, action, ssid);

        _applyTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        // ProcessProfileAsync returns immediately with 'Processing'; the actual
        // outcome is delivered later via the ProfileManager.Data event.
        EMDKResults results = _profileManager.ProcessProfileAsync(
            ProfileName,
            ProfileManager.PROFILE_FLAG.Set,
            new[] { modifyData });

        if (results.StatusCode != EMDKResults.STATUS_CODE.Processing)
            _applyTcs.TrySetResult("Set profile failed.");

        return _applyTcs.Task;
    }

    private void OnProfileData(object? sender, ProfileManager.DataEventArgs e)
    {
        EMDKResults results = e.P0!.Result;
        _applyTcs?.TrySetResult(CheckXmlError(results));
    }

    /// <summary>
    /// Builds the Wi-Fi provisioning XML. Faithful port of SetProfile() from the
    /// original sample: enable/disable the Wi-Fi radio, then optionally add /
    /// remove / connect / disconnect a network profile by SSID.
    /// </summary>
    private static string BuildProfileXml(bool enableWifi, NetworkAction action, string ssid)
    {
        string wifiAction = enableWifi ? "enable" : "disable";

        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        sb.Append("<characteristic type=\"Profile\">");
        sb.Append("<parm name=\"ProfileName\" value=\"").Append(ProfileName).Append("\"/>");
        sb.Append("<characteristic type=\"Wi-Fi\">");
        sb.Append("<parm name=\"emdk_name\" value=\"WifiMgr1\"/>");
        sb.Append("<characteristic type=\"System\">");
        sb.Append("<parm name=\"WiFiAction\" value=\"").Append(wifiAction).Append("\"/>");
        sb.Append("</characteristic>");
        sb.Append("<parm name=\"UseRegulatory\" value=\"0\"/>");
        sb.Append("<parm name=\"UseAdvancedOptions\" value=\"0\"/>");

        switch (action)
        {
            case NetworkAction.Add:
                sb.Append("<parm name=\"NetworkAction\" value=\"Add\"/>");
                sb.Append("<characteristic type=\"network-profile\">");
                sb.Append("<parm name=\"SSID\" value=\"").Append(ssid).Append("\"/>");
                sb.Append("<parm name=\"SecurityMode\" value=\"0\"/>");
                sb.Append("<parm name=\"UseDHCP\" value=\"1\"/>");
                sb.Append("<parm name=\"UseProxy\" value=\"0\"/>");
                sb.Append("</characteristic>");
                break;

            case NetworkAction.Remove:
                sb.Append("<parm name=\"NetworkAction\" value=\"Remove\"/>");
                sb.Append("<characteristic type=\"network-profile\">");
                sb.Append("<parm name=\"SSID\" value=\"").Append(ssid).Append("\"/>");
                sb.Append("</characteristic>");
                break;

            case NetworkAction.Connect:
                sb.Append("<parm name=\"NetworkAction\" value=\"Connect\"/>");
                sb.Append("<characteristic type=\"network-profile\">");
                sb.Append("<parm name=\"SSID\" value=\"").Append(ssid).Append("\"/>");
                sb.Append("</characteristic>");
                break;

            case NetworkAction.Disconnect:
                sb.Append("<parm name=\"NetworkAction\" value=\"Disconnect\"/>");
                sb.Append("<characteristic type=\"network-profile\">");
                sb.Append("<parm name=\"SSID\" value=\"").Append(ssid).Append("\"/>");
                sb.Append("</characteristic>");
                break;

            case NetworkAction.DoNothing:
            default:
                break;
        }

        sb.Append("</characteristic></characteristic>");
        return sb.ToString();
    }

    /// <summary>
    /// Parses the EMDK result. When the status is CheckXml the response body
    /// contains parm-error / characteristic-error nodes describing what failed.
    /// Port of CheckXmlError() from the original sample.
    /// </summary>
    private static string CheckXmlError(EMDKResults results)
    {
        if (results.StatusCode != EMDKResults.STATUS_CODE.CheckXml)
        {
            return results.StatusCode == EMDKResults.STATUS_CODE.Success
                ? "Profile applied successfully ..."
                : "Profile processing returned: " + results.StatusCode;
        }

        bool isFailure = false;
        string status = string.Empty;

        using var stringReader = new StringReader(results.StatusString);
        using var reader = XmlReader.Create(stringReader);

        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element)
                continue;

            switch (reader.Name)
            {
                case "parm-error":
                    isFailure = true;
                    status = $"Name: {reader.GetAttribute("name")}, Error Description: {reader.GetAttribute("desc")}";
                    break;
                case "characteristic-error":
                    isFailure = true;
                    status = $"Type: {reader.GetAttribute("type")}, Error Description: {reader.GetAttribute("desc")}";
                    break;
            }
        }

        return isFailure ? status : "Profile applied successfully ...";
    }

    private void Release()
    {
        if (_profileManager is not null)
        {
            _profileManager.Data -= OnProfileData;
            _profileManager = null;
        }

        if (_emdkManager is not null)
        {
            _emdkManager.Release();
            _emdkManager = null;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            Release();

        base.Dispose(disposing);
    }
}
