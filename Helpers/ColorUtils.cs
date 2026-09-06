using WpfColor = System.Windows.Media.Color;
using WpfColorConverter = System.Windows.Media.ColorConverter;
using WpfColors = System.Windows.Media.Colors;
using System.Windows.Media;

namespace TopBar.Helpers;

internal static class ColorUtils
{
    public static WpfColor Parse(string hex)
    {
        try { return (WpfColor)WpfColorConverter.ConvertFromString(hex)!; }
        catch { return WpfColor.FromRgb(0x33, 0x33, 0x33); }
    }

    public static SolidColorBrush ParseBrush(string hex) => new(Parse(hex));

    /// <summary>Blends toward white by the given 0–1 amount — used for hover states and elevated surfaces.</summary>
    public static WpfColor Lighten(WpfColor c, double amount) => Blend(c, WpfColors.White, amount);

    /// <summary>Blends toward black by the given 0–1 amount — used for pressed states and muted text.</summary>
    public static WpfColor Darken(WpfColor c, double amount) => Blend(c, WpfColors.Black, amount);

    private static WpfColor Blend(WpfColor c, WpfColor target, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        byte Mix(byte a, byte b) => (byte)(a + (b - a) * amount);
        return WpfColor.FromRgb(Mix(c.R, target.R), Mix(c.G, target.G), Mix(c.B, target.B));
    }
}
