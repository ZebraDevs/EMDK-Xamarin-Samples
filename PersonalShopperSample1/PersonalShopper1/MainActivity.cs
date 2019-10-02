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
using Symbol.XamarinEMDK.PersonalShopper;
using PersonalShopperSample1;


namespace EMDKXamarinPersonalShopper
{
    [Activity(Label = "PersonalShopperSample1", MainLauncher = true, Icon = "@drawable/icon", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
    public class MainActivity : Activity, EMDKManager.IEMDKListener
    {
        private EMDKManager emdkManager = null;
        private PersonalShopperMgr PsObject = null;
        bool mLedsmooth = false;
        private TextView textViewStatus = null;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);
            textViewStatus = FindViewById<TextView>(Resource.Id.PStextView);

            EMDKResults results = EMDKManager.GetEMDKManager(ApplicationContext, this);
            if (results.StatusCode != EMDKResults.STATUS_CODE.Success)
            {
                textViewStatus.Text = string.Empty;
                textViewStatus.SetText("Status: " + "Failed in getEMDKManager::" + results.StatusCode, TextView.BufferType.Normal);
            }
            else
            {
                textViewStatus.Text = string.Empty;
                textViewStatus.SetText("Status: " + "getEMDKManager Success", TextView.BufferType.Normal);
            }

            addCrdInfoButtonListener();
            addbtnUnlockButtonListener();
            addFlashLedsButtonListener();
            addFCCheckboxListener();
            addDiagnosticButtonListener();

        }

        private void addDiagnosticButtonListener()
        {
            Button btnSetCfgLeds = FindViewById<Button>(Resource.Id.Diagnosticdata);
            btnSetCfgLeds.Click += DiagnosticButton_Click;
        }

        private void DiagnosticButton_Click(object sender, EventArgs e)
        {
            textViewStatus.Text = string.Empty;
            getDiagnosticData();
        }

        private void getDiagnosticData()
        {
            DiagnosticData diagnosticData = null;
            DiagnosticParamId diagnosticparamID = new DiagnosticParamId();
            int paramId = DiagnosticParamId.All;

            DiagnosticConfig diagnosticconfig = new DiagnosticConfig(200, 60);

            if (null != PsObject.Diagnostic)
            {
                try
                {
                    diagnosticData = PsObject.Diagnostic.GetDiagnosticData(paramId, diagnosticconfig);
                }
                catch (DiagnosticException e)
                {
                    e.PrintStackTrace();
                    textViewStatus.SetText("Status: " + e.Message, TextView.BufferType.Normal);
                }
                if (diagnosticData != null)
                {
                    showMessage("\n Battery Capacity in Per : " + diagnosticData.BatteryStateOfCharge
                                + "\n Battery Capacity in mins: " + diagnosticData.BatteryTimeToEmpty
                                + "\n Battery SOH in Per: " + diagnosticData.BatteryStateOfHealth
                                + "\n Battery Charging Time Required mins: " + diagnosticData.BatteryChargingTime
                                + "\n Battery Replacement in days: " + diagnosticData.TimeSinceBatteryReplaced
                                + "\n Time since Last reboot  mins: " + diagnosticData.TimeSinceReboot
                                + "\n Battery Charging Time Elapsed mins: " + diagnosticData.BatteryChargingTimeElapsed
                                + "\n Manufacturing Date: " + diagnosticData.BatteryDateOfManufacture, ToastLength.Long
                            );
                }
            }

        }

        private void addSmmothCheckboxListener()
        {
		    if(null!=PsObject.Cradle){
                    CheckBox mFCState = FindViewById<CheckBox>(Resource.Id.checkBox1);
                    mLedsmooth = mFCState.Checked;
                }
        }

        private void addFCCheckboxListener()
        {
            CheckBox mFCState = FindViewById<CheckBox>(Resource.Id.ChecBoxfastcharge);
            
            mFCState.Click += mFCState_Click;
        }

        private void mFCState_Click(object sender, EventArgs e)
        {
            textViewStatus.Text = string.Empty;
            if (null != PsObject.Cradle)
            {
                try
                {
                    if (((CheckBox)(sender)).Checked)
                    {
                        PsObject.Cradle.Config.SetFastChargingState(true);
                        if (PsObject.Cradle.Config.FastChargingState)
                        {
                            textViewStatus.Text = string.Empty;
                            textViewStatus.SetText("Status: " + "fast charge enabled", TextView.BufferType.Normal);
                        }
                        else
                        {
                            textViewStatus.Text = string.Empty;
                            textViewStatus.SetText("Status: " + "fast charge enabling failed", TextView.BufferType.Normal);
                        }
                    }
                    else
                    {
                        PsObject.Cradle.Config.SetFastChargingState(false);
                        if (!(PsObject.Cradle.Config.FastChargingState))
                        {
                            textViewStatus.Text = string.Empty;
                            textViewStatus.SetText("Status: " + "fast charge disabled", TextView.BufferType.Normal);
                        }
                        else
                        {
                            textViewStatus.Text = string.Empty;
                            textViewStatus.SetText("Status: " + "fast charge disabling failed", TextView.BufferType.Normal);
                        }
                    }
                }
                catch (CradleException ex)
                {
                    ex.PrintStackTrace();
                    textViewStatus.Text = string.Empty;
                    textViewStatus.SetText("Status: " + ex.Message, TextView.BufferType.Normal);
                }
            }

        }


        public void showMessage(String text)
        {
            Toast toast = Toast.MakeText(ApplicationContext, text, ToastLength.Long);
            toast.Show();
        }

        public void showMessage(String text, ToastLength duration)
        {
            Toast toast = Toast.MakeText(ApplicationContext, text, duration);
            toast.Show();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            disable();
            if (PsObject != null)
            {
                PsObject = null;
            }

            if (emdkManager != null)
            {
                emdkManager.Release();
                emdkManager = null;
            }
        }

        public void OnClosed()
        {
            emdkManager.Release();
        }

        void addFlashLedsButtonListener()
        {
            Button btnFlashLeds = FindViewById<Button>(Resource.Id.FlashLEDButton1);
            btnFlashLeds.Click += BtnFlashLeds_Click;

        }

        private void BtnFlashLeds_Click(object sender, EventArgs e)
        {
            textViewStatus.Text = string.Empty;
            flashLeds();
        }

        private void flashLeds()
        {
            if (null != PsObject.Cradle)
            {
                int onDuration = 2000;
                int offDuration = 1000;
                int flashCount = 5;

                try
                {
                    addSmmothCheckboxListener();
                    CradleLedFlashInfo ledFlashInfo = new CradleLedFlashInfo(onDuration, offDuration, mLedsmooth);
                    CradleResults result = PsObject.Cradle.FlashLed(flashCount, ledFlashInfo);
                    if (result == CradleResults.Success)
                    {
                        textViewStatus.Text = string.Empty;
                        textViewStatus.SetText("Status: " + "Flashed LEDs ", TextView.BufferType.Normal);
                    }
                    else
                    {
                        textViewStatus.Text = string.Empty;
                        textViewStatus.SetText("Status: " + "Failed error " + result.Description, TextView.BufferType.Normal);
                    }
                }
                catch (CradleException e)
                {
                    // TODO Auto-generated catch block
                    e.PrintStackTrace();
                    textViewStatus.Text = string.Empty;
                    textViewStatus.SetText("Status: " + e.Message, TextView.BufferType.Normal);
                }
            }
        }

        private void addbtnUnlockButtonListener()
        {
            Button btnUnlock = FindViewById<Button>(Resource.Id.UnlockButton1);
            btnUnlock.Click += BtnUnlock_Click;
            
        }

        private void BtnUnlock_Click(object sender, EventArgs e)
        {
            textViewStatus.Text = string.Empty;
            unlock();
        }

        private void unlock()
        {
            if (null != PsObject.Cradle)
            {
                int onDuration = 500;
                int offDuration = 500;
                int unlockDuration = 15;

                try
                {
                    addSmmothCheckboxListener();
                    CradleLedFlashInfo ledFlashInfo = new CradleLedFlashInfo(onDuration, offDuration, mLedsmooth);
                    CradleResults result = PsObject.Cradle.Unlock(unlockDuration, ledFlashInfo);
                    if (result == CradleResults.Success)
                    {
                        textViewStatus.Text = string.Empty;
                        textViewStatus.SetText("Status: " + "Unlocked", TextView.BufferType.Normal);
                    }
                    else
                    {
                        textViewStatus.Text = string.Empty;
                        textViewStatus.SetText("Status: " + "Failed error " + result.Description, TextView.BufferType.Normal);
                    }
                }
                catch (CradleException e)
                {
                    // TODO Auto-generated catch block
                    e.PrintStackTrace();
                    textViewStatus.Text = string.Empty;
                    textViewStatus.SetText("Status: " + e.Message, TextView.BufferType.Normal);
                }
            }
        }

        private void addCrdInfoButtonListener()
        {
            Button btnCrdInfo = FindViewById<Button>(Resource.Id.CradleInfoButton1);
            btnCrdInfo.Click += BtnCrdInfo_Click;
	    }

        private void BtnCrdInfo_Click(object sender, EventArgs e)
        {
            textViewStatus.Text = string.Empty;
            getCradleInfo();
        }

        private void getCradleInfo()
        {
            CradleInfo cradleInfo = null;
            if (null != PsObject.Cradle)
            {
                try
                {
                    cradleInfo = PsObject.Cradle.CradleInfo;
                }
                catch (CradleException e)
                {
                    e.PrintStackTrace();
                    textViewStatus.Text = string.Empty;
                    textViewStatus.SetText("Status: " + e.Message, TextView.BufferType.Normal);
                }
                if (cradleInfo != null)
                {
                 //   Log.d("PartNo", "part No=" + cradleInfo.getPartNumber());
                    showMessage("FirmwareVersion: " + cradleInfo.FirmwareVersion
                                + "\nDateOfManufacturing: " + cradleInfo.DateOfManufacture
                                + "\nHardwareID: " + cradleInfo.HardwareID
                                + "\nPartnumber: " + cradleInfo.PartNumber
                                + "\nSerialNumber: " + cradleInfo.SerialNumber, ToastLength.Long
                            );
                }
            }
        }

        protected void disable()
        {
            try
            {
                if (PsObject != null)
                {
                    if (null != PsObject.Cradle)
                    {
                        PsObject.Cradle.Disable();
                    }
                }
            }
            catch (CradleException e)
            {
                // TODO Auto-generated catch block
                e.PrintStackTrace();
                textViewStatus.Text = string.Empty;
                textViewStatus.SetText("Status: " + e.Message, TextView.BufferType.Normal);
            }
        }

        protected void enable()
        {
            try
            {
                if (null != PsObject.Cradle)
                {
                    PsObject.Cradle.Enable();
                }
            }
            catch (CradleException e)
            {
                // TODO Auto-generated catch block
                e.PrintStackTrace();
                textViewStatus.Text = string.Empty;
                textViewStatus.SetText("Status: " + e.Message, TextView.BufferType.Normal);
            }
        }

        public void OnOpened(EMDKManager emdkManager)
        {
            this.emdkManager = emdkManager;
            try
            {
                PsObject = (PersonalShopperMgr)this.emdkManager.GetInstance(Symbol.XamarinEMDK.EMDKManager.FEATURE_TYPE.Personalshopper);
            }
            catch (Exception e)
            {
                textViewStatus.Text = string.Empty;
                textViewStatus.SetText("Status: " + e.Message, TextView.BufferType.Normal);
            }

            if (PsObject == null)
            {
                Toast.MakeText(this, "PersonalShopper feature is NOT supported", ToastLength.Short).Show();
                Finish();
            }
            else
            {
                enable();
            }
        }
    }
}

