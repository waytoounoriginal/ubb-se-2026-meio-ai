namespace ubb_se_2026_meio_ai.Features.ReelsFeed.Services
{
    /// <summary>
    /// Controls video playback state for the auto-playing feed.
    /// Owner: Tudor
    /// </summary>
    public interface IClipPlaybackService
    {
        Task PlayAsync(string videoUrl);
        Task PauseAsync();
        Task SeekAsync(double positionSeconds);
        bool IsPlaying { get; }
    }
}
