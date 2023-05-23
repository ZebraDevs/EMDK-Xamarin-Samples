using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.AppCompat.App;
using Symbol.XamarinEMDK;
using System.Xml;

namespace MauiProfileClockSample1;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity, EMDKManager.IEMDKListener
{
    // Declare a variable to store EMDKManager object
    private EMDKManager emdkManager = null;
    // Declare a variable to store ProfileManager object
    private ProfileManager profileManager = null;

    private MainPage _mainPage = null;

    protected override void OnCreate(Bundle bundle)
    {
        base.OnCreate(bundle);
        AppCompatDelegate.DefaultNightMode = AppCompatDelegate.ModeNightNo;
    }
    public void OnClosed()
    {
        if (emdkManager != null)
        {
            emdkManager.Release();
            emdkManager = null;
        }

        //statusTextView.Text = "EMDK closed unexpectedly! Please close and restart the application.";
    }

    public void OnOpened(EMDKManager emdkManagerInstance)
    {
        //statusTextView.Text = "EMDK open success.";
        _mainPage.updateStatusText("EMDK open success.");
        this.emdkManager = emdkManagerInstance;

        try
        {
            // Get the ProfileManager object to process the profiles
            profileManager = (ProfileManager)emdkManager.GetInstance(EMDKManager.FEATURE_TYPE.Profile);

            // Add listener to get async results
            profileManager.Data += profileManager_Data;
        }
        catch (Exception e)
        {
            _mainPage.updateStatusText("Error loading profile manager: " + e);
        }
    }

    void profileManager_Data(object sender, ProfileManager.DataEventArgs e)
    {
        // Call back with the result of the processProfileAsync

        EMDKResults results = e.P0.Result;

        string statusString = CheckXmlError(results);

        RunOnUiThread(() =>
        _mainPage.updateStatusText(statusString)
        );
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        // Clean up the objects created by EMDK manager
        if (profileManager != null)
        {
            profileManager = null;
        }

        if (emdkManager != null)
        {
            emdkManager.Release();
            emdkManager = null;
        }
    }

    public void onClickSetProfile(string timeZone, string date, string time)
    {
        if (readValues(timeZone, date, time))
        {
            string profileName = "ClockProfile-1";
            string[] modifyData = new string[1];
            modifyData[0] =
             "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                     "<characteristic type=\"Profile\">" +
                     "<parm name=\"ProfileName\" value=\"ClockProfile-1\"/>" +
                     "<characteristic type=\"Clock\" version=\"0.2\">" +
                     "<parm name=\"TimeZone\" value=\"" + timeZone + "\"/>" +
                     "<parm name=\"Date\" value=\"" + date + "\"/>" +
                     "<parm name=\"Time\" value=\"" + time + "\"/>" +
                     "</characteristic>" +
                     "</characteristic>";

            // Call processPrfoileAsync with profile name, 'Set' flag and modify data to update the profile
            EMDKResults results = profileManager.ProcessProfileAsync(profileName, ProfileManager.PROFILE_FLAG.Set, modifyData);

            // Check the return status of processProfileAsync
            string resultString = results.StatusCode == EMDKResults.STATUS_CODE.Processing ? "Set profile in-progress..." : "Set profile failed.";

            _mainPage.updateStatusText(resultString);
        }
        else
        {
            _mainPage.updateStatusText("The above fields cannot be empty.");
        }
    }

    private string CheckXmlError(EMDKResults results)
    {
        StringReader stringReader = null;
        string checkXmlStatus = "";
        bool isFailure = false;

        try
        {
            if (results.StatusCode == EMDKResults.STATUS_CODE.CheckXml)
            {
                stringReader = new StringReader(results.StatusString);

                using (XmlReader reader = XmlReader.Create(stringReader))
                {
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            switch (reader.Name)
                            {
                                case "parm-error":
                                    isFailure = true;
                                    string parmName = reader.GetAttribute("name");
                                    string parmErrorDescription = reader.GetAttribute("desc");
                                    checkXmlStatus = "Name: " + parmName + ", Error Description: " + parmErrorDescription;
                                    break;
                                case "characteristic-error":
                                    isFailure = true;
                                    string errorType = reader.GetAttribute("type");
                                    string charErrorDescription = reader.GetAttribute("desc");
                                    checkXmlStatus = "Type: " + errorType + ", Error Description: " + charErrorDescription;
                                    break;
                            }
                        }
                    }

                    if (!isFailure)
                    {
                        checkXmlStatus = "Profile applied successfully ...";
                    }

                }
            }
            else
            {
                checkXmlStatus = results.StatusCode.ToString();
            }
        }
        finally
        {
            if (stringReader != null)
            {
                stringReader.Dispose();
            }
        }

        return checkXmlStatus;
    }

    private Boolean readValues(String timeZone, String date, String time)
    {
        if ((timeZone != null) && (timeZone.Length > 0))
        {
            if ((date != null) && (date.Length > 0))
            {
                if ((time != null) && (time.Length > 0))
                {
                    return true;
                }
            }
        }

        return false;
    }

    protected override void OnPostCreate(Bundle savedInstanceState)
    {
        base.OnPostCreate(savedInstanceState);
        _mainPage = MainPage.getInstance();
        _mainPage.setMainActivity(this);
        EMDKResults results = EMDKManager.GetEMDKManager(ApplicationContext, this);
    }
}
