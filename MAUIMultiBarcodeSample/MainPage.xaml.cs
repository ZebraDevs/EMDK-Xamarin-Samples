using System.Collections.ObjectModel;
using MAUIMultiBarcodeSample1.Services;

namespace MAUIMultiBarcodeSample1;

public partial class MainPage : ContentPage
{
    private readonly IBarcodeScannerService _scannerService;
    private readonly ObservableCollection<ScannedBarcode> _results = new();

    public MainPage(IBarcodeScannerService scannerService)
    {
        InitializeComponent();

        _scannerService = scannerService;
        ResultsView.ItemsSource = _results;

        // Trigger types (order matches the Android service: 0 = Hard, 1 = Soft).
        TriggerPicker.ItemsSource = new List<string> { "HARD", "SOFT" };
        TriggerPicker.SelectedIndex = 1;

        _scannerService.StatusChanged += OnStatusChanged;
        _scannerService.ScannersEnumerated += OnScannersEnumerated;
        _scannerService.BarcodesScanned += OnBarcodesScanned;

        _scannerService.Initialize();
    }

    private void OnStatusChanged(object? sender, string status) =>
        MainThread.BeginInvokeOnMainThread(() => StatusLabel.Text = status);

    private void OnScannersEnumerated(object? sender, ScannerListEventArgs e) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ScannerPicker.ItemsSource = new List<string>(e.Names);
            if (e.Names.Count > 0)
            {
                ScannerPicker.SelectedIndex = e.DefaultIndex;
            }
        });

    private void OnBarcodesScanned(object? sender, IReadOnlyList<ScannedBarcode> barcodes) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _results.Clear();
            foreach (ScannedBarcode b in barcodes)
            {
                _results.Add(b);
            }
        });

    private void OnScannerSelected(object? sender, EventArgs e)
    {
        if (ScannerPicker.SelectedIndex >= 0)
        {
            _scannerService.SelectScanner(ScannerPicker.SelectedIndex);
        }
    }

    private void OnTriggerSelected(object? sender, EventArgs e)
    {
        if (TriggerPicker.SelectedIndex >= 0)
        {
            _scannerService.SetTriggerType(TriggerPicker.SelectedIndex);
        }
    }

    private void OnBarcodeCountChanged(object? sender, TextChangedEventArgs e)
    {
        if (int.TryParse(BarcodeCountEntry.Text, out int count) && count is >= 2 and <= 10)
        {
            _scannerService.SetBarcodeCount(count);
        }
    }

    private async void OnScanClicked(object? sender, EventArgs e)
    {
        if (int.TryParse(BarcodeCountEntry.Text, out int count) && count is >= 2 and <= 10)
        {
            _scannerService.StartScan();
        }
        else
        {
            await DisplayAlertAsync("MultiBarcode Sample", "Number should be between 2-10", "OK");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _scannerService.StatusChanged -= OnStatusChanged;
        _scannerService.ScannersEnumerated -= OnScannersEnumerated;
        _scannerService.BarcodesScanned -= OnBarcodesScanned;
    }
}
