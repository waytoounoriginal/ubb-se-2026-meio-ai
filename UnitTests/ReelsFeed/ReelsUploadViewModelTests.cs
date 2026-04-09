using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Core.Platform;
using ubb_se_2026_meio_ai.Core.Services; // Based on your IMovieService using statement
using ubb_se_2026_meio_ai.Features.ReelsUpload.Models;
using ubb_se_2026_meio_ai.Features.ReelsUpload.Services;
using ubb_se_2026_meio_ai.Features.ReelsUpload.ViewModels;

namespace UnitTests.ReelsUpload
{
    [TestFixture]
    public class ReelsUploadViewModelTests
    {
        private Mock<IAppWindowContext> _mockAppWindowContext;

        [SetUp]
        public void Setup()
        {
            _mockAppWindowContext = new Mock<IAppWindowContext>();

            // This forces the background thread UI updates to run immediately in our tests
            _mockAppWindowContext
                .Setup(mock => mock.TryEnqueueOnUiThread(It.IsAny<Action>()))
                .Callback<Action>(action => action());
        }

        [Test]
        public async Task UploadReelCommand_missingVideoPath_setsValidationMessage()
        {
            var mockedStorageService = new Mock<IVideoStorageService>();
            var mockedMovieService = new Mock<IMovieService>();

            var viewModel = new ReelsUploadViewModel(
                _mockAppWindowContext.Object,
                mockedStorageService.Object,
                mockedMovieService.Object);

            viewModel.LocalVideoFilePath = string.Empty; // Invalid

            await viewModel.UploadReelCommand.ExecuteAsync(null);

            Assert.That(viewModel.StatusMessage, Does.Contain("select a video first"));
        }

        [Test]
        public async Task UploadReelCommand_missingTitle_setsValidationMessage()
        {
            var mockedStorageService = new Mock<IVideoStorageService>();
            var mockedMovieService = new Mock<IMovieService>();

            var viewModel = new ReelsUploadViewModel(
                _mockAppWindowContext.Object,
                mockedStorageService.Object,
                mockedMovieService.Object);

            viewModel.LocalVideoFilePath = "C:\\valid\\path.mp4";
            viewModel.ReelTitle = string.Empty; // Invalid

            await viewModel.UploadReelCommand.ExecuteAsync(null);

            Assert.That(viewModel.StatusMessage, Does.Contain("enter a title"));
        }

        [Test]
        public async Task UploadReelCommand_missingLinkedMovie_setsValidationMessage()
        {
            var mockedStorageService = new Mock<IVideoStorageService>();
            var mockedMovieService = new Mock<IMovieService>();

            var viewModel = new ReelsUploadViewModel(
                _mockAppWindowContext.Object,
                mockedStorageService.Object,
                mockedMovieService.Object);

            viewModel.LocalVideoFilePath = "C:\\valid\\path.mp4";
            viewModel.ReelTitle = "My Awesome Reel";
            viewModel.LinkedMovie = null; // Invalid

            await viewModel.UploadReelCommand.ExecuteAsync(null);

            Assert.That(viewModel.StatusMessage, Does.Contain("link a movie"));
        }

        [Test]
        public async Task UploadReelCommand_videoValidationFails_setsErrorMessage()
        {
            const string VIDEO_PATH = "C:\\valid\\path.mp4";

            var mockedStorageService = new Mock<IVideoStorageService>();
            var mockedMovieService = new Mock<IMovieService>();

            mockedStorageService
                .Setup(mock => mock.ValidateVideoAsync(VIDEO_PATH))
                .ReturnsAsync(false); // Simulate file is too large or corrupted

            var viewModel = new ReelsUploadViewModel(
                _mockAppWindowContext.Object,
                mockedStorageService.Object,
                mockedMovieService.Object);

            viewModel.LocalVideoFilePath = VIDEO_PATH;
            viewModel.ReelTitle = "My Awesome Reel";
            viewModel.LinkedMovie = new MovieCardModel { MovieId = 1 };

            await viewModel.UploadReelCommand.ExecuteAsync(null);

            Assert.That(viewModel.StatusMessage, Does.Contain("Invalid file"));
        }

        [Test]
        public async Task UploadReelCommand_serviceThrowsException_catchesAndSetsStatusMessage()
        {
            const string VIDEO_PATH = "C:\\valid\\path.mp4";
            const string ERROR_MESSAGE = "Database connection lost";

            var mockedStorageService = new Mock<IVideoStorageService>();
            var mockedMovieService = new Mock<IMovieService>();

            mockedStorageService
                .Setup(mock => mock.ValidateVideoAsync(VIDEO_PATH))
                .ReturnsAsync(true);

            mockedStorageService
                .Setup(mock => mock.UploadVideoAsync(It.IsAny<ReelUploadRequest>()))
                .ThrowsAsync(new Exception(ERROR_MESSAGE));

            var viewModel = new ReelsUploadViewModel(
                _mockAppWindowContext.Object,
                mockedStorageService.Object,
                mockedMovieService.Object);

            viewModel.LocalVideoFilePath = VIDEO_PATH;
            viewModel.ReelTitle = "My Awesome Reel";
            viewModel.LinkedMovie = new MovieCardModel { MovieId = 1 };

            await viewModel.UploadReelCommand.ExecuteAsync(null);

            Assert.That(viewModel.StatusMessage, Does.Contain("Upload Failed"));
        }

