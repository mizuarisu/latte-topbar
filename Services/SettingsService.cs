using System.IO;
using System.Text.Json;
using TopBar.Models;

namespace TopBar.Services;

public sealed class SettingsService
{
    private static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TopBar");
    private static readonly string FilePath = Path.Combine(Dir, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public AppSettings Current { get; private set; } = new();

    public event Action? SettingsChanged;

    public void Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                Current = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
            // Corrupt/unreadable file — fall back to defaults rather than crash on startup
            Current = new AppSettings();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(Dir);
        var json = JsonSerializer.Serialize(Current, JsonOptions);
        File.WriteAllText(FilePath, json);
        SettingsChanged?.Invoke();
    }
}
