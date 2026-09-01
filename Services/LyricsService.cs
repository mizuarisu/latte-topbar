using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace TopBar.Services;

internal sealed class LyricsService : IDisposable
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(6) };

    public async Task<string?> GetPlainLyricsAsync(string artist, string title, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(artist) || string.IsNullOrWhiteSpace(title))
            return null;

        var url = $"https://lrclib.net/api/get?artist_name={Uri.EscapeDataString(artist)}" +
                   $"&track_name={Uri.EscapeDataString(title)}";
        try
        {
            var result = await _http.GetFromJsonAsync<LrcLibResult>(url, ct);
            return result?.PlainLyrics ?? result?.SyncedLyrics;
        }
        catch
        {
            return null; // no match / offline / rate-limited — widget just shows "No lyrics found"
        }
    }

    public void Dispose() => _http.Dispose();

    private sealed class LrcLibResult
    {
        [JsonPropertyName("plainLyrics")] public string? PlainLyrics { get; set; }
        [JsonPropertyName("syncedLyrics")] public string? SyncedLyrics { get; set; }
    }
}
