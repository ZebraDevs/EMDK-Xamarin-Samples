using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Symbol.XamarinEMDK;
using symbol.xamarinemdk.EMDKXamarinSerialComm;
using Java.Lang;
using Java.Util;
using Android;
using Symbol.XamarinEMDK;
using Symbol.XamarinEMDK.SerialComm;

namespace EMDKXamarinSerialComm
{
    [Activity(Label = "EMDKXamarinSerialComm", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity, EMDKManager.IEMDKListener
    {
        private EMDKManager emdkManager = null;
        private Symbol.XamarinEMDK.SerialComm.SerialCommMgr serialComm = null;

        private EditText editText = null;
        private TextView statusView = null;
        private Button readButton = null;
        private Button writeButton = null;
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            // Set our view from the "main" layout resource
            SetContentView(symbol.xamarinemdk.EMDKXamarinSerialComm.Resource.Layout.Main);
            editText = FindViewById<EditText>(symbol.xamarinemdk.EMDKXamarinSerialComm.Resource.Id.editText1);
            editText.SetText("Serial Communication Write Data Testing.", EditText.BufferType.Normal);

            statusView = FindViewById<TextView>(symbol.xamarinemdk.EMDKXamarinSerialComm.Resource.Id.statusView);
            statusView.SetText("", TextView.BufferType.Normal);
            statusView.RequestFocus();

            EMDKResults results = EMDKManager.GetEMDKManager(ApplicationContext, this);
            if (results.StatusCode != EMDKResults.STATUS_CODE.Success)
            {
                statusView.SetText("Failed to open EMDK", TextView.BufferType.Normal);
            }
            else
            {
                statusView.SetText("Opening EMDK...", TextView.BufferType.Normal);
            }

            addReadButtonEvents();
            writeButtonEvents();
            setEnabled(false);
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


        public void OnClosed()
        {
            if (emdkManager != null)
            {
                emdkManager.Release();
            }
            displayMessage("EMDK closed abruptly.");
        }


        public void OnOpened(EMDKManager emdkManager)
        {
            this.emdkManager = emdkManager;
            try
            {
                serialComm = (Symbol.XamarinEMDK.SerialComm.SerialCommMgr)this.emdkManager.GetInstance(Symbol.XamarinEMDK.EMDKManager.FEATURE_TYPE.Serialcomm);
                RunOnUiThread(() =>
            {
                string statusText = "";
                if (serialComm != null)
                {
                    try
                    {
                        serialComm.Enable();
                        statusText = "Serial comm channel enabled";
                        setEnabled(true);

                    }
                    catch (SerialCommException e)
                    {

                        statusText = e.Message;
                        setEnabled(false);
                    }
                }
                else
                {
                    statusText = Symbol.XamarinEMDK.EMDKManager.FEATURE_TYPE.Serialcomm.ToString() + " " + "Feature not supported or initilization error.";
                    setEnabled(false);
                }
                displayMessage(statusText);

            }
                    );
            }
            catch (Java.Lang.Exception e)
            {
                displayMessage(e.Message);
            }
        }

        private void addReadButtonEvents()
        {
            readButton = FindViewById<Button>(symbol.xamarinemdk.EMDKXamarinSerialComm.Resource.Id.ReadButton);
            readButton.Click += ReadButton_Click;
        }

        private void ReadButton_Click(object sender, EventArgs e)
        {
            RunOnUiThread(() =>
            {
                setEnabled(false);
                string statusText = "";
                try
                {

                    byte[] readBuffer = serialComm.Read(10000); //Timeout after 10 seconds

                    if (readBuffer != null)
                    {
                        string tempString = new string(System.Text.Encoding.UTF8.GetChars(readBuffer));
                        statusText = "Data Read:\n" + tempString;
                    }
                    else
                    {
                        statusText = "No Data Available";
                    }

                }
                catch (SerialCommException ex)
                {
                    statusText = "read:" + ex.Result.Description;
                }
                catch (Java.Lang.Exception exp)
                {
                    statusText = "read:" + exp.Message;
                }
                setEnabled(true);
                displayMessage(statusText);

            });

        }

        private void displayMessage(string message)
        {
            string tempMessage = message;
            RunOnUiThread(() =>
            {
                statusView.SetText(tempMessage + "\n", TextView.BufferType.Normal);
            });
        }

        private void writeButtonEvents()
        {
            writeButton = FindViewById<Button>(symbol.xamarinemdk.EMDKXamarinSerialComm.Resource.Id.WriteButton);
            writeButton.Click += WriteButton_Click;
        }

        private void WriteButton_Click(object sender, EventArgs e)
        {
            RunOnUiThread(() =>
            {
                setEnabled(false);
                try
                {
                    string writeData = editText.Text.ToString();
                    char[] data=writeData.ToCharArray();
                    
                    int bytesWritten = serialComm.Write(System.Text.Encoding.ASCII.GetBytes(writeData), System.Text.Encoding.ASCII.GetBytes(writeData).Length);
                    statusView.SetText("Bytes written: " + bytesWritten , TextView.BufferType.Normal);
                }
                catch (SerialCommException ex)
                {
                    statusView.SetText("write: " + ex.Result.Description, TextView.BufferType.Normal);
                }
                catch (Java.Lang.Exception exp)
                {
                    statusView.SetText("write: " + exp.Message + "\n", TextView.BufferType.Normal);
                }
                setEnabled(true);

            });
        }

        private void setEnabled(bool enableState)
        {
            bool tempState = enableState;
            RunOnUiThread(() =>
            {
                readButton.Enabled = tempState;
                writeButton.Enabled = tempState;
                editText.Enabled = tempState;
            });

        }


    }
}


