/*
* Copyright (C) 2015-2017 Zebra Technologies Corp
* All rights reserved.
*/
using Symbol.XamarinEMDK.SimulScan;
using Java.Util;
using Symbol.XamarinEMDK.SimulScanSample1;
using Android.OS;
using Android.App;
using Android.Widget;
using Android.Util;
using System.Collections.Generic;

[Activity(Label = "ResultsActivity", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
public class ResultsActivity : Activity {

    private string TAG = typeof(DeviceControl).Name;

    protected override void OnCreate(Bundle savedInstanceState) {
        base.OnCreate(savedInstanceState);
        Log.Verbose(TAG, "SSC onCreate");
        //setRequestedOrientation(ActivityInfo.SCREEN_ORIENTATION_PORTRAIT);
        SetContentView(Resource.Layout.activity_results);
    
        if(DeviceControl.simulscanDataList.Count> 0){
            SimulScanData processedForm;
            lock (DeviceControl.simulscanDataList) {
                processedForm =((SimulScanData) DeviceControl.simulscanDataList[DeviceControl.simulscanDataList.Count-1]);
            }
            onFormProcessed(processedForm);
            lock (DeviceControl.simulscanDataList) {
                DeviceControl.simulscanDataList.Clear();
            }
        }else{
            Log.Warn(TAG, "SimulScanData list is empty");
        }
    }

    public void onFormProcessed(SimulScanData processedForm) {

        TextView mtvTimestamp = (TextView)FindViewById(Resource.Id.tvTimestamp);
        string timestamp = processedForm.Timestamp;
        Log.Verbose(TAG, "onFormProcessed: timestamp-" + timestamp.ToString());
        mtvTimestamp.SetText(timestamp, TextView.BufferType.Normal);

        //get list of elements, extract regions from groups
        IList<SimulScanElement> processedElements = processedForm.Elements;//processedForm.Elements;
        List<SimulScanRegion> copyProcessedRegions = new List<SimulScanRegion>(); //expanding elements into regions
        foreach (SimulScanElement curElement in processedElements)
        {
            if (curElement is SimulScanRegion)
            {
                copyProcessedRegions.Add((SimulScanRegion) curElement);
            }
            else if (curElement is SimulScanGroup)
            {
                IList<SimulScanRegion> regionsInGroup = ((SimulScanGroup) curElement).Regions;
                foreach (SimulScanRegion curRegion in regionsInGroup)
                {
                    copyProcessedRegions.Add(curRegion);
                }
            }
        }

        //ArrayAdapter obj = new ArrayAdapter(this, Resource.Layout.region_item, copyProcessedRegions);
        //RegionArrayAdapter adapter =(RegionArrayAdapter)obj;
        RegionArrayAdapter adapter = new RegionArrayAdapter(this, Resource.Layout.region_item, Resource.Id.regionName, copyProcessedRegions);
        ListView mlvProcessedRegions = (ListView) this.FindViewById(Resource.Id.regionLV);
        mlvProcessedRegions.Adapter = adapter;
    }

    protected override void OnPause() {
        // TODO Auto-generated method stub
        Log.Verbose(TAG, "SSC onPause");
        base.OnPause();
    }

    
    protected override void OnResume() {
        // TODO Auto-generated method stub
        Log.Verbose(TAG, "SSC onResume");
        base.OnResume();
    }

    
    protected override void OnDestroy() {
        // TODO Auto-generated method stub
        Log.Verbose(TAG, "SSC onDestroy");
        base.OnDestroy();
    }
}