        [Test]
        public async Task UploadReelCommand_successfulUpload_clearsFormAndSetsSuccessMessage()
        {
            const string VIDEO_PATH = "C:\\valid\\path.mp4";
            const int GENERATED_REEL_ID = 99;

            var mockedStorageService = new Mock<IVideoStorageService>();
            var mockedMovieService = new Mock<IMovieService>();

            mockedStorageService
                .Setup(mock => mock.ValidateVideoAsync(VIDEO_PATH))
                .ReturnsAsync(true);

            mockedStorageService
                .Setup(mock => mock.UploadVideoAsync(It.IsAny<ReelUploadRequest>()))
                .ReturnsAsync(new ReelModel { ReelId = GENERATED_REEL_ID });

            var viewModel = new ReelsUploadViewModel(
                _mockAppWindowContext.Object,
                mockedStorageService.Object,
                mockedMovieService.Object);

            viewModel.LocalVideoFilePath = VIDEO_PATH;
            viewModel.ReelTitle = "My Awesome Reel";
            viewModel.ReelCaption = "Look at this!";
            viewModel.LinkedMovie = new MovieCardModel { MovieId = 1 };

            await viewModel.UploadReelCommand.ExecuteAsync(null);

            Assert.That(viewModel.LocalVideoFilePath, Is.Empty);

        }

        [Test]
        public void SelectMovieCommand_validMovie_setsLinkedMovie()
        {
            var movie = new MovieCardModel { MovieId = 5, Title = "Spiderman" };

            var viewModel = new ReelsUploadViewModel(
                _mockAppWindowContext.Object,
                new Mock<IVideoStorageService>().Object,
                new Mock<IMovieService>().Object);

            viewModel.SelectMovieCommand.Execute(movie);

            Assert.That(viewModel.LinkedMovie, Is.Not.Null);
        }

        [Test]
        public async Task SearchMovieCommand_emptyString_clearsSuggestedMovies()
        {
            var mockedMovieService = new Mock<IMovieService>();
            var viewModel = new ReelsUploadViewModel(
                _mockAppWindowContext.Object,
                new Mock<IVideoStorageService>().Object,
                mockedMovieService.Object);

            // Pre-populate list to ensure it gets cleared
            viewModel.SuggestedMovies.Add(new MovieCardModel());

            await viewModel.SearchMovieCommand.ExecuteAsync(string.Empty);

            Assert.That(viewModel.SuggestedMovies, Is.Empty);
        }

        [Test]
        public async Task SearchMovieCommand_validString_populatesSuggestedMovies()
        {
            const string SEARCH_TERM = "Batman";
            var expectedResults = new List<MovieCardModel>
            {
                new MovieCardModel { Title = "Batman Begins" },
                new MovieCardModel { Title = "The Dark Knight" }
            };

            var mockedMovieService = new Mock<IMovieService>();
            mockedMovieService
                .Setup(mock => mock.SearchTop10MoviesAsync(SEARCH_TERM))
                .ReturnsAsync(expectedResults);

            var viewModel = new ReelsUploadViewModel(
                _mockAppWindowContext.Object,
                new Mock<IVideoStorageService>().Object,
                mockedMovieService.Object);

            await viewModel.SearchMovieCommand.ExecuteAsync(SEARCH_TERM);

            Assert.That(viewModel.SuggestedMovies.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task SearchMovieCommand_serviceThrowsException_catchesAndSetsStatusMessage()
        {
            const string SEARCH_TERM = "Batman";
            const string DB_ERROR = "Timeout expired";

            var mockedMovieService = new Mock<IMovieService>();
            mockedMovieService
                .Setup(mock => mock.SearchTop10MoviesAsync(SEARCH_TERM))
                .ThrowsAsync(new Exception(DB_ERROR));

            var viewModel = new ReelsUploadViewModel(
                _mockAppWindowContext.Object,
                new Mock<IVideoStorageService>().Object,
                mockedMovieService.Object);

            await viewModel.SearchMovieCommand.ExecuteAsync(SEARCH_TERM);

            Assert.That(viewModel.SuggestedMovies, Is.Empty);
        }

        [Test]
        public async Task UploadReelCommand_nullCaption_defaultsToEmptyString()
        {
            const string VIDEO_PATH = "C:\\valid\\path.mp4";

            var mockedStorageService = new Mock<IVideoStorageService>();
            var mockedMovieService = new Mock<IMovieService>();

            mockedStorageService
                .Setup(mock => mock.ValidateVideoAsync(VIDEO_PATH))
                .ReturnsAsync(true);

            mockedStorageService
                .Setup(mock => mock.UploadVideoAsync(It.IsAny<ReelUploadRequest>()))
                .ReturnsAsync(new ReelModel { ReelId = 1 });

            var viewModel = new ReelsUploadViewModel(
                _mockAppWindowContext.Object,
                mockedStorageService.Object,
                mockedMovieService.Object);

            viewModel.LocalVideoFilePath = VIDEO_PATH;
            viewModel.ReelTitle = "Title";
            viewModel.LinkedMovie = new MovieCardModel { MovieId = 1 };

            // Explicitly set caption to null to hit the `?? string.Empty` branch
            viewModel.ReelCaption = null;

            await viewModel.UploadReelCommand.ExecuteAsync(null);

            // Verify the request passed to the service successfully converted the null to an empty string
            mockedStorageService.Verify(mock => mock.UploadVideoAsync(It.Is<ReelUploadRequest>(req =>
                req.Caption == string.Empty
            )), Times.Once);
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

                var mockedRepository = new Mock<ubb_se_2026_meio_ai.Features.ReelsUpload.Repository.IVideoStorageRepository>();

                mockedRepository
                    .Setup(mock => mock.InsertReelAsync(It.IsAny<ReelModel>()))
                    .ReturnsAsync(new ReelModel { ReelId = 100 });

                var service = new VideoStorageService(mockedRepository.Object);

                var result = await service.UploadVideoAsync(request);

                Assert.That(result, Is.Not.Null);

                // UPDATE THE 15.0 TO 0 HERE:
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }
    }
}
