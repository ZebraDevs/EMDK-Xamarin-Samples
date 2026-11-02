using MAUIScanAndPairSample1.Services;

namespace MAUIScanAndPairSample1;

public partial class MainPage : ContentPage
{
    private readonly IScanAndPairService _scanAndPair;

    public MainPage(IScanAndPairService scanAndPair)
    {
        InitializeComponent();

        _scanAndPair = scanAndPair;

        ScanDataTypePicker.ItemsSource = Enum.GetValues(typeof(ScanDataTypeOption));
        ScanDataTypePicker.SelectedIndex = 0;

        _scanAndPair.StatusChanged += OnStatusChanged;
        _scanAndPair.DeviceDiscovered += OnDeviceDiscovered;

        _scanAndPair.Initialize();
    }

    private void OnStatusChanged(object? sender, string status) =>
        MainThread.BeginInvokeOnMainThread(() => StatusLabel.Text = status);

    private void OnDeviceDiscovered(object? sender, DiscoveredDevice device) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            NameEntry.Text = device.DeviceName;
            AddressEntry.Text = device.MacAddress;
        });

    private void OnAlwaysScanChanged(object? sender, CheckedChangedEventArgs e)
    {
        // When "Always Scan" is on, the device name/address are discovered by scanning,
        // so those fields are disabled; otherwise the user types them in.
        bool manualEntry = !AlwaysScanCheck.IsChecked;
        NameEntry.IsEnabled = manualEntry;
        AddressEntry.IsEnabled = manualEntry;
    }

    private ScanPairRequest BuildRequest() => new()
    {
        AlwaysScan = AlwaysScanCheck.IsChecked,
        HardTrigger = HardTriggerCheck.IsChecked,
        ScanDataType = (ScanDataTypeOption)(ScanDataTypePicker.SelectedItem ?? ScanDataTypeOption.MacAddress),
        DeviceName = NameEntry.Text ?? string.Empty,
        MacAddress = AddressEntry.Text ?? string.Empty
    };

    private void OnPairClicked(object? sender, EventArgs e) => _scanAndPair.Pair(BuildRequest());

    private void OnUnpairClicked(object? sender, EventArgs e) => _scanAndPair.Unpair(BuildRequest());

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _scanAndPair.StatusChanged -= OnStatusChanged;
        _scanAndPair.DeviceDiscovered -= OnDeviceDiscovered;
    }
}
