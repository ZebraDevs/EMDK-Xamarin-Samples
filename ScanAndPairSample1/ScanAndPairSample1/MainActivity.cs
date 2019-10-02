/*
 * Copyright (C) 2016-2019 Zebra Technologies Corporation and/or its affiliates
 * All rights reserved.
 */
using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Symbol.XamarinEMDK;
using Symbol.XamarinEMDK.ScanAndPair;
using System.Collections;

using Java.Lang;
using Java.Util;


namespace ScanAndPairSample1
{
    [Activity(Label = "ScanAndPairSample1", MainLauncher = true, Icon = "@drawable/icon", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait, WindowSoftInputMode =SoftInput.StateHidden)]
    public class MainActivity : Activity, EMDKManager.IEMDKListener
    {
        private EditText btName = null;
        private EditText btAddress = null;
        private CheckBox checkboxHardTrigger = null;
        private CheckBox checkBoxAlwaysScan = null;
        private Button scanAndPairButton = null;
        private Button scanAndUnpairButton = null;
        private Spinner scandataType = null;
        private TextView statusView = null;
        private EMDKManager emdkManager = null;
        ScanAndPairManager scanAndPairMgr = null;
        
        //ScanAndPairManager.StatusEventArgs statusCallbackObj = this;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);
            btName = FindViewById<EditText>(Resource.Id.name);
            btAddress = (EditText)FindViewById(Resource.Id.address);
            checkBoxAlwaysScan = (CheckBox)FindViewById(Resource.Id.alwaysscan);
            checkboxHardTrigger = (CheckBox)FindViewById(Resource.Id.triggerType);
            scanAndPairButton = (Button)FindViewById(Resource.Id.scanandpair);
            scanAndUnpairButton = (Button)FindViewById(Resource.Id.scanandunpair);
            statusView = (TextView)FindViewById(Resource.Id.logs);
            scandataType = (Spinner)FindViewById(Resource.Id.scanDataType);
            statusView.SetText("\n", TextView.BufferType.Normal);
            
            btName.Enabled= false;
            btAddress.Enabled = false;

            // The EMDKManager object creation and object will be returned in the callback.
            EMDKResults results = EMDKManager.GetEMDKManager(ApplicationContext, this);

            // Check the return status of getEMDKManager ()
            if (results.StatusCode == EMDKResults.STATUS_CODE.Success)
            {
                statusView.SetText("Please wait, initialization in progress...", TextView.BufferType.Normal);
            }
            else
            {
                statusView.SetText("Initialization failed!", TextView.BufferType.Normal);
            }
            System.Collections.Generic.List <Symbol.XamarinEMDK.ScanAndPair.ScanAndPairConfig.ScanDataType> scanDataTypes = new System.Collections.Generic.List<ScanAndPairConfig.ScanDataType>();
            scanDataTypes.Add(Symbol.XamarinEMDK.ScanAndPair.ScanAndPairConfig.ScanDataType.MacAddress);
            scanDataTypes.Add(Symbol.XamarinEMDK.ScanAndPair.ScanAndPairConfig.ScanDataType.DeviceName);
            scanDataTypes.Add(Symbol.XamarinEMDK.ScanAndPair.ScanAndPairConfig.ScanDataType.Unspecified);

            ArrayAdapter<Symbol.XamarinEMDK.ScanAndPair.ScanAndPairConfig.ScanDataType> arrayAdapter = new ArrayAdapter<Symbol.XamarinEMDK.ScanAndPair.ScanAndPairConfig.ScanDataType>(ApplicationContext, Resource.Layout.simple_spinner_item, scanDataTypes);
            scandataType.Adapter = arrayAdapter;
            
            registerForButtonEvents();
            addCheckBoxListener();
        }

        private void registerForButtonEvents()
        {
            addScanAndPairButtonEvents();
            addScanAndUnpairButtonEvents();
        }

        private void addScanAndPairButtonEvents()
        {
            scanAndPairButton = (Button)FindViewById(Resource.Id.scanandpair);
            scanAndPairButton.Click += ScanAndPairButton_Click;
        }


