using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace ubb_se_2026_meio_ai.Features.TrailerScraping.Services
{
    /// <summary>
    /// Downloads YouTube videos as MP4 files using yt-dlp.
    /// Stores them in a local folder and returns the file path.
    /// Owner: Andrei
    /// </summary>
    public class VideoDownloadService
    {
        private readonly string _downloadFolder;
        private readonly YoutubeDL _ytdl;
        private bool _isInitialized;

        public VideoDownloadService(string? downloadFolder = null)
        {
            _downloadFolder = downloadFolder
                ?? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MeioAI", "Videos");

            Directory.CreateDirectory(_downloadFolder);

            _ytdl = new YoutubeDL
            {
                YoutubeDLPath = "yt-dlp.exe",
                FFmpegPath = "ffmpeg.exe",
                OutputFolder = _downloadFolder,
                OutputFileTemplate = "%(id)s.%(ext)s",
            };
        }

        /// <summary>
        /// Ensures yt-dlp and ffmpeg binaries are available.
        /// Downloads them automatically if missing.
        /// </summary>
        public async Task EnsureDependenciesAsync()
        {
            if (_isInitialized)
            {
                return;
            }

            // Try to auto-download yt-dlp and ffmpeg if not found on PATH
            try
            {
                await YoutubeDLSharp.Utils.DownloadYtDlp(_downloadFolder);
                await YoutubeDLSharp.Utils.DownloadFFmpeg(_downloadFolder);
                _ytdl.YoutubeDLPath = Path.Combine(_downloadFolder, "yt-dlp.exe");
                _ytdl.FFmpegPath = Path.Combine(_downloadFolder, "ffmpeg.exe");
            }
            catch
            {
                // If auto-download fails, hope that yt-dlp and ffmpeg are on PATH
            }

            _isInitialized = true;
        }

        /// <summary>
        /// Downloads a YouTube video as MP4 and returns the local file path.
        /// Returns null if the download fails.
        /// </summary>
        /// <param name="youtubeUrl">Full YouTube URL (e.g. https://www.youtube.com/watch?v=...)</param>
        /// <param name="maxDurationSeconds">Maximum duration to download (0 = full video).</param>
        public async Task<string?> DownloadVideoAsMp4Async(string youtubeUrl, int maxDurationSeconds = 60)
        {
            await EnsureDependenciesAsync();

            var options = new OptionSet
            {
                Format = "bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]/best",
                MergeOutputFormat = DownloadMergeFormat.Mp4,
                NoPlaylist = true,
            };

            // Limit video length via postprocessor args (ffmpeg -t flag)
            if (maxDurationSeconds > 0)
            {
                options.PostprocessorArgs = $"ffmpeg:-t {maxDurationSeconds}";
            }

            var result = await _ytdl.RunVideoDownload(youtubeUrl, overrideOptions: options);

            if (result.Success && !string.IsNullOrEmpty(result.Data))
            {
                return result.Data; // returns the downloaded file path
            }

            return null;
        }

        /// <summary>
        /// Gets the path where a video for a given YouTube video ID would be stored.
        /// </summary>
        public string GetExpectedFilePath(string videoId)
        {
            return Path.Combine(_downloadFolder, $"{videoId}.mp4");
        }
    }
}
