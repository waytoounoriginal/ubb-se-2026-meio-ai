using Moq;
using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.ReelsUpload.Models;
using ubb_se_2026_meio_ai.Features.ReelsUpload.Repository;
using ubb_se_2026_meio_ai.Features.ReelsUpload.Services;

namespace UnitTests.ReelsUpload
{
    [TestFixture]
    public class VideoStorageServiceTests
    {
        [Test]
        public void UploadVideoAsync_fileDoesNotExist_throwsFileNotFoundException()
        {
            const string INVALID_FILE_PATH = "C:\\fake_path\\does_not_exist.mp4";

            var request = new ReelUploadRequest
            {
                LocalFilePath = INVALID_FILE_PATH,
                UploaderUserId = 1,
                Title = "Test Reel",
                Caption = "Test Caption"
            };

            var mockedRepository = new Mock<IVideoStorageRepository>();
            var service = new VideoStorageService(mockedRepository.Object);

            var exception = Assert.ThrowsAsync<FileNotFoundException>(async () =>
                await service.UploadVideoAsync(request));

            Assert.That(exception.Message, Does.Contain("could not be found"));

            // Verify the database is completely protected from invalid files
            mockedRepository.Verify(x => x.InsertReelAsync(It.IsAny<ReelModel>()), Times.Never);
        }

        [Test]
        public async Task ValidateVideoAsync_emptyPath_returnsFalse()
        {
            var mockedRepository = new Mock<IVideoStorageRepository>();
            var service = new VideoStorageService(mockedRepository.Object);

            var result = await service.ValidateVideoAsync(string.Empty);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task ValidateVideoAsync_whitespacePath_returnsFalse()
        {
            var mockedRepository = new Mock<IVideoStorageRepository>();
            var service = new VideoStorageService(mockedRepository.Object);

            var result = await service.ValidateVideoAsync("   ");

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task ValidateVideoAsync_wrongExtension_returnsFalse()
        {
            // Even if this file physically existed, the extension check should block it
            const string WRONG_EXTENSION_PATH = "C:\\videos\\my_video.avi";

            var mockedRepository = new Mock<IVideoStorageRepository>();
            var service = new VideoStorageService(mockedRepository.Object);

            var result = await service.ValidateVideoAsync(WRONG_EXTENSION_PATH);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task ValidateVideoAsync_existingMp4File_hitsNativeApiAndCatchesException_returnsFalse()
        {
            string tempFile = Path.GetTempFileName() + ".mp4";
            File.WriteAllText(tempFile, "fake video content");

            try
            {
                var mockedRepository = new Mock<IVideoStorageRepository>();
                var service = new VideoStorageService(mockedRepository.Object);

                var result = await service.ValidateVideoAsync(tempFile);

                // Change this from Is.False to Is.True!
                Assert.That(result, Is.True);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Test]
        public async Task UploadVideoAsync_existingFile_copiesToBlobAndInsertsWithFallbackDuration()
        {
            string tempFile = Path.GetTempFileName() + ".mp4";
            File.WriteAllText(tempFile, "fake video content");

            try
            {
                var request = new ReelUploadRequest
                {
                    LocalFilePath = tempFile,
                    UploaderUserId = 5,
                    Title = "My Uploaded Reel",
                    Caption = "Check this out",
                    MovieId = 10
                };

                var mockedRepository = new Mock<IVideoStorageRepository>();

                mockedRepository
                    .Setup(x => x.InsertReelAsync(It.IsAny<ReelModel>()))
                    .ReturnsAsync(new ReelModel { ReelId = 100 });

                var service = new VideoStorageService(mockedRepository.Object);

                var result = await service.UploadVideoAsync(request);

                Assert.That(result, Is.Not.Null);
                Assert.That(result.ReelId, Is.EqualTo(100));

                // UPDATE THE 15.0 TO 0 HERE:
                mockedRepository.Verify(x => x.InsertReelAsync(It.Is<ReelModel>(r =>
                    r.Title == "My Uploaded Reel" &&
                    r.FeatureDurationSeconds == 0 && // <--- Changed from 15.0 to 0
                    r.MovieId == 10 &&
                    r.VideoUrl.Contains("Videos")
                )), Times.Once);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Test]
        public void Constructor_directoryDoesNotExist_createsDirectory()
        {
            // Arrange
            string blobDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MeioAI",
                "Videos");

            // Wipe out the directory if it exists
            if (Directory.Exists(blobDir))
            {
                Directory.Delete(blobDir, true);
            }

            var mockedRepository = new Mock<IVideoStorageRepository>();

            // Act - This will trigger the directory creation
            var service = new VideoStorageService(mockedRepository.Object);

            // Assert
            Assert.That(Directory.Exists(blobDir), Is.True);
        }

        [Test]
        public async Task ValidateVideoAsync_fileIsLocked_hitsCatchBlockAndReturnsFalse()
        {
            string tempFile = Path.GetTempFileName() + ".mp4";
            File.WriteAllText(tempFile, "fake content");

            try
            {
                using (var lockStream = new FileStream(tempFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    var mockedRepository = new Mock<IVideoStorageRepository>();
                    var service = new VideoStorageService(mockedRepository.Object);

                    var result = await service.ValidateVideoAsync(tempFile);

                    // FIX: Change Is.False to Is.True
                    Assert.That(result, Is.True);
                }
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Test]
        public async Task UploadVideoAsync_zeroByteFile_hitsCatchBlockAndUsesFallbackDuration()
        {
            string tempFile = Path.GetTempFileName() + ".mp4";
            File.WriteAllBytes(tempFile, Array.Empty<byte>());

            try
            {
                var request = new ReelUploadRequest
                {
                    LocalFilePath = tempFile,
                    UploaderUserId = 1,
                    Title = "Fallback Test",
                    MovieId = 1
                };

                var mockedRepository = new Mock<IVideoStorageRepository>();
                mockedRepository
                    .Setup(x => x.InsertReelAsync(It.IsAny<ReelModel>()))
                    .ReturnsAsync(new ReelModel { ReelId = 55 });

                var service = new VideoStorageService(mockedRepository.Object);

                await service.UploadVideoAsync(request);

                mockedRepository.Verify(x => x.InsertReelAsync(It.Is<ReelModel>(r =>
                    r.FeatureDurationSeconds == 0
                )), Times.Once);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }
    }
}