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
        Task ResumeAsync();
        Task SeekAsync(double positionSeconds);
        double GetElapsedSeconds();
        Task PrefetchClipAsync(string videoUrl);
        bool IsPlaying { get; }
    }
}
