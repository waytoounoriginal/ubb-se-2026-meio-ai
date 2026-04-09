namespace UnitTests.ReelsEditing
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Threading.Tasks;
    using Moq;
    using NUnit.Framework;
    using ubb_se_2026_meio_ai.Core.Models;
    using ubb_se_2026_meio_ai.Features.ReelsEditing.Services;

    /// <summary>
    /// Integration tests for <see cref="VideoProcessingService"/> that execute real ffmpeg/ffprobe processes.
    /// </summary>
    [TestFixture]
    [Category("Integration")]
    [NonParallelizable]
    public class VideoProcessingServiceIntegrationTests
    {
        private const int TestTrackId = 42;

        private Mock<IAudioLibraryRepository> mockAudioLibrary = null!;
        private VideoProcessingService service = null!;
        private string tempDirectory = string.Empty;

        [SetUp]
        public void SetUp()
        {
            this.tempDirectory = Path.Combine(Path.GetTempPath(), "meioai-videoprocessing-it", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(this.tempDirectory);

            this.ConfigureFfmpegToolsPath();
            EnsureBinaryAvailableOrIgnore("ffmpeg");
            EnsureBinaryAvailableOrIgnore("ffprobe");

            this.mockAudioLibrary = new Mock<IAudioLibraryRepository>();
            this.service = new VideoProcessingService(this.mockAudioLibrary.Object);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (Directory.Exists(this.tempDirectory))
                {
                    Directory.Delete(this.tempDirectory, recursive: true);
                }
            }
            catch
            {
                // Ignore cleanup failures in test temp directories.
            }

            string ffmpegPath = Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe");
            string ffprobePath = Path.Combine(AppContext.BaseDirectory, "ffprobe.exe");
            TryDeleteFile(ffmpegPath);
            TryDeleteFile(ffprobePath);

            SetPrivateStaticField("FfmpegTimeout", TimeSpan.FromMinutes(5));
            SetPrivateStaticField("CropOutputPostProcessHook", null);
        }

        [Test]
        public async Task ApplyCropAsync_WithRealVideo_ProducesPlayableCroppedOutput()
        {
            string sourceFixture = GetFixtureVideoPath();
            string testVideoPath = Path.Combine(this.tempDirectory, "crop_input.mp4");
            File.Copy(sourceFixture, testVideoPath, overwrite: true);

            var (originalWidth, originalHeight) = await GetVideoDimensionsAsync(testVideoPath);
            string cropJson = "{\"x\":0,\"y\":0,\"width\":960,\"height\":540}";

            string resultPath = await this.service.ApplyCropAsync(testVideoPath, cropJson);

            Assert.That(resultPath, Is.EqualTo(testVideoPath));
            Assert.That(File.Exists(resultPath), Is.True);

            var (croppedWidth, croppedHeight) = await GetVideoDimensionsAsync(resultPath);
            Assert.That(croppedWidth, Is.LessThan(originalWidth));
            Assert.That(croppedHeight, Is.LessThan(originalHeight));

            double duration = await GetMediaDurationSecondsAsync(resultPath);
            Assert.That(duration, Is.GreaterThan(0.1));
        }

        [Test]
        public async Task MergeAudioAsync_WithRealVideoAndGeneratedAudio_AddsAudioTrack()
        {
            string sourceFixture = GetFixtureVideoPath();
            string testVideoPath = Path.Combine(this.tempDirectory, "music_input.mp4");
            File.Copy(sourceFixture, testVideoPath, overwrite: true);

            string audioPath = Path.Combine(this.tempDirectory, "tone.wav");
            await CreateTestAudioAsync(audioPath, durationSeconds: 2.0);

            this.mockAudioLibrary
                .Setup(repository => repository.GetTrackByIdAsync(TestTrackId))
                .ReturnsAsync(new MusicTrackModel
                {
                    MusicTrackId = TestTrackId,
                    TrackName = "Test Tone",
                    AudioUrl = audioPath,
                    DurationSeconds = 2.0,
                });

            string resultPath = await this.service.MergeAudioAsync(
                testVideoPath,
                TestTrackId,
                startOffsetSec: 0,
                musicDurationSec: 1.5,
                musicVolumePercent: 75);

            Assert.That(resultPath, Is.EqualTo(testVideoPath));
            Assert.That(File.Exists(resultPath), Is.True);

            bool hasAudioTrack = await HasAudioStreamAsync(resultPath);
            Assert.That(hasAudioTrack, Is.True, "Expected merged output to contain an audio stream.");

            double duration = await GetMediaDurationSecondsAsync(resultPath);
            Assert.That(duration, Is.GreaterThan(0.1));
        }

        [Test]
        public async Task ApplyCropAsync_VideoFileDoesNotExist_ReturnsOriginalPath()
        {
            string fakePath = "C:\\does_not_exist_video.mp4";
            string cropJson = "{\"x\": 10, \"y\": 10, \"width\": 100, \"height\": 100}";

            string result = await this.service.ApplyCropAsync(fakePath, cropJson);

            Assert.That(result, Is.EqualTo(fakePath));
        }

        [Test]
        public async Task ApplyCropAsync_EmptyOrNullJson_ReturnsOriginalPath()
        {
            string existingFilePath = Assembly.GetExecutingAssembly().Location;
            string? emptyJson = null;

            string result = await this.service.ApplyCropAsync(existingFilePath, emptyJson!);

            Assert.That(result, Is.EqualTo(existingFilePath));
        }

        [Test]
        public async Task ApplyCropAsync_WhitespaceVideoPath_ReturnsOriginalPath()
        {
            string whitespacePath = "   ";
            string cropJson = "{\"x\": 10, \"y\": 10, \"width\": 100, \"height\": 100}";

            string result = await this.service.ApplyCropAsync(whitespacePath, cropJson);

            Assert.That(result, Is.EqualTo(whitespacePath));
        }

        [Test]
        public async Task ApplyCropAsync_WhenTempOutputDeletedAfterFfmpeg_ThrowsCropOutputMissing()
        {
            EnsureBinaryAvailableOrIgnore("ffmpeg");

            string fixtureVideoPath = GetFixtureVideoPath();
            string workingVideoPath = Path.Combine(Path.GetTempPath(), $"crop_missing_output_{Guid.NewGuid():N}.mp4");
            string cropJson = "{\"x\":0,\"y\":0,\"width\":960,\"height\":540}";

            try
            {
                File.Copy(fixtureVideoPath, workingVideoPath, overwrite: true);

                Action<string> deleteOutputHook = (tempPath) =>
                {
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                };

                SetPrivateStaticField("CropOutputPostProcessHook", deleteOutputHook);

                var exception = Assert.ThrowsAsync<InvalidOperationException>(
                    async () => await this.service.ApplyCropAsync(workingVideoPath, cropJson));

                Assert.That(exception, Is.Not.Null);
                Assert.That(exception!.Message, Does.Contain("cropped output file"));
            }
            finally
            {
                SetPrivateStaticField("CropOutputPostProcessHook", null);
                TryDeleteFile(workingVideoPath);
            }
        }

        [Test]
        public async Task MergeAudioAsync_VideoFileDoesNotExist_ReturnsOriginalPath()
        {
            string fakePath = "C:\\does_not_exist_video.mp4";

            string result = await this.service.MergeAudioAsync(fakePath, 1, 0, 30, 100);

            Assert.That(result, Is.EqualTo(fakePath));
        }

        [Test]
        public async Task MergeAudioAsync_WhitespaceVideoPath_ReturnsOriginalPath()
        {
            string whitespacePath = " ";

            string result = await this.service.MergeAudioAsync(whitespacePath, 1, 0, 30, 100);

            Assert.That(result, Is.EqualTo(whitespacePath));
        }

        [Test]
        public async Task MergeAudioAsync_TrackNotFound_ReturnsOriginalPath()
        {
            string existingFilePath = Assembly.GetExecutingAssembly().Location;
            this.mockAudioLibrary.Setup(repository => repository.GetTrackByIdAsync(99)).ReturnsAsync((MusicTrackModel?)null);

            string result = await this.service.MergeAudioAsync(existingFilePath, 99, 0, 30, 100);

            Assert.That(result, Is.EqualTo(existingFilePath));
        }

        [Test]
        public async Task MergeAudioAsync_TrackHasNoAudioUrl_ReturnsOriginalPath()
        {
            string existingFilePath = Assembly.GetExecutingAssembly().Location;
            this.mockAudioLibrary
                .Setup(repository => repository.GetTrackByIdAsync(100))
                .ReturnsAsync(new MusicTrackModel { MusicTrackId = 100, AudioUrl = string.Empty });

            string result = await this.service.MergeAudioAsync(existingFilePath, 100, 0, 30, 100);

            Assert.That(result, Is.EqualTo(existingFilePath));
        }

        [Test]
        public void MergeAudioAsync_MissingAudioFile_ThrowsFileNotFoundException()
        {
            string existingFilePath = Assembly.GetExecutingAssembly().Location;
            string missingAudioPath = Path.Combine(Path.GetTempPath(), $"missing_audio_{Guid.NewGuid():N}.wav");

            this.mockAudioLibrary
                .Setup(repository => repository.GetTrackByIdAsync(101))
                .ReturnsAsync(new MusicTrackModel
                {
                    MusicTrackId = 101,
                    AudioUrl = missingAudioPath,
                    DurationSeconds = 2.0,
                });

            Assert.That(
                async () => await this.service.MergeAudioAsync(existingFilePath, 101, 0, 30, 100),
                Throws.TypeOf<FileNotFoundException>());
        }

        [Test]
        public async Task MergeAudioAsync_FallbacksToTrackDurationWhenAudioProbeFails_ThrowsFfmpegExitError()
        {
            EnsureBinaryAvailableOrIgnore("ffmpeg");
            EnsureBinaryAvailableOrIgnore("ffprobe");

            string fixtureVideoPath = GetFixtureVideoPath();
            string workingVideoPath = Path.Combine(Path.GetTempPath(), $"video_for_merge_{Guid.NewGuid():N}.mp4");
            string invalidAudioPath = Path.Combine(Path.GetTempPath(), $"invalid_audio_{Guid.NewGuid():N}.txt");

            try
            {
                File.Copy(fixtureVideoPath, workingVideoPath, overwrite: true);
                File.WriteAllText(invalidAudioPath, "not a valid media file");

                this.mockAudioLibrary
                    .Setup(repository => repository.GetTrackByIdAsync(102))
                    .ReturnsAsync(new MusicTrackModel
                    {
                        MusicTrackId = 102,
                        AudioUrl = invalidAudioPath,
                        DurationSeconds = 2.5,
                    });

                Assert.That(
                    async () => await this.service.MergeAudioAsync(workingVideoPath, 102, startOffsetSec: 10, musicDurationSec: 0, musicVolumePercent: 120),
                    Throws.TypeOf<InvalidOperationException>());
            }
            finally
            {
                TryDeleteFile(workingVideoPath);
                TryDeleteFile(invalidAudioPath);
            }
        }

        [Test]
        public async Task MergeAudioAsync_WhenVideoDurationUnknownAndRequestedDurationZero_UsesFallbackPathAndFailsInFfmpeg()
        {
            EnsureBinaryAvailableOrIgnore("ffmpeg");
            EnsureBinaryAvailableOrIgnore("ffprobe");

            string nonVideoExistingPath = Assembly.GetExecutingAssembly().Location;
            string invalidAudioPath = Path.Combine(Path.GetTempPath(), $"invalid_audio_default_duration_{Guid.NewGuid():N}.txt");

            try
            {
                File.WriteAllText(invalidAudioPath, "not an audio track");

                this.mockAudioLibrary
                    .Setup(repository => repository.GetTrackByIdAsync(105))
                    .ReturnsAsync(new MusicTrackModel
                    {
                        MusicTrackId = 105,
                        AudioUrl = invalidAudioPath,
                        DurationSeconds = 0,
                    });

                Assert.That(
                    async () => await this.service.MergeAudioAsync(nonVideoExistingPath, 105, startOffsetSec: 0, musicDurationSec: 0, musicVolumePercent: 50),
                    Throws.TypeOf<InvalidOperationException>());
            }
            finally
            {
                TryDeleteFile(invalidAudioPath);
            }
        }

        [Test]
        public async Task MergeAudioAsync_WithHttpAudioUrl_SkipsFileExistenceCheckAndFailsInFfmpeg()
        {
            EnsureBinaryAvailableOrIgnore("ffmpeg");
            EnsureBinaryAvailableOrIgnore("ffprobe");

            string fixtureVideoPath = GetFixtureVideoPath();
            string workingVideoPath = Path.Combine(Path.GetTempPath(), $"video_http_audio_{Guid.NewGuid():N}.mp4");

            try
            {
                File.Copy(fixtureVideoPath, workingVideoPath, overwrite: true);

                this.mockAudioLibrary
                    .Setup(repository => repository.GetTrackByIdAsync(106))
                    .ReturnsAsync(new MusicTrackModel
                    {
                        MusicTrackId = 106,
                        AudioUrl = "https://example.invalid/nonexistent.wav",
                        DurationSeconds = 3,
                    });

                Assert.That(
                    async () => await this.service.MergeAudioAsync(workingVideoPath, 106, startOffsetSec: 0, musicDurationSec: 1, musicVolumePercent: 50),
                    Throws.TypeOf<InvalidOperationException>());
            }
            finally
            {
                TryDeleteFile(workingVideoPath);
            }
        }

        [Test]
        public async Task MergeAudioAsync_StartOffsetNearEnd_ResetsAndMergesSuccessfully()
        {
            EnsureBinaryAvailableOrIgnore("ffmpeg");
            EnsureBinaryAvailableOrIgnore("ffprobe");

            string fixtureVideoPath = GetFixtureVideoPath();
            string workingVideoPath = Path.Combine(Path.GetTempPath(), $"video_offset_margin_{Guid.NewGuid():N}.mp4");
            string audioPath = Path.Combine(Path.GetTempPath(), $"audio_offset_margin_{Guid.NewGuid():N}.wav");

            try
            {
                File.Copy(fixtureVideoPath, workingVideoPath, overwrite: true);
                await CreateToneAudioAsync(audioPath, durationSeconds: 2.0);

                this.mockAudioLibrary
                    .Setup(repository => repository.GetTrackByIdAsync(103))
                    .ReturnsAsync(new MusicTrackModel
                    {
                        MusicTrackId = 103,
                        AudioUrl = audioPath,
                        DurationSeconds = 2.0,
                    });

                string result = await this.service.MergeAudioAsync(
                    workingVideoPath,
                    103,
                    startOffsetSec: 1.9,
                    musicDurationSec: 1.0,
                    musicVolumePercent: 80);

                Assert.That(result, Is.EqualTo(workingVideoPath));
                Assert.That(File.Exists(result), Is.True);
            }
            finally
            {
                TryDeleteFile(workingVideoPath);
                TryDeleteFile(audioPath);
            }
        }

        [Test]
        public async Task MergeAudioAsync_AvailableAfterStartUnderOneSecond_ResetsAndMergesSuccessfully()
        {
            EnsureBinaryAvailableOrIgnore("ffmpeg");
            EnsureBinaryAvailableOrIgnore("ffprobe");

            string fixtureVideoPath = GetFixtureVideoPath();
            string workingVideoPath = Path.Combine(Path.GetTempPath(), $"video_offset_available_{Guid.NewGuid():N}.mp4");
            string audioPath = Path.Combine(Path.GetTempPath(), $"audio_offset_available_{Guid.NewGuid():N}.wav");

            try
            {
                File.Copy(fixtureVideoPath, workingVideoPath, overwrite: true);
                await CreateToneAudioAsync(audioPath, durationSeconds: 1.1);

                this.mockAudioLibrary
                    .Setup(repository => repository.GetTrackByIdAsync(104))
                    .ReturnsAsync(new MusicTrackModel
                    {
                        MusicTrackId = 104,
                        AudioUrl = audioPath,
                        DurationSeconds = 1.1,
                    });

                string result = await this.service.MergeAudioAsync(
                    workingVideoPath,
                    104,
                    startOffsetSec: 0.3,
                    musicDurationSec: 1.0,
                    musicVolumePercent: 60);

                Assert.That(result, Is.EqualTo(workingVideoPath));
                Assert.That(File.Exists(result), Is.True);
            }
            finally
            {
                TryDeleteFile(workingVideoPath);
                TryDeleteFile(audioPath);
            }
        }

        [Test]
        public async Task ApplyCropAsync_WithInvalidMediaInput_ThrowsFfmpegExitError()
        {
            EnsureBinaryAvailableOrIgnore("ffmpeg");

            string invalidVideoPath = Path.Combine(Path.GetTempPath(), $"invalid_video_{Guid.NewGuid():N}.mp4");
            string cropJson = "{\"x\":10,\"y\":10,\"width\":100,\"height\":100}";

            try
            {
                File.WriteAllText(invalidVideoPath, "not a real mp4 stream");

                Assert.That(
                    async () => await this.service.ApplyCropAsync(invalidVideoPath, cropJson),
                    Throws.TypeOf<InvalidOperationException>());
            }
            finally
            {
                TryDeleteFile(invalidVideoPath);
            }
        }

        [Test]
        public async Task TryGetMediaDurationSecondsAsync_InvalidMedia_ReturnsNull()
        {
            EnsureBinaryAvailableOrIgnore("ffprobe");

            string invalidMediaPath = Path.Combine(Path.GetTempPath(), $"invalid_duration_probe_{Guid.NewGuid():N}.bin");
            try
            {
                File.WriteAllText(invalidMediaPath, "invalid probe content");

                MethodInfo method = GetPrivateStaticMethod("TryGetMediaDurationSecondsAsync");
                var task = (Task<double?>)method.Invoke(null, new object[] { invalidMediaPath, Path.GetTempPath() })!;
                double? duration = await task;

                Assert.That(duration, Is.Null);
            }
            finally
            {
                TryDeleteFile(invalidMediaPath);
            }
        }

        [Test]
        public async Task TryGetMediaDurationSecondsAsync_WhenProbeTimesOut_ReturnsNull()
        {
            EnsureBinaryAvailableOrIgnore("ffprobe");

            string fixtureVideoPath = GetFixtureVideoPath();
            string workingDirectory = Path.GetDirectoryName(fixtureVideoPath)!;

            SetPrivateStaticField("FfmpegTimeout", TimeSpan.FromMilliseconds(1));

            MethodInfo method = GetPrivateStaticMethod("TryGetMediaDurationSecondsAsync");
            var task = (Task<double?>)method.Invoke(null, new object[] { fixtureVideoPath, workingDirectory })!;
            double? duration = await task;

            Assert.That(duration, Is.Null);
        }

        [Test]
        public async Task RunFfmpegAsync_WhenOperationTimesOut_ThrowsTimeoutException()
        {
            string localFfmpegPath = Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe");
            string cmdPath = Path.Combine(Environment.SystemDirectory, "cmd.exe");

            if (!File.Exists(cmdPath))
            {
                Assert.Ignore("Skipping timeout test because cmd.exe was not found.");
            }

            File.Copy(cmdPath, localFfmpegPath, overwrite: true);

            SetPrivateStaticField("FfmpegTimeout", TimeSpan.FromMilliseconds(1));

            MethodInfo method = GetPrivateStaticMethod("RunFfmpegAsync");
            var task = (Task)method.Invoke(null, new object[] { "/c ping 127.0.0.1 -n 6 > nul", Path.GetTempPath() })!;

            var exception = Assert.ThrowsAsync<InvalidOperationException>(async () => await task);

            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Message, Does.Contain("timed out"));
        }

        [Test]
        public void ResolveFfmpegPath_WhenLocalExecutableExists_ReturnsLocalPath()
        {
            string localFfmpegPath = Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe");
            File.WriteAllText(localFfmpegPath, "stub");

            MethodInfo method = GetPrivateStaticMethod("ResolveFfmpegPath");
            string resolved = (string)method.Invoke(null, null)!;

            Assert.That(resolved, Is.EqualTo(localFfmpegPath));
        }

        [Test]
        public void ResolveFfprobePath_WhenLocalExecutableExists_ReturnsLocalPath()
        {
            string localFfprobePath = Path.Combine(AppContext.BaseDirectory, "ffprobe.exe");
            File.WriteAllText(localFfprobePath, "stub");

            MethodInfo method = GetPrivateStaticMethod("ResolveFfprobePath");
            string resolved = (string)method.Invoke(null, null)!;

            Assert.That(resolved, Is.EqualTo(localFfprobePath));
        }

        [Test]
        public void ResolveMediaInput_WithFileUri_ReturnsLocalPath()
        {
            MethodInfo method = GetPrivateStaticMethod("ResolveMediaInput");
            string localPath = Path.Combine(Path.GetTempPath(), "sample.mp4");
            string fileUri = new Uri(localPath).AbsoluteUri;

            string resolved = (string)method.Invoke(null, new object[] { fileUri })!;

            Assert.That(resolved, Is.EqualTo(localPath));
        }

        [Test]
        public void ResolveMediaInput_WithPlainString_ReturnsOriginalValue()
        {
            MethodInfo method = GetPrivateStaticMethod("ResolveMediaInput");
            string value = "relative/path/video.mp4";

            string resolved = (string)method.Invoke(null, new object[] { value })!;

            Assert.That(resolved, Is.EqualTo(value));
        }

        [Test]
        public void IsHttpUrl_WithHttpAndHttpsAndInvalidValues_HandlesAllBranches()
        {
            MethodInfo method = GetPrivateStaticMethod("IsHttpUrl");

            bool http = (bool)method.Invoke(null, new object[] { "http://example.com" })!;
            bool https = (bool)method.Invoke(null, new object[] { "https://example.com" })!;
            bool invalid = (bool)method.Invoke(null, new object[] { "not_a_uri" })!;

            Assert.That(http, Is.True);
            Assert.That(https, Is.True);
            Assert.That(invalid, Is.False);
        }

        [Test]
        public void ReadCropData_WithStringValues_ParsesAndClamps()
        {
            MethodInfo method = GetPrivateStaticMethod("ReadCropData");
            string cropJson = "{\"x\":\"1919\",\"y\":\"1079\",\"width\":\"100\",\"height\":\"100\"}";

            var result = ((int CropX, int CropY, int CropWidth, int CropHeight))method.Invoke(null, new object[] { cropJson })!;

            Assert.That(result.CropX, Is.EqualTo(1919));
            Assert.That(result.CropY, Is.EqualTo(1079));
            Assert.That(result.CropWidth, Is.EqualTo(1));
            Assert.That(result.CropHeight, Is.EqualTo(1));
        }

        [Test]
        public void ReadInt_WithMissingProperty_ReturnsFallback()
        {
            MethodInfo readIntMethod = GetPrivateStaticMethod("ReadInt");
            using var document = System.Text.Json.JsonDocument.Parse("{\"present\":123}");

            int result = (int)readIntMethod.Invoke(null, new object[] { document.RootElement, "missing", 777 })!;

            Assert.That(result, Is.EqualTo(777));
        }

        [Test]
        public void ReadInt_WithNonNumericStringValue_ReturnsFallback()
        {
            MethodInfo readIntMethod = GetPrivateStaticMethod("ReadInt");
            using var document = System.Text.Json.JsonDocument.Parse("{\"value\":\"abc\"}");

            int result = (int)readIntMethod.Invoke(null, new object[] { document.RootElement, "value", 555 })!;

            Assert.That(result, Is.EqualTo(555));
        }

        [Test]
        public void FinalizeProcessedFile_WhenSourceLocked_UsesFallbackPath()
        {
            MethodInfo method = GetPrivateStaticMethod("FinalizeProcessedFile");

            string directory = Path.Combine(Path.GetTempPath(), "finalize_locked", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            string sourcePath = Path.Combine(directory, "source.mp4");
            string tempPath = Path.Combine(directory, "temp.mp4");
            File.WriteAllText(sourcePath, "source");
            File.WriteAllText(tempPath, "temp");

            try
            {
                using var lockStream = new FileStream(sourcePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                string finalPath = (string)method.Invoke(null, new object[] { sourcePath, tempPath, "_fallback_" })!;

                Assert.That(finalPath, Is.Not.EqualTo(sourcePath));
                Assert.That(finalPath, Does.Contain("_fallback_"));
                Assert.That(File.Exists(finalPath), Is.True);
            }
            finally
            {
                TryDeleteFile(sourcePath);
                TryDeleteFile(tempPath);

                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, recursive: true);
                }
            }
        }

        [Test]
        public void FinalizeProcessedFile_WhenFallbackMoveFails_ExecutesFinallyDeletePath()
        {
            MethodInfo method = GetPrivateStaticMethod("FinalizeProcessedFile");

            string directory = Path.Combine(Path.GetTempPath(), "finalize_cleanup", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            string samePath = Path.Combine(directory, "same_locked.mp4");
            File.WriteAllText(samePath, "locked");

            try
            {
                using var lockStream = new FileStream(samePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

                Assert.That(
                    () => method.Invoke(null, new object[] { samePath, samePath, "_fallback_" }),
                    Throws.Exception);

                Assert.That(File.Exists(samePath), Is.True);
            }
            finally
            {
                TryDeleteFile(samePath);
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, recursive: true);
                }
            }
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null)
            {
                string solutionPath = Path.Combine(directory.FullName, "ubb-se-2026-meio-ai.sln");
                if (File.Exists(solutionPath))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate repository root from test output directory.");
        }

        private static string GetFixtureVideoPath()
        {
            string repositoryRoot = FindRepositoryRoot();
            string fixturePath = Path.Combine(repositoryRoot, "test_res", "reels_upload", "videos", "video_1.mp4");
            if (!File.Exists(fixturePath))
            {
                throw new FileNotFoundException($"Missing test fixture video at '{fixturePath}'.");
            }

            return fixturePath;
        }

        private void ConfigureFfmpegToolsPath()
        {
            string repositoryRoot = FindRepositoryRoot();
            string toolsDirectory = Path.Combine(repositoryRoot, "tools", "ffmpeg");
            string localFfmpeg = Path.Combine(toolsDirectory, "ffmpeg.exe");
            string localFfprobe = Path.Combine(toolsDirectory, "ffprobe.exe");

            if (!File.Exists(localFfmpeg) || !File.Exists(localFfprobe))
            {
                return;
            }

            string currentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            string pathSeparator = Path.PathSeparator.ToString(CultureInfo.InvariantCulture);

            if (!currentPath.Contains(toolsDirectory, StringComparison.OrdinalIgnoreCase))
            {
                Environment.SetEnvironmentVariable("PATH", toolsDirectory + pathSeparator + currentPath);
            }
        }

        private static void EnsureBinaryAvailableOrIgnore(string binaryName)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = binaryName,
                Arguments = "-version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            try
            {
                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    Assert.Ignore($"Integration test skipped: '{binaryName}' is not available.");
                    return;
                }

                bool exited = process.WaitForExit(milliseconds: 3000);
                if (!exited || process.ExitCode != 0)
                {
                    Assert.Ignore($"Integration test skipped: '{binaryName}' is not executable.");
                }
            }
            catch
            {
                Assert.Ignore($"Integration test skipped: '{binaryName}' is not available on PATH or in tools/ffmpeg.");
            }
        }

        private static MethodInfo GetPrivateStaticMethod(string methodName)
        {
            return typeof(VideoProcessingService).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new MissingMethodException($"Could not find private static method '{methodName}'.");
        }

        private static void SetPrivateStaticField(string fieldName, object? value)
        {
            FieldInfo field = typeof(VideoProcessingService).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new MissingFieldException($"Could not find private static field '{fieldName}'.");
            field.SetValue(null, value);
        }

        private static async Task CreateTestAudioAsync(string destinationPath, double durationSeconds)
        {
            string arguments = string.Format(
                CultureInfo.InvariantCulture,
                "-hide_banner -loglevel error -f lavfi -i \"sine=frequency=1000:duration={0:0.###}\" -c:a pcm_s16le -y \"{1}\"",
                durationSeconds,
                destinationPath);

            await RunProcessAndRequireSuccessAsync("ffmpeg", arguments, Path.GetDirectoryName(destinationPath)!);

            Assert.That(File.Exists(destinationPath), Is.True, "Expected generated audio fixture file to exist.");
        }

        private static async Task CreateToneAudioAsync(string outputPath, double durationSeconds)
        {
            string arguments = string.Format(
                CultureInfo.InvariantCulture,
                "-hide_banner -loglevel error -f lavfi -i \"sine=frequency=880:duration={0:0.###}\" -c:a pcm_s16le -y \"{1}\"",
                durationSeconds,
                outputPath);

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(outputPath)!,
            };

            using var process = Process.Start(processStartInfo)
                ?? throw new InvalidOperationException("Failed to start ffmpeg while creating audio fixture.");

            string standardOutput = await process.StandardOutput.ReadToEndAsync();
            string standardError = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"ffmpeg failed while creating audio fixture with exit code {process.ExitCode}.{Environment.NewLine}{standardError}{Environment.NewLine}{standardOutput}");
            }
        }

        private static async Task<(int Width, int Height)> GetVideoDimensionsAsync(string videoPath)
        {
            string arguments = string.Format(
                CultureInfo.InvariantCulture,
                "-v error -select_streams v:0 -show_entries stream=width,height -of csv=s=x:p=0 \"{0}\"",
                videoPath);

            string output = await RunProcessAndCaptureOutputAsync("ffprobe", arguments, Path.GetDirectoryName(videoPath)!);
            string trimmed = output.Trim();

            string[] parts = trimmed.Split('x', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 ||
                !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var width) ||
                !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var height))
            {
                throw new InvalidOperationException($"Could not parse ffprobe dimensions output: '{trimmed}'.");
            }

            return (width, height);
        }

        private static async Task<bool> HasAudioStreamAsync(string videoPath)
        {
            string arguments = string.Format(
                CultureInfo.InvariantCulture,
                "-v error -select_streams a:0 -show_entries stream=codec_type -of default=noprint_wrappers=1:nokey=1 \"{0}\"",
                videoPath);

            string output = await RunProcessAndCaptureOutputAsync("ffprobe", arguments, Path.GetDirectoryName(videoPath)!);
            return output.Contains("audio", StringComparison.OrdinalIgnoreCase);
        }

        private static async Task<double> GetMediaDurationSecondsAsync(string mediaPath)
        {
            string arguments = string.Format(
                CultureInfo.InvariantCulture,
                "-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{0}\"",
                mediaPath);

            string output = await RunProcessAndCaptureOutputAsync("ffprobe", arguments, Path.GetDirectoryName(mediaPath)!);
            if (double.TryParse(output.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var duration))
            {
                return duration;
            }

            throw new InvalidOperationException($"Could not parse ffprobe duration output: '{output}'.");
        }

        private static async Task RunProcessAndRequireSuccessAsync(string executable, string arguments, string workingDirectory)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory,
            };

            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException($"Failed to start process '{executable}'.");

            string standardOutput = await process.StandardOutput.ReadToEndAsync();
            string standardError = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Process '{executable}' failed with code {process.ExitCode}.{Environment.NewLine}{standardError}{Environment.NewLine}{standardOutput}");
            }
        }

        private static async Task<string> RunProcessAndCaptureOutputAsync(string executable, string arguments, string workingDirectory)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory,
            };

            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException($"Failed to start process '{executable}'.");

            string standardOutput = await process.StandardOutput.ReadToEndAsync();
            string standardError = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Process '{executable}' failed with code {process.ExitCode}.{Environment.NewLine}{standardError}");
            }

            return standardOutput;
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // best-effort cleanup
            }
        }
    }
}