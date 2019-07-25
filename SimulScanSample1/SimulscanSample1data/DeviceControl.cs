/*
* Copyright (C) 2015-2017 Zebra Technologies Corp
* All rights reserved.
*/

using Java.Util;
using Symbol.XamarinEMDK.SimulScan;
using Symbol.XamarinEMDK;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Util;

using Android.Net;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Symbol.XamarinEMDK.SimulScanSample1;
using System.Threading;
using System;
using Symbol.XamarinEMDK.SerialComm;


public class DeviceControl : Fragment, EMDKManager.IEMDKListener
{

    private static string TAG = typeof(DeviceControl).Name;

    private static TextView textViewStatus = null;
    private Spinner spinner2 = null;
    private Button readBtn = null;
    private Button stopReadBtn = null;
    private EMDKManager emdkManager = null;
    
    private SimulScanManager simulscanManager = null;
    IList<SimulScanReaderInfo> readerInfoList = null;
    // DeviceIdentifier selectedDeviceIdentifier = DeviceIdentifier.DEFAULT;
    EMDKResults results;
    SimulScanReader selectedSimulScanReader = null;
    //public class SynchronizedCollection<SimulScanData> : IList<SimulScanData> { }
    //Collections.SynchronizedList(<SimulScanData>) list = new Collections.SynchronizedList<SimulScanData>(new IgnoreLocking());
    //IList<SimulScanData> myList = new List<SimulScanData>();

    //myList = new SynchronizedCollection<int>(); // using the common interface IList<T>
    public static System.Collections.IList simulscanDataList = Collections.SynchronizedList(new List<SimulScanData>());
    SimulScanException lastException;
    // private List<SimulScanReaderInfo> deviceList = null;
    private int readerIndex = 0;

    private string statusString = "";


    public override void OnCreate(Bundle savedInstanceState) {
        base.OnCreate(savedInstanceState);
        Log.Debug(TAG, "SSC onCreate");
        // getActivity().setRequestedOrientation(ActivityInfo.SCREEN_ORIENTATION_PORTRAIT);
        results = EMDKManager.GetEMDKManager(Activity.ApplicationContext, this);

                
    }
    public override Android.Views.View OnCreateView(Android.Views.LayoutInflater inflater, Android.Views.ViewGroup container, Android.OS.Bundle savedInstanceState){
        Log.Debug(TAG, "SSC onCreateView");

        View rootView = inflater.Inflate(Resource.Layout.fragment_main, container,
                false);
       // textViewStatus = FindViewById<TextView>(Resource.Id.textViewStatus) as TextView;
        textViewStatus = (TextView) rootView.FindViewById(Resource.Id.textView1);
        textViewStatus.Text ="Status: " + " Starting..";

        
        spinner2 = (Spinner) rootView.FindViewById(Resource.Id.spinner2);
        spinner2.ItemSelected+=spinner2_ItemSelected;

        readBtn = (Button) rootView.FindViewById(Resource.Id.button4);
        readBtn.Click +=readBtn_Click;

        stopReadBtn = (Button) rootView.FindViewById(Resource.Id.button5);
        stopReadBtn.Click +=stopReadBtn_Click;

        if (results.StatusCode != EMDKResults.STATUS_CODE.Success) {
            Log.Error(TAG, "EMDKManager object request failed!");
            textViewStatus.Text = "Status: "
                    + "EMDKManager object request failed!";
        }

        return rootView;
 	}

    void stopReadBtn_Click(object sender, System.EventArgs e){
        Log.Debug(TAG, "Stop Read clicked");
        try {
        stopReadCurrentScanner();
        } catch (SimulScanException ex) {
        lastException = ex;
        textViewStatus.Text = "Status: " + lastException.Message;
        Log.Error(TAG, "Exception while cancelling read: " + ex.Message);
        ex.PrintStackTrace();
        }
    }
    

    void readBtn_Click(object sender, System.EventArgs e){
 	   
        Log.Debug(TAG, "Read clicked");
        try {
           readCurrentScanner();
        } catch (SimulScanException ex) {
        lastException = ex;
        textViewStatus.Text = "Status: " + lastException.Message;
        Log.Error(TAG, "Exception while starting read: " + ex.Message);
        ex.PrintStackTrace();
        }
    }

    void spinner2_ItemSelected(object sender, Android.Widget.AdapterView.ItemSelectedEventArgs e){
        if (sender.Equals(spinner2)) { 
            prepareScanner(e.Position);
        }
    }
 
