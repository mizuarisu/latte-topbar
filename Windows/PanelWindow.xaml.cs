using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TopBar.Services;

namespace TopBar.Windows;

public partial class PanelWindow : Window
{
    private readonly SettingsService _settings;
    private readonly SystemStatsService _stats = new();
    private readonly WeatherService _weather = new();
    private readonly MediaService _media = new();
    private readonly LyricsService _lyrics = new();

    private string _lastLyricsKey = "";

    public PanelWindow(SettingsService settings)
    {
        InitializeComponent();
        _settings = settings;
        _settings.SettingsChanged += ApplyTheme;

        Loaded += async (_, _) =>
        {
            ApplyTheme();
            PositionTopCenter();
            StartClock();
            StartStatsLoop();
            StartWeatherLoop();

            await _media.InitializeAsync();
            _media.MediaChanged += () => Dispatcher.Invoke(RefreshMedia);
            RefreshMedia();
        };
    }

    // ---------- Visibility / positioning ----------

    public void Toggle()
    {
        if (Visibility == Visibility.Visible)
        {
            Hide();
        }
        else
        {
            PositionTopCenter();
            Show();
            Activate();
        }
    }

    private void PositionTopCenter()
    {
        Left = (SystemParameters.PrimaryScreenWidth - Width) / 2;
        Top = 8;
    }

    private void OnDeactivated(object sender, EventArgs e) => Hide();

    // ---------- Theme ----------

    private void ApplyTheme()
    {
        var s = _settings.Current;
        RootBorder.Background = Brush(s.MainColor);

        foreach (var tab in new[] { TabDashboard, TabMedia, TabPerformance, TabWeather })
            tab.Foreground = Brush(s.TextColor);

        CpuBar.Foreground = RamBar.Foreground = DiskBar.Foreground = Brush(s.TertiaryColor);
        CpuBar.Background = RamBar.Background = DiskBar.Background = Brush(s.SecondaryColor);
        PfpBorder.Background = Brush(s.SecondaryColor);

        foreach (var tb in new[] { ClockText, MediaTitleText, LyricsText, CpuLabel, RamLabel, DiskLabel,
                                    NetworkLabel, WeatherTempText })
            tb.Foreground = Brush(s.TextColor);

        if (!string.IsNullOrEmpty(s.ProfilePicturePath) && System.IO.File.Exists(s.ProfilePicturePath))
        {
            try { PfpImage.Source = new BitmapImage(new Uri(s.ProfilePicturePath)); }
            catch { /* bad file — leave placeholder */ }
        }
    }

    private static SolidColorBrush Brush(string hex)
    {
        try { return (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!; }
        catch { return System.Windows.Media.Brushes.Gray; }
    }

    // ---------- Tabs ----------

    private void OnTabClick(object sender, RoutedEventArgs e)
    {
        foreach (var tab in new[] { TabDashboard, TabMedia, TabPerformance, TabWeather })
            if (tab != sender) tab.IsChecked = false;
        ((ToggleButton)sender).IsChecked = true;

        DashboardPanel.Visibility = Visibility.Collapsed;
        MediaPanel.Visibility = Visibility.Collapsed;
        PerformancePanel.Visibility = Visibility.Collapsed;
        WeatherPanel.Visibility = Visibility.Collapsed;

        switch (((ToggleButton)sender).Tag)
        {
            case "Dashboard": DashboardPanel.Visibility = Visibility.Visible; break;
            case "Media": MediaPanel.Visibility = Visibility.Visible; break;
            case "Performance": PerformancePanel.Visibility = Visibility.Visible; break;
            case "Weather": WeatherPanel.Visibility = Visibility.Visible; break;
        }
    }

    // ---------- Dashboard ----------

    private void StartClock()
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (_, _) =>
        {
            ClockText.Text = DateTime.Now.ToString("HH:mm:ss");
            DateText.Text = DateTime.Now.ToString("dddd, MMMM d");
        };
        timer.Start();
    }

    // ---------- Performance ----------

    private void StartStatsLoop()
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        timer.Tick += (_, _) =>
        {
            var cpu = _stats.ReadCpuPercent();
            var (usedGb, totalGb, ramPct) = _stats.ReadRam();
            var diskPct = _stats.ReadDiskUsagePercent();
            var (downKbs, upKbs) = _stats.ReadNetworkKbps();

            CpuBar.Value = cpu; CpuLabel.Text = $"{cpu:0}%";
            RamBar.Value = ramPct; RamLabel.Text = $"{ramPct:0}% ({usedGb:0.1} / {totalGb:0.1} GB)";
            DiskBar.Value = diskPct; DiskLabel.Text = $"{diskPct:0}%";
            NetworkLabel.Text = $"↓ {downKbs:0} KB/s  ↑ {upKbs:0} KB/s";
        };
        timer.Start();
    }

    // ---------- Weather ----------

    private void StartWeatherLoop()
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(15) };
        timer.Tick += async (_, _) => await RefreshWeatherAsync();
        timer.Start();
        _ = RefreshWeatherAsync();
    }

    private async Task RefreshWeatherAsync()
    {
        var s = _settings.Current;
        _weather.Latitude = s.WeatherLat;
        _weather.Longitude = s.WeatherLon;
        WeatherLocationText.Text = s.WeatherLabel;

        var reading = await _weather.GetCurrentAsync();
        if (reading is null)
        {
            WeatherTempText.Text = "--°";
            WeatherDescText.Text = "Unavailable";
            return;
        }

        var temp = s.WeatherFahrenheit ? reading.Value.TempC * 9 / 5 + 32 : reading.Value.TempC;
        WeatherTempText.Text = $"{temp:0}°{(s.WeatherFahrenheit ? "F" : "C")}";
        WeatherDescText.Text = reading.Value.Description;
    }

    // ---------- Media ----------

    private async void RefreshMedia()
    {
        MediaTitleText.Text = string.IsNullOrEmpty(_media.Title) ? "Nothing playing" : _media.Title;
        MediaArtistText.Text = _media.Artist;

        if (!_media.IsSpotify || string.IsNullOrEmpty(_media.Title))
        {
            LyricsText.Text = _media.IsSpotify ? "" : "Lyrics are only fetched for Spotify.";
            return;
        }

        var key = $"{_media.Artist}::{_media.Title}";
        if (key == _lastLyricsKey) return;
        _lastLyricsKey = key;

        LyricsText.Text = "Looking up lyrics…";
        var lyrics = await _lyrics.GetPlainLyricsAsync(_media.Artist, _media.Title);
        // Guard against a fast track-skip racing the lookup
        if ($"{_media.Artist}::{_media.Title}" == key)
            LyricsText.Text = lyrics ?? "No lyrics found.";
    }
}
