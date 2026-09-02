using Windows.Media;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace TopBar.Services;

public sealed class MediaService
{
    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private GlobalSystemMediaTransportControlsSession? _currentSession;

    public event Action? MediaChanged;
    public event Action? TimelineChanged;

    public string Title { get; private set; } = "";
    public string Artist { get; private set; } = "";
    public string Album { get; private set; } = "";
    public bool IsPlaying { get; private set; }
    public string SourceAppId { get; private set; } = "";
    public bool IsSpotify => SourceAppId.Contains("spotify", StringComparison.OrdinalIgnoreCase);
    public byte[]? AlbumArt { get; private set; }

    public TimeSpan Position { get; private set; }
    public TimeSpan Duration { get; private set; }
    public bool IsShuffleActive { get; private set; }
    public MediaPlaybackAutoRepeatMode? RepeatMode { get; private set; }

    public async Task InitializeAsync()
    {
        _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        _manager.CurrentSessionChanged += (_, _) => HookCurrentSession();
        HookCurrentSession();
    }

    /// <summary>Call periodically (e.g. every second) while the Media tab is visible — most
    /// apps don't fire timeline change events reliably, polling is the robust option.</summary>
    public void PollTimeline()
    {
        var timeline = _currentSession?.GetTimelineProperties();
        if (timeline is null) return;
        Position = timeline.Position;
        Duration = timeline.EndTime - timeline.StartTime;
        TimelineChanged?.Invoke();
    }

    private void HookCurrentSession()
    {
        _currentSession = _manager?.GetCurrentSession();
        var session = _currentSession;

        if (session is null)
        {
            Title = ""; Artist = ""; Album = ""; IsPlaying = false; SourceAppId = ""; AlbumArt = null;
            MediaChanged?.Invoke();
            return;
        }

        SourceAppId = session.SourceAppUserModelId ?? "";
        session.MediaPropertiesChanged += async (s, _) => await RefreshPropertiesAsync(s);
        session.PlaybackInfoChanged += (s, _) =>
        {
            var info = s.GetPlaybackInfo();
            IsPlaying = info.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
            IsShuffleActive = info.IsShuffleActive ?? false;
            RepeatMode = info.AutoRepeatMode;
            MediaChanged?.Invoke();
        };

        _ = RefreshPropertiesAsync(session);
    }

    private async Task RefreshPropertiesAsync(GlobalSystemMediaTransportControlsSession session)
    {
        var props = await session.TryGetMediaPropertiesAsync();
        Title = props.Title ?? "";
        Artist = props.Artist ?? "";
        Album = props.AlbumTitle ?? "";
        AlbumArt = await ReadThumbnailAsync(props.Thumbnail);
        MediaChanged?.Invoke();
    }

    private static async Task<byte[]?> ReadThumbnailAsync(IRandomAccessStreamReference? thumbRef)
    {
        if (thumbRef is null) return null;
        try
        {
            using var stream = await thumbRef.OpenReadAsync();
            using var netStream = stream.AsStreamForRead();
            using var ms = new MemoryStream();
            await netStream.CopyToAsync(ms);
            return ms.ToArray();
        }
        catch
        {
            return null; // some sources don't provide art — widget falls back to a placeholder
        }
    }

    public void TogglePlayPause() => _currentSession?.TryTogglePlayPauseAsync();
    public void Next() => _currentSession?.TrySkipNextAsync();
    public void Previous() => _currentSession?.TrySkipPreviousAsync();

    public void ToggleShuffle() => _currentSession?.TryChangeShuffleActiveAsync(!IsShuffleActive);

    public void CycleRepeat()
    {
        var next = RepeatMode switch
        {
            MediaPlaybackAutoRepeatMode.None or null => MediaPlaybackAutoRepeatMode.Track,
            MediaPlaybackAutoRepeatMode.Track => MediaPlaybackAutoRepeatMode.List,
            _ => MediaPlaybackAutoRepeatMode.None
        };
        _currentSession?.TryChangeAutoRepeatModeAsync(next);
    }
}