    public override void OnStart() {
        base.OnStart();
        Log.Debug(TAG, "SSC onStart");
        if (selectedSimulScanReader != null)
            try {
                if (!selectedSimulScanReader.IsEnabled)
                    selectedSimulScanReader.Enable();
            } catch (SimulScanException e) {
                Log.Error(TAG, "Error enabling reader: " + e.Message);
                e.PrintStackTrace();
                textViewStatus.Text = "Status: " + "Error enabling reader" ;
            }
    }

    public override void OnResume() {
        Log.Debug(TAG, "SSC onResume");
        base.OnResume();
        // The application is in foreground

        // Acquire the SimulScan manager resources
        if (emdkManager != null) {
            simulscanManager = (SimulScanManager) emdkManager
                    .GetInstance(Symbol.XamarinEMDK.EMDKManager.FEATURE_TYPE.Simulscan);

            // Initialize scanner
            try {
                if (simulscanManager != null) {
                    prepareScanner(spinner2.SelectedItemPosition);
                } else {
                    Log.Debug(TAG, "SSC onResume simulscanManager is null");
                }
            } catch (SimulScanException e) {
                // TODO Auto-generated catch block
                e.PrintStackTrace();
            }
        }
    }

    public override void OnPause() {
        Log.Debug(TAG, "SSC onPause");
        base.OnPause();
        // The application is in background

        // De-initialize scanner
        try {
            deinitCurrentScanner();
        } catch (SimulScanException e) {
            // TODO Auto-generated catch block
            e.PrintStackTrace();
        }

        // Remove connection listener
        if (simulscanManager != null) {
            simulscanManager = null;
        }

        // Release the SimulScan manager resources
        if (emdkManager != null) {
            emdkManager.Release(Symbol.XamarinEMDK.EMDKManager.FEATURE_TYPE.Simulscan);
        }

    }

    
    public override void OnStop() {
        Log.Debug(TAG, "SSC onStop");
        if (selectedSimulScanReader != null) {
            if ((bool)selectedSimulScanReader.IsReadPending()) {
                try {
                    selectedSimulScanReader.CancelRead();
                } catch (SimulScanException e) {
                    Log.Error(TAG, "Error stopping reader: " + e.Message);
                    e.PrintStackTrace();
                }
            }
            try {
                if (selectedSimulScanReader.IsEnabled) {
                    selectedSimulScanReader.Disable();
                }
            } catch (SimulScanException e) {
                Log.Error(TAG, "Error disabling reader: " + e.Message);
                e.PrintStackTrace();
            }
        }
        base.OnStop();
    }

    void selectedSimulScanReader_Data(object sender, SimulScanReader.DataEventArgs e)
    {
        // TODO Auto-generated method stub
        SimulScanData scanData = e.P0;
        Log.Verbose(TAG, "onData");
       Intent intent = new Intent(Activity, typeof(ResultsActivity));
        lock (simulscanDataList)
        {
            simulscanDataList.Add(scanData);
        }
        StartActivity(intent);
    }

    public SimulScanStatusData m_simulScanStatusData;
    void selectedSimulScanReader_Status(object sender, SimulScanReader.StatusEventArgs e) 
    {
        SimulScanStatusData statusData = e.P0;
        SimulScanStatusData.SimulScanStatus state = e.P0.State;

        m_simulScanStatusData = statusData;
        textViewStatus.Post(new Action(RunStatusDataRunnable));

    }

    public void RunStatusDataRunnable()
    {
        new StatusDataRunnable(m_simulScanStatusData);
    }

    public override void OnDestroy() {
        base.OnDestroy();

        Log.Debug(TAG, "SSC onDestroy");

        if (selectedSimulScanReader != null) {

            
            selectedSimulScanReader.Data -= selectedSimulScanReader_Data;
            //selectedSimulScanReader.RemoveDataListener((SimulScanReader.IDataListerner)this);
            selectedSimulScanReader.Status -= selectedSimulScanReader_Status;
            //selectedSimulScanReader.RemoveStatusListener((SimulScanReader.IStatusListerner)this);
        }

        if (simulscanManager != null) {
            // simulscanManager.release();
            emdkManager.Release(Symbol.XamarinEMDK.EMDKManager.FEATURE_TYPE.Simulscan);
            
            simulscanManager = null;
        }

        if (emdkManager != null) {
            emdkManager.Release();
            emdkManager = null;
        }
    }

    private void addItemsOnSpinner(Spinner spinner, List<string> list) {

        ArrayAdapter<string> dataAdapter = new ArrayAdapter<string>(
                Activity, Android.Resource.Layout.SimpleSpinnerItem, list);
        dataAdapter
                .SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
        spinner.Adapter = dataAdapter;
    }


