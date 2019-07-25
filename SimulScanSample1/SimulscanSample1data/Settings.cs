/*
* Copyright (C) 2015-2017 Zebra Technologies Corp
* All rights reserved.
*/
using Symbol.XamarinEMDK.SimulScanSample1;
using Java.IO;
using System;
using System.Collections.Generic;

public class Settings {
   public int selectedFileIndex = 0;
   public List<File> fileList; 
    public bool enableAutoCapture;
    public bool enableFeedbackAudio;
    public bool enableDebugMode;
    public bool enableHaptic;
    public bool enableLED;
    public bool enableResultConfirmation;
    public int identificationTimeout;
    public int processingTimeout;
    
}
