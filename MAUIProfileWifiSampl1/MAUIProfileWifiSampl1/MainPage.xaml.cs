using MAUIProfileWifiSampl1.Services;

namespace MAUIProfileWifiSampl1;

public partial class MainPage : ContentPage
{
    private readonly IWifiProfileService _wifiProfileService;

    public MainPage(IWifiProfileService wifiProfileService)
    {
        InitializeComponent();
        _wifiProfileService = wifiProfileService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Initialize the EMDKManager once the page is shown.
        StatusLabel.Text = "Initializing EMDK...";
        string status = await _wifiProfileService.InitializeAsync();
        StatusLabel.Text = status;
    }

    private async void OnApplyClicked(object? sender, EventArgs e)
    {
        StatusLabel.Text = "Set profile in-progress...";

        bool enableWifi = WifiEnableRadio.IsChecked;
        NetworkAction action = (NetworkAction)Math.Max(0, ActionPicker.SelectedIndex);
        string ssid = SsidEntry.Text ?? string.Empty;

        try
        {
            string result = await _wifiProfileService.ApplyWifiProfileAsync(enableWifi, action, ssid);
            StatusLabel.Text = result;
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "Set profile failed: " + ex.Message;
        }
    }
}
