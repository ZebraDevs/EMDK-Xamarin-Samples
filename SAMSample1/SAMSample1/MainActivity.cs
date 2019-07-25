/*
 * Copyright (C) 2019 Zebra Technologies Corporation and/or its affiliates.
 * All rights reserved.
 */

using Android.App;
using Android.Widget;
using Android.OS;
using System.Collections.Generic;
using System;
using Android.Nfc;
using System.Collections;
using Android.Content;
using Android.Nfc.Tech;
using Java.Lang;

using Symbol.XamarinEMDK.Sam;

namespace Symbol.XamarinEMDK.SAMSample1
{
    [Activity(Label = "SAMSample1", MainLauncher = true, Icon = "@drawable/icon", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
    public class MainActivity : Activity, EMDKManager.IEMDKListener
    {
        private static string TAG = typeof(MainActivity).Name;
        //Declare a variable to store EMDKManager object
        private EMDKManager emdkManager = null;
        private SAMManager samManager = null;
        bool tagOperationInProgress = false;
        private Button btnGetSAMInfo = null;
        private Dictionary<int, SAM> presetSAMList = new Dictionary<int, SAM>();
        private PendingIntent nfcIntent = null;
        private NfcAdapter nfcAdapter = null;
        private TextView txtStatus = null;
        private Dictionary<SAMType, byte[]> getVersionAPDUs = new Dictionary<SAMType, byte[]>();
        private string detectedTag = "";

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Main);

            txtStatus = (TextView)FindViewById(Resource.Id.txtStatus);
            btnGetSAMInfo = (Button)FindViewById(Resource.Id.btnGetSAMInfo);
            btnGetSAMInfo.Click += onClickGetSAMInfo;

            getVersionAPDUs.Add(SAMType.Mifare, new byte[] { (byte)0x80, (byte)0x60, (byte)0x00, (byte)0x00,/*(byte)0x00,*/(byte)0x00 });
            getVersionAPDUs.Add(SAMType.Felica, new byte[] { (byte)0xA0, (byte)0x00, (byte)0x00, (byte)0x00, (byte)0x05, (byte)0x00, (byte)0x00, (byte)0x00, (byte)0xE6, (byte)0x00, (byte)0x00 });

            nfcAdapter = NfcAdapter.GetDefaultAdapter(this);

            if (nfcAdapter == null)
            {
                Toast.MakeText(this, GetString(Resource.String.message_nfc_not_supported), ToastLength.Long).Show();
                Finish();
                return;
            }

            nfcIntent = PendingIntent.GetActivity(this, 0,
                    new Intent(this, this.Class)
                            .AddFlags(ActivityFlags.SingleTop), 0);

            //The EMDKManager object will be created and returned in the callback.
            EMDKResults results = EMDKManager.GetEMDKManager(Application.Context, this);

            //Check the return status of EMDKManager object creation.
            if (results.StatusCode == EMDKResults.STATUS_CODE.Success)
            {
                //EMDKManager object creation success
            }
            else
            {
                //EMDKManager object creation failed
            }

        }

        public void OnOpened(EMDKManager emdkManager)
        {
            this.emdkManager = emdkManager;
            initSAMManager();
            enumerateSAMsAndGetInfo();
        }

        void enableDisableUIComponents(bool enabled)
        {
            RunOnUiThread(() => btnGetSAMInfo.Enabled = enabled);
        }

        private void enumerateSAMsAndGetInfo()
        {
            if (samManager != null)
            {
                RunOnUiThread(() => txtStatus.Text = "");
                IList<SAM> samList = null;
                presetSAMList.Clear();
                try
                {
                    samList = samManager.EnumerateSAMs();
                }
                catch (SAMException ex)
                {
                    updateStatus(GetString(Resource.String.message_sam_exception_enumerate) + " " + SAMResults.GetErrorDescription(ex.Result));
                    return;
                }

                if ((samList != null) && (samList.Count != 0))
                {
                    int i = 0;
                    foreach (SAM sam in samList)
                    {
                        presetSAMList.Add(sam.SamIndex, sam);
                        getSAMInfo(sam);
                        i++;
                    }
                    enableDisableUIComponents(true);
                }
                else
                {
                    enableDisableUIComponents(false);
                    updateStatus(GetString(Resource.String.message_failed_to_get_sams));
                }
            }
        }

        public void onClickGetSAMInfo(object sender, EventArgs e)
        {
            enumerateSAMsAndGetInfo();
        }

        void updateStatus(string s)
        {
            RunOnUiThread(() =>
                {
                    string text = txtStatus.Text;
                    text = s + "\n\n" + text;
                    if (text.Length > 1000)
                        text = s;

                    txtStatus.Text = text;
                }
            );
        }

        public static string getHexString(byte[] buf)
        {
            StringBuffer sb = new StringBuffer();
            foreach (byte b in buf)
            {
                sb.Append(Java.Lang.String.Format("0x%02x", b));
            }
            return sb.ToString();
        }

        private void getSAMInfo(SAM sam)
        {

            string text = "";
            if (sam != null)
            {
                long tick = DateTime.Now.Ticks;

                /** Connect [Start] */
                try
                {
                    if (!sam.IsConnected)
                    {
                        sam.Connect();
                        text += GetString(Resource.String.message_sam_connected_successfully) + " " + sam.SamType + "(Slot " + sam.SamIndex + ")\n";
                    }
                }
                catch (SAMException ex)
                {
                    updateStatus(GetString(Resource.String.message_connect_error) + " " + SAMResults.GetErrorDescription(ex.Result));
                    return;
                }
                /** Connect [End] */

                /** Transceive [Start] */
                byte[] getVersionAPDU = null;
                getVersionAPDUs.TryGetValue(sam.SamType, out getVersionAPDU);
                byte[] response = null;
                try
                {
                    text += GetString(Resource.String.message_transceive) + "\n";
                    response = sam.Transceive(getVersionAPDU, (short)0, false);
                    if (response != null)
                    {
                        text += GetString(Resource.String.version) + " " + getHexString(response) + "\n";
                    }
                    else
                    {
                        text += GetString(Resource.String.version_error) + "\n";
                    }
                }
                catch (SAMException ex)
                {
                    text += GetString(Resource.String.message_transceive_failed) + " " + SAMResults.GetErrorDescription(ex.Result) + "\n";
                }
                /**Transceive [End] */

                /** Disconnect [Start] */
                if (sam.IsConnected)
                {
                    sam.Disconnect();
                    text += GetString(Resource.String.message_disconnecting) + " " + sam.SamType + "(Slot " + sam.SamIndex + ")\n";
                }
                /** Disconnect [End] */

                long timetook = (DateTime.Now.Ticks - tick) / 10000;
                text += "Time taken to get version: " + timetook + "ms";
                updateStatus(text);
            }
        }

        void initSAMManager()
        {
            if (emdkManager != null)
            {
                //Get the SAMManager object to process the profiles
                samManager = (SAMManager)emdkManager.GetInstance(EMDKManager.FEATURE_TYPE.Sam);
            }
        }

        private void deinitSAMManager()
        {
            //Clean up the objects created by EMDK samManager
            if (samManager != null)
            {
                emdkManager.Release(EMDKManager.FEATURE_TYPE.Sam);
                samManager = null;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            deinitSAMManager();

            if (emdkManager != null)
            {
                emdkManager.Release();
                emdkManager = null;
            }
        }

        public void OnClosed()
        {
            //This callback will be issued when the EMDK closes unexpectedly.
            if (emdkManager != null)
            {
                emdkManager.Release();
                emdkManager = null;
            }
        }


        protected override void OnResume()
        {
            base.OnResume();
            initSAMManager();

            if (tagOperationInProgress)
                tagOperationInProgress = false;

            if (nfcAdapter != null)
            {
                nfcAdapter.EnableForegroundDispatch(this, nfcIntent, null, null);
            }
        }

        protected override void OnPause()
        {
            base.OnPause();
            deinitSAMManager();
            if (nfcAdapter != null)
            {
                nfcAdapter.DisableForegroundDispatch(this);
            }
        }


        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            tagDetection(intent);
        }

        private void tagDetection(Intent intent)
        {

            tagOperationInProgress = true;

            if (samManager == null)
            {
                initSAMManager();
            }

            if (NfcAdapter.ActionNdefDiscovered.Equals(intent.Action)
                    || NfcAdapter.ActionTagDiscovered.Equals(intent.Action)
                    || NfcAdapter.ActionTechDiscovered.Equals(intent.Action))
            {

                Tag lTag = (Tag)intent.GetParcelableExtra(NfcAdapter.ExtraTag);
                SAM sam = null;
                SAMType samTypeForTag = null;

                samTypeForTag = findCompatibleSAM(lTag);

                string text = detectedTag + " " + GetString(Resource.String.message_tag_detected) + "\n";

                string compatibeSAMText = "";


                foreach (KeyValuePair<int, SAM> entry in presetSAMList)
                {
                    if (entry.Value.SamType == samTypeForTag)
                    {
                        sam = entry.Value;
                        compatibeSAMText += "\n\t" + sam.SamType + "(Slot " + sam.SamIndex + ")";
                    }
                }


                if (presetSAMList.Count > 0)
                {
                    if (compatibeSAMText != "")
                    {
                        text += GetString(Resource.String.message_tag_compatible) + " " + compatibeSAMText;

                        /**
                         * Connect to the appropriate SAM based on the Tag detected.
                         */
                        /**
                         //Connect [Start]
                         try
                         {
                             if (!sam.IsConnected)
                             {
                                 sam.Connect();
                             }
                         }
                         catch (SAMException ex)
                         {
                             SAMResults.GetErrorDescription(ex.Result);
                         }
                         //Connect [End]

                         //Transceive [Start]

                         byte[] response = null;
                         try
                         {
                             response = sam.Transceive(transceive_apdu_1, (short)0, false);
                             response = sam.Transceive(transceive_apdu_2, (short)0, false);
                             response = sam.Transceive(transceive_apdu_3, (short)0, false);
                             response = sam.Transceive(transceive_apdu_4, (short)0, false);

                         }
                         catch (SAMException ex)
                         {
                             SAMResults.GetErrorDescription(ex.Result);
                         }
                         //Transceive [End]

                         //Disconnect [Start]
                         if (sam.IsConnected)
                         {
                             sam.Disconnect();
                         }
                         //Disconnect [End]
                         */
                    }
                    else
                    {
                        text += GetString(Resource.String.message_tag_not_compatible);
                    }
                }
                updateStatus(text);
            }
        }

        private SAMType findCompatibleSAM(Tag aTag)
        {
            SAMType lType = SAMType.Unknown;
            detectedTag = "UNKNOWN";

            if (isNDEFTag(aTag))
            {
                //NDEF Tag
                detectedTag = "NDEF";
            }
            else if (isFelicaTag(aTag))
            {
                lType = SAMType.Felica;
                detectedTag = "FELICA";
            }
            else if (isMIFAREClassicTag(aTag))
            {
                lType = SAMType.Mifare;//MIFARE_CLASSIC;
                detectedTag = "MIFARE_CLASSIC";
            }
            else if (isMIFARETag(aTag))
            {
                lType = SAMType.Mifare;
            }
            return lType;
        }

        private bool isNDEFTag(Tag tag)
        {
            bool returnVar = false;
            Ndef lNdefTag = Ndef.Get(tag);
            if (lNdefTag != null)
                returnVar = true;
            return returnVar;
        }

        private bool isFelicaTag(Tag tag)
        {
            bool returnVar = false;
            NfcF mNfcF = NfcF.Get(tag);
            if (mNfcF != null)
            {
                byte[] mPMm;
                mPMm = mNfcF.GetManufacturer();
                if (mPMm[0] == 0x01 && mPMm[1] == 0x20)
                {
                    returnVar = true;

                }
                else if (mPMm[0] == 0x03 && mPMm[1] == 0x32)
                {
                    returnVar = true;
                }
            }
            return returnVar;
        }

        private bool isMIFAREClassicTag(Tag tag)
        {
            bool returnVar = false;
            MifareClassic mifareClassic = MifareClassic.Get(tag); //Get MIFARE CLASSIC tag
            if (mifareClassic != null)
                returnVar = true;
            return returnVar;
        }

        private bool isMIFARETag(Tag tag)
        {
            bool returnVar = false;
            ArrayList arr = new ArrayList();
            arr.AddRange(tag.GetTechList());
            if (arr.Contains("android.nfc.tech.NfcA"))
            {
                NfcA nfc_A = NfcA.Get(tag);
                byte[] atqa = nfc_A.GetAtqa();
                int Sak = int.Parse(nfc_A.Sak.ToString());
                switch (atqa[1])
                {
                    case 0x00:
                        if (atqa[0] == 0x44 || atqa[0] == 0x42
                                || atqa[0] == 0x02 || atqa[0] == 0x04)
                        {
                            if (Sak == 0x20)
                            {
                                returnVar = true;
                                detectedTag = "MIFARE_PLUS_SL3";
                            }
                            else if (Sak == 0x10)
                            {
                                returnVar = true;
                                detectedTag = "MIFARE_PLUS_SL2";
                            }
                        }
                        break;
                    case 0x03:
                        if (atqa[0] == 0x44 || atqa[0] == 0x4)
                        {
                            returnVar = true;
                            detectedTag = "MIFARE_DESFIRE";
                        }
                        break;
                    default:
                        break;
                }
            }
            return returnVar;
        }
    }
}

