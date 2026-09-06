namespace TopBar.Models;

public sealed class AppSettings
{
    // Hotkey: modifiers is a comma-separated combo of Alt/Control/Shift/Windows.
    // Key is the WPF Key enum name (e.g. "Q", "Space", "OemTilde").
    public string HotkeyModifiers { get; set; } = "Alt";
    public string HotkeyKey { get; set; } = "Q";

    public bool AutoStart { get; set; } = false;

    // Three-tier theme, hex strings so they serialize/edit cleanly
    public string MainColor { get; set; } = "#1E1E2E";      // panel background
    public string SecondaryColor { get; set; } = "#313244"; // cards / tab strip
    public string TertiaryColor { get; set; } = "#89B4FA";  // accent (active tab, highlights)
    public string TextColor { get; set; } = "#CDD6F4";

    // Shape/elevation — the other half of "make it customizable" alongside color
    public double CornerRadius { get; set; } = 20;
    public bool ShadowEnabled { get; set; } = true;

    public double WeatherLat { get; set; } = -6.2088;
    public double WeatherLon { get; set; } = 106.8456;
    public string WeatherLabel { get; set; } = "Jakarta";
    public bool WeatherFahrenheit { get; set; } = false;

    public string? ProfilePicturePath { get; set; } = null;
}
