/*
* Copyright (C) 2015-2017 Zebra Technologies Corp
* All rights reserved.
*/
using Symbol.XamarinEMDK.SimulScanSample1;
using Java.Util;
using Symbol.XamarinEMDK;
using Android.App;
using Android.Content;
using Android.OS;
using Java.IO;
using Android.Views;
using Android.Widget;
using Android.Preferences;
using Android.Util;
using System.Collections.Generic;
using Android.Runtime;
using Java.Lang;
//using Xamarin.Forms;

public class SettingsFragment : PreferenceFragment , ISharedPreferencesOnSharedPreferenceChangeListener{

    private string TAG = typeof(SettingsFragment).Name;
    private  const string NO_TEMPLATE_FOUND = "(No templates found)";
    private  const string SETTINGS_LAST_TEMPLATE_POS = "lastTemplatePos";
    Settings localSettings = new Settings();

    public void OnSharedPreferenceChanged(ISharedPreferences prefs, string key)
     {
        MainActivity parentActivity = (MainActivity)Activity;
        if (key.CompareTo(Resources.GetString(Resource.String.pref_key))== 0) {
            ListPreference connectionPref = (ListPreference) FindPreference(key);
            Log.Debug(TAG, "Template PreferenceChanged: " + connectionPref.Value);
            parentActivity.localSettings.selectedFileIndex = int.Parse(connectionPref.Value);
            connectionPref.Summary = connectionPref.Entry;
            //save last template used
            /*ISharedPreferences iprefs = null;
            iprefs.Edit();
            iprefs = (ISharedPreferences)prefs.PutInt(SETTINGS_LAST_TEMPLATE_POS, parentActivity.localSettings.selectedFileIndex);
            prefs.Apply();*/
            
            ISharedPreferencesEditor editor = (ISharedPreferencesEditor) prefs.Edit();
            editor.PutInt(SETTINGS_LAST_TEMPLATE_POS, parentActivity.localSettings.selectedFileIndex);
            editor.Apply();
        }   else if(key.CompareTo("timeout_identification")==0){
            EditTextPreference connectionPref = (EditTextPreference) FindPreference(key);
            Log.Debug(TAG, "Identification PreferenceChanged: " + connectionPref.Text);

            int idto = parentActivity.localSettings.identificationTimeout;

            try {
                idto = int.Parse(connectionPref.Text);
                if (idto < 5000)
                    idto = 5000;

                
            }
            catch(IllegalFormatException ex)
            {
                Log.Error(TAG, "Invalid identification timeout exception: " + ex.Message);
                Toast.MakeText(parentActivity.ApplicationContext, "Invalid identification timeout value", ToastLength.Long).Show();
            }
            catch (System.OverflowException ex)
            {
                Log.Error(TAG, "Invalid identification timeout exception: " + ex.Message);
                Toast.MakeText(parentActivity.ApplicationContext, "Invalid identification timeout value", ToastLength.Long).Show();
            }

            connectionPref.Text = idto.ToString();
            parentActivity.localSettings.identificationTimeout = idto;

        }else if(key.CompareTo("timeout_processing")==0){
            EditTextPreference connectionPref = (EditTextPreference) FindPreference(key);
            Log.Debug(TAG, "Processing PreferenceChanged: " + connectionPref.Text);

            int processingTimeout = parentActivity.localSettings.processingTimeout;;

            try
            {
                processingTimeout = int.Parse(connectionPref.Text);
                
            }
            catch (IllegalFormatException ex)
            {
                Log.Error(TAG, "Invalid processing timeout exception: " + ex.Message);
                Toast.MakeText(parentActivity.ApplicationContext, "Invalid processing timeout value", ToastLength.Long).Show();
            }
            catch (System.OverflowException ex)
            {
                Log.Error(TAG, "Invalid processing timeout exception: " + ex.Message);
                Toast.MakeText(parentActivity.ApplicationContext, "Invalid processing timeout value", ToastLength.Long).Show();
            }
            connectionPref.Text = processingTimeout.ToString();
            parentActivity.localSettings.processingTimeout = processingTimeout;

        }else if(key.CompareTo("ui_result_confirmation")==0){
            CheckBoxPreference connectionPref = (CheckBoxPreference) FindPreference(key);
            Log.Debug(TAG, "result confrmation PreferenceChanged: " + connectionPref.Checked);
            parentActivity.localSettings.enableResultConfirmation = connectionPref.Checked;
        }else if(key.CompareTo("auto_capture")==0){
            CheckBoxPreference connectionPref = (CheckBoxPreference) FindPreference(key);
            Log.Debug(TAG, "Auto capture PreferenceChanged: " + connectionPref.Checked);
            parentActivity.localSettings.enableAutoCapture = connectionPref.Checked;
        }else if (key.CompareTo("debug") == 0){
            CheckBoxPreference connectionPref = (CheckBoxPreference) FindPreference(key);
            Log.Debug(TAG, "Debug PreferenceChanged: " + connectionPref.Checked);
            parentActivity.localSettings.enableDebugMode = connectionPref.Checked;
        }else if (key.CompareTo("feedback_audio") == 0){
            CheckBoxPreference connectionPref = (CheckBoxPreference) FindPreference(key);
            Log.Debug(TAG, "Audio PreferenceChanged: " + connectionPref.Checked);
            parentActivity.localSettings.enableFeedbackAudio = connectionPref.Checked;
        }else if(key.CompareTo("feedback_haptic")==0){
            CheckBoxPreference connectionPref = (CheckBoxPreference) FindPreference(key);
            Log.Debug(TAG, "Haptic PreferenceChanged: " + connectionPref.Checked);
            parentActivity.localSettings.enableHaptic = connectionPref.Checked;
        }else if(key.CompareTo("feedback_led")==0){
            CheckBoxPreference connectionPref = (CheckBoxPreference) FindPreference(key);
            Log.Debug(TAG, "LED PreferenceChanged: " + connectionPref.Checked);
            parentActivity.localSettings.enableLED = connectionPref.Checked;
        }
    }

