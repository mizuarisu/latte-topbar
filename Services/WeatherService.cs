using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace TopBar.Services;

internal sealed class WeatherService : IDisposable
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(8) };

    // Defaults to Jakarta; change lat/lon or wire up geolocation as you like.
    public double Latitude { get; set; } = -6.2088;
    public double Longitude { get; set; } = 106.8456;

    public async Task<WeatherReading?> GetCurrentAsync(CancellationToken ct = default)
    {
        var url = $"https://api.open-meteo.com/v1/forecast?latitude={Latitude}&longitude={Longitude}" +
                  "&current=temperature_2m,weather_code&timezone=auto";
        try
        {
            var resp = await _http.GetFromJsonAsync<OpenMeteoResponse>(url, ct);
            if (resp?.Current is null) return null;
            return new WeatherReading(resp.Current.Temperature, DescribeCode(resp.Current.WeatherCode));
        }
        catch
        {
            return null; // widget just shows "--" on failure, never crashes the bar
        }
    }

    private static string DescribeCode(int code) => code switch
    {
        0 => "Clear",
        1 or 2 or 3 => "Cloudy",
        45 or 48 => "Fog",
        51 or 53 or 55 => "Drizzle",
        61 or 63 or 65 => "Rain",
        71 or 73 or 75 => "Snow",
        80 or 81 or 82 => "Showers",
        95 => "Storm",
        _ => "—"
    };

    public void Dispose() => _http.Dispose();

    private sealed class OpenMeteoResponse
    {
        [JsonPropertyName("current")] public CurrentBlock? Current { get; set; }
        public sealed class CurrentBlock
        {
            [JsonPropertyName("temperature_2m")] public double Temperature { get; set; }
            [JsonPropertyName("weather_code")] public int WeatherCode { get; set; }
        }
    }
}

internal readonly record struct WeatherReading(double TempC, string Description);