    public void deinitCurrentScanner()  {
        try {
        if (selectedSimulScanReader != null) {
            if ((bool)selectedSimulScanReader.IsReadPending())
                selectedSimulScanReader.CancelRead();
            if (selectedSimulScanReader.IsEnabled)
                selectedSimulScanReader.Disable();
            selectedSimulScanReader.Data -= selectedSimulScanReader_Data;
           // selectedSimulScanReader.RemoveDataListener((SimulScanReader.IDataListerner)this);
            selectedSimulScanReader.Status -= selectedSimulScanReader_Status;
           // selectedSimulScanReader.RemoveStatusListener((SimulScanReader.IStatusListerner)this);
            selectedSimulScanReader = null;
        }
        } catch (SimulScanException e) {
            e.PrintStackTrace();
        }
    }

    public void initCurrentScanner()  {
        try {

            selectedSimulScanReader.Data += selectedSimulScanReader_Data;
            selectedSimulScanReader.Status += selectedSimulScanReader_Status;
            //selectedSimulScanReader.AddStatusListener((SimulScanReader.IStatusListerner)this);
            //selectedSimulScanReader.AddDataListener((SimulScanReader.IDataListerner)this);
            if(!selectedSimulScanReader.IsEnabled)
            selectedSimulScanReader.Enable();
        } catch (SimulScanException e) {
            e.PrintStackTrace();
        }   
    }

    public void prepareScanner(int pos) {
        if (simulscanManager != null) {
            SimulScanReaderInfo readerInfo =(SimulScanReaderInfo) readerInfoList[pos];
            
            if (readerInfo != null) {
                Log.Debug(TAG, "onItemSelected:" + readerInfo.FriendlyName);
                if (readerIndex != pos) {
                    readerIndex = pos;
                }
//				if (readerInfo.getDeviceIdentifier() != selectedDeviceIdentifier) {
//					selectedDeviceIdentifier = readerInfo.getDeviceIdentifier();
                try {
                    deinitCurrentScanner();
                    //SimulScanReaderInfo readerInfo1 = (SimulScanReaderInfo)readerInfoList[readerIndex];
                    //selectedSimulScanReader = simulscanManager.GetDevice(readerInfo1);
                    selectedSimulScanReader = simulscanManager.GetDevice(readerInfoList[readerIndex]);
                    initCurrentScanner();
                } catch (SimulScanException e) {
                    Log.Error(TAG, "Error enabling reader: " + e.Message);
                    e.PrintStackTrace();
                    textViewStatus.Text = "Status: " + "Error enabling reader";
                }
            }

        }
    }

    public void onNothingSelected(AdapterView parent) {
      
            Log.Debug(TAG, "onNothingSelected");
            try {
                deinitCurrentScanner();
            } catch (SimulScanException e) {
                Log.Error(TAG, "Error disabling reader: " + e.Message);
                e.PrintStackTrace();    
            }
        
    }
    
    public void OnOpened(EMDKManager emdkManager) {
        Log.Debug(TAG, "onOpened");
        textViewStatus.Text = "Status: " + "EMDK open success!";
        this.emdkManager = emdkManager;
        simulscanManager = (SimulScanManager) this.emdkManager
                .GetInstance(Symbol.XamarinEMDK.EMDKManager.FEATURE_TYPE.Simulscan);
        if (null == simulscanManager) {
            Log.Error(TAG, "Get SimulScanManager instance failed!");
            textViewStatus.Text = "Status: "
                    + "Get SimulScanManager instance failed!";
            return;
        }

        readerInfoList = simulscanManager.SupportedDevicesInfo;
        List<string> nameList = new List<string>();
        foreach (SimulScanReaderInfo rinfo in readerInfoList) {
            nameList.Add(rinfo.FriendlyName);
        }
        addItemsOnSpinner(spinner2, nameList);

        spinner2.ItemSelected += spinner2_ItemSelected;
        readerIndex = 0;
        try {
            selectedSimulScanReader = simulscanManager.GetDevice((SimulScanReaderInfo)readerInfoList[readerIndex]);
            initCurrentScanner();
        } catch (SimulScanException e) {
            // TODO Auto-generated catch block
            e.PrintStackTrace();
        }

    }


    public void OnClosed()
    {
        Log.Debug(TAG, "onClosed: EMDK closed unexpectedly");

        emdkManager.Release();
        emdkManager = null;

        textViewStatus.SetText("Status: " + "EMDK closed unexpectedly!", TextView.BufferType.Normal);
    }

