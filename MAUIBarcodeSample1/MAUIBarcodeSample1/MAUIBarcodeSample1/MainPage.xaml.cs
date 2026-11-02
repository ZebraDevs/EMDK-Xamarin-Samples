using Symbol.XamarinEMDK.Barcode;

namespace MAUIBarcodeSample1;

public partial class MainPage : ContentPage
{
    private static MainPage _mainPage = null;
    private MainActivity _mainActivity = null;


    public MainPage()
    {
        InitializeComponent();
        _mainPage = this;

        triggerTypePicker.SelectedIndex= 0;
        scannerPicker.SelectedIndexChanged += OnScannerSelectedIndexChanged;
        triggerTypePicker.SelectedIndexChanged += OnTriggerTypeIndexChanged;

        //
        continueScanningCheckBox.CheckedChanged += checkBoxContinuous_CheckedChange;
        AddCheckBoxDecodersListener();
    }

    private void OnTriggerTypeIndexChanged(object sender, EventArgs e)
    {
        string selectedTriggerType = triggerTypePicker.Items[triggerTypePicker.SelectedIndex];
        int selectedIndex = triggerTypePicker.SelectedIndex;

        // Do something with the selected trigger type
        Console.WriteLine("Selected Scanner: " + selectedTriggerType);

        _mainActivity.OnSelectedTriggerTypeIndexSelected(selectedTriggerType,selectedIndex);
    }

    private void OnScannerSelectedIndexChanged(object sender, EventArgs e)
    {
        // Get the selected scanner from the picker list
        string selectedScanner = scannerPicker.Items[scannerPicker.SelectedIndex];
        int selectedIndex = scannerPicker.SelectedIndex;

        // Do something with the selected scanner
        Console.WriteLine("Selected Scanner: " + selectedScanner);

        _mainActivity.OnSelectedScannerIndexSelected(selectedScanner, selectedIndex);
    }

    public static MainPage getInstance()
    {
        return _mainPage;
    }

    public void setMainActivity(MainActivity mainActivity)
    {
        _mainPage._mainActivity = mainActivity;
    }

    public void updateStatusText(string message)
    {
        statusTxt.Text = message;
    }

    public void getScannerList(List<String> scannerList)
    {
          
        foreach (string scanner in scannerList)
        {
            if (!scannerPicker.Items.Contains(scanner)) {
                scannerPicker.Items.Add(scanner);
            }
            
        }
    }

    public void setDecoderCheckBox(ScannerConfig config)
    {
        // Set EAN8
        config.DecoderParams.Ean8.Enabled = checkBoxEAN8.IsChecked;

        // Set EAN13
        config.DecoderParams.Ean13.Enabled = checkBoxEAN13.IsChecked;

        // Set Code39
        config.DecoderParams.Code39.Enabled = checkBoxCode39.IsChecked;

        // Set Code128
        config.DecoderParams.Code128.Enabled = checkBoxCode128.IsChecked;
    }

    private void AddCheckBoxDecodersListener()
    {
       
        checkBoxEAN8.CheckedChanged += decoders_CheckedChange;
        checkBoxEAN13.CheckedChanged += decoders_CheckedChange;
        checkBoxCode39.CheckedChanged += decoders_CheckedChange;
        checkBoxCode128.CheckedChanged += decoders_CheckedChange;
    }

    private void decoders_CheckedChange(object sender, CheckedChangedEventArgs e)
    {
        _mainActivity.SetDecoders();
    }

    public void EnableUIControls(bool isEnabled)
    {
       
        checkBoxEAN8.IsEnabled = isEnabled;
        checkBoxEAN13.IsEnabled = isEnabled;
        checkBoxCode39.IsEnabled = isEnabled;
        checkBoxCode128.IsEnabled = isEnabled;

        scannerPicker.IsEnabled = isEnabled;
        triggerTypePicker.IsEnabled = isEnabled;

    }

    public void updateDataText(string data, Boolean isClear)
    {
        if (isClear) {
            dataTxt.Text = "";
        }
        else {
            dataTxt.Text += data + "\r\n";
        }
        
    }

    private void OnStartBtnEvent(object sender, EventArgs e)
    {
        _mainActivity.StartButtonEventClicked(sender, e);
    }

    private void OnStopBtnEvent(object sender, EventArgs e)
    {
        _mainActivity.StopButtonEventClicked(sender, e);
    }

    internal Boolean getContinuousCheckBox()
    {
        return continueScanningCheckBox.IsChecked;
    }

    internal void SetContinuousCheckBox(Boolean isChecked)
    {
        continueScanningCheckBox.IsChecked = isChecked;
    }

    private void checkBoxContinuous_CheckedChange(object sender, CheckedChangedEventArgs e)
    {
        _mainActivity.SetCheckBoxContinuousMode(e.Value);
    }

    internal void setScannerSelection(int defaultIndex)
    {
        scannerPicker.SelectedIndex = defaultIndex;
    }

    internal void setTriggerSelection(int selectedIndex)
    {
        triggerTypePicker.SelectedIndex = selectedIndex;
    }

    internal void SetDefaultDecodersForCheckBox(Boolean isChecked)
    {
        checkBoxEAN8.IsChecked = isChecked;
        checkBoxEAN13.IsChecked = isChecked;
        checkBoxCode39.IsChecked = isChecked;
        checkBoxCode128.IsChecked = isChecked;
    }

}