        private void ScanAndPairButton_Click(object sender, EventArgs e)
        {
            try
            {
                statusView.SetText("ScanAndPair started..." + "\n", TextView.BufferType.Normal);

                if (scanAndPairMgr == null)
                {
                    scanAndPairMgr = (ScanAndPairManager)emdkManager.GetInstance(Symbol.XamarinEMDK.EMDKManager.FEATURE_TYPE.Scanandpair);
                    

                    if (scanAndPairMgr != null)
                    {
                        
                        scanAndPairMgr.StatusEvent += scanAndPairMgr_StatusEvent;
                                                
                       
                        //    scanAndPairMgr.addStatusListener(statusCallbackObj);                        
                    }
                }

                if (scanAndPairMgr != null)
                {
                    scanAndPairMgr.Config.AlwaysScan =(Java.Lang.Boolean) checkBoxAlwaysScan.Checked;
                    scanAndPairMgr.Config.NotificationType = Symbol.XamarinEMDK.ScanAndPair.ScanAndPairConfig.NotificationTypes.Beeper;
                    if (!checkBoxAlwaysScan.Checked)
                    {
                        scanAndPairMgr.Config.BluetoothInfo.MacAddress = btAddress.Text.ToString().Trim();
                        scanAndPairMgr.Config.BluetoothInfo.DeviceName = btName.Text.ToString().Trim();
                    }
                    else
                    {
                        scanAndPairMgr.Config.ScanInfo.ScanTimeout = 5000;
                        if (checkboxHardTrigger.Checked)
                        {
                            scanAndPairMgr.Config.ScanInfo.TriggerType = Symbol.XamarinEMDK.ScanAndPair.ScanAndPairConfig.TriggerType.Hard;
                        }
                        else
                        {
                            scanAndPairMgr.Config.ScanInfo.TriggerType = Symbol.XamarinEMDK.ScanAndPair.ScanAndPairConfig.TriggerType.Soft;
                        }

                        scanAndPairMgr.Config.ScanInfo.ScanDataType = (Symbol.XamarinEMDK.ScanAndPair.ScanAndPairConfig.ScanDataType)scandataType.SelectedItem;
                    }
                    

                    ScanAndPairResults resultCode = scanAndPairMgr.ScanAndPair("0000");

                    if (!resultCode.Equals(ScanAndPairResults.Success))
                        statusView.Append(resultCode.ToString() + "\n\n");

                }
                else
                {
                    statusView.Append("ScanAndPairmanager intialization failed!");
                }
            }
            catch (Java.Lang.Exception ex)
            {
                statusView.SetText("ScanAndUnpair Error:" + ex.Message + "\n", TextView.BufferType.Normal);
            }
        }

        private void addScanAndUnpairButtonEvents()
        {
            scanAndUnpairButton = (Button)FindViewById(Resource.Id.scanandunpair);
            scanAndUnpairButton.Click += ScanAndUnpairButton_Click;
        }