    public class StatusDataRunnable {
        SimulScanStatusData statusData = null;

        public StatusDataRunnable(SimulScanStatusData statusData)
        {
            this.statusData = statusData;
            run();
        }

         void StatusDataRunnableRun() 
         {
            Thread newThread = new Thread(new ThreadStart(run));
            newThread.Start(); 
         }
        
        public void run() {
            if (statusData != null) {
                switch (statusData.State.Name()) {
                    case "DISABLED":
                        textViewStatus.SetText("Status: "
                                + statusData.FriendlyName
                                + ": Closed reader successfully", TextView.BufferType.Normal);
                        Log.Debug(TAG, "onDisabled");
                        break;
                    case "ENABLED":
                        textViewStatus.SetText("Status: "
                                + statusData.FriendlyName
                                + ": Opened reader successfully", TextView.BufferType.Normal);
                        Log.Debug(TAG, "onEnabled");
                        break;
                    case "SCANNING":
                        textViewStatus.SetText("Status: "
                                + statusData.FriendlyName
                                + ": Started reader successfully", TextView.BufferType.Normal);
                        Log.Debug(TAG, "Scanning");
                        break;
                    case "IDLE":
                        textViewStatus.SetText("Status: "
                                + statusData.FriendlyName
                                + ": Stopped reader successfully", TextView.BufferType.Normal);
                        Log.Debug(TAG, "Idle");
                        break;
                    case "ERROR":
                        textViewStatus.SetText("Status: "
                                + statusData.FriendlyName + ": Error-"
                                + statusData.State, TextView.BufferType.Normal);
                        Log.Error(TAG, "ERROR: " + statusData.FriendlyName
                                + ": Error-" + statusData.State);
                        break;
                    case "UNKNOWN":
                    default:
                        break;
                }
            }
        }
    }

    private void setCurrentConfig() {
        try {
            if (selectedSimulScanReader != null) {
                SimulScanConfig config = selectedSimulScanReader.Config;
                if (config != null) {

                    // set template
                    if (config.MultiTemplate != null) {
                        Log.Debug(TAG,
                                "Conf template name: "
                                        + config.MultiTemplate.TemplateName);
                    } else
                        Log.Debug(TAG, "Conf template is null");

                    MainActivity parentActivity = (MainActivity) Activity;
                    if ((parentActivity.localSettings.fileList == null)
                            || (parentActivity.localSettings.fileList.Count == 0))
                    {
                        Log.Error(TAG, "Invalid template Path");
                       // throw new Exception();
                    }

                    Log.Debug(TAG, "Template index: "
                            + parentActivity.localSettings.selectedFileIndex);
                    /*Java.IO.File PrintingFile;
                    for(int counter=0;counter<=parentActivity.localSettings.selectedFileIndex;counter++)
                    {
                        //if(fileList1[counter])

                    }*/
                    SimulScanMultiTemplate multiTemplate;
                    multiTemplate = new SimulScanMultiTemplate(
                            simulscanManager,
                          Android.Net.Uri.FromFile(parentActivity.localSettings.fileList[parentActivity.localSettings.selectedFileIndex]));

                    if (multiTemplate != null)
                        config.MultiTemplate = multiTemplate;

                    config.AutoCapture = (Java.Lang.Boolean)parentActivity.localSettings.enableAutoCapture;
                    config.DebugMode = (Java.Lang.Boolean)parentActivity.localSettings.enableDebugMode;
                    config.AudioFeedback = (Java.Lang.Boolean)parentActivity.localSettings.enableFeedbackAudio;
                    config.HapticFeedback = (Java.Lang.Boolean)parentActivity.localSettings.enableHaptic;
                    config.LedFeedback = (Java.Lang.Boolean)parentActivity.localSettings.enableLED;
                    config.UserConfirmationOnScan = (Java.Lang.Boolean)parentActivity.localSettings.enableResultConfirmation;
                    config.IdentificationTimeout = parentActivity.localSettings.identificationTimeout;
                    config.ProcessingTimeout = parentActivity.localSettings.processingTimeout;
                    

                    selectedSimulScanReader.Config = config;
                }
            }
        } catch (SimulScanException e) {
            e.PrintStackTrace();
        } 
    }

    public void readCurrentScanner()
    {

        setCurrentConfig();
        if (selectedSimulScanReader != null)
        {
            selectedSimulScanReader.Read();
        }
    }
       

    public void stopReadCurrentScanner()  {
  
        if (selectedSimulScanReader != null)
            selectedSimulScanReader.CancelRead();
   
    }
    
}