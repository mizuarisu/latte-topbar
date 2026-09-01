using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using TopBar.Services;

namespace TopBar.Windows;

public partial class SettingsWindow : Window
{
    private readonly SettingsService _settings;
    private readonly HotkeyService _hotkey;
    private string _capturedKey;

    public SettingsWindow(SettingsService settings, HotkeyService hotkey)
    {
        InitializeComponent();
        _settings = settings;
        _hotkey = hotkey;
        _capturedKey = _settings.Current.HotkeyKey;

        var s = _settings.Current;
        HotkeyBox.Text = $"Alt + {_capturedKey}";
        AutoStartCheck.IsChecked = s.AutoStart;
        MainColorBox.Text = s.MainColor;
        SecondaryColorBox.Text = s.SecondaryColor;
        TertiaryColorBox.Text = s.TertiaryColor;
        PfpPathBox.Text = s.ProfilePicturePath ?? "";
        WeatherLabelBox.Text = s.WeatherLabel;
        LatBox.Text = s.WeatherLat.ToString("0.####");
        LonBox.Text = s.WeatherLon.ToString("0.####");
        FahrenheitCheck.IsChecked = s.WeatherFahrenheit;
    }

    private void OnHotkeyPreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Ignore bare modifier presses — we're capturing the non-modifier key that pairs with Alt
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key is Key.LeftAlt or Key.RightAlt or Key.LeftCtrl or Key.RightCtrl
                 or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
        {
            e.Handled = true;
            return;
        }

        _capturedKey = key.ToString();
        HotkeyBox.Text = $"Alt + {_capturedKey}";
        e.Handled = true;
    }

    private void OnBrowsePfp(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.gif" };
        if (dialog.ShowDialog() == true)
            PfpPathBox.Text = dialog.FileName;
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        var s = _settings.Current;
        s.HotkeyModifiers = "Alt";
        s.HotkeyKey = _capturedKey;
        s.AutoStart = AutoStartCheck.IsChecked == true;
        s.MainColor = MainColorBox.Text.Trim();
        s.SecondaryColor = SecondaryColorBox.Text.Trim();
        s.TertiaryColor = TertiaryColorBox.Text.Trim();
        s.ProfilePicturePath = string.IsNullOrWhiteSpace(PfpPathBox.Text) ? null : PfpPathBox.Text.Trim();
        s.WeatherLabel = WeatherLabelBox.Text.Trim();

        if (double.TryParse(LatBox.Text, out var lat)) s.WeatherLat = lat;
        if (double.TryParse(LonBox.Text, out var lon)) s.WeatherLon = lon;
        s.WeatherFahrenheit = FahrenheitCheck.IsChecked == true;

        _settings.Save(); // raises SettingsChanged, which the panel listens to for live theme refresh

        AutoStartService.SetEnabled(s.AutoStart);

        if (!_hotkey.Register(s.HotkeyModifiers, s.HotkeyKey))
            MessageBox.Show(this, "That key combo couldn't be registered — it may already be in use by " +
                "another app. Try a different key.", "Hotkey", MessageBoxButton.OK, MessageBoxImage.Warning);

        Close();
    }
}
