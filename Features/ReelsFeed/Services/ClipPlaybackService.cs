namespace ubb_se_2026_meio_ai.Features.ReelsFeed.Services
{
    /// <summary>
    /// Prefetches clip URLs and returns transmission data for feed playback.
    /// Media object creation is intentionally handled outside this service.
    /// Owner: Tudor.
    /// </summary>
    public class ClipPlaybackService : IClipPlaybackService
    {
        private readonly HashSet<string> _prefetchedClipUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <inheritdoc />
        public Task PrefetchClipAsync(string videoUrl)
        {
            if (!string.IsNullOrWhiteSpace(videoUrl))
            {
                this._prefetchedClipUrls.Add(videoUrl);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets transmission data for the specified clip URL.
        /// Removes prefetch marker when consumed to preserve one-time handoff semantics.
        /// </summary>
        /// <param name="videoUrl">The clip URL.</param>
        /// <returns>A DTO containing playback transmission metadata.</returns>
        public ClipMediaSourceTransmission GetClipTransmission(string videoUrl)
        {
            bool wasPrefetched = this._prefetchedClipUrls.Remove(videoUrl);
            return new ClipMediaSourceTransmission
            {
                VideoUrl = videoUrl,
                WasPrefetched = wasPrefetched,
            };
        }
    }
}
