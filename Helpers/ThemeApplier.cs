using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using TopBar.Models;

namespace TopBar.Helpers;

internal static class ThemeApplier
{
    public static void Apply(AppSettings s)
        => Apply(s.MainColor, s.SecondaryColor, s.TertiaryColor, s.TextColor, s.CornerRadius, s.ShadowEnabled);

    /// <summary>
    /// Pushes the 4 user-chosen colors plus shape/elevation settings into live app resources.
    /// Safe to call on every keystroke while editing — invalid/partial hex just falls back to a
    /// neutral gray via ColorUtils.Parse rather than throwing, so typing doesn't glitch the preview.
    /// </summary>
    public static void Apply(string mainHex, string cardHex, string accentHex, string textHex,
        double cornerRadius, bool shadowEnabled)
    {
        var app = Application.Current;
        if (app is null) return;

        var surface = ColorUtils.Parse(mainHex);
        var card = ColorUtils.Parse(cardHex);
        var accent = ColorUtils.Parse(accentHex);
        var text = ColorUtils.Parse(textHex);

        app.Resources["ThemeSurfaceBrush"] = new SolidColorBrush(surface);
        app.Resources["ThemeCardBrush"] = new SolidColorBrush(card);
        app.Resources["ThemeCardBorderBrush"] = new SolidColorBrush(ColorUtils.Lighten(card, 0.14));
        app.Resources["ThemeElevatedBrush"] = new SolidColorBrush(ColorUtils.Lighten(card, 0.08));
        app.Resources["ThemeElevatedHoverBrush"] = new SolidColorBrush(ColorUtils.Lighten(card, 0.16));

        app.Resources["ThemeAccentBrush"] = new SolidColorBrush(accent);
        app.Resources["ThemeAccentHoverBrush"] = new SolidColorBrush(ColorUtils.Lighten(accent, 0.15));
        app.Resources["ThemeAccentPressedBrush"] = new SolidColorBrush(ColorUtils.Darken(accent, 0.15));
        app.Resources["ThemeTabSelectedBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0x55, accent.R, accent.G, accent.B));
        app.Resources["ThemeAccentSoftBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0x2A, accent.R, accent.G, accent.B));

        app.Resources["ThemeTextBrush"] = new SolidColorBrush(text);
        app.Resources["ThemeMutedTextBrush"] = new SolidColorBrush(ColorUtils.Darken(text, 0.35));
        app.Resources["ThemeFaintTextBrush"] = new SolidColorBrush(ColorUtils.Darken(text, 0.55));

        app.Resources["ThemeCardRadius"] = new CornerRadius(cornerRadius);
        app.Resources["ThemePanelRadius"] = new CornerRadius(cornerRadius + 6);
        app.Resources["ThemeFieldRadius"] = new CornerRadius(Math.Max(4, cornerRadius - 10));

        app.Resources["ThemeCardShadow"] = shadowEnabled
            ? new DropShadowEffect { Color = Colors.Black, Opacity = 0.28, BlurRadius = 18, ShadowDepth = 4, Direction = 270 }
            : null;
    }
}
