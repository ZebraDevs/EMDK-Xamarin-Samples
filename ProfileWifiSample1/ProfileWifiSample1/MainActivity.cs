/*
 * Copyright (C) 2015-2019 Zebra Technologies Corporation and/or its affiliates
 * All rights reserved.
 */
using System;
using System.Xml;
using System.IO;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Util;

using Symbol.XamarinEMDK;

namespace Symbol.XamarinEMDK.ProfileWifiSample1
{
    [Activity(Label = "ProfileWifiSample1", MainLauncher = true, Icon = "@drawable/icon", WindowSoftInputMode = SoftInput.AdjustPan, ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
    public class MainActivity : Activity, EMDKManager.IEMDKListener
    {
        // Declare a variable to store EMDKManager object
        private EMDKManager emdkManager = null;
        // Declare a variable to store ProfileManager object
        private ProfileManager profileManager = null;

        // Assign the 'ProfileName' used in EMDKConfig.xml
        private string profileName = "WifiProfile-1";

        private TextView statusTextView = null;
        private RadioButton wifiRadioEnable = null;
        private RadioButton wifiRadioDisable = null;
        private Spinner ActionSpinner = null;
        private EditText ssidEditText = null;

        private Action action = Action.DO_NOTHING;

        private String[] ActionStrings = { "Do Nothing", "Add", "Remove", "Connect", "Disconnect", "Enable", "Disable" };
        private String[] RadioStateStrings = { "enable", "disable" };

        private enum Action
        {
            DO_NOTHING,
            ADD,
            REMOVE,
            CONNECT,
            DISCONNECT,
            ENABLE,
            DISABLE
        };

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

			// Disable auto rotation of the app.
			RequestedOrientation = Android.Content.PM.ScreenOrientation.Nosensor;

			// Get current rotation angle of the screen from its default/natural orientation.
			var windowManager = (IWindowManager)ApplicationContext.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
			var rotation = windowManager.DefaultDisplay.Rotation;

			// Determine width/height in pixels based on the rotation angle.
			DisplayMetrics dm = new DisplayMetrics();
			WindowManager.DefaultDisplay.GetMetrics (dm);

			int width = 0;
			int height = 0;

			switch (rotation) {
			case SurfaceOrientation.Rotation0:
				width = dm.WidthPixels;
				height = dm.HeightPixels;
				break;
			case SurfaceOrientation.Rotation90:       
			case SurfaceOrientation.Rotation270:
				width = dm.WidthPixels;
				height = dm.HeightPixels;
				break;
			default:
				break;
			}

			// Set corresponding layout dynamically based on the default/natural orientation.
			if(width > height){
				SetContentView(Resource.Layout.Landscape_Layout);
			} else {
				SetContentView(Resource.Layout.Portrait_Layout);
			}
            statusTextView = FindViewById(Resource.Id.textViewStatus) as TextView;
            wifiRadioEnable = FindViewById(Resource.Id.radio0) as RadioButton;
            wifiRadioDisable = FindViewById(Resource.Id.radio1) as RadioButton;
            ActionSpinner = FindViewById(Resource.Id.spinner1) as Spinner;
            ssidEditText = FindViewById(Resource.Id.editTextSSID) as EditText;

            AddSetButtonListener();

            EMDKResults results = EMDKManager.GetEMDKManager(Application.Context, this);

            // Check the return status of processProfile
            if (results.StatusCode != EMDKResults.STATUS_CODE.Success)
            {
                // EMDKManager object initialization failed
                statusTextView.Text = "EMDKManager object creation failed ...";
            }
            else
            {
                // EMDKManager object initialization succeeded
                statusTextView.Text = "EMDKManager object creation succeeded ...";
            }

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

        public void OnClosed()
        {
            if (emdkManager != null)
            {
                emdkManager.Release();
                emdkManager = null;
            }

            statusTextView.Text = "EMDK closed unexpectedly! Please close and restart the application.";
        }

        public void OnOpened(EMDKManager emdkManagerInstance)
        {
            statusTextView.Text = "EMDK open success.";

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
                statusTextView.Text = "Error loading profile manager.";
                Console.WriteLine("Exception: " + e.StackTrace);
            }
        }

        void profileManager_Data(object sender, ProfileManager.DataEventArgs e)
        {
            // Call back with the result of the processProfileAsync

            EMDKResults results = e.P0.Result;

            string statusString = CheckXmlError(results);
            RunOnUiThread(() => statusTextView.Text = statusString);
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

        void AddSetButtonListener()
        {
            Button btnSet = FindViewById<Button>(Resource.Id.buttonSet) as Button;

            btnSet.Click += btnSet_Click;
        }

        void btnSet_Click(object sender, EventArgs e)
        {
            // Set profile on UI button click
            SetProfile();
        }

        private void SetProfile()
        {
            string[] modifyData = new string[1];

            statusTextView.Text = "";
            RadioGroup radiogroup = FindViewById(Resource.Id.radioGroup1) as RadioGroup;

            if (wifiRadioEnable.Checked)
            {
                modifyData[0] =
                   "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                   "<characteristic type=\"Profile\">" +
                       "<parm name=\"ProfileName\" value=\"WifiProfile-1\"/>" +
                        "<characteristic type=\"Wi-Fi\">" +
                        "<parm name=\"emdk_name\" value=\"WifiMgr1\"/>" +
                           "<characteristic type=\"System\">" +
                               "<parm name=\"WiFiAction\" value=\"enable\"/>" +
                           "</characteristic>" +
                           "<parm name=\"UseRegulatory\" value=\"0\"/>" +
                            "<parm name=\"UseAdvancedOptions\" value=\"0\"/>";

            }
            else
            {
                modifyData[0] =
                    "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                    "<characteristic type=\"Profile\">" +
                        "<parm name=\"ProfileName\" value=\"WifiProfile-1\"/>" +
                         "<characteristic type=\"Wi-Fi\">" +
                         "<parm name=\"emdk_name\" value=\"WifiMgr1\"/>" +
                            "<characteristic type=\"System\">" +
                                "<parm name=\"WiFiAction\" value=\"disable\"/>" +
                            "</characteristic>" +
                            "<parm name=\"UseRegulatory\" value=\"0\"/>" +
                            "<parm name=\"UseAdvancedOptions\" value=\"0\"/>";
            }


            action = (Action)ActionSpinner.SelectedItemId;

            switch (action)
            {
                case Action.ADD:
                    modifyData[0] += "<parm name=\"NetworkAction\" value=\"Add\"/>" +
                     "<characteristic type=\"network-profile\">" +
                     "<parm name=\"SSID\" value=\"" + ssidEditText.Text + "\"/>" +
                     "<parm name=\"SecurityMode\" value=\"0\"/>" +
                    "<parm name=\"UseDHCP\" value=\"1\"/>" +
                    "<parm name=\"UseProxy\" value=\"0\"/>" +
                    "</characteristic>";
                    break;

                case Action.REMOVE:
                    modifyData[0] += "<parm name=\"NetworkAction\" value=\"Remove\"/>" +
                    "<characteristic type=\"network-profile\">" +
                    "<parm name=\"SSID\" value=\"" + ssidEditText.Text + "\"/>" +
                    "</characteristic>";
                    break;

                case Action.CONNECT:
                    modifyData[0] += "<parm name=\"NetworkAction\" value=\"Connect\"/>" +
                    "<characteristic type=\"network-profile\">" +
                    "<parm name=\"SSID\" value=\"" + ssidEditText.Text + "\"/>" +
                    "</characteristic>";
                    break;

                case Action.DISCONNECT:
                    modifyData[0] += "<parm name=\"NetworkAction\" value=\"Disconnect\"/>" +
                    "<characteristic type=\"network-profile\">" +
                    "<parm name=\"SSID\" value=\"" + ssidEditText.Text + "\"/>" +
                    "</characteristic>";
                    break;

                default:
                    break;
            }

            modifyData[0] += "</characteristic></characteristic>";

            // Call processPrfoileAsync with profile name, 'Set' flag and modify data to update the profile
            EMDKResults results = profileManager.ProcessProfileAsync(profileName, ProfileManager.PROFILE_FLAG.Set, modifyData);

            // Check the return status of processProfileAsync
            string resultString = results.StatusCode == EMDKResults.STATUS_CODE.Processing ? "Set profile in-progress..." : "Set profile failed.";

            if (statusTextView != null)
            {
                statusTextView.Text = resultString;
            }

        }


    }
}





