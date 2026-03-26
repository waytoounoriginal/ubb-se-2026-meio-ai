using System.Diagnostics;

namespace ubb_se_2026_meio_ai.Features.TrailerScraping.Services
{
    /// <summary>
    /// Downloads YouTube videos as MP4 files using yt-dlp.
    /// Stores them in a local folder and returns the file path.
    /// Calls yt-dlp directly via Process to avoid YoutubeDLSharp native crashes.
    /// Owner: Andrei
    /// </summary>
    public class VideoDownloadService
    {
        private readonly string _downloadFolder;
        private string _ytdlPath = "yt-dlp.exe";
        private string _ffmpegPath = "ffmpeg.exe";
        private bool _isInitialized;

        public VideoDownloadService(string? downloadFolder = null)
        {
            _downloadFolder = downloadFolder
                ?? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MeioAI", "Videos");

            Directory.CreateDirectory(_downloadFolder);
        }

        /// <summary>
        /// Ensures yt-dlp and ffmpeg binaries are available.
        /// </summary>
        public Task EnsureDependenciesAsync()
        {
            if (_isInitialized)
            {
                return Task.CompletedTask;
            }

            // 1. Check WinGet links folder first (winget install yt-dlp / ffmpeg)
            string wingetLinks = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "WinGet", "Links");
            string wingetYtDlp = Path.Combine(wingetLinks, "yt-dlp.exe");
            string wingetFfmpeg = Path.Combine(wingetLinks, "ffmpeg.exe");

            if (File.Exists(wingetYtDlp) && File.Exists(wingetFfmpeg))
            {
                _ytdlPath = wingetYtDlp;
                _ffmpegPath = wingetFfmpeg;
                _isInitialized = true;
                return Task.CompletedTask;
            }

            // 2. Check if binaries exist in the download folder
            string localYtDlp = Path.Combine(_downloadFolder, "yt-dlp.exe");
            string localFfmpeg = Path.Combine(_downloadFolder, "ffmpeg.exe");

            if (File.Exists(localYtDlp) && File.Exists(localFfmpeg))
            {
                _ytdlPath = localYtDlp;
                _ffmpegPath = localFfmpeg;
                _isInitialized = true;
                return Task.CompletedTask;
            }

            // 3. Fall back to PATH (set during constructor default)
            _isInitialized = true;
            return Task.CompletedTask;
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

            // Build yt-dlp arguments
            string outputTemplate = Path.Combine(_downloadFolder, "%(id)s.%(ext)s");
            string args = $"--no-playlist " +
                          $"--ffmpeg-location \"{_ffmpegPath}\" " +
                          $"-f \"bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]/best\" " +
                          $"--merge-output-format mp4 " +
                          $"-o \"{outputTemplate}\" ";

            if (maxDurationSeconds > 0)
            {
                args += $"--postprocessor-args \"ffmpeg:-t {maxDurationSeconds}\" ";
            }

            args += $"\"{youtubeUrl}\"";

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = _ytdlPath,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };

                using var process = Process.Start(psi);
                if (process == null)
                {
                    LastError = "Failed to start yt-dlp process";
                    return null;
                }

                // Read stdout and stderr CONCURRENTLY to avoid deadlock.
                // Sequential reads can deadlock when the child process fills one
                // stream's buffer while we are blocked reading the other.
                var stdoutTask = process.StandardOutput.ReadToEndAsync();
                var stderrTask = process.StandardError.ReadToEndAsync();

                // Apply a timeout so a hung yt-dlp/ffmpeg process doesn't block forever
                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
                try
                {
                    await process.WaitForExitAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    try { process.Kill(entireProcessTree: true); } catch { }
                    LastError = "yt-dlp process timed out after 5 minutes";
                    return null;
                }

                string stdout = await stdoutTask;
                string stderr = await stderrTask;

                if (process.ExitCode != 0)
                {
                    LastError = $"yt-dlp exit code {process.ExitCode}: {stderr}";
                    return null;
                }

                // Find the downloaded .mp4 file — yt-dlp prints the destination path
                // Look for "[Merger] Merging formats into" or "[download] Destination:"
                // or just find the newest .mp4 in the folder
                string? filePath = FindDownloadedFile(stdout);
                if (filePath != null && File.Exists(filePath))
                {
                    LastError = null;
                    return filePath;
                }

                LastError = $"yt-dlp succeeded but no MP4 file found. stdout: {stdout}";
                return null;
            }
            catch (Exception ex)
            {
                LastError = $"yt-dlp process error: {ex.Message}";
                return null;
            }
        }

        /// <summary>
        /// Parses yt-dlp stdout to find the downloaded file path.
        /// Falls back to the newest .mp4 in the download folder.
        /// </summary>
        private string? FindDownloadedFile(string stdout)
        {
            // yt-dlp outputs lines like:
            //   [Merger] Merging formats into "C:\...\VIDEO_ID.mp4"
            //   [download] C:\...\VIDEO_ID.mp4 has already been downloaded
            //   [download] Destination: C:\...\VIDEO_ID.mp4
            foreach (string line in stdout.Split('\n'))
            {
                string trimmed = line.Trim();

                // [Merger] Merging formats into "path"
                if (trimmed.StartsWith("[Merger]") && trimmed.Contains('"'))
                {
                    int first = trimmed.IndexOf('"') + 1;
                    int last = trimmed.LastIndexOf('"');
                    if (last > first)
                    {
                        string path = trimmed[first..last];
                        if (File.Exists(path))
                        {
                            return path;
                        }
                    }
                }

                // [download] Destination: path  OR  [download] path has already been downloaded
                if (trimmed.StartsWith("[download]"))
                {
                    string rest = trimmed["[download]".Length..].Trim();

                    if (rest.StartsWith("Destination:"))
                    {
                        string path = rest["Destination:".Length..].Trim();
                        if (File.Exists(path))
                        {
                            return path;
                        }
                    }

                    if (rest.EndsWith("has already been downloaded"))
                    {
                        string path = rest.Replace(" has already been downloaded", "").Trim();
                        if (File.Exists(path))
                        {
                            return path;
                        }
                    }
                }
            }

            // Fallback: newest .mp4 in the download folder
            try
            {
                var newest = new DirectoryInfo(_downloadFolder)
                    .GetFiles("*.mp4")
                    .OrderByDescending(f => f.LastWriteTimeUtc)
                    .FirstOrDefault();
                return newest?.FullName;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Contains the error message from the last failed download, if any.
        /// </summary>
        public string? LastError { get; private set; }

        /// <summary>
        /// Gets the path where a video for a given YouTube video ID would be stored.
        /// </summary>
        public string GetExpectedFilePath(string videoId)
        {
            return Path.Combine(_downloadFolder, $"{videoId}.mp4");
        }
    }
}
