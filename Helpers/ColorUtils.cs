using System.Windows.Media;

namespace TopBar.Helpers;

internal static class ColorUtils
{
    public static Color Parse(string hex)
    {
        try { return (Color)ColorConverter.ConvertFromString(hex)!; }
        catch { return Color.FromRgb(0x33, 0x33, 0x33); }
    }

    public static SolidColorBrush ParseBrush(string hex) => new(Parse(hex));

    /// <summary>Blends toward white by the given 0–1 amount — used for hover states and elevated surfaces.</summary>
    public static Color Lighten(Color c, double amount) => Blend(c, Colors.White, amount);

    /// <summary>Blends toward black by the given 0–1 amount — used for pressed states and muted text.</summary>
    public static Color Darken(Color c, double amount) => Blend(c, Colors.Black, amount);

    private static Color Blend(Color c, Color target, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        byte Mix(byte a, byte b) => (byte)(a + (b - a) * amount);
        return Color.FromRgb(Mix(c.R, target.R), Mix(c.G, target.G), Mix(c.B, target.B));
    }
}