    public override void OnCreate(Bundle savedInstanceState) {
        base.OnCreate(savedInstanceState);

        //getActivity().setRequestedOrientation(ActivityInfo.SCREEN_ORIENTATION_PORTRAIT);

        // Load the preferences from an XML resource
        AddPreferencesFromResource(Resource.Xml.preferences);

        // Get the Preference Category which we want to add the ListPreference
        ListPreference customListPref = (ListPreference) FindPreference("pref_template");

        MainActivity parentActivity = (MainActivity)Activity;

        // Retrieve SimulScan default templates from the system
        RetrieveTemplates();

        if (customListPref != null) {

            List<string> entries = new List<string>();
            List<string> entryValues = new List<string>();

            string path = Environment.ExternalStorageDirectory.ToString() + "/simulscan/templates";
            Log.Debug(TAG, "Path: " + path);
            File f = new File(path);

            File []file = f.ListFiles();
            if (file != null) {
                for (int i = 0; i < file.Length; i++) {
                    Log.Debug(TAG, "FileName:" + file[i].Name);
                    //Log.d("Files", "value:" + file[i].getAbsolutePath());
                    entries.Add(file[i].Name);
                    entryValues.Add(i.ToString());
                }
            } else {
                Log.Debug(TAG, "Cant find folder");
            }

            if (entries.Count == 0){
                entries.Add(NO_TEMPLATE_FOUND);
                entryValues.Add("");
            }
            customListPref.SetEntries(entries.ToArray());
            customListPref.SetEntryValues(entryValues.ToArray());

            //customListPref.setPersistent(true);

            ISharedPreferences prefs = PreferenceScreen.SharedPreferences;
            int lastTemplatePos = prefs.GetInt(SETTINGS_LAST_TEMPLATE_POS, 0);
            if(lastTemplatePos >= entries.Count)
                lastTemplatePos = 0;
            customListPref.SetValueIndex(lastTemplatePos);
            customListPref.Summary=entries[lastTemplatePos];
           

            if(entryValues[lastTemplatePos].CompareTo("") != 0){
                parentActivity.localSettings.fileList = new List<File>(file);
                parentActivity.localSettings.selectedFileIndex = lastTemplatePos;
            }
        }

        EditTextPreference pref1 = (EditTextPreference) FindPreference("timeout_identification");
        parentActivity.localSettings.identificationTimeout = int.Parse(pref1.Text);
        EditTextPreference pref2 = (EditTextPreference) FindPreference("timeout_processing");
        parentActivity.localSettings.processingTimeout = int.Parse(pref2.Text);
        CheckBoxPreference pref3 = (CheckBoxPreference) FindPreference("ui_result_confirmation");
        parentActivity.localSettings.enableResultConfirmation = pref3.Checked;
        CheckBoxPreference pref4 = (CheckBoxPreference) FindPreference("auto_capture");
        parentActivity.localSettings.enableAutoCapture = pref4.Checked;
        CheckBoxPreference pref5 = (CheckBoxPreference) FindPreference("debug");
        parentActivity.localSettings.enableDebugMode = pref5.Checked;
        CheckBoxPreference pref6 = (CheckBoxPreference) FindPreference("feedback_audio");
        parentActivity.localSettings.enableFeedbackAudio = pref6.Checked;
        CheckBoxPreference pref7 = (CheckBoxPreference) FindPreference("feedback_haptic");
        parentActivity.localSettings.enableHaptic = pref7.Checked;
        CheckBoxPreference pref8 = (CheckBoxPreference) FindPreference("feedback_led");
        parentActivity.localSettings.enableLED = pref8.Checked;
    }

    
    public override void OnViewCreated(Android.Views.View view, Bundle savedInstanceState) {
        base.OnViewCreated(view, savedInstanceState);
    }

