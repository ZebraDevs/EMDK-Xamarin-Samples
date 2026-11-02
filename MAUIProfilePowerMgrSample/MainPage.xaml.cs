using MAUIProfilePowerMgrSample1.Services;

namespace MAUIProfilePowerMgrSample1;

public partial class MainPage : ContentPage
{
    private readonly IPowerMgrService _powerMgr;

    public MainPage(IPowerMgrService powerMgr)
    {
        InitializeComponent();

        _powerMgr = powerMgr;
        _powerMgr.StatusChanged += OnStatusChanged;

        // Create the EMDKManager; status arrives via the StatusChanged event.
        _powerMgr.Initialize();
    }

    private void OnStatusChanged(object? sender, string status)
    {
        // EMDK callbacks arrive on a background thread; marshal to the UI thread.
        MainThread.BeginInvokeOnMainThread(() => StatusLabel.Text = status);
    }

    private void OnSetClicked(object? sender, EventArgs e)
    {
        StatusLabel.Text = string.Empty;
        _powerMgr.SetProfile(RadioSuspend.IsChecked);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _powerMgr.StatusChanged -= OnStatusChanged;
    }
}
