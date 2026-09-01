using System.Windows;
using System.Windows.Interop;
using TopBar.Services;
using TopBar.Windows;
using Forms = System.Windows.Forms;

namespace TopBar;

public partial class App : System.Windows.Application
{
    private readonly SettingsService _settingsService = new();
    private readonly HotkeyService _hotkeyService = new();
    private PanelWindow? _panel;
    private Forms.NotifyIcon? _trayIcon;
    private HwndSource? _messageSource;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown; // no bar/panel is a "main window" to close on

        _settingsService.Load();

        // A hidden message-only window purely to receive WM_HOTKEY
        var parameters = new HwndSourceParameters("TopBarHotkeySink") { Width = 0, Height = 0 };
        _messageSource = new HwndSource(parameters);
        _hotkeyService.Initialize(_messageSource);
        _hotkeyService.Register(_settingsService.Current.HotkeyModifiers, _settingsService.Current.HotkeyKey);
        _hotkeyService.Pressed += () => _panel?.Toggle();

        _panel = new PanelWindow(_settingsService);

        SetupTrayIcon();
    }

    private void SetupTrayIcon()
    {
        _trayIcon = new Forms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Visible = true,
            Text = "TopBar"
        };

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Toggle panel", null, (_, _) => _panel?.Toggle());
        menu.Items.Add("Settings…", null, (_, _) => OpenSettings());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => Shutdown());
        _trayIcon.ContextMenuStrip = menu;

        _trayIcon.DoubleClick += (_, _) => _panel?.Toggle();
    }

    private void OpenSettings()
    {
        var window = new SettingsWindow(_settingsService, _hotkeyService);
        window.ShowDialog();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        _hotkeyService.Dispose();
        base.OnExit(e);
    }
}
