/*
 * Copyright (C) 2015-2019 Zebra Technologies Corporation and/or its affiliates
 * All rights reserved.
 */
using Symbol.XamarinEMDK.SimulScan;
using Java.Util;
using Symbol.XamarinEMDK;
using Android.App;
using Android.Content;
using Android.Net;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.IO;
using Android.Graphics;
using Symbol.XamarinEMDK.SimulScanSample1;
using Android.Runtime;
using System;
using System.Collections.Generic;
using Java.Lang;

namespace Symbol.XamarinEMDK.SimulScanSample1{
public class RegionArrayAdapter : ArrayAdapter<SimulScanRegion> {
    static SimulScanRegion region;    

    private List <SimulScanRegion> mRegions = null;      
     public RegionArrayAdapter(Context context, int regio_id,int textViewResourceId, List<SimulScanRegion> regions): base(context, regio_id,textViewResourceId, regions){
        this.mRegions = regions;
                
    }
     
    
   
   
     ICharSequence[] omrStatus;
    
   
   

    public override View GetView(int position, View convertView, ViewGroup parent) {

        
        View v = convertView;
        omrStatus = new Java.Lang.String[2];
        omrStatus[0] = new Java.Lang.String("Checked");
        omrStatus[1] = new Java.Lang.String("Unchecked"); 
        
        if (v == null) {
            LayoutInflater vi = (   LayoutInflater) this.Context.GetSystemService(Context.LayoutInflaterService);
            v = vi.Inflate(Resource.Layout.region_item, parent, false);
        }
         
       
        region = mRegions[position];
        if (null != region) {
            ImageView imgImage = (ImageView) v.FindViewById(Resource.Id.regionImage);
            TextView txtName = (TextView) v.FindViewById(Resource.Id.regionName);
            TextView txtData = (TextView) v.FindViewById(Resource.Id.regionData);
            TextView txtProcessingMode = (TextView) v.FindViewById(Resource.Id.regionProcessingMode);
            TextView txtAbsConf = (TextView) v.FindViewById(Resource.Id.regionAbsConf);
            TextView txtRelConf = (TextView) v.FindViewById(Resource.Id.regionRelConf);

            if ((null != imgImage) && (null != region.Image)) {
                System.IO.MemoryStream  baoStream = new  System.IO.MemoryStream();

                region.Image.CompressToJpeg(new Rect(0, 0, region.Image.Width, region.Image.Height), 100, baoStream);

                byte[] bitmapData=baoStream.ToArray();

                Bitmap bm = BitmapFactory.DecodeByteArray(bitmapData,0,bitmapData.Length);
                imgImage.SetImageBitmap(bm);
            }
            //if no image, clear ImageView so incorrect images don't show
            else if (region.Image == null)
            {
                imgImage.SetImageResource(Android.Resource.Color.Transparent);
            }

            if ((null != txtName) && (null != region.Name)) {                
                txtName.SetText(region.Name.ToString(), TextView.BufferType.Normal);
            }

            if ((null != txtProcessingMode) && (null != region.RegionType.Name())) {
                txtProcessingMode.SetText(region.RegionType.Name(), TextView.BufferType.Normal);
            }

            if (txtAbsConf != null)
            {
                string absConfText = "AC: ";
                if (region.AbsoluteConfidence == -1)
                {
                    absConfText = "";
                }
                else
                {
                    absConfText += region.AbsoluteConfidence;
                }
                txtAbsConf.SetText(absConfText, TextView.BufferType.Normal);
            }

            if (txtRelConf != null)
            {
                string relConfText = "RC: ";
                if (region.RelativeConfidence == -1)
                {
                    relConfText = "";
                }
                else
                {
                    relConfText += region.RelativeConfidence;
                }
                txtRelConf.SetText(relConfText, TextView.BufferType.Normal);
            }

            if (null != txtData) {
                string sText = "";
                if (region.RegionType == RegionType.Ocr)
                {
                    if (region.Data != null)
                    {
                        string[] OCRResults = (string[]) region.Data;
                        if (null != OCRResults) {
                            for (int nIndex = 0; nIndex < OCRResults.Length; nIndex++)
                            {
                                if (nIndex != 0) //if not first index, prepend with newline
                                    sText = sText + ("\n");
                                sText = sText+OCRResults[nIndex];
                            }
                        }
                    }
                    txtData.SetText(sText, TextView.BufferType.Normal);
                }
                else if (region.RegionType == RegionType.Omr)
                {
                    if (region.Data != null)
                    {
                        int iChecked = (int)region.Data;

                        switch (iChecked) {
                            case 1 :
                                sText = sText+omrStatus[0].ToString();
                                break;
                            case -1 :
                                sText = sText +(omrStatus[1].ToString());
                                break;
                            default :
                                break;
                        }
                    }
                    else
                    {
                        sText = sText +(omrStatus[1].ToString()); //default to unchecked
                    }
                    txtData.SetText(sText, TextView.BufferType.Normal);
                }
                else if (region.RegionType == RegionType.Barcode)
                {
                    if (region.Data != null)
                    {
                        try
                        {
                            sText = sText+((string) region.Data);
                        }
                        catch (ClassCastException e) //will get here if post-processing is off
                        {
                            sText = "Post-processing is off";
                        }
                    }
                    txtData.SetText(sText, TextView.BufferType.Normal);
                }
                else if (region.RegionType == RegionType.Picture)
                {
                    if (region.Data != null)
                    {
                        //byte[] jpegPicture = (byte[])region.getData();
                        txtData.SetText("", TextView.BufferType.Normal);
                    }
                }
                else {
                    txtData.SetText(region.Data.ToString(), TextView.BufferType.Normal);
                }
            } else {

            }

            if ((null != txtProcessingMode) && (null != region.RegionType)) {
                txtProcessingMode.SetText(region.RegionType.Name(), TextView.BufferType.Normal);
            }
        }

        return v;
    }

}
}