using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using TopBar.Helpers;
using TopBar.Services;

namespace TopBar.Windows;

public partial class SettingsWindow : Window
{
    private readonly SettingsService _settings;
    private readonly HotkeyService _hotkey;
    private string _capturedKey;
    private bool _initialized;

    public SettingsWindow(SettingsService settings, HotkeyService hotkey)
    {
        InitializeComponent();
        MaxHeight = SystemParameters.WorkArea.Height * 0.88; // leaves room so it never exceeds the screen

        _settings = settings;
        _hotkey = hotkey;
        _capturedKey = _settings.Current.HotkeyKey;

        var s = _settings.Current;
        HotkeyBox.Text = $"Alt + {_capturedKey}";
        AutoStartCheck.IsChecked = s.AutoStart;
        MainColorBox.Text = s.MainColor;
        SecondaryColorBox.Text = s.SecondaryColor;
        TertiaryColorBox.Text = s.TertiaryColor;
        TextColorBox.Text = s.TextColor;
        CornerRadiusSlider.Value = s.CornerRadius;
        CornerRadiusLabel.Text = $"{s.CornerRadius:0}px";
        ShadowCheck.IsChecked = s.ShadowEnabled;
        PfpPathBox.Text = s.ProfilePicturePath ?? "";
        MediaImagePathBox.Text = s.MediaImagePath ?? "";
        WeatherLabelBox.Text = s.WeatherLabel;
        // Invariant culture explicitly — otherwise this renders with a comma decimal
        // separator on locales like id-ID, which then fails to round-trip on Save.
        LatBox.Text = s.WeatherLat.ToString("0.####", CultureInfo.InvariantCulture);
        LonBox.Text = s.WeatherLon.ToString("0.####", CultureInfo.InvariantCulture);
        FahrenheitCheck.IsChecked = s.WeatherFahrenheit;

        _initialized = true; // now that fields are populated, live-preview handlers are safe to fire
    }

    // ---------- Live preview: every edit below pushes straight to the app's live theme
    // resources, so the whole app (including this window) reflects it instantly — nothing
    // is persisted to disk until Save is clicked. ----------

    private void OnLiveThemeChanged(object sender, RoutedEventArgs e) => PushLivePreview();

    private void OnCornerRadiusChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (CornerRadiusLabel is not null) CornerRadiusLabel.Text = $"{e.NewValue:0}px";
        PushLivePreview();
    }

    private void PushLivePreview()
    {
        if (!_initialized) return; // don't fire while InitializeComponent is still setting up fields
        ThemeApplier.Apply(
            MainColorBox.Text, SecondaryColorBox.Text, TertiaryColorBox.Text, TextColorBox.Text,
            CornerRadiusSlider.Value, ShadowCheck.IsChecked == true);
    }

    private void OnHeaderDrag(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed) DragMove();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        ThemeApplier.Apply(_settings.Current); // discard unsaved live-preview edits
        Close();
    }

    private void OnHotkeyPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
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
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.gif" };
        if (dialog.ShowDialog() == true)
            PfpPathBox.Text = dialog.FileName;
    }

    private void OnBrowseMediaImage(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.gif" };
        if (dialog.ShowDialog() == true)
            MediaImagePathBox.Text = dialog.FileName;
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
        s.TextColor = TextColorBox.Text.Trim();
        s.CornerRadius = CornerRadiusSlider.Value;
        s.ShadowEnabled = ShadowCheck.IsChecked == true;
        s.ProfilePicturePath = string.IsNullOrWhiteSpace(PfpPathBox.Text) ? null : PfpPathBox.Text.Trim();
        s.MediaImagePath = string.IsNullOrWhiteSpace(MediaImagePathBox.Text) ? null : MediaImagePathBox.Text.Trim();
        s.WeatherLabel = WeatherLabelBox.Text.Trim();

        if (double.TryParse(LatBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat)) s.WeatherLat = lat;
        if (double.TryParse(LonBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon)) s.WeatherLon = lon;
        s.WeatherFahrenheit = FahrenheitCheck.IsChecked == true;

        _settings.Save(); // raises SettingsChanged, which the panel listens to for the pfp refresh
        ThemeApplier.Apply(s); // idempotent — already live via preview, this just makes it official

        AutoStartService.SetEnabled(s.AutoStart);

        if (!_hotkey.Register(s.HotkeyModifiers, s.HotkeyKey))
            System.Windows.MessageBox.Show(this, "That key combo couldn't be registered — it may already be in use by " +
                "another app. Try a different key.", "Hotkey", MessageBoxButton.OK, MessageBoxImage.Warning);

        Close();
    }
}
