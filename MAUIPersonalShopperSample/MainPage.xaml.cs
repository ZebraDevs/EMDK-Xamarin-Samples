using MAUIPersonalShopperSample.Services;

namespace MAUIPersonalShopperSample;

public partial class MainPage : ContentPage
{
    private readonly IPersonalShopperService _ps;

    public MainPage(IPersonalShopperService ps)
    {
        InitializeComponent();
        _ps = ps;
        _ps.StatusChanged += OnStatusChanged;
        _ps.Initialize();
    }

    private void OnStatusChanged(object? sender, string status) =>
        MainThread.BeginInvokeOnMainThread(() => StatusLabel.Text = status);

    private void OnFastChargeChanged(object? sender, CheckedChangedEventArgs e) =>
        _ps.SetFastCharging(FastChargeCheck.IsChecked);

    private void OnCradleInfoClicked(object? sender, EventArgs e) => _ps.GetCradleInfo();
    private void OnDiagnosticClicked(object? sender, EventArgs e) => _ps.GetDiagnosticData();
    private void OnUnlockClicked(object? sender, EventArgs e) => _ps.Unlock(SmoothCheck.IsChecked);
    private void OnFlashClicked(object? sender, EventArgs e) => _ps.FlashLeds(SmoothCheck.IsChecked);

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _ps.StatusChanged -= OnStatusChanged;
    }
}
