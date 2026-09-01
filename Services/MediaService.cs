using Windows.Media.Control;

namespace TopBar.Services;

internal sealed class MediaService
{
    private GlobalSystemMediaTransportControlsSessionManager? _manager;

    public event Action? MediaChanged;

    public string Title { get; private set; } = "";
    public string Artist { get; private set; } = "";
    public bool IsPlaying { get; private set; }
    public string SourceAppId { get; private set; } = "";
    public bool IsSpotify => SourceAppId.Contains("spotify", StringComparison.OrdinalIgnoreCase);

    public async Task InitializeAsync()
    {
        _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        _manager.CurrentSessionChanged += (_, _) => HookCurrentSession();
        HookCurrentSession();
    }

    private void HookCurrentSession()
    {
        var session = _manager?.GetCurrentSession();
        if (session is null)
        {
            Title = "";
            Artist = "";
            IsPlaying = false;
            SourceAppId = "";
            MediaChanged?.Invoke();
            return;
        }

        SourceAppId = session.SourceAppUserModelId ?? "";
        session.MediaPropertiesChanged += async (s, _) => await RefreshPropertiesAsync(s);
        session.PlaybackInfoChanged += (s, _) =>
        {
            IsPlaying = s.GetPlaybackInfo().PlaybackStatus ==
                        GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
            MediaChanged?.Invoke();
        };

        _ = RefreshPropertiesAsync(session);
    }

    private async Task RefreshPropertiesAsync(GlobalSystemMediaTransportControlsSession session)
    {
        var props = await session.TryGetMediaPropertiesAsync();
        Title = props.Title ?? "";
        Artist = props.Artist ?? "";
        MediaChanged?.Invoke();
    }

    public void TogglePlayPause() => _manager?.GetCurrentSession()?.TryTogglePlayPauseAsync();
    public void Next() => _manager?.GetCurrentSession()?.TrySkipNextAsync();
    public void Previous() => _manager?.GetCurrentSession()?.TrySkipPreviousAsync();
}
