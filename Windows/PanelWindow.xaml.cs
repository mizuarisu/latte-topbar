using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using TopBar.Services;
using Windows.Media;

namespace TopBar.Windows;

public partial class PanelWindow : Window
{
    private readonly SettingsService _settings;
    private readonly WeatherService _weather = new();
    private readonly MediaService _media = new();
    private readonly LyricsService _lyrics = new();

    private DateTime _calendarMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private string _lastLyricsKey = "";
    private readonly List<System.Windows.Shapes.Rectangle> _visualizerBars = new();
    private readonly Random _rng = new();

    public PanelWindow(SettingsService settings)
    {
        InitializeComponent();
        _settings = settings;
        _settings.SettingsChanged += ApplyTheme;

        Loaded += async (_, _) =>
        {
            ApplyTheme();
            PositionTopCenter();
            BuildVisualizerRing();
            BuildCalendarWeekdayHeader();
            RenderCalendar();

            StartClock();
            StartWeatherLoop();
            StartUptimeLoop();
            StartMediaTimelineLoop();
            StartVisualizerLoop();

            await _media.InitializeAsync();
            _media.MediaChanged += () => Dispatcher.Invoke(RefreshMedia);
            RefreshMedia();
        };
    }

    // ---------- Visibility / positioning ----------

    public void Toggle()
    {
        if (Visibility == Visibility.Visible) { Hide(); }
        else { PositionTopCenter(); Show(); Activate(); }
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
        foreach (var tab in new[] { TabDashboard, TabMedia })
            if (tab != sender) tab.IsChecked = false;
        ((ToggleButton)sender).IsChecked = true;

        DashboardPanel.Visibility = Visibility.Collapsed;
        MediaTabPanel.Visibility = Visibility.Collapsed;

        switch (((ToggleButton)sender).Tag)
        {
            case "Dashboard": DashboardPanel.Visibility = Visibility.Visible; break;
            case "Media": MediaTabPanel.Visibility = Visibility.Visible; break;
        }
    }

    // ---------- Dashboard: clock/date ----------