        private void ScanAndUnpairButton_Click(object sender, EventArgs e)
        {
            try
            {
                statusView.SetText("ScanAndUnpair started..." + "\n", TextView.BufferType.Normal);
                if (scanAndPairMgr == null)
                {
                    scanAndPairMgr = (ScanAndPairManager)emdkManager.GetInstance(Symbol.XamarinEMDK.EMDKManager.FEATURE_TYPE.Scanandpair);
                    if (scanAndPairMgr != null)
                    {
                        scanAndPairMgr.StatusEvent += scanAndPairMgr_StatusEvent;
                    //    scanAndPairMgr.addStatusListener(statusCallbackObj);
                    }
                }

                if (scanAndPairMgr != null)
                {
                    scanAndPairMgr.Config.AlwaysScan =(Java.Lang.Boolean) checkBoxAlwaysScan.Checked;
                    scanAndPairMgr.Config.NotificationType = Symbol.XamarinEMDK.ScanAndPair.ScanAndPairConfig.NotificationTypes.Beeper;
                    if (!checkBoxAlwaysScan.Checked)
                    {
                        scanAndPairMgr.Config.BluetoothInfo.MacAddress = btAddress.Text.ToString().Trim();
                        scanAndPairMgr.Config.BluetoothInfo.DeviceName = btName.Text.ToString().Trim();
                    }
                    else
                    {
                        scanAndPairMgr.Config.ScanInfo.ScanTimeout = 5000;

                        if (checkboxHardTrigger.Checked)
                        {
                            scanAndPairMgr.Config.ScanInfo.TriggerType = Symbol.XamarinEMDK.ScanAndPair.ScanAndPairConfig.TriggerType.Hard;
                        }
                        else
                        {
                            scanAndPairMgr.Config.ScanInfo.TriggerType = Symbol.XamarinEMDK.ScanAndPair.ScanAndPairConfig.TriggerType.Soft;
                        }
                        //scanAndPairMgr.config.scanInfo.deviceIdentifier = DeviceIdentifier.INTERNAL_CAMERA1;
                        scanAndPairMgr.Config.ScanInfo.ScanDataType = (Symbol.XamarinEMDK.ScanAndPair.ScanAndPairConfig.ScanDataType)scandataType.SelectedItem;
                        
                    }
                    
                    ScanAndPairResults resultCode = scanAndPairMgr.ScanAndUnpair();

                    if (!resultCode.Equals(ScanAndPairResults.Success))
                        statusView.Append(resultCode.ToString() + "\n\n");
                }
                else
                {
                    statusView.Append("ScanAndPairmanager intialization failed!");
                }
            }
            catch (Java.Lang.Exception ex)
            {
                statusView.SetText("ScanAndUnpair Error:" + ex.Message + "\n", TextView.BufferType.Normal);
            }
        }

        void scanAndPairMgr_StatusEvent(object sender, ScanAndPairManager.StatusEventArgs e)
        {
            onStatus(e.P0);
        }

        public void onStatus(StatusData statusData)
        {
            StringBuilder text = new StringBuilder();
            bool isUpdateAddress = false;
            switch (statusData.State.ToString().ToUpper())
            {
                case "WAITING":
                    text.Append("Waiting for trigger press to scan the barcode");
                    break;

                case "SCANNING":
                    text.Append("Scanner Beam is on, aim at the barcode.");
                    break;

                case "DISCOVERING":
                    text.Append("Discovering for the Bluetooth device");
                    isUpdateAddress = true;
                    break;

                case "PAIRED":
                    text.Append("Bluetooth device is paired successfully");
                    break;

                case "UNPAIRED":
                    text.Append("Bluetooth device is un-paired successfully");
                    break;

                default:
                case "ERROR":
                    text.Append("\n" + statusData.GetType().Name + ": " + statusData.Result);
                    break;
            }

            bool isUpdateUI = isUpdateAddress;
            RunOnUiThread(()=>{
                statusView.SetText(text + "\n", TextView.BufferType.Normal);
                if (isUpdateUI)
                {
                    btName.SetText(scanAndPairMgr.Config.BluetoothInfo.DeviceName, TextView.BufferType.Normal);
                    btAddress.SetText(scanAndPairMgr.Config.BluetoothInfo.MacAddress, TextView.BufferType.Normal);
                }
            });
        }

        private void addCheckBoxListener()
        {
            checkBoxAlwaysScan.Click += CheckBoxAlwaysScan_Click;
        }

        private void CheckBoxAlwaysScan_Click(object sender, EventArgs e)
        {
            if (checkBoxAlwaysScan.Checked)
            {
                btName.Enabled=false;
                btAddress.Enabled = false;
            }
            else
            {
                btName.Enabled = true;
                btAddress.Enabled = true;
            }
            
        }

        public void OnClosed()
        {
            RunOnUiThread(() =>
            {
                statusView.SetText("Error!! Restart the application!!", TextView.BufferType.Normal);
            });
        }

        public void OnOpened(EMDKManager emdkManager)
        {
            this.emdkManager = emdkManager;
            RunOnUiThread(() =>
            {
                statusView.SetText("Application Initialized.", TextView.BufferType.Normal);
            });
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (emdkManager != null)
            {
                // Clean up the objects created by EMDK manager
                emdkManager.Release();
                emdkManager = null;
            }
        }
     }
}

