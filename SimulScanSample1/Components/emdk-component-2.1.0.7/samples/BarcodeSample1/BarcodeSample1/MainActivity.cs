using System;
using System.Threading;
using System.Collections.Generic;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

using Symbol.XamarinEMDK;
using Symbol.XamarinEMDK.Barcode;

namespace Symbol.XamarinEMDK.BarcodeSample1
{
    [Activity(Label = "BarcodeSample1", MainLauncher = true, Icon = "@drawable/icon", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
    public class MainActivity : Activity, EMDKManager.IEMDKListener
    {
        // Declare a variable to store EMDKManager object
        private EMDKManager emdkManager = null;

        // Declare a variable to store BarcodeManager object
        private BarcodeManager barcodeManager = null;

        // Declare a variable to store Scanner object
        private Scanner scanner = null;

        // Declare a flag for continuous scan mode
        private bool isContinuousMode = false;

        // Declare a flag to save the current state of continuous mode flag during OnPause() and Bluetooth scanner Disconnected event.
        private bool isContinuousModeSaved = false;

        private TextView textViewData = null;
        private TextView textViewStatus = null;

        private CheckBox checkBoxEAN8 = null;
        private CheckBox checkBoxEAN13 = null;
        private CheckBox checkBoxCode39 = null;
        private CheckBox checkBoxCode128 = null;
        private CheckBox checkBoxContinuous = null;

        private Spinner spinnerScanners = null;
        private Spinner spinnerTriggers = null;

        private IList<ScannerInfo> scannerList = null;

        private int scannerIndex = 0; // Keep the selected scanner
        private int defaultIndex = 0; // Keep the default scanner 
        private int triggerIndex = 0; // Keep the selected trigger

        private int dataCount = 0;

        private string statusString = "";

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            textViewData = FindViewById<TextView>(Resource.Id.textViewData) as TextView;
            textViewStatus = FindViewById<TextView>(Resource.Id.textViewStatus) as TextView;
            
            AddStartScanButtonListener();
            AddStopScanButtonListener();
            AddSpinnerScannersListener();
            AddSpinnerTriggersListener();
            AddCheckBoxContinuousListener();
            AddCheckBoxDecodersListener();

            PopulateTriggers();

            // The EMDKManager object will be created and returned in the callback
            EMDKResults results = EMDKManager.GetEMDKManager(Application.Context, this);

            // Check the return status of GetEMDKManager
            if (results.StatusCode != EMDKResults.STATUS_CODE.Success)
            {
                // EMDKManager object initialization success
                textViewStatus.Text = "Status: EMDKManager object creation failed.";
            }
            else
            {
                // EMDKManager object initialization failed
                textViewStatus.Text = "Status: EMDKManager object creation succeeded.";
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // De-initialize scanner
            DeInitScanner();

            // Clean up the objects created by EMDK manager
            if (barcodeManager != null)
            {
                // Remove connection listener
                barcodeManager.Connection -= barcodeManager_Connection;
                barcodeManager = null;
            }

            if (emdkManager != null)
            {
                emdkManager.Release();
                emdkManager = null;
            }
        }

        protected override void OnPause()
        {
            base.OnPause();
            // The application is in background

            // Save the current state of continuous mode flag
            isContinuousModeSaved = isContinuousMode;

            // Reset continuous flag 
            isContinuousMode = false;

            // De-initialize scanner
            DeInitScanner();
 
            if (barcodeManager != null)
            {
                // Remove connection listener
                barcodeManager.Connection -= barcodeManager_Connection;
                barcodeManager = null;

                // Clear scanner list
                scannerList = null;      
            }

            // Release the barcode manager resources
            if (emdkManager != null)
            {
                emdkManager.Release(EMDKManager.FEATURE_TYPE.Barcode);
            }
        }

        protected override void OnResume()
        {
            base.OnResume();
            // The application is in foreground 

            // Restore continuous mode flag
            isContinuousMode = isContinuousModeSaved;

            // Acquire the barcode manager resources
            if (emdkManager != null)
            {
                try
                {
                    barcodeManager = (BarcodeManager)emdkManager.GetInstance(EMDKManager.FEATURE_TYPE.Barcode);

                    if (barcodeManager != null)
                    {
                        // Add connection listener
                        barcodeManager.Connection += barcodeManager_Connection;
                    }

                    // Enumerate scanners 
                    EnumerateScanners();

                    // Set selected scanner 
                    spinnerScanners.SetSelection(scannerIndex);

                    // Set selected trigger
                    spinnerTriggers.SetSelection(triggerIndex);
                }
                catch (Exception e)
                {
                    textViewStatus.Text = "Status: BarcodeManager object creation failed.";
                    Console.WriteLine("Exception: " + e.StackTrace);
                }
            }
        }

        #region IEMDKListener Members

        public void OnClosed()
        {
            // This callback will be issued when the EMDK closes unexpectedly.

            if (emdkManager != null)
            {     
                if (barcodeManager != null)
                {
                    // Remove connection listener
                    barcodeManager.Connection -= barcodeManager_Connection;
                    barcodeManager = null;
                }

                // Release all the resources
                emdkManager.Release();
                emdkManager = null;
            }

            textViewStatus.Text = "Status: EMDK closed unexpectedly! Please close and restart the application.";
        }

        public void OnOpened(EMDKManager emdkManagerInstance)
        {
            // This callback will be issued when the EMDK is ready to use.
            textViewStatus.Text = "Status: EMDK open success.";

            this.emdkManager = emdkManagerInstance;

            try
            {
                // Acquire the barcode manager resources
                barcodeManager = (BarcodeManager)emdkManager.GetInstance(EMDKManager.FEATURE_TYPE.Barcode);
   
                if (barcodeManager != null)
                {
                    // Add connection listener
                    barcodeManager.Connection += barcodeManager_Connection;
                }
		
                // Enumerate scanner devices
                EnumerateScanners();

                // Set default scanner
                spinnerScanners.SetSelection(defaultIndex);

                // Set trigger (App default - HARD)
                spinnerTriggers.SetSelection(triggerIndex);
            }
            catch (Exception e)
            {
                textViewStatus.Text = "Status: BarcodeManager object creation failed.";
                Console.WriteLine("Exception:" + e.StackTrace);
            }
        }

        void barcodeManager_Connection(object sender, BarcodeManager.ScannerConnectionEventArgs e)
        {
            string status;
            string scannerName = "";

            ScannerInfo scannerInfo = e.P0;
            BarcodeManager.ConnectionState connectionState = e.P1;

            string statusBT = connectionState.ToString();
            string scannerNameBT = scannerInfo.FriendlyName;

            if (scannerList.Count != 0)
            {
                scannerName = scannerList[scannerIndex].FriendlyName;
            }

            if (scannerName.ToLower().Equals(scannerNameBT.ToLower()))
            {
                status = "Status: " + scannerNameBT + ":" + statusBT;
                RunOnUiThread(() => textViewStatus.Text = status);
                
                if(connectionState == BarcodeManager.ConnectionState.Connected)
                {
                    // Bluetooth scanner connected

                    // Restore continuous mode flag
                    isContinuousMode = isContinuousModeSaved;  
 
                    // Initialize scanner
                    InitScanner();
                    SetTrigger();
                    SetDecoders();
                }
                
                if(connectionState == BarcodeManager.ConnectionState.Disconnected)
                {
                    // Bluetooth scanner disconnected

                    // Save the current state of continuous mode flag
                    isContinuousModeSaved = isContinuousMode;

                    // Reset continuous flag 
                    isContinuousMode = false;

                    // De-initialize scanner
                    DeInitScanner();
                    
                    // Enable UI Controls
                    RunOnUiThread(() => EnableUIControls(true));
                }
            }
            else
            {
                status = "Status: " + statusString + " " + scannerNameBT + ":" + statusBT;
                RunOnUiThread(() => textViewStatus.Text = status);
            }
        }

        #endregion

        private void AddStartScanButtonListener()
        {
            Button buttonStartScan = FindViewById<Button>(Resource.Id.buttonStartScan);
            buttonStartScan.Click += buttonStartScan_Click;
        }

        void buttonStartScan_Click(object sender, EventArgs e)
        {
            if (scanner == null)
            {
                InitScanner();
            }

            if (scanner != null)
            {
                try
                {
                    // Set continuous flag
                    isContinuousMode = checkBoxContinuous.Checked;

                    // Submit a new read.
                    scanner.Read();

                    // Disable UI controls
                    RunOnUiThread(() => EnableUIControls(false));
                }
                catch (ScannerException ex)
                {
                    textViewStatus.Text = "Status: " + ex.Message;
                    Console.WriteLine(ex.StackTrace);
                }
            }
        }

        private void AddStopScanButtonListener()
        {
            Button buttonStopScan = FindViewById<Button>(Resource.Id.buttonStopScan);
            buttonStopScan.Click += buttonStopScan_Click;

        }

        void buttonStopScan_Click(object sender, EventArgs e)
        {
            if (scanner != null)
            {
                try
                {
                    // Reset continuous flag 
                    isContinuousMode = false;

                    // Cancel the pending read.
                    scanner.CancelRead();

                    // Enable UI controls
                    RunOnUiThread(() => EnableUIControls(true));
                }
                catch (ScannerException ex)
                {
                    textViewStatus.Text = "Status: " + ex.Message;
                    Console.WriteLine(ex.StackTrace);
                }
            }
        }

        private void AddSpinnerScannersListener()
        {
            spinnerScanners = FindViewById<Spinner>(Resource.Id.spinnerScanners);
            spinnerScanners.ItemSelected += spinnerScanners_ItemSelected;
        }

        void spinnerScanners_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            scannerIndex = e.Position;
            DeInitScanner();
            InitScanner();
            SetTrigger();
            SetDecoders();
        }

        private void AddSpinnerTriggersListener()
        {
            spinnerTriggers = FindViewById<Spinner>(Resource.Id.spinnerTriggers);
            spinnerTriggers.ItemSelected += spinnerTriggers_ItemSelected;
        }

        void spinnerTriggers_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            triggerIndex = e.Position;
            SetTrigger();
        }

        private void AddCheckBoxContinuousListener()
        {
            checkBoxContinuous = FindViewById<CheckBox>(Resource.Id.checkBoxContinuous);
            checkBoxContinuous.CheckedChange += checkBoxContinuous_CheckedChange;
        }

        void checkBoxContinuous_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            isContinuousMode = e.IsChecked;
        }

