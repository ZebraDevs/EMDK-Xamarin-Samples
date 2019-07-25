using Android.App;
using Android.Widget;
using Android.OS;
using System;
using Symbol.XamarinEMDK;
using Symbol.XamarinEMDK.Barcode;
using System.Collections.Generic;
using Android;
using Android.Views;
using Java.Lang;

namespace MultiBarcodeSample1
{
    [Activity(Label = "MultiBarcodeSample1", MainLauncher = true, Icon = "@drawable/icon", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
    public class MainActivity : Activity, EMDKManager.IEMDKListener
    {
        private EMDKManager emdkManager = null;
        private BarcodeManager barcodeManager = null;
        
        private Scanner scanner = null;
        private static Activity present_Activity;

        private TextView textViewStatus = null;
        private Spinner spinnerScannerDevices = null;
        private Spinner spinnerTriggers = null;
        private EditText barcodeCount = null;
        private TextView textView1 = null;
        private TextView textView4 = null;
        private TextView textView2 = null;
        private TextView textView = null;
        private TextView textViewType = null;
        private TableLayout tableView = null;
        private ScrollView scrollView = null;
        private string statusString = "";
        private int scannerIndex = 0;
        private int defaultIndex = 0;
        private int triggerIndex = 1;
        private int dataCount = 0;
        
        private IList<ScannerInfo> deviceList = null;


        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            present_Activity = this;
            SetContentView(Resource.Layout.Main);

            /*tableView = FindViewById<TableLayout>(Resource.Id.tableView);
            scrollView = FindViewById<ScrollView>(Resource.Id.scrollView);
            textViewStatus = FindViewById<TextView>(Resource.Id.textViewStatus) as TextView;
            textView = (TextView) FindViewById<TextView>(Resource.Id.textView);
            textView1 = FindViewById<TextView>(Resource.Id.textView1) as TextView;
            textView2 = FindViewById<TextView>(Resource.Id.textView2) as TextView;
            textView4 = FindViewById<TextView>(Resource.Id.textView4) as TextView;
            textViewType = FindViewById<TextView>(Resource.Id.textViewType) as TextView;

            spinnerScannerDevices = FindViewById<Spinner>(Resource.Id.spinnerScannerDevices) as Spinner;
            spinnerTriggers = FindViewById<Spinner>(Resource.Id.spinnerTriggers);
            barcodeCount = FindViewById<EditText>(Resource.Id.barcodeCount);
            Button StartScanbtn = FindViewById<Button>(Resource.Id.buttonStartScan);*/
            textViewStatus = (TextView)FindViewById(Resource.Id.textViewStatus);
            spinnerScannerDevices = (Spinner)FindViewById(Resource.Id.spinnerScannerDevices);
            spinnerTriggers = (Spinner)FindViewById(Resource.Id.spinnerTriggers);
            barcodeCount = (EditText)FindViewById(Resource.Id.barcodeCount);
            barcodeCount.TextChanged += BarcodeCount_TextChanged;


            EMDKResults results = EMDKManager.GetEMDKManager(Application.Context, this);

            // Check the return status of GetEMDKManager
            if (results.StatusCode != EMDKResults.STATUS_CODE.Success)
            {
                // EMDKManager object initialization failed
                textViewStatus.Text = "Status: EMDKManager object creation failed.";
            }
            else
            {
                // EMDKManager object initialization succeeded
                textViewStatus.Text = "Status: EMDKManager object creation succeeded.";
            }

            



            

            AddBarcodeCountListener();
            AddStartScanButtonListener();
            AddSpinnerScannerDevicesListener();
            AddSpinnerTriggersListener();
            PopulateTriggers();


        }

        private void BarcodeCount_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
           
                SetConfig();
           
          
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            DeInitScanner();

            // Remove connection listener
            if (barcodeManager != null)
            {
                barcodeManager.Connection -= barcodeManager_Connection;
                barcodeManager = null;
            }

            // Release all the resources
            if (emdkManager != null)
            {
                emdkManager.Release();
                emdkManager = null;

            }
        }
        
       

        private void PopulateTriggers()
        {
            // Populate the trigger types into spinner
            var spinnerAdapter = ArrayAdapter.CreateFromResource(this, Resource.Array.triggerItems, Android.Resource.Layout.SimpleSpinnerItem);
            spinnerAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            spinnerTriggers.Adapter = spinnerAdapter;
        }
        private void AddSpinnerTriggersListener()
        {
            spinnerTriggers = FindViewById<Spinner>(Resource.Id.spinnerTriggers);
            
            spinnerTriggers.ItemSelected += spinnerTriggers_ItemSelected;
        }

        private void spinnerTriggers_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            triggerIndex = e.Position;
            SetTrigger();
        }