    private void StartClock()
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (_, _) =>
        {
            ClockText.Text = DateTime.Now.ToString("h:mm");
            AmPmText.Text = DateTime.Now.ToString("tt");
            DateText.Text = DateTime.Now.ToString("dddd, MMM d");
        };
        timer.Start();
        ClockText.Text = DateTime.Now.ToString("h:mm");
        AmPmText.Text = DateTime.Now.ToString("tt");
        DateText.Text = DateTime.Now.ToString("dddd, MMM d");
    }

    // ---------- Dashboard: uptime / profile ----------

    private void StartUptimeLoop()
    {
        ProfileNameText.Text = Environment.UserName;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        timer.Tick += (_, _) => UptimeText.Text = FormatUptime();
        timer.Start();
        UptimeText.Text = FormatUptime();
    }

    private static string FormatUptime()
    {
        var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
        return uptime.TotalHours >= 1
            ? $"up {(int)uptime.TotalHours}h {uptime.Minutes}m"
            : $"up {uptime.Minutes}m";
    }

    // ---------- Dashboard: weather ----------

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
        WeatherIconText.Text = reading.Value.Description switch
        {
            "Clear" => "☀",
            "Cloudy" => "☁",
            "Fog" => "🌫",
            "Drizzle" or "Rain" or "Showers" => "🌧",
            "Snow" => "❄",
            "Storm" => "⛈",
            _ => "⛅"
        };
    }

    // ---------- Dashboard: calendar ----------

    private void BuildCalendarWeekdayHeader()
    {
        foreach (var label in new[] { "S", "M", "T", "W", "T", "F", "S" })
        {
            CalendarWeekdayHeader.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 10,
                Foreground = Brush("#5C6079"),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 2)
            });
        }
    }

    private void OnCalendarPrev(object sender, RoutedEventArgs e)
    {
        _calendarMonth = _calendarMonth.AddMonths(-1);
        RenderCalendar();
    }

    private void OnCalendarNext(object sender, RoutedEventArgs e)
    {
        _calendarMonth = _calendarMonth.AddMonths(1);
        RenderCalendar();
    }

    private void RenderCalendar()
    {
        CalendarMonthText.Text = _calendarMonth.ToString("MMMM yyyy");
        CalendarDaysGrid.Children.Clear();

        int daysInMonth = DateTime.DaysInMonth(_calendarMonth.Year, _calendarMonth.Month);
        int leadingBlanks = (int)_calendarMonth.DayOfWeek; // Sunday = 0

        for (int i = 0; i < leadingBlanks; i++)
            CalendarDaysGrid.Children.Add(new TextBlock());

        for (int day = 1; day <= daysInMonth; day++)
        {
            bool isToday = _calendarMonth.Year == DateTime.Today.Year
                         && _calendarMonth.Month == DateTime.Today.Month
                         && day == DateTime.Today.Day;

            var cell = new Border
            {
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(1),
                Background = isToday ? Brush("#89B4FA") : System.Windows.Media.Brushes.Transparent,
                Child = new TextBlock
                {
                    Text = day.ToString(),
                    FontSize = 11,
                    Foreground = isToday ? Brush("#1B1B29") : Brush("#CDD6F4"),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    VerticalAlignment = System.Windows.VerticalAlignment.Center
                }
            };
            CalendarDaysGrid.Children.Add(cell);
        }
    }

    // ---------- Media ----------

    private async void RefreshMedia()
    {
        bool hasMedia = !string.IsNullOrEmpty(_media.Title);

        MediaTitleText.Text = hasMedia ? _media.Title : "Nothing playing";
        MediaArtistText.Text = _media.Artist;
        MediaAlbumText.Text = _media.Album;
        LiteTitleText.Text = hasMedia ? _media.Title : "No media";
        LiteArtistText.Text = _media.Artist;

        PlayPauseButton.Content = _media.IsPlaying ? "⏸" : "▶";
        LitePlayButton.Content = _media.IsPlaying ? "⏸" : "▶";
        ShuffleButton.IsChecked = _media.IsShuffleActive;
        RepeatButton.IsChecked = _media.RepeatMode is MediaPlaybackAutoRepeatMode.Track or MediaPlaybackAutoRepeatMode.List;

        if (_media.AlbumArt is { Length: > 0 })
        {
            try
            {
                using var ms = new MemoryStream(_media.AlbumArt);
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = ms;
                bmp.EndInit();
                bmp.Freeze();
                AlbumImage.Source = bmp;
                LiteAlbumImage.Source = bmp;
            }
            catch { AlbumImage.Source = null; LiteAlbumImage.Source = null; }
        }
        else
        {
            AlbumImage.Source = null;
            LiteAlbumImage.Source = null;
        }

        SpotifyBadge.Visibility = _media.IsSpotify ? Visibility.Visible : Visibility.Collapsed;

        if (!_media.IsSpotify || !hasMedia)
        {
            LyricsText.Text = hasMedia ? "" : "Nothing playing.";
            _lastLyricsKey = "";
            return;
        }

        var key = $"{_media.Artist}::{_media.Title}";
        if (key == _lastLyricsKey) return;
        _lastLyricsKey = key;

        LyricsText.Text = "Looking up lyrics…";
        var lyrics = await _lyrics.GetPlainLyricsAsync(_media.Artist, _media.Title);
        if ($"{_media.Artist}::{_media.Title}" == key)
            LyricsText.Text = lyrics ?? "No lyrics found.";
    }

    private void StartMediaTimelineLoop()
    {
        _media.TimelineChanged += () => Dispatcher.Invoke(UpdateProgressUi);
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (_, _) => _media.PollTimeline();
        timer.Start();
    }

    private void UpdateProgressUi()
    {
        PositionText.Text = FormatTime(_media.Position);
        DurationText.Text = FormatTime(_media.Duration);

        double fraction = _media.Duration.TotalSeconds > 0
            ? Math.Clamp(_media.Position.TotalSeconds / _media.Duration.TotalSeconds, 0, 1)
            : 0;
        ProgressFillBar.Width = ProgressTrack.ActualWidth * fraction;
    }

    private static string FormatTime(TimeSpan t) => t.TotalHours >= 1
        ? t.ToString(@"h\:mm\:ss")
        : t.ToString(@"m\:ss");

    private void OnPlayPauseClick(object sender, RoutedEventArgs e) => _media.TogglePlayPause();
    private void OnPrevClick(object sender, RoutedEventArgs e) => _media.Previous();
    private void OnNextClick(object sender, RoutedEventArgs e) => _media.Next();
    private void OnShuffleClick(object sender, RoutedEventArgs e) => _media.ToggleShuffle();
    private void OnRepeatClick(object sender, RoutedEventArgs e) => _media.CycleRepeat();

    // ---------- Visualizer ring (decorative — not real audio spectrum analysis) ----------

    private void BuildVisualizerRing()
    {
        const int barCount = 40;
        const double radius = 100;
        const double centerX = 110, centerY = 110;

        for (int i = 0; i < barCount; i++)
        {
            double angle = i * (360.0 / barCount) * Math.PI / 180.0;
            var bar = new System.Windows.Shapes.Rectangle
            {
                Width = 3,
                Height = 6,
                RadiusX = 1.5,
                RadiusY = 1.5,
                Fill = Brush("#45475A")
            };

            double x = centerX + radius * Math.Cos(angle);
            double y = centerY + radius * Math.Sin(angle);

            bar.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
            bar.RenderTransform = new RotateTransform(angle * 180.0 / Math.PI + 90);
            Canvas.SetLeft(bar, x - bar.Width / 2);
            Canvas.SetTop(bar, y - bar.Height / 2);

            VisualizerCanvas.Children.Add(bar);
            _visualizerBars.Add(bar);
        }
    }

    private void StartVisualizerLoop()
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(220) };
        timer.Tick += (_, _) =>
        {
            if (!_media.IsPlaying || MediaTabPanel.Visibility != Visibility.Visible)
            {
                foreach (var bar in _visualizerBars) { bar.Height = 6; bar.Fill = Brush("#45475A"); }
                return;
            }

            foreach (var bar in _visualizerBars)
            {
                bar.Height = 6 + _rng.NextDouble() * 22;
                bar.Fill = Brush("#89B4FA");
            }
        };
        timer.Start();
    }
}
