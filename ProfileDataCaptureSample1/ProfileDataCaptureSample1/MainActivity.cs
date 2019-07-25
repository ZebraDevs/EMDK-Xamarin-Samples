using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Util;

using Symbol.XamarinEMDK;
namespace symbol.xamarinemdk.profiledatacapturesample1
{
    [Activity(Name = "symbol.xamarinemdk.profiledatacapturesample1.MainActivity", Label = "ProfileDataCaptureSample1", MainLauncher = true, Icon = "@drawable/icon", WindowSoftInputMode = SoftInput.AdjustPan, ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
    public class MainActivity : Activity, EMDKManager.IEMDKListener
    {
        // Declare a variable to store EMDKManager object
        private EMDKManager emdkManager = null;
        // Declare a variable to store ProfileManager object
        private ProfileManager profileManager = null;

        // Assign the 'ProfileName' used in EMDKConfig.xml
        private string profileName = "DataCaptureProfile-1";
        // Assign the 'emdk_name' used in EMDKConfig.xml for the 'Barcode' feature that used for name-value pairs
        private string featureName = "Barcode1";

        private TextView statusTextView = null;
        private CheckBox checkBoxCode128 = null;
        private CheckBox checkBoxCode39 = null;
        private CheckBox checkBoxEAN8 = null;
        private CheckBox checkBoxEAN13 = null;
        private CheckBox checkBoxUPCA = null;
        private CheckBox checkBoxUPCE0 = null;

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
				SetContentView(Resource.Layout.Landscape);
			} else {
				SetContentView(Resource.Layout.Portrait);
			}


            statusTextView = FindViewById<TextView>(Resource.Id.textViewStatus) as TextView;
            checkBoxCode128 = FindViewById<CheckBox>(Resource.Id.checkBoxCode128);
            checkBoxCode39 = FindViewById<CheckBox>(Resource.Id.checkBoxCode39);
            checkBoxEAN8 = FindViewById<CheckBox>(Resource.Id.checkBoxEAN8);
            checkBoxEAN13 = FindViewById<CheckBox>(Resource.Id.checkBoxEAN13);
            checkBoxUPCA = FindViewById<CheckBox>(Resource.Id.checkBoxUPCE);
            checkBoxUPCE0 = FindViewById<CheckBox>(Resource.Id.checkBoxUPCE0);

            // Set listener to the button
            AddSetButtonListener();

            // The EMDKManager object will be created and returned in the callback
            EMDKResults results = EMDKManager.GetEMDKManager(Android.App.Application.Context, this);

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

        #region IEMDKListener Members

        public void OnClosed()
        {
            // This callback will be issued when the EMDK closes unexpectedly.

            if (emdkManager != null)
            {
                emdkManager.Release();
                emdkManager = null;
            }

            statusTextView.Text = "EMDK closed unexpectedly! Please close and restart the application.";
        }

        public void OnOpened(EMDKManager emdkManagerInstance)
        {
            // This callback will be issued when the EMDK is ready to use.
            statusTextView.Text = "EMDK open success.";

            this.emdkManager = emdkManagerInstance;

            try
            {
                // Get the ProfileManager object to process the profiles
                profileManager = (ProfileManager)emdkManager.GetInstance(EMDKManager.FEATURE_TYPE.Profile);

                // Add listener to get async results
                profileManager.Data += profileManager_Data;

                // Set profile at the start
                SetProfile();
            }
            catch (Exception e)
            {
                statusTextView.Text = "Error setting the profile.";
                Console.WriteLine("Exception:" + e.StackTrace);
            }
        }

        #endregion

        void profileManager_Data(object sender, ProfileManager.DataEventArgs e)
        {
            // Call back with the result of the processProfileAsync

            EMDKResults results = e.P0.Result;

            // Check the return status of processProfileAsync
            string resultString = results.StatusCode == EMDKResults.STATUS_CODE.Success ? "Set profile success." : "Set profile failed.";

            if (statusTextView != null)
            {
                RunOnUiThread(() => statusTextView.Text = resultString);
                Console.WriteLine("Status: " + results.StatusCode + " ExtendedStatus: " + results.ExtendedStatusCode + "\n" + results.StatusString);
            }
        }

        void AddSetButtonListener()
        {
            Button btnSet = FindViewById<Button>(Resource.Id.buttonSet);

            btnSet.Click += btnSet_Click;
        }

        void btnSet_Click(object sender, EventArgs e)
        {
            // Set profile on UI button click
            SetProfile();
        }

        private void SetProfile()
        {
            statusTextView.Text = "";

            string[] modifyData = new string[6];

            // Prepare name-value pairs to modify the existing profile
            modifyData[0] = ProfileManager.CreateNameValuePair(featureName, "decoder_code128", checkBoxCode128.Checked.ToString().ToLower());
            modifyData[1] = ProfileManager.CreateNameValuePair(featureName, "decoder_code39", checkBoxCode39.Checked.ToString().ToLower());
            modifyData[2] = ProfileManager.CreateNameValuePair(featureName, "decoder_ean8", checkBoxEAN8.Checked.ToString().ToLower());
            modifyData[3] = ProfileManager.CreateNameValuePair(featureName, "decoder_ean13", checkBoxEAN13.Checked.ToString().ToLower());
            modifyData[4] = ProfileManager.CreateNameValuePair(featureName, "decoder_upca", checkBoxUPCA.Checked.ToString().ToLower());
            modifyData[5] = ProfileManager.CreateNameValuePair(featureName, "decoder_upce0", checkBoxUPCE0.Checked.ToString().ToLower());

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

