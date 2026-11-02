/*
 * .NET MAUI port of the Zebra EMDK ProfilePowerMgrSample1 (Xamarin).
 * The EMDK ProfileManager logic is Android-only and lives here.
 */
using System.Xml;
using Android.Runtime;
using Symbol.XamarinEMDK;
using MAUIProfilePowerMgrSample1.Services;
using Application = Android.App.Application;

namespace MAUIProfilePowerMgrSample1.Platforms.Android;

/// <summary>
/// Android EMDK implementation of <see cref="IPowerMgrService"/>. Mirrors the original
/// sample's MainActivity: it opens an EMDKManager, gets the ProfileManager and applies
/// the "PowerMgrProfile-1" profile's PowerMgr feature (ResetAction).
/// </summary>
public class PowerMgrService : Java.Lang.Object, IPowerMgrService, EMDKManager.IEMDKListener
{
    // 'ProfileName' used in EMDKConfig.xml.
    private const string ProfileName = "PowerMgrProfile-1";
    // 'emdk_name' used in EMDKConfig.xml for the 'PowerMgr' feature (name-value pairs).
    private const string FeatureName = "PowerMgr1";

    private EMDKManager? _emdkManager;
    private ProfileManager? _profileManager;

    public event EventHandler<string>? StatusChanged;

    private void SetStatus(string message) => StatusChanged?.Invoke(this, message);

    public void Initialize()
    {
        // The EMDKManager object will be created and returned in the OnOpened callback.
        EMDKResults results = EMDKManager.GetEMDKManager(Application.Context, this);

        SetStatus(results.StatusCode == EMDKResults.STATUS_CODE.Success
            ? "EMDKManager object creation succeeded ..."
            : "EMDKManager object creation failed ...");
    }

    public void OnOpened(EMDKManager emdkManagerInstance)
    {
        SetStatus("EMDK open success.");
        _emdkManager = emdkManagerInstance;

        try
        {
            // Get the ProfileManager object to process the profiles.
            _profileManager = (ProfileManager)_emdkManager.GetInstance(EMDKManager.FEATURE_TYPE.Profile);

            // Add listener to get async results.
            _profileManager.Data += ProfileManager_Data;
        }
        catch (Exception e)
        {
            SetStatus("Error loading profile manager.");
            Console.WriteLine("Exception: " + e.StackTrace);
        }
    }

    public void OnClosed()
    {
        Release();
        SetStatus("EMDK closed unexpectedly! Please close and restart the application.");
    }

    private void ProfileManager_Data(object? sender, ProfileManager.DataEventArgs e)
    {
        // Call back with the result of the ProcessProfileAsync call.
        EMDKResults results = e.P0.Result;
        SetStatus(CheckXmlError(results));
    }

    public void SetProfile(bool suspend)
    {
        if (_profileManager == null)
        {
            SetStatus("Profile manager not ready. Please wait for EMDK to open.");
            return;
        }

        // Prepare name-value pairs to modify the existing profile.
        // ResetAction: "1" = Suspend (sleep), "4" = Reset (reboot).
        string[] modifyData =
        {
            ProfileManager.CreateNameValuePair(FeatureName, "ResetAction", suspend ? "1" : "4")
        };

        // Update the profile using the 'Set' flag and the modify data.
        EMDKResults results = _profileManager.ProcessProfileAsync(ProfileName, ProfileManager.PROFILE_FLAG.Set, modifyData);

        SetStatus(results.StatusCode == EMDKResults.STATUS_CODE.Processing
            ? "Set profile in-progress..."
            : "Set profile failed.");
    }

    public void Release()
    {
        _profileManager = null;

        if (_emdkManager != null)
        {
            _emdkManager.Release();
            _emdkManager = null;
        }
    }

    /// <summary>Parses the EMDK CheckXml result and returns a human-readable status.</summary>
    private static string CheckXmlError(EMDKResults results)
    {
        string checkXmlStatus = "";
        bool isFailure = false;

        if (results.StatusCode != EMDKResults.STATUS_CODE.CheckXml)
        {
            return checkXmlStatus;
        }

        using var stringReader = new StringReader(results.StatusString);
        using XmlReader reader = XmlReader.Create(stringReader);

        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            switch (reader.Name)
            {
                case "parm-error":
                    isFailure = true;
                    checkXmlStatus = "Name: " + reader.GetAttribute("name") +
                                     ", Error Description: " + reader.GetAttribute("desc");
                    break;
                case "characteristic-error":
                    isFailure = true;
                    checkXmlStatus = "Type: " + reader.GetAttribute("type") +
                                     ", Error Description: " + reader.GetAttribute("desc");
                    break;
            }
        }

        if (!isFailure)
        {
            checkXmlStatus = "Profile applied successfully ...";
        }

        return checkXmlStatus;
    }
}