        private void AddSpinnerScannerDevicesListener()
        {
            spinnerScannerDevices = FindViewById<Spinner>(Resource.Id.spinnerScannerDevices);
            spinnerScannerDevices.ItemSelected += spinnerScanners_ItemSelected;
        }

        private void spinnerScanners_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            if ((scannerIndex != e.Position) || (scanner == null))
            {
                scannerIndex = e.Position;
                DeInitScanner();
                InitScanner();
                SetTrigger();
                SetConfig();
               
            }
        }

        private void AddStartScanButtonListener()
        {

            Button StartScanbtn = FindViewById<Button>(Resource.Id.buttonStartScan);
            StartScanbtn.Click += buttonStartScan_Click;
        }

        private void buttonStartScan_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(barcodeCount.Text) && int.Parse(barcodeCount.Text.ToString()) >= 2 && int.Parse(barcodeCount.Text.ToString()) <= 10)
            {

                if (scanner == null)
                {
                    InitScanner();
                }

                if (scanner != null)
                {
                    try
                    {
                        if (scanner.IsEnabled)
                        {

                            // Submit a new read.
                            scanner.Read();

                        }
                        else
                        {
                            textViewStatus.Text = "Status: Scanner is not enabled";
                        }
                    }
                    catch (ScannerException ex)
                    {
                        textViewStatus.Text = "Status: " + ex.Message;
                        Console.WriteLine(ex.StackTrace);
                    }
                }
            }
            else
            {
                AlertDialog.Builder alertDialog = new AlertDialog.Builder(this);

                // Setting Dialog Title
                alertDialog.SetTitle("MutiBarcode Sample");

                // Setting Dialog Message
                alertDialog.SetMessage("Number should be between 2-10");
                alertDialog.Show();
            }
        }

        private void AddBarcodeCountListener()
        {
            barcodeCount = FindViewById<EditText>(Resource.Id.barcodeCount);
            SetConfig();
        }

        

        public void OnClosed()
        {
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
                spinnerScannerDevices.SetSelection(defaultIndex);

                // Set trigger (App default - HARD)
                spinnerTriggers.SetSelection(triggerIndex);
                
            }
            catch (System.Exception e)
            {
                textViewStatus.Text = "Status: BarcodeManager object creation failed.";
                Console.WriteLine("Exception:" + e.StackTrace);
            }
        }

        private void barcodeManager_Connection(object sender, BarcodeManager.ScannerConnectionEventArgs e)
        {
            string status;
            string scannerName = "";

            ScannerInfo scannerInfo = e.P0;
            BarcodeManager.ConnectionState connectionState = e.P1;

            string statusBT = connectionState.ToString();
            string scannerNameBT = scannerInfo.FriendlyName;



            if (deviceList.Count != 0)
            {
                scannerName = deviceList[scannerIndex].FriendlyName;
            }

            if (scannerName.ToLower().Equals(scannerNameBT.ToLower()))
            {
                status = "Status: " + scannerNameBT + ":" + statusBT;
                RunOnUiThread(() => textViewStatus.Text = status);

                if (connectionState == BarcodeManager.ConnectionState.Connected)
                {

                    DeInitScanner();
                    InitScanner();
                    SetTrigger();
                    SetConfig();
                }

                if (connectionState == BarcodeManager.ConnectionState.Disconnected)
                {
                    // De-initialize scanner
                    DeInitScanner();

                                  }
                status = "Status: " + scannerNameBT + ":" + statusBT;
                RunOnUiThread(() => textViewStatus.Text = status);
            }
            else
            {
                status = "Status: " + statusString + " " + scannerNameBT + ":" + statusBT;
                RunOnUiThread(() => textViewStatus.Text = status);
            }
            
        }

        private void SetConfig()
        {

            if (scanner == null)
            {
                InitScanner();
            }

            if ((scanner != null) && scanner.IsEnabled)
            {
                try
                {

                    ScannerConfig config = scanner.GetConfig();


                    // Scan Mode set to Multi Barcode
                    config.ReaderParams.ReaderSpecific.ImagerSpecific.ScanMode = ScannerConfig.ScanMode.MultiBarcode;

                    // Setting the barcode count
                    if (barcodeCount.Text.ToString().Length > 0)
                    {
                        config.MultiBarcodeParams.BarcodeCount = int.Parse(barcodeCount.Text.ToString());
                        
                    }

                    scanner.SetConfig(config);

                    textViewStatus.Text = "Status: Configuration changed!";

                }
                catch (ScannerException e)
                {

                    textViewStatus.Text = "Status: " + e.Message;
                }
            }

        }
        private void EnumerateScanners()
        {
            if (barcodeManager != null)
            {
                int spinnerIndex = 0;
                List<string> friendlyNameList = new List<string>();

                // Query the supported scanners on the device
                deviceList = barcodeManager.SupportedDevicesInfo;

                if ((deviceList != null) && (deviceList.Count > 0))
                {
                    foreach (ScannerInfo scnInfo in deviceList)
                    {
                        friendlyNameList.Add(scnInfo.FriendlyName);

                        // Save index of the default scanner (device specific one)
                        if (scnInfo.IsDefaultScanner)
                        {
                            defaultIndex = spinnerIndex;
                        }

                        ++spinnerIndex;
                    }
                    textViewStatus.Text = "Status: " + "Scanner not there .";
                }
                else
                {
                    textViewStatus.Text = "Status: Failed to get the list of supported scanner devices! Please close and restart the application.";
                }

                // Populate the friendly names of the supported scanners into spinner
                ArrayAdapter<string> spinnerAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, friendlyNameList);
                spinnerAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
                spinnerScannerDevices.Adapter = spinnerAdapter;
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

        private void InitScanner()
        {
            if (scanner == null)
            {
                if ((deviceList != null) && (deviceList.Count > 0))
                {
                    // Get new scanner device based on the selected index
                    scanner = barcodeManager.GetDevice(deviceList[scannerIndex]);
                }
                else
                {
                    textViewStatus.Text = "Status: Failed to get the specified scanner device! Please close and restart the application.";
                    return;
                }

                if (scanner != null)
                {
                    // Add data listener
                    scanner.Data += scanner_Data;

                    // Add status listener
                    scanner.Status += scanner_Status;

                    try
                    {
                        // Enable the scanner
                        scanner.Enable();
                        checkMultiBarcodeSupport();
                    }
                    catch (ScannerException e)
                    {
                        textViewStatus.Text = "Status: " + e.Message;
                        Console.WriteLine(e.StackTrace);
                    }
                }
                else
                {
                    textViewStatus.Text = "Status: Failed to initialize the scanner device.";
                }
            }
        }

        private void checkMultiBarcodeSupport()
        {
            if (scanner != null)
            {
                try
                {
                    ScannerConfig config = scanner.GetConfig();
                    if (!config.IsParamSupported("config.multiBarcodeParams.barcodeCount"))
                    {
                        new AsyncMultiDataUpdate(new List<TableRow>()).Execute("NOTE: Multibarcode feature is unsupported in the selected scanner");
                    }
                    else
                    {
                        new AsyncMultiDataUpdate(new List<TableRow>()).Execute("");
                    }
                }
                catch (System.Exception e)
                {
                    textViewStatus.Text = "Status: " + e.Message;
                }
            }
        }

        class AsyncMultiDataUpdate: AsyncTask<string, Java.Lang.Void, string>
        {
            private IList<TableRow> rows;
            public AsyncMultiDataUpdate(IList<TableRow> rows)
            {
                this.rows = rows;
            }
            protected override Java.Lang.Object DoInBackground(params Java.Lang.Object[] @params)
            {
                return @params[0];
            }
            
            protected override string RunInBackground(params string[] @params)
            {
                return @params[0];
            }
            protected override void OnPostExecute(string result)
            {

                TextView tv = present_Activity.FindViewById<TextView>(Resource.Id.textViewStatus);
                tv.Text= result;

                TableLayout tl = (TableLayout)present_Activity.FindViewById(Resource.Id.tableView);

                tl.RemoveAllViews();
                foreach (TableRow row in rows)
                {
                    tl.AddView(row);
                }
            }

           
        }

        class AsyncStatusUpdate : AsyncTask<string, Java.Lang.Void, string>
        {
           
           
            protected override Java.Lang.Object DoInBackground(params Java.Lang.Object[] @params)
            {
                return @params[0];
            }
            protected override string RunInBackground(params string[] @params)
            {
                return @params[0];
            }
            protected override void OnPostExecute(string result)
            {
                

                TextView tv = present_Activity.FindViewById<TextView>(Resource.Id.textViewStatus);
                tv.Text = result;
               
            }

          
        }

        

        private void scanner_Status(object sender, Scanner.StatusEventArgs e)
        {
            StatusData statusData = e.P0;
            StatusData.ScannerStates state = e.P0.State;
            new AsyncStatusUpdate().Execute(state.ToString());

            /*if (state == StatusData.ScannerStates.Idle)
            {
                statusString = "Status: " + statusData.FriendlyName + " is enabled and idle...";
                RunOnUiThread(() => textViewStatus.Text = statusString);


                if (state == StatusData.ScannerStates.Waiting)
                {
                    statusString = "Status: Scanner is waiting for trigger press...";
                    RunOnUiThread(() =>
                    {
                        textViewStatus.Text = statusString;
                    });
                }

                if (state == StatusData.ScannerStates.Scanning)
                {
                    statusString = "Status: Scanning...";
                    RunOnUiThread(() =>
                    {
                        textViewStatus.Text = statusString;

                    });
                }

                if (state == StatusData.ScannerStates.Disabled)
                {
                    statusString = "Status: " + statusData.FriendlyName + " is disabled.";
                    RunOnUiThread(() =>
                    {
                        textViewStatus.Text = statusString;

                    });
                }

                if (state == StatusData.ScannerStates.Error)
                {
                    statusString = "Status: An error has occurred.";
                    RunOnUiThread(() =>
                    {
                        textViewStatus.Text = statusString;

                    });
                }

            }*/
        }

        
        private void scanner_Data(object sender, Scanner.DataEventArgs e)
        {

            ScanDataCollection scanDataCollection = e.P0;

            if ((scanDataCollection != null) && (scanDataCollection.Result == ScannerResults.Success))
            {

                if (scanDataCollection.GetScanData() != null)
                {
                    IList<TableRow> rows = new List<TableRow>();
                    TableRow row = new TableRow(this);
                    row.SetBackgroundColor(Android.Graphics.Color.Black);
                    row.SetPadding(1, 1, 1, 1);
                    TableRow.LayoutParams llp = new TableRow.LayoutParams(TableLayout.LayoutParams.MatchParent, TableLayout.LayoutParams.MatchParent);
                    llp.SetMargins(0, 0, 2, 0);

                    TextView keyText = new TextView(this);
                    keyText.SetPadding(5, 5, 5, 5);
                    keyText.LayoutParameters = llp;                    
                    //keyText.SetLayerType(llp);                        
                    keyText.SetBackgroundColor(Android.Graphics.Color.White);
                    keyText.Text = "Type";
                    row.AddView(keyText);

                    TextView valueText = new TextView(this);
                    valueText.SetPadding(5, 5, 5, 5);
                    valueText.SetBackgroundColor(Android.Graphics.Color.White);
                    valueText.Text = "Value";
                    row.AddView(valueText);

                    rows.Add(row);

                    IList<ScanDataCollection.ScanData> scanData = scanDataCollection.GetScanData();

                    foreach (ScanDataCollection.ScanData data in scanData)
                    {
                        string dataString = data.Data;                        
                        row = new TableRow(this);
                        row.SetBackgroundColor(Android.Graphics.Color.Black);
                        row.SetPadding(1, 1, 1, 1);

                        string mKey = data.LabelType.ToString();
                        string mValue = data.Data;

                        keyText = new TextView(this);
                        keyText.SetPadding(5, 5, 5, 5);
                        keyText.LayoutParameters = llp;
                        keyText.SetBackgroundColor(Android.Graphics.Color.White);
                        keyText.Text = mKey;
                        row.AddView(keyText);

                        valueText = new TextView(this);
                        valueText.SetPadding(5, 5, 5, 5);
                        valueText.SetBackgroundColor(Android.Graphics.Color.White);
                        valueText.LayoutParameters = llp;
                        valueText.Text = mValue;
                        row.AddView(valueText);

                        rows.Add(row);
                        //RunOnUiThread(() => DisplayScanData(mValue));
                        RunOnUiThread(() => DisplayScanData(dataString));
                    }
                    new AsyncMultiDataUpdate(rows).Execute("MultiBarcode Scanning result");

                }



            }



        }
        private void DisplayScanData(string data)
        {

            if (dataCount++ > 100)
            {
                // Clear the cache after 100 scans
                textViewStatus.Text = "";
                dataCount = 0;
            }

           // textViewStatus.Append(data + "\r\n");

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
    }
}




 