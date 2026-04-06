using System.Diagnostics;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace ubb_se_2026_meio_ai.Features.ReelsFeed.Services
{
    /// <summary>
    /// Tracks playback state and prefetches clip media sources.
    /// Implements IDisposable to clean up cached MediaSource COM objects at shutdown.
    /// Owner: Tudor.
    /// </summary>
    public class ClipPlaybackService : IClipPlaybackService, IDisposable
    {
        private readonly Dictionary<string, MediaSource> _prefetchedMediaSources = [];
        private readonly Stopwatch _elapsedStopwatch = new ();

        /// <inheritdoc />
        public bool IsPlaying { get; private set; }

        /// <inheritdoc />
        public Task PlayAsync(string videoUrl)
        {
            this.IsPlaying = true;
            this._elapsedStopwatch.Restart();
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task PauseAsync()
        {
            this.IsPlaying = false;
            this._elapsedStopwatch.Stop();
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task ResumeAsync()
        {
            this.IsPlaying = true;
            this._elapsedStopwatch.Start();
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task SeekAsync(double positionSeconds)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public double GetElapsedSeconds()
        {
            return this._elapsedStopwatch.Elapsed.TotalSeconds;
        }

        /// <inheritdoc />
        public Task PrefetchClipAsync(string videoUrl)
        {
            if (!string.IsNullOrEmpty(videoUrl) && !this._prefetchedMediaSources.ContainsKey(videoUrl))
            {
                try
                {
                    this._prefetchedMediaSources[videoUrl] = MediaSource.CreateFromUri(new Uri(videoUrl));
                }
                catch
                {
                    // Ignore bad URIs silently.
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets a media source for the specified clip URL.
        /// Reuses and removes a prefetched source when available; otherwise creates a new source.
        /// </summary>
        /// <param name="videoUrl">The clip URL.</param>
        /// <returns>A media source instance suitable for a player.</returns>
        public MediaSource GetMediaSource(string videoUrl)
        {
            // Remove from cache on retrieval — each MediaPlayer must own its own
            // MediaSource COM object. Sharing a single source across multiple players
            // causes COM access violations when one player disposes or recycles it.
            if (this._prefetchedMediaSources.Remove(videoUrl, out var prefetchedMediaSource))
            {
                return prefetchedMediaSource;
            }

            return MediaSource.CreateFromUri(new Uri(videoUrl));
        }

        /// <summary>
        /// Disposes all cached media sources and clears the prefetch cache.
        /// </summary>
        public void Dispose()
        {
            foreach (var cachedMediaSource in this._prefetchedMediaSources.Values)
            {
                try
                {
                    cachedMediaSource.Dispose();
                }
                catch
                {
                }
            }

            this._prefetchedMediaSources.Clear();
        }
    }
}
