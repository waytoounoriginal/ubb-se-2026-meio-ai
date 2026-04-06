namespace ubb_se_2026_meio_ai.Features.ReelsFeed.Services
{
    /// <summary>
    /// Controls logical playback state for the auto-playing feed.
    /// Current implementation tracks playback time using an internal stopwatch
    /// and supports clip source prefetching.
    /// Owner: Tudor.
    /// </summary>
    public interface IClipPlaybackService
    {
        /// <summary>
        /// Gets a value indicating whether playback is currently marked as active by the service.
        /// </summary>
        bool IsPlaying { get; }

        /// <summary>
        /// Marks playback as active and restarts elapsed-time tracking from zero.
        /// </summary>
        /// <param name="videoUrl">The clip URL associated with the play request.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task PlayAsync(string videoUrl);

        /// <summary>
        /// Marks playback as paused and stops elapsed-time tracking.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task PauseAsync();

        /// <summary>
        /// Marks playback as active and resumes elapsed-time tracking.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ResumeAsync();

        /// <summary>
        /// Seeks playback to the specified position.
        /// In the current implementation, this is a no-op placeholder.
        /// </summary>
        /// <param name="positionSeconds">Target playback position in seconds.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SeekAsync(double positionSeconds);

        /// <summary>
        /// Gets the total elapsed playback time in seconds tracked by the service stopwatch.
        /// </summary>
        /// <returns>Elapsed playback time in seconds.</returns>
        double GetElapsedSeconds();

        /// <summary>
        /// Prefetches and caches a media source for the provided clip URL.
        /// Existing cached URLs are skipped, and invalid URLs are ignored silently.
        /// </summary>
        /// <param name="videoUrl">The clip URL to prefetch.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task PrefetchClipAsync(string videoUrl);
    }
}