        private void AddCheckBoxDecodersListener()
        {
            checkBoxEAN8 = FindViewById<CheckBox>(Resource.Id.checkBoxEAN8);
            checkBoxEAN13 = FindViewById<CheckBox>(Resource.Id.checkBoxEAN13);
            checkBoxCode39 = FindViewById<CheckBox>(Resource.Id.checkBoxCode39);
            checkBoxCode128 = FindViewById<CheckBox>(Resource.Id.checkBoxCode128);

            checkBoxEAN8.CheckedChange += decoders_CheckedChange;
            checkBoxEAN13.CheckedChange += decoders_CheckedChange;
            checkBoxCode39.CheckedChange += decoders_CheckedChange;
            checkBoxCode128.CheckedChange += decoders_CheckedChange;
        }

        void decoders_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            SetDecoders();
        }

        private void EnumerateScanners()
        {
            if (barcodeManager != null)
            {
                int spinnerIndex = 0;
                List<string> friendlyNameList = new List<string>();
              
                // Query the supported scanners on the device
                scannerList = barcodeManager.SupportedDevicesInfo;

                if ((scannerList != null) && (scannerList.Count > 0))
                {
                    foreach(ScannerInfo scnInfo in scannerList)
                    {
                        friendlyNameList.Add(scnInfo.FriendlyName);
                        
                        // Save index of the default scanner (device specific one)
                        if(scnInfo.IsDefaultScanner)
                        {
                            defaultIndex = spinnerIndex;
                        }

                       ++spinnerIndex;
                    }
                }
                else
                {
                    textViewStatus.Text = "Status: Failed to get the list of supported scanner devices! Please close and restart the application.";
                }

                // Populate the friendly names of the supported scanners into spinner
                ArrayAdapter<string> spinnerAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, friendlyNameList);
                spinnerAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
                spinnerScanners.Adapter = spinnerAdapter;
            }
        }

        private void PopulateTriggers()
        {
            // Populate the trigger types into spinner
            var spinnerAdapter = ArrayAdapter.CreateFromResource(this, Resource.Array.triggers_array, Android.Resource.Layout.SimpleSpinnerItem);
            spinnerAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            spinnerTriggers.Adapter = spinnerAdapter;
        }

        private void InitScanner()
        {
            if (scanner == null)
            {
                if ((scannerList != null) && (scannerList.Count > 0))
                {
                    // Get new scanner device based on the selected index
                    scanner = barcodeManager.GetDevice(scannerList[scannerIndex]);
                }
                else
                {
                    textViewStatus.Text ="Status: Failed to get the specified scanner device! Please close and restart the application.";
                    return;
                }

                if (scanner != null)
                {
                    // Add data listener
                    scanner.Data +=scanner_Data;

                    // Add status listener
                    scanner.Status +=scanner_Status;

                    try
                    {
                        // Enable the scanner
                        scanner.Enable();
                    }
                    catch (ScannerException e)
                    {
                        textViewStatus.Text = "Status: " + e.Message;
                        Console.WriteLine(e.StackTrace);
                    }
                }
                else
                {
                    textViewStatus.Text ="Status: Failed to initialize the scanner device.";
                }
            }
        }

        void scanner_Status(object sender, Scanner.StatusEventArgs e)
        {
            StatusData statusData = e.P0;
            StatusData.ScannerStates state = e.P0.State;

            if (state == StatusData.ScannerStates.Idle)
            {
                statusString = "Status: " + statusData.FriendlyName + " is enabled and idle...";
                RunOnUiThread(() => textViewStatus.Text = statusString);

                if (isContinuousMode)
                {
                    try
                    {
                        // An attempt to use the scanner continuously and rapidly (with a delay < 100 ms between scans) 
                        // may cause the scanner to pause momentarily before resuming the scanning. 
                        // Hence add some delay (>= 100ms) before submitting the next read.
                        try
                        {
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.StackTrace);
                        }

                        // Submit another read to keep the continuation
                        scanner.Read();
                    }
                    catch (ScannerException ex)
                    {
                        statusString = "Status: " + ex.Message;
                        RunOnUiThread(() => textViewStatus.Text = statusString);
                        Console.WriteLine(ex.StackTrace);
                    }
                    catch (NullReferenceException ex)
                    {
                        statusString = "Status: An error has occurred.";
                        RunOnUiThread(() => textViewStatus.Text = statusString);
                        Console.WriteLine(ex.StackTrace);
                    }
                }

                RunOnUiThread(() => EnableUIControls(true));
            }

            if(state == StatusData.ScannerStates.Waiting)
            {
                statusString = "Status: Scanner is waiting for trigger press...";
                RunOnUiThread(() => 
                {
                    textViewStatus.Text = statusString;
                    EnableUIControls(false);
                });
            }

            if(state == StatusData.ScannerStates.Scanning)
            {
                statusString = "Status: Scanning...";
                RunOnUiThread(() => 
                {
                    textViewStatus.Text = statusString;
                    EnableUIControls(false);
                });
            }

            if(state == StatusData.ScannerStates.Disabled)
            {
                statusString = "Status: " + statusData.FriendlyName + " is disabled.";
                RunOnUiThread(() => 
                {
                    textViewStatus.Text = statusString;
                    EnableUIControls(true);
                });
            }

            if(state == StatusData.ScannerStates.Error)
            {
                statusString = "Status: An error has occurred.";
                RunOnUiThread(() => 
                {
                    textViewStatus.Text = statusString;
                    EnableUIControls(true);
                });
            }
        }

        void scanner_Data(object sender, Scanner.DataEventArgs e)
        {
            ScanDataCollection scanDataCollection = e.P0;

		    if ((scanDataCollection != null) && (scanDataCollection.Result == ScannerResults.Success)) 
            {
                IList<ScanDataCollection.ScanData> scanData = scanDataCollection.GetScanData();

			    foreach(ScanDataCollection.ScanData data in scanData) 
                {
                    string dataString = data.Data;
                    RunOnUiThread(() => DisplayScanData(dataString));
			    }
		    }
        }

        private void DeInitScanner()
        {
            if (scanner != null)
            {
                try
                {
                    // Cancel if there is any pending read
                    scanner.CancelRead();

                    // Disable the scanner 
                    scanner.Disable();
                }
                catch (ScannerException e)
                {
                    textViewStatus.Text = "Status: " + e.Message;
                    Console.WriteLine(e.StackTrace);
                }

                // Remove data listener
                scanner.Data -= scanner_Data;

                // Remove status listener
                scanner.Status -= scanner_Status;

                try
                {
                    // Release the scanner
                    scanner.Release();
                }
                catch (ScannerException e)
                {
                    textViewStatus.Text = "Status: " + e.Message;
                    Console.WriteLine(e.StackTrace);
                }

                scanner = null;
            }
        }

        private void SetTrigger()
        {
            if (scanner == null)
            {
                InitScanner();
            }

            if (scanner != null)
            {
                switch (triggerIndex)
                {
                    case 0: // Selected "HARD"
                        scanner.TriggerType = Scanner.TriggerTypes.Hard;
                        break;
                    case 1: // Selected "SOFT"
                        scanner.TriggerType = Scanner.TriggerTypes.SoftAlways;
                        break;
                    default:
                        break;
                }
            }
        }

        private void SetDecoders()
        {
            if (scanner == null)
            {
                InitScanner();
            }

            if (scanner != null)
            {
                try
                {
                    // Config object should be taken out before changing.
                    ScannerConfig config = scanner.GetConfig();

                    // Set EAN8
                    config.DecoderParams.Ean8.Enabled = checkBoxEAN8.Checked;

                    // Set EAN13
                    config.DecoderParams.Ean13.Enabled = checkBoxEAN13.Checked;

                    // Set Code39
                    config.DecoderParams.Code39.Enabled = checkBoxCode39.Checked;

                    // Set Code128
                    config.DecoderParams.Code128.Enabled = checkBoxCode128.Checked;

                    // Should be assigned back to the property to get the changes to the lower layers.
                    scanner.SetConfig(config);
                }
                catch (ScannerException e)
                {
                    textViewStatus.Text = "Status: " + e.Message;
                    Console.WriteLine(e.StackTrace);
                }
            }
        }

        private void EnableUIControls(bool isEnabled)
        {
            checkBoxEAN8.Enabled = isEnabled;
            checkBoxEAN13.Enabled = isEnabled;
            checkBoxCode39.Enabled = isEnabled;
            checkBoxCode128.Enabled = isEnabled;
            spinnerScanners.Enabled = isEnabled;
            spinnerTriggers.Enabled = isEnabled;
        }

        private void DisplayScanData(string data)
        {
			if(dataCount ++ > 100) 
            { 
                // Clear the cache after 100 scans
				textViewData.Text = "";
				dataCount = 0;
			}

            textViewData.Append(data + "\r\n");
          
            var scrollview = FindViewById<ScrollView>(Resource.Id.scrollView1);
            scrollview.FullScroll(FocusSearchDirection.Down);
        }
    }
}

