using System.Windows.Media;

namespace TopBar.Helpers;

internal static class ColorUtils
{
    public static Color Parse(string hex)
    {
        try { return (Color)ColorConverter.ConvertFromString(hex)!; }
        catch { return Color.FromRgb(0x33, 0x33, 0x33); }
    }

    /// <summary>Blends toward white by the given 0–1 amount.</summary>
    public static Color Lighten(Color c, double amount) => Blend(c, Colors.White, amount);

    /// <summary>Blends toward black by the given 0–1 amount.</summary>
    public static Color Darken(Color c, double amount) => Blend(c, Colors.Black, amount);

    private static Color Blend(Color c, Color target, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        byte Mix(byte a, byte b) => (byte)(a + (b - a) * amount);
        return Color.FromRgb(Mix(c.R, target.R), Mix(c.G, target.G), Mix(c.B, target.B));
    }

    /// <summary>A subtle top-to-bottom gradient for panel backgrounds — reads as depth rather than flat color.</summary>
    public static LinearGradientBrush VerticalDepthGradient(Color baseColor)
    {
        var brush = new LinearGradientBrush { StartPoint = new System.Windows.Point(0, 0), EndPoint = new System.Windows.Point(0, 1) };
        brush.GradientStops.Add(new GradientStop(Lighten(baseColor, 0.06), 0.0));
        brush.GradientStops.Add(new GradientStop(baseColor, 0.5));
        brush.GradientStops.Add(new GradientStop(Darken(baseColor, 0.08), 1.0));
        brush.Freeze();
        return brush;
    }

    /// <summary>A faint diagonal "glass edge" highlight — light at the top-left corner fading to transparent.</summary>
    public static LinearGradientBrush GlassEdgeBrush()
    {
        var brush = new LinearGradientBrush { StartPoint = new System.Windows.Point(0, 0), EndPoint = new System.Windows.Point(1, 1) };
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF), 0.0));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF), 0.5));
        brush.Freeze();
        return brush;
    }
}