    public override void OnResume() {
        base.OnResume();
        PreferenceScreen.SharedPreferences.RegisterOnSharedPreferenceChangeListener(this);
    }

    
    public override void OnPause() {
        base.OnPause();
        PreferenceScreen.SharedPreferences.UnregisterOnSharedPreferenceChangeListener(this);
    }

    public void RetrieveTemplates() {

        Java.IO.File source = new Java.IO.File("/enterprise/device/settings/datawedge/templates");
        Java.IO.File dest = new Java.IO.File(Environment.ExternalStorageDirectory.AbsolutePath+"/simulscan/templates");

        try {
            copyTemplateDirectory(source, dest);
        } catch (Java.IO.IOException e) {
            Log.Error(TAG, "Exception while retrieving templates : " + e.Message);
            e.PrintStackTrace();
        }
    }

    public static void copyTemplateDirectory(File sourceLocation, File targetLocation) {

        try {

        if (sourceLocation.IsDirectory) {
            if (!targetLocation.Exists()) {
                //System.IO.Directory.CreateDirectory("targetLocation");
                targetLocation.Mkdir();
            }

            string[] children = sourceLocation.List();
            for (int i = 0; i < sourceLocation.ListFiles().Length; i++) {

                // Skip templates.properties file and copy only the template (XML) files
                if (!children[i].Contains("templates.properties")) {
                    copyTemplateDirectory(new File(sourceLocation, children[i]), new File(targetLocation, children[i]));
                }
            }
        } else {

            InputStream input = new FileInputStream(sourceLocation);
            OutputStream output = new FileOutputStream(targetLocation);

            // Copy the bits from input stream to output stream
            byte[] buf = new byte[1024];
            int len;
            while ((len = input.Read(buf)) > 0)
            {
                output.Write(buf, 0, len);
            }

            input.Close();
            output.Close();
        }
       } catch (Java.IO.IOException e) {
                e.PrintStackTrace();
            }
    }
}
