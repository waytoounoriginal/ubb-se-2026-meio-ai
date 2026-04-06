namespace ubb_se_2026_meio_ai.Features.ReelsFeed.Services
{
    /// <summary>
    /// Provides clip source prefetching for the auto-playing feed.
    /// Owner: Tudor.
    /// </summary>
    public interface IClipPlaybackService
    {
        /// <summary>
        /// Prefetches and caches a media source for the provided clip URL.
        /// Existing cached URLs are skipped, and invalid URLs are ignored silently.
        /// </summary>
        /// <param name="videoUrl">The clip URL to prefetch.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task PrefetchClipAsync(string videoUrl);
    }
}
