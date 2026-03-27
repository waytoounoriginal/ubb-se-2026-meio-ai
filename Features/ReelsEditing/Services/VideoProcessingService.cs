using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace ubb_se_2026_meio_ai.Features.ReelsEditing.Services
{
    /// <summary>
    /// Implements IVideoProcessingService using ffmpeg.
    /// Requires ffmpeg.exe to be present in PATH or the app's working directory.
    /// Owner: Beatrice
    /// </summary>
    public class VideoProcessingService : IVideoProcessingService
    {
        private readonly IAudioLibraryService _audioLibrary;

        private const int BaseWidth = 1920;
        private const int BaseHeight = 1080;
        private static readonly TimeSpan FfmpegTimeout = TimeSpan.FromMinutes(5);
        private static readonly string LocalFfmpegPath = Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe");
        private static readonly string LocalFfprobePath = Path.Combine(AppContext.BaseDirectory, "ffprobe.exe");

        public VideoProcessingService(IAudioLibraryService audioLibrary)
        {
            _audioLibrary = audioLibrary;
        }

        public async Task<string> ApplyCropAsync(string videoPath, string cropDataJson)
        {
            string sourcePath = ResolveMediaInput(videoPath);
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
                return videoPath;

            var (x, y, w, h) = ReadCropData(cropDataJson);

            // No-op crop (full frame)
            if (x == 0 && y == 0 && w == BaseWidth && h == BaseHeight)
                return videoPath;

            double xRatio = (double)x / BaseWidth;
            double yRatio = (double)y / BaseHeight;
            double wRatio = (double)w / BaseWidth;
            double hRatio = (double)h / BaseHeight;

            string cropFilter = string.Format(
                CultureInfo.InvariantCulture,
                "crop=iw*{0:0.######}:ih*{1:0.######}:iw*{2:0.######}:ih*{3:0.######}",
                wRatio, hRatio, xRatio, yRatio);

            string directory = Path.GetDirectoryName(sourcePath)!;
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(sourcePath);
            string extension = Path.GetExtension(sourcePath);
            string tempPath = Path.Combine(directory, $"{fileNameWithoutExt}_crop_tmp_{Guid.NewGuid():N}{extension}");

            string ffmpegArgs =
                $"-hide_banner -loglevel error -i \"{sourcePath}\" -vf \"{cropFilter}\" " +
                $"-c:v libx264 -preset veryfast -crf 20 -c:a copy -movflags +faststart -y \"{tempPath}\"";

            await RunFfmpegAsync(ffmpegArgs, directory);

            if (!File.Exists(tempPath))
                throw new InvalidOperationException("FFmpeg did not produce the cropped output file.");

            return FinalizeProcessedFile(sourcePath, tempPath, "_cropped_");
        }

        public async Task<string> MergeAudioAsync(
            string videoPath,
            int musicTrackId,
            double startOffsetSec,
            double musicDurationSec,
            double musicVolumePercent)
        {
            string sourcePath = ResolveMediaInput(videoPath);
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
                return videoPath;

            var track = await _audioLibrary.GetTrackByIdAsync(musicTrackId);
            if (track == null || string.IsNullOrWhiteSpace(track.AudioUrl))
                return videoPath;

            string audioInput = ResolveMediaInput(track.AudioUrl);
            if (!IsHttpUrl(audioInput) && !File.Exists(audioInput))
                throw new FileNotFoundException($"Music file not found: {audioInput}");

            double safeStart = Math.Clamp(startOffsetSec, 0, 300);
            double safeVolume = Math.Clamp(musicVolumePercent / 100.0, 0.0, 2.0);

            string directory = Path.GetDirectoryName(sourcePath)!;

            // Probe the video duration so the music covers the entire reel
            double? videoDuration = await TryGetMediaDurationSecondsAsync(sourcePath, directory: directory);
            double targetDuration = videoDuration.HasValue && videoDuration.Value > 0
                ? videoDuration.Value
                : (musicDurationSec > 0 ? musicDurationSec : 30.0);

            double? probedAudioDuration = await TryGetMediaDurationSecondsAsync(audioInput, directory: directory);
            if (!probedAudioDuration.HasValue && track.DurationSeconds > 1.0)
                probedAudioDuration = track.DurationSeconds;

            if (probedAudioDuration.HasValue && probedAudioDuration.Value > 0)
            {
                double audioDuration = probedAudioDuration.Value;
                if (safeStart >= audioDuration - 0.25)
                    safeStart = 0;

                double availableAfterStart = audioDuration - safeStart;
                if (availableAfterStart < 1.0)
                {
                    safeStart = 0;
                }
            }

            string durationFilter = $",atrim=duration={ToInvariantNumber(targetDuration)}";
            string volumeFilter = $",volume={ToInvariantNumber(safeVolume)}";

            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(sourcePath);
            string extension = Path.GetExtension(sourcePath);
            string tempPath = Path.Combine(directory, $"{fileNameWithoutExt}_music_tmp_{Guid.NewGuid():N}{extension}");

            string filterComplex =
                $"[1:a]aresample=async=1:first_pts=0{durationFilter}{volumeFilter},apad[aout]";

            // -stream_loop -1 loops the audio infinitely so short tracks cover the full video
            string ffmpegArgs =
                $"-hide_banner -loglevel error -i \"{sourcePath}\" " +
                $"-stream_loop -1 -ss {ToInvariantNumber(safeStart)} -i \"{audioInput}\" " +
                $"-filter_complex \"{filterComplex}\" " +
                "-map 0:v:0 -map \"[aout]\" -c:v copy -c:a aac -b:a 192k " +
                $"-movflags +faststart -shortest -y \"{tempPath}\"";

            await RunFfmpegAsync(ffmpegArgs, directory);

            if (!File.Exists(tempPath))
                throw new InvalidOperationException("FFmpeg did not produce the merged-audio output file.");

            return FinalizeProcessedFile(sourcePath, tempPath, "_withmusic_");
        }

        private static async Task<double?> TryGetMediaDurationSecondsAsync(string mediaInput, string directory)
        {
            var psi = new ProcessStartInfo
            {
                FileName = ResolveFfprobePath(),
                Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{mediaInput}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = directory,
            };

            using var process = Process.Start(psi);
            if (process == null)
                return null;

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            using var cts = new CancellationTokenSource(FfmpegTimeout);
            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                return null;
            }

            string stdout = (await stdoutTask).Trim();
            _ = await stderrTask;

            if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(stdout))
                return null;

            if (double.TryParse(stdout, NumberStyles.Float, CultureInfo.InvariantCulture, out var duration) &&
                duration > 0)
            {
                return duration;
            }

            return null;
        }

        private static string FinalizeProcessedFile(string sourcePath, string tempPath, string fallbackSuffix)
        {
            string directory = Path.GetDirectoryName(sourcePath)!;
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(sourcePath);
            string extension = Path.GetExtension(sourcePath);
            string fallbackPath = Path.Combine(
                directory,
                $"{fileNameWithoutExt}{fallbackSuffix}{DateTime.UtcNow:yyyyMMddHHmmssfff}{extension}");

            try
            {
                File.Move(tempPath, sourcePath, overwrite: true);
                return sourcePath;
            }
            catch (IOException)
            {
                // The current video can be locked by the media player; keep output under a new file.
                File.Move(tempPath, fallbackPath, overwrite: true);
                return fallbackPath;
            }
            catch (UnauthorizedAccessException)
            {
                File.Move(tempPath, fallbackPath, overwrite: true);
                return fallbackPath;
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); }
                    catch { }
                }
            }
        }

        private static async Task RunFfmpegAsync(string arguments, string workingDirectory)
        {
            var psi = new ProcessStartInfo
            {
                FileName = ResolveFfmpegPath(),
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory,
            };

            using var process = Process.Start(psi)
                ?? throw new InvalidOperationException("Failed to start ffmpeg. Ensure ffmpeg is installed and available.");

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            using var cts = new CancellationTokenSource(FfmpegTimeout);
            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                throw new InvalidOperationException("ffmpeg timed out after 5 minutes.");
            }

            string stdout = await stdoutTask;
            string stderr = await stderrTask;

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"ffmpeg exited with code {process.ExitCode}:{Environment.NewLine}{stderr}{Environment.NewLine}{stdout}");
            }
        }

        private static string ResolveFfmpegPath()
        {
            if (File.Exists(LocalFfmpegPath))
                return LocalFfmpegPath;

            return "ffmpeg";
        }

        private static string ResolveFfprobePath()
        {
            if (File.Exists(LocalFfprobePath))
                return LocalFfprobePath;

            return "ffprobe";
        }

        private static string ResolveMediaInput(string value)
        {
            if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.IsFile)
                return uri.LocalPath;

            return value;
        }

        private static bool IsHttpUrl(string value)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
                return false;

            return string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
        }

        private static string ToInvariantNumber(double value)
            => value.ToString("0.###", CultureInfo.InvariantCulture);

        private static (int X, int Y, int Width, int Height) ReadCropData(string cropDataJson)
        {
            if (string.IsNullOrWhiteSpace(cropDataJson))
                return (0, 0, BaseWidth, BaseHeight);

            using var doc = JsonDocument.Parse(cropDataJson);
            var root = doc.RootElement;

            int x = ReadInt(root, "x", 0);
            int y = ReadInt(root, "y", 0);
            int w = ReadInt(root, "width", BaseWidth);
            int h = ReadInt(root, "height", BaseHeight);

            x = Math.Clamp(x, 0, BaseWidth - 1);
            y = Math.Clamp(y, 0, BaseHeight - 1);
            w = Math.Clamp(w, 1, BaseWidth - x);
            h = Math.Clamp(h, 1, BaseHeight - y);

            return (x, y, w, h);
        }

        private static int ReadInt(JsonElement root, string name, int fallback)
        {
            if (root.TryGetProperty(name, out var value))
            {
                if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var i))
                    return i;

                if (value.ValueKind == JsonValueKind.String &&
                    int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                    return parsed;
            }

            return fallback;
        }
    }
}
