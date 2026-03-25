using System.Diagnostics;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace ubb_se_2026_meio_ai.Features.ReelsFeed.Services
{
    /// <summary>
    /// Tracks playback state and prefetches clip media sources.
    /// Owner: Tudor
    /// </summary>
    public class ClipPlaybackService : IClipPlaybackService
    {
        private readonly Dictionary<string, MediaSource> _prefetchedSources = new();
        private readonly Stopwatch _elapsed = new();

        public bool IsPlaying { get; private set; }

        public Task PlayAsync(string videoUrl)
        {
            IsPlaying = true;
            _elapsed.Restart();
            return Task.CompletedTask;
        }

        public Task PauseAsync()
        {
            IsPlaying = false;
            _elapsed.Stop();
            return Task.CompletedTask;
        }

        public Task ResumeAsync()
        {
            IsPlaying = true;
            _elapsed.Start();
            return Task.CompletedTask;
        }

        public Task SeekAsync(double positionSeconds)
        {
            return Task.CompletedTask;
        }

        public double GetElapsedSeconds()
        {
            return _elapsed.Elapsed.TotalSeconds;
        }

        public Task PrefetchClipAsync(string videoUrl)
        {
            if (!string.IsNullOrEmpty(videoUrl) && !_prefetchedSources.ContainsKey(videoUrl))
            {
                try
                {
                    _prefetchedSources[videoUrl] = MediaSource.CreateFromUri(new Uri(videoUrl));
                }
                catch { } // Ignore bad URIs silently
            }
            return Task.CompletedTask;
        }

        public MediaSource GetMediaSource(string videoUrl)
        {
            if (_prefetchedSources.TryGetValue(videoUrl, out var source))
            {
                return source;
            }
            return MediaSource.CreateFromUri(new Uri(videoUrl));
        }
    }
}
