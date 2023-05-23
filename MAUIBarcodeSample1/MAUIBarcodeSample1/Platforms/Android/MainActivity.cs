using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.AppCompat.App;
using Symbol.XamarinEMDK;
using Symbol.XamarinEMDK.Barcode;
namespace MAUIBarcodeSample1;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity, EMDKManager.IEMDKListener
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

    private IList<ScannerInfo> scannerList = null;

    private int scannerIndex = 0; // Keep the selected scanner
    private int defaultIndex = 0; // Keep the default scanner 
    private int triggerIndex = 0; // Keep the selected trigger

    private int dataCount = 0;

    private string statusString = "";

    private MainPage _mainPage = null;
    public List<string> Items { get; set; }

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

        _mainPage.updateStatusText("EMDK closed unexpectedly! Please close and restart the application.");
    }

    public void OnOpened(EMDKManager eMDKManager)
    {
        // This callback will be issued when the EMDK is ready to use.
        _mainPage.updateStatusText("EMDK open success");

        this.emdkManager = eMDKManager;

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
            scannerIndex = defaultIndex;

            // Initialize scanner
            InitScanner();
            SetDecoders();

            // Set default scanner
            _mainPage.setScannerSelection(defaultIndex);

            //Set Trigger Type
            _mainPage.setTriggerSelection(triggerIndex);


        }
        catch (Exception e)
        {
            _mainPage.updateStatusText("BarcodeManager object creation failed.");
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
            status = scannerNameBT + ":" + statusBT;
            RunOnUiThread(() => _mainPage.updateStatusText(status));

            if (connectionState == BarcodeManager.ConnectionState.Connected)
            {
                // Bluetooth scanner connected

                // Restore continuous mode flag
                isContinuousMode = isContinuousModeSaved;
                // De-initialize scanner
                DeInitScanner();
                // Initialize scanner
                InitScanner();
                SetTrigger();
                SetDecoders();
            }

            if (connectionState == BarcodeManager.ConnectionState.Disconnected)
            {
                // Bluetooth scanner disconnected

                // Save the current state of continuous mode flag
                isContinuousModeSaved = isContinuousMode;

                // Reset continuous flag 
                isContinuousMode = false;

                // De-initialize scanner
                DeInitScanner();

                // Enable UI Controls
                RunOnUiThread(() => _mainPage.EnableUIControls(true));
            }
            status = scannerNameBT + ":" + statusBT;
            RunOnUiThread(() => _mainPage.updateStatusText(status));
        }
        else
        {
            status = statusString + " " + scannerNameBT + ":" + statusBT;
            RunOnUiThread(() => _mainPage.updateStatusText(status));
        }
    }

    protected override void OnCreate(Bundle bundle)
    {
        base.OnCreate(bundle);
        AppCompatDelegate.DefaultNightMode = AppCompatDelegate.ModeNightNo;
    }

    protected override void OnPostCreate(Bundle savedInstanceState)
    {
        base.OnPostCreate(savedInstanceState);
        _mainPage = MainPage.getInstance();
        _mainPage.setMainActivity(this);
        _mainPage.SetContinuousCheckBox(isContinuousMode);
        _mainPage.SetDefaultDecodersForCheckBox(true);
        _mainPage.updateDataText("", true);

        // The EMDKManager object will be created and returned in the callback
        EMDKResults results = EMDKManager.GetEMDKManager(ApplicationContext, this);
        // Check the return status of GetEMDKManager
        if (results.StatusCode != EMDKResults.STATUS_CODE.Success)
        {
            // EMDKManager object initialization failed
            _mainPage.updateStatusText("EMDKManager object creation failed.");
        }
        else
        {
            // EMDKManager object initialization succeeded
            _mainPage.updateStatusText("EMDKManager object creation succeeded.");
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

                // Initialize scanner
                InitScanner();
                SetTrigger();
                SetDecoders();

                // Set selected scanner 
                _mainPage.setScannerSelection(scannerIndex);

            }
            catch (Exception e)
            {
                _mainPage.updateStatusText("BarcodeManager object creation failed.");
                Console.WriteLine("Exception: " + e.StackTrace);
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
            scannerList = barcodeManager.SupportedDevicesInfo;

            if ((scannerList != null) && (scannerList.Count > 0))
            {
                foreach (ScannerInfo scnInfo in scannerList)
                {
                    friendlyNameList.Add(scnInfo.FriendlyName);

                    // Save index of the default scanner (device specific one)
                    if (scnInfo.IsDefaultScanner)
                    {
                        defaultIndex = spinnerIndex;
                    }

                    ++spinnerIndex;
                }
                RunOnUiThread(() => _mainPage.updateStatusText("Scanner is not there"));

            }
            else
            {
                _mainPage.updateStatusText("Failed to get the list of supported scanner devices! Please close and restart the application.");
            }
            _mainPage.getScannerList(friendlyNameList);



        }
    }

    internal void OnSelectedScannerIndexSelected(string selectedScanner, int selectedIndex)
    {

        if ((scannerIndex != selectedIndex) || (scanner == null))
        {
            scannerIndex = selectedIndex;
            DeInitScanner();
            InitScanner();
            SetTrigger();
            SetDecoders();

        }
    }

    internal void OnSelectedTriggerTypeIndexSelected(string selectedTriggerType, int selectedIndex)
    {
        triggerIndex = selectedIndex;
        SetTrigger();
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

    internal void SetDecoders()
    {
        if (scanner == null)
        {
            InitScanner();
        }

        if ((scanner != null) && (scanner.IsEnabled))
        {
            try
            {
                // Config object should be taken out before changing.
                ScannerConfig config = scanner.GetConfig();

                _mainPage.setDecoderCheckBox(config);
                // Should be assigned back to the property to get the changes to the lower layers.
                scanner.SetConfig(config);
            }
            catch (ScannerException e)
            {
                _mainPage.updateStatusText(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }
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
                _mainPage.updateStatusText("Failed to get the specified scanner device! Please close and restart the application.");
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
                }
                catch (ScannerException e)
                {
                    _mainPage.updateStatusText(e.Message);
                    Console.WriteLine(e.StackTrace);
                }
            }
            else
            {
                _mainPage.updateStatusText("Failed to initialize the scanner device.");
            }
        }
    }

    void scanner_Status(object sender, Scanner.StatusEventArgs e)
    {
        StatusData statusData = e.P0;
        StatusData.ScannerStates state = e.P0.State;

        if (state == StatusData.ScannerStates.Idle)
        {
            statusString = statusData.FriendlyName + " is enabled and idle...";
            RunOnUiThread(() => _mainPage.updateStatusText(statusString));

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
                    statusString = ex.Message;
                    RunOnUiThread(() => _mainPage.updateStatusText(statusString));
                    Console.WriteLine(ex.StackTrace);
                }
                catch (NullReferenceException ex)
                {
                    statusString = "An error has occurred.";
                    RunOnUiThread(() => _mainPage.updateStatusText(statusString));
                    Console.WriteLine(ex.StackTrace);
                }
            }

            RunOnUiThread(() => _mainPage.EnableUIControls(true));
        }

        if (state == StatusData.ScannerStates.Waiting)
        {
            statusString = "Scanner is waiting for trigger press...";
            RunOnUiThread(() =>
            {
                _mainPage.updateStatusText(statusString);
                _mainPage.EnableUIControls(false);
            });
        }

        if (state == StatusData.ScannerStates.Scanning)
        {
            statusString = "Scanning...";
            RunOnUiThread(() =>
            {
                _mainPage.updateStatusText(statusString);
                _mainPage.EnableUIControls(false);
            });
        }

        if (state == StatusData.ScannerStates.Disabled)
        {
            statusString = statusData.FriendlyName + " is disabled.";
            RunOnUiThread(() =>
            {
                _mainPage.updateStatusText(statusString);
                _mainPage.EnableUIControls(true);
            });
        }

        if (state == StatusData.ScannerStates.Error)
        {
            statusString = "An error has occurred.";
            RunOnUiThread(() =>
            {
                _mainPage.updateStatusText(statusString);
                _mainPage.EnableUIControls(true);
            });
        }

    }

    void scanner_Data(object sender, Scanner.DataEventArgs e)
    {
        ScanDataCollection scanDataCollection = e.P0;

        if ((scanDataCollection != null) && (scanDataCollection.Result == ScannerResults.Success))
        {
            IList<ScanDataCollection.ScanData> scanData = scanDataCollection.GetScanData();

            foreach (ScanDataCollection.ScanData data in scanData)
            {
                string dataString = data.Data;
                RunOnUiThread(() => DisplayScanData(dataString));
            }
        }
    }

    private void DisplayScanData(string data)
    {
        if (dataCount++ > 100)
        {
            // Clear the cache after 100 scans
            _mainPage.updateDataText(data, true);
            dataCount = 0;
        }
        _mainPage.updateDataText(data, false);


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
                _mainPage.updateStatusText(e.Message);
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
                _mainPage.updateStatusText(e.Message);
                Console.WriteLine(e.StackTrace);
            }

            scanner = null;
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

    internal void StartButtonEventClicked(object sender, EventArgs e)
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
                    // Set continuous flag
                    isContinuousMode = _mainPage.getContinuousCheckBox();

                    // Submit a new read.
                    scanner.Read();

                    // Disable UI controls
                    RunOnUiThread(() => _mainPage.EnableUIControls(false));
                }
                else
                {
                    _mainPage.updateStatusText("Scanner is not enabled");
                }
            }
            catch (ScannerException ex)
            {
                _mainPage.updateStatusText(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }
    }



    internal void StopButtonEventClicked(object sender, EventArgs e)
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
                RunOnUiThread(() => _mainPage.EnableUIControls(true));
            }
            catch (ScannerException ex)
            {
                _mainPage.updateStatusText(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }
    }

    internal void SetCheckBoxContinuousMode(Boolean continuousMode)
    {
        isContinuousMode = continuousMode;
    }
}


