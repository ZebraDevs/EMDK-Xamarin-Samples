/*
* Copyright (C) 2015-2017 Zebra Technologies Corp
* All rights reserved.
*/

using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using Android.Views;
using Symbol.XamarinEMDK.SimulScan;

namespace Symbol.XamarinEMDK.SimulScanSample1
{
    [Activity(Label = "SimulScanSample1", MainLauncher = true, Icon = "@drawable/icon", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
    public class MainActivity : Activity
    {

    private BroadcastReceiver broadcastReceiver = null;

    public Settings localSettings = new Settings();
    public DeviceControl dc = new DeviceControl();

    protected override void OnCreate(Bundle bundle) 
    {
        base.OnCreate(bundle);
        //setRequestedOrientation(ActivityInfo.SCREEN_ORIENTATION_PORTRAIT);

        // Initialize intent broadcast receiver
        IntentFilter filter = new IntentFilter(Intent.ActionScreenOn);
        filter.AddAction(Intent.ActionScreenOff);
        broadcastReceiver = new ScreenReceiver();
        RegisterReceiver(broadcastReceiver, filter);

        SetContentView(Resource.Layout.activity_main_2);
        if (bundle == null) {
            FragmentTransaction transaction = FragmentManager.BeginTransaction();
            transaction.Add(Resource.Id.device_controls, dc);
            transaction.Add(Resource.Id.settings, new SettingsFragment());
            transaction.Commit();
        }
    }

    protected override void OnPause() {
        // TODO Auto-generated method stub
        Log.Debug("MainActivity","SSC onPause");
        base.OnPause();
    }

    
    protected override void OnResume() {
        // TODO Auto-generated method stub
        Log.Debug("MainActivity","SSC onResume");
        base.OnResume();
    }

    
    protected override void OnDestroy() {
        // TODO Auto-generated method stub
        Log.Debug("MainActivity","SSC onDestroy");
        UnregisterReceiver(broadcastReceiver);
        base.OnDestroy();
    }

    public class ScreenReceiver : BroadcastReceiver {

        public override void OnReceive(Context context, Intent intent) {

            Log.Debug("ScreenReceiver","SSC onReceive");

            if (intent.Action.Equals(Intent.ActionScreenOff)) {
                // Screen Off
                Log.Debug("ScreenReceiver" , "SSC Screen Off");
                new AsyncDeInitScanner().Execute();
                //new AsyncUiControlUpdate().execute(true);
            } else if (intent.Action.Equals(Intent.ActionScreenOn)) {
                // Screen On
                Log.Debug("ScreenReceiver" , "SSC Screen On");
                new AsyncInitScanner().Execute();
            }
        }
    }

    public class AsyncDeInitScanner : AsyncTask {

        
         protected override  Java.Lang.Object DoInBackground (params Java.Lang.Object[] @params) {


            return null;
        }
    }

    private class AsyncInitScanner : AsyncTask {

        protected override Java.Lang.Object DoInBackground(params Java.Lang.Object[] @params)
        {


            return null;
        }
       
    }
}
}