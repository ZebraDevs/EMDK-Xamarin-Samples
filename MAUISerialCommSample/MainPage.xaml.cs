using MAUISerialCommSample.Services;

namespace MAUISerialCommSample;

public partial class MainPage : ContentPage
{
    private readonly ISerialCommService _serial;

    public MainPage(ISerialCommService serial)
    {
        InitializeComponent();
        _serial = serial;
        _serial.StatusChanged += OnStatusChanged;
        _serial.ReadyStateChanged += OnReadyStateChanged;
        _serial.Initialize();
    }

    private void OnStatusChanged(object? sender, string status) =>
        MainThread.BeginInvokeOnMainThread(() => StatusLabel.Text = status);

    private void OnReadyStateChanged(object? sender, bool ready) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            WriteButton.IsEnabled = ready;
            ReadButton.IsEnabled = ready;
            WriteEntry.IsEnabled = ready;
        });

    private void OnWriteClicked(object? sender, EventArgs e) => _serial.Write(WriteEntry.Text ?? string.Empty);
    private void OnReadClicked(object? sender, EventArgs e) => _serial.Read();

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _serial.StatusChanged -= OnStatusChanged;
        _serial.ReadyStateChanged -= OnReadyStateChanged;
    }
}
