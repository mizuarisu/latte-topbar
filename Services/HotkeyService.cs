using System.Windows.Input;
using System.Windows.Interop;

namespace TopBar.Services;

public sealed class HotkeyService : IDisposable
{
    private const int HotkeyId = 0xB00F;
    private HwndSource? _source;

    public event Action? Pressed;

    /// <summary>Call once at startup with a hidden message-only window handle.</summary>
    public void Initialize(HwndSource source)
    {
        _source = source;
        _source.AddHook(WndProc);
    }

    /// <summary>Registers (replacing any previous registration) the combo from settings.</summary>
    public bool Register(string modifiersCsv, string keyName)
    {
        if (_source is null) return false;

        Win32.UnregisterHotKey(_source.Handle, HotkeyId);

        uint mods = Win32.MOD_NOREPEAT;
        foreach (var part in modifiersCsv.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            mods |= part switch
            {
                "Alt" => Win32.MOD_ALT,
                "Control" => Win32.MOD_CONTROL,
                "Shift" => Win32.MOD_SHIFT,
                "Windows" => Win32.MOD_WIN,
                _ => 0u
            };
        }

        if (!Enum.TryParse<Key>(keyName, out var key))
            return false;

        uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);
        return Win32.RegisterHotKey(_source.Handle, HotkeyId, mods, vk);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == Win32.WM_HOTKEY && wParam.ToInt32() == HotkeyId)
        {
            Pressed?.Invoke();
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_source is not null)
            Win32.UnregisterHotKey(_source.Handle, HotkeyId);
    }
}
