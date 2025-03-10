using Android.Widget;

namespace MauiProfileClockSample1;

public partial class MainPage : ContentPage
{
	static MainPage _mainPage = null;
	MainActivity _mainActivity = null;

	public MainPage()
	{
		InitializeComponent();
		_mainPage = this;
    }

	public static MainPage getInstance()
	{
        return _mainPage;
	}

	public void setMainActivity(MainActivity mainActivity)
	{
        _mainPage._mainActivity = mainActivity;
    }

    private void onSetClockClicked(object sender, EventArgs e)
    {
		_mainActivity.onClickSetProfile(txtTimeZone.Text.Trim(), txtDate.Text.Trim(), txtTime.Text.Trim());
    }

	public void updateStatusText(string message)
	{
        lblStatus.Text = "Status: " + message;
    }

}

