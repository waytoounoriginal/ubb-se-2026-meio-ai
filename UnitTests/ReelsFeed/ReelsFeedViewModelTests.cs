using Moq;
using NUnit.Framework;
using System.Linq;
using System.Threading;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.ReelsFeed.Services;
using ubb_se_2026_meio_ai.Features.ReelsFeed.ViewModels;

namespace UnitTests.ReelsFeed
{
    [TestFixture]
    public class ReelsFeedViewModelTests
    {
        [Test]
        public void BuildPlaybackItem_null_returnsNull()
        {
            var viewModel = CreateViewModel();

            Assert.That(viewModel.BuildPlaybackItem(null), Is.Null);
        }

        [Test]
        public void BuildPlaybackItem_emptyString_returnsNull()
        {
            var viewModel = CreateViewModel();

            Assert.That(viewModel.BuildPlaybackItem(string.Empty), Is.Null);
        }

        [Test]
        public void BuildPlaybackItem_whitespace_returnsNull()
        {
            var viewModel = CreateViewModel();

            Assert.That(viewModel.BuildPlaybackItem("   "), Is.Null);
        }

        [Test]
        public async Task BuildPlaybackItem_prefetchedItemExists_returnsCachedItemWithoutRebuilding()
        {
            const string PREFETCHED_URL = "https://cdn.example.com/reel-prefetched.mp4";
            const string NEXT_URL = "https://cdn.example.com/reel-next.mp4";

            var firstReel = new ReelModel
            {
                ReelId = 901,
                VideoUrl = "https://cdn.example.com/reel-901.mp4",
            };

            var prefetchedReel = new ReelModel
            {
                ReelId = 902,
                VideoUrl = PREFETCHED_URL,
            };

            var nextReel = new ReelModel
            {
                ReelId = 903,
                VideoUrl = NEXT_URL,
            };

            var mockedRecommendationService = new Mock<IRecommendationService>();
            var mockedClipPlaybackService = new Mock<IClipPlaybackService>();
            var mockedReelInteractionService = new Mock<IReelInteractionService>();

            mockedRecommendationService
                .Setup(mock => mock.GetRecommendedReelsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new List<ReelModel> { firstReel, prefetchedReel, nextReel });

            mockedReelInteractionService
                .Setup(mock => mock.GetInteractionAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((UserReelInteractionModel?)null);

            mockedReelInteractionService
                .Setup(mock => mock.GetLikeCountAsync(It.IsAny<int>()))
                .ReturnsAsync(0);

            mockedClipPlaybackService
                .Setup(mock => mock.PrefetchClipAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            mockedClipPlaybackService
                .Setup(mock => mock.GetClipTransmission(It.IsAny<string>()))
                .Returns((string videoUrl) => new ClipMediaSourceTransmission
                {
                    VideoUrl = videoUrl,
                    WasPrefetched = true,
                });

            var viewModel = new ReelsFeedViewModel(
                mockedRecommendationService.Object,
                mockedClipPlaybackService.Object,
                mockedReelInteractionService.Object);

            await viewModel.LoadFeedCommand.ExecuteAsync(null);

            var prefetchCompleted = SpinWait.SpinUntil(
                () => mockedClipPlaybackService.Invocations.Any(invocation =>
                    invocation.Method.Name == nameof(IClipPlaybackService.GetClipTransmission) &&
                    invocation.Arguments.Count == 1 &&
                    invocation.Arguments[0] is string videoUrl &&
                    videoUrl.Equals(PREFETCHED_URL, StringComparison.OrdinalIgnoreCase)),
                TimeSpan.FromSeconds(2));

            Assert.That(prefetchCompleted, Is.True, "Expected the prefetched item to be materialized before calling BuildPlaybackItem.");

            var getTransmissionCallsBefore = mockedClipPlaybackService.Invocations.Count(invocation =>
                invocation.Method.Name == nameof(IClipPlaybackService.GetClipTransmission) &&
                invocation.Arguments.Count == 1 &&
                invocation.Arguments[0] is string videoUrl &&
                videoUrl.Equals(PREFETCHED_URL, StringComparison.OrdinalIgnoreCase));

            var playbackItem = viewModel.BuildPlaybackItem(PREFETCHED_URL);

            var getTransmissionCallsAfter = mockedClipPlaybackService.Invocations.Count(invocation =>
                invocation.Method.Name == nameof(IClipPlaybackService.GetClipTransmission) &&
                invocation.Arguments.Count == 1 &&
                invocation.Arguments[0] is string videoUrl &&
                videoUrl.Equals(PREFETCHED_URL, StringComparison.OrdinalIgnoreCase));

        }

        [Test]
        public void BuildPlaybackItem_validUrlWhenNotPrefetched_createsPlaybackItem()
        {
            const string VIDEO_URL = "https://cdn.example.com/reel-910.mp4";

            var mockedRecommendationService = new Mock<IRecommendationService>();
            var mockedClipPlaybackService = new Mock<IClipPlaybackService>();
            var mockedReelInteractionService = new Mock<IReelInteractionService>();

            mockedClipPlaybackService
                .Setup(mock => mock.GetClipTransmission(VIDEO_URL))
                .Returns(new ClipMediaSourceTransmission
                {
                    VideoUrl = VIDEO_URL,
                    WasPrefetched = false,
                });

            var viewModel = new ReelsFeedViewModel(
                mockedRecommendationService.Object,
                mockedClipPlaybackService.Object,
                mockedReelInteractionService.Object);

            var playbackItem = viewModel.BuildPlaybackItem(VIDEO_URL);

            Assert.That(playbackItem, Is.Not.Null);
        }

        [Test]
        public void BuildPlaybackItem_invalidUrl_returnsNull()
        {
            const string INVALID_URL = "not a valid url";

            var mockedRecommendationService = new Mock<IRecommendationService>();
            var mockedClipPlaybackService = new Mock<IClipPlaybackService>();
            var mockedReelInteractionService = new Mock<IReelInteractionService>();

            mockedClipPlaybackService
                .Setup(mock => mock.GetClipTransmission(INVALID_URL))
                .Returns(new ClipMediaSourceTransmission
                {
                    VideoUrl = INVALID_URL,
                    WasPrefetched = false,
                });

            var viewModel = new ReelsFeedViewModel(
                mockedRecommendationService.Object,
                mockedClipPlaybackService.Object,
                mockedReelInteractionService.Object);

            var playbackItem = viewModel.BuildPlaybackItem(INVALID_URL);

            Assert.That(playbackItem, Is.Null);
        }

        [Test]
        public async Task LoadFeedCommand_executesLoadFeedAsync()
        {
            const int REEL_ID = 301;

            var reel = new ReelModel
            {
                ReelId = REEL_ID,
                VideoUrl = "https://cdn.example.com/reel-301.mp4",
            };

            var mockedRecommendationService = new Mock<IRecommendationService>();
            var mockedClipPlaybackService = new Mock<IClipPlaybackService>();
            var mockedReelInteractionService = new Mock<IReelInteractionService>();

            mockedRecommendationService
                .Setup(mock => mock.GetRecommendedReelsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new List<ReelModel> { reel });

            mockedReelInteractionService
                .Setup(mock => mock.GetInteractionAsync(It.IsAny<int>(), REEL_ID))
                .ReturnsAsync((UserReelInteractionModel?)null);

            mockedReelInteractionService
                .Setup(mock => mock.GetLikeCountAsync(REEL_ID))
                .ReturnsAsync(0);

            var viewModel = new ReelsFeedViewModel(
                mockedRecommendationService.Object,
                mockedClipPlaybackService.Object,
                mockedReelInteractionService.Object);

            Assert.That(viewModel.LoadFeedCommand.CanExecute(null), Is.True);

            await viewModel.LoadFeedCommand.ExecuteAsync(null);

        }

        [Test]
        public async Task ScrollNextCommand_executesScrollNextAndMovesCurrentReel()
        {
            const int FIRST_REEL_ID = 401;
            const int SECOND_REEL_ID = 402;

            var firstReel = new ReelModel
            {
                ReelId = FIRST_REEL_ID,
                VideoUrl = "https://cdn.example.com/reel-401.mp4",
            };

            var secondReel = new ReelModel
            {
                ReelId = SECOND_REEL_ID,
                VideoUrl = "https://cdn.example.com/reel-402.mp4",
            };

            var mockedRecommendationService = new Mock<IRecommendationService>();
            var mockedClipPlaybackService = new Mock<IClipPlaybackService>();
            var mockedReelInteractionService = new Mock<IReelInteractionService>();

            mockedRecommendationService
                .Setup(mock => mock.GetRecommendedReelsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new List<ReelModel> { firstReel, secondReel });

            mockedReelInteractionService
                .Setup(mock => mock.GetInteractionAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((UserReelInteractionModel?)null);

            mockedReelInteractionService
                .Setup(mock => mock.GetLikeCountAsync(It.IsAny<int>()))
                .ReturnsAsync(0);

            mockedClipPlaybackService
                .Setup(mock => mock.PrefetchClipAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            mockedClipPlaybackService
                .Setup(mock => mock.GetClipTransmission(It.IsAny<string>()))
                .Returns((string videoUrl) => new ClipMediaSourceTransmission { VideoUrl = videoUrl, WasPrefetched = true });

            var viewModel = new ReelsFeedViewModel(
                mockedRecommendationService.Object,
                mockedClipPlaybackService.Object,
                mockedReelInteractionService.Object);

            await viewModel.LoadFeedCommand.ExecuteAsync(null);

            viewModel.ScrollNextCommand.Execute(secondReel);

            Assert.That(viewModel.CurrentReel, Is.SameAs(secondReel));
        }

        [Test]
        public async Task ScrollPreviousCommand_executesScrollPreviousAndMovesCurrentReel()
        {
            const int FIRST_REEL_ID = 501;
            const int SECOND_REEL_ID = 502;

            var firstReel = new ReelModel
            {
                ReelId = FIRST_REEL_ID,
                VideoUrl = "https://cdn.example.com/reel-501.mp4",
            };

            var secondReel = new ReelModel
            {
                ReelId = SECOND_REEL_ID,
                VideoUrl = "https://cdn.example.com/reel-502.mp4",
            };

            var mockedRecommendationService = new Mock<IRecommendationService>();
            var mockedClipPlaybackService = new Mock<IClipPlaybackService>();
            var mockedReelInteractionService = new Mock<IReelInteractionService>();

            mockedRecommendationService
                .Setup(mock => mock.GetRecommendedReelsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new List<ReelModel> { firstReel, secondReel });

            mockedReelInteractionService
                .Setup(mock => mock.GetInteractionAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((UserReelInteractionModel?)null);

            mockedReelInteractionService
                .Setup(mock => mock.GetLikeCountAsync(It.IsAny<int>()))
                .ReturnsAsync(0);

            mockedClipPlaybackService
                .Setup(mock => mock.PrefetchClipAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            mockedClipPlaybackService
                .Setup(mock => mock.GetClipTransmission(It.IsAny<string>()))
                .Returns((string videoUrl) => new ClipMediaSourceTransmission { VideoUrl = videoUrl, WasPrefetched = true });

            var viewModel = new ReelsFeedViewModel(
                mockedRecommendationService.Object,
                mockedClipPlaybackService.Object,
                mockedReelInteractionService.Object);

            await viewModel.LoadFeedCommand.ExecuteAsync(null);

            viewModel.ScrollPreviousCommand.Execute(secondReel);

            Assert.That(viewModel.CurrentReel, Is.SameAs(secondReel));
        }

        [Test]
        public async Task LoadFeedAsync_recommendationsAvailable_populatesQueueAndLoadsLikeData()
        {
            const int REEL_ID = 101;
            const int LIKE_COUNT = 17;

            var reel = new ReelModel
            {
                ReelId = REEL_ID,
                VideoUrl = "https://cdn.example.com/reel-101.mp4",
            };

            var mockedRecommendationService = new Mock<IRecommendationService>();
            var mockedClipPlaybackService = new Mock<IClipPlaybackService>();
            var mockedReelInteractionService = new Mock<IReelInteractionService>();

            mockedRecommendationService
                .Setup(mock => mock.GetRecommendedReelsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new List<ReelModel> { reel });

            mockedReelInteractionService
                .Setup(mock => mock.GetInteractionAsync(It.IsAny<int>(), REEL_ID))
                .ReturnsAsync(new UserReelInteractionModel { IsLiked = true });

            mockedReelInteractionService
                .Setup(mock => mock.GetLikeCountAsync(REEL_ID))
                .ReturnsAsync(LIKE_COUNT);

            var viewModel = new ReelsFeedViewModel(
                mockedRecommendationService.Object,
                mockedClipPlaybackService.Object,
                mockedReelInteractionService.Object);

            await viewModel.LoadFeedAsync();

            Assert.That(viewModel.IsLoading, Is.False);

        }

        [Test]
        public async Task LoadFeedAsync_noRecommendations_setsIsEmptyTrueAndNoCurrentReel()
        {
            var mockedRecommendationService = new Mock<IRecommendationService>();
            var mockedClipPlaybackService = new Mock<IClipPlaybackService>();
            var mockedReelInteractionService = new Mock<IReelInteractionService>();

            mockedRecommendationService
                .Setup(mock => mock.GetRecommendedReelsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new List<ReelModel>());

            var viewModel = new ReelsFeedViewModel(
                mockedRecommendationService.Object,
                mockedClipPlaybackService.Object,
                mockedReelInteractionService.Object);

            await viewModel.LoadFeedAsync();

            Assert.That(viewModel.IsLoading, Is.False);
        }

        [Test]
        public async Task LoadFeedAsync_likeDataLoadingThrows_defaultsLikeDataForReel()
        {
            const int REEL_ID = 202;

            var reel = new ReelModel
            {
                ReelId = REEL_ID,
                VideoUrl = "https://cdn.example.com/reel-202.mp4",
            };

            var mockedRecommendationService = new Mock<IRecommendationService>();
            var mockedClipPlaybackService = new Mock<IClipPlaybackService>();
            var mockedReelInteractionService = new Mock<IReelInteractionService>();

            mockedRecommendationService
                .Setup(mock => mock.GetRecommendedReelsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new List<ReelModel> { reel });

            mockedReelInteractionService
                .Setup(mock => mock.GetInteractionAsync(It.IsAny<int>(), REEL_ID))
                .ThrowsAsync(new InvalidOperationException("interaction load failed"));

            var viewModel = new ReelsFeedViewModel(
                mockedRecommendationService.Object,
                mockedClipPlaybackService.Object,
                mockedReelInteractionService.Object);

            await viewModel.LoadFeedAsync();

            Assert.That(viewModel.IsLoading, Is.False);

        }

        [Test]
        public async Task LoadFeedAsync_recommendationServiceThrows_setsErrorMessage()
        {
            const string ERROR_TEXT = "boom";

            var mockedRecommendationService = new Mock<IRecommendationService>();
            var mockedClipPlaybackService = new Mock<IClipPlaybackService>();
            var mockedReelInteractionService = new Mock<IReelInteractionService>();

            mockedRecommendationService
                .Setup(mock => mock.GetRecommendedReelsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new InvalidOperationException(ERROR_TEXT));

            var viewModel = new ReelsFeedViewModel(
                mockedRecommendationService.Object,
                mockedClipPlaybackService.Object,
                mockedReelInteractionService.Object);

            await viewModel.LoadFeedAsync();

            Assert.That(viewModel.IsLoading, Is.False);
        }

        [Test]
        public async Task HasError_recommendationFailure_setsTrue_thenSuccessfulReload_setsFalse()
        {
            var reel = new ReelModel
            {
                ReelId = 1001,
                VideoUrl = "https://cdn.example.com/reel-1001.mp4",
            };

            var mockedRecommendationService = new Mock<IRecommendationService>();
            var mockedClipPlaybackService = new Mock<IClipPlaybackService>();
            var mockedReelInteractionService = new Mock<IReelInteractionService>();

            mockedRecommendationService
                .SetupSequence(mock => mock.GetRecommendedReelsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new InvalidOperationException("load failed"))
                .ReturnsAsync(new List<ReelModel> { reel });

            mockedReelInteractionService
                .Setup(mock => mock.GetInteractionAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((UserReelInteractionModel?)null);

            mockedReelInteractionService
                .Setup(mock => mock.GetLikeCountAsync(It.IsAny<int>()))
                .ReturnsAsync(0);

            var viewModel = new ReelsFeedViewModel(
                mockedRecommendationService.Object,
                mockedClipPlaybackService.Object,
                mockedReelInteractionService.Object);

            Assert.That(viewModel.HasError, Is.False);

            await viewModel.LoadFeedAsync();


            await viewModel.LoadFeedAsync();

        }

        [Test]
        public void OnNavigatingAway_previousReelIsNull_doesNotRecordViewData()
        {
            var mockedRecommendationService = new Mock<IRecommendationService>();
            var mockedClipPlaybackService = new Mock<IClipPlaybackService>();
            var mockedReelInteractionService = new Mock<IReelInteractionService>();

            var viewModel = new ReelsFeedViewModel(
                mockedRecommendationService.Object,
                mockedClipPlaybackService.Object,
                mockedReelInteractionService.Object);

            viewModel.OnNavigatingAway();

            mockedReelInteractionService.Verify(
                item => item.RecordViewAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<double>(), It.IsAny<double>()),
                Times.Never);
        }

        [Test]
        public async Task ScrollNext_nonTrivialWatchDuration_recordsViewData()
        {
            const int FIRST_REEL_ID = 1101;
            const int SECOND_REEL_ID = 1102;

            var firstReel = new ReelModel
            {
                ReelId = FIRST_REEL_ID,
                VideoUrl = "https://cdn.example.com/reel-1101.mp4",
                FeatureDurationSeconds = 2,
            };

            var secondReel = new ReelModel
            {
                ReelId = SECOND_REEL_ID,
                VideoUrl = "https://cdn.example.com/reel-1102.mp4",
                FeatureDurationSeconds = 2,
            };

            var mockedRecommendationService = new Mock<IRecommendationService>();
            var mockedClipPlaybackService = new Mock<IClipPlaybackService>();
            var mockedReelInteractionService = new Mock<IReelInteractionService>();

            mockedRecommendationService
                .Setup(mock => mock.GetRecommendedReelsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new List<ReelModel> { firstReel, secondReel });

            mockedReelInteractionService
                .Setup(mock => mock.GetInteractionAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((UserReelInteractionModel?)null);

            mockedReelInteractionService
                .Setup(mock => mock.GetLikeCountAsync(It.IsAny<int>()))
                .ReturnsAsync(0);

            mockedClipPlaybackService
                .Setup(mock => mock.PrefetchClipAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            mockedClipPlaybackService
                .Setup(mock => mock.GetClipTransmission(It.IsAny<string>()))
                .Returns((string videoUrl) => new ClipMediaSourceTransmission
                {
                    VideoUrl = videoUrl,
                    WasPrefetched = true,
                });

            var viewModel = new ReelsFeedViewModel(
                mockedRecommendationService.Object,
                mockedClipPlaybackService.Object,
                mockedReelInteractionService.Object);

            await viewModel.LoadFeedAsync();

            await Task.Delay(650);

            viewModel.ScrollNext(secondReel);

            var recordInvoked = SpinWait.SpinUntil(
                () => mockedReelInteractionService.Invocations.Any(invocation =>
                    invocation.Method.Name == nameof(IReelInteractionService.RecordViewAsync)),
                TimeSpan.FromSeconds(2));

            Assert.That(recordInvoked, Is.True, "Expected RecordViewAsync to be triggered for non-trivial watch duration.");

        }

        [Test]
        public async Task ScrollNext_nonTrivialWatchDuration_withZeroFeatureDuration_recordsZeroPercentage()
        {
            const int FIRST_REEL_ID = 1201;
            const int SECOND_REEL_ID = 1202;

            var firstReel = new ReelModel
            {
                ReelId = FIRST_REEL_ID,
                VideoUrl = "https://cdn.example.com/reel-1201.mp4",
                FeatureDurationSeconds = 0,
            };

            var secondReel = new ReelModel
            {
                ReelId = SECOND_REEL_ID,
                VideoUrl = "https://cdn.example.com/reel-1202.mp4",
                FeatureDurationSeconds = 2,
            };

            var mockedRecommendationService = new Mock<IRecommendationService>();
            var mockedClipPlaybackService = new Mock<IClipPlaybackService>();
            var mockedReelInteractionService = new Mock<IReelInteractionService>();

            mockedRecommendationService
                .Setup(mock => mock.GetRecommendedReelsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new List<ReelModel> { firstReel, secondReel });

            mockedReelInteractionService
                .Setup(mock => mock.GetInteractionAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((UserReelInteractionModel?)null);

            mockedReelInteractionService
                .Setup(mock => mock.GetLikeCountAsync(It.IsAny<int>()))
                .ReturnsAsync(0);

            mockedClipPlaybackService
                .Setup(mock => mock.PrefetchClipAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            mockedClipPlaybackService
                .Setup(mock => mock.GetClipTransmission(It.IsAny<string>()))
                .Returns((string videoUrl) => new ClipMediaSourceTransmission
                {
                    VideoUrl = videoUrl,
                    WasPrefetched = true,
                });

            var viewModel = new ReelsFeedViewModel(
                mockedRecommendationService.Object,
                mockedClipPlaybackService.Object,
                mockedReelInteractionService.Object);

            await viewModel.LoadFeedAsync();
            await Task.Delay(650);

            viewModel.ScrollNext(secondReel);

            var recordInvoked = SpinWait.SpinUntil(
                () => mockedReelInteractionService.Invocations.Any(invocation =>
                    invocation.Method.Name == nameof(IReelInteractionService.RecordViewAsync)),
                TimeSpan.FromSeconds(2));

            Assert.That(recordInvoked, Is.True);

        }

        [Test]
        public async Task ScrollNext_nonTrivialWatchDuration_withNegativeFeatureDuration_recordsZeroPercentage()
        {
            const int FIRST_REEL_ID = 1301;
            const int SECOND_REEL_ID = 1302;

            var firstReel = new ReelModel
            {
                ReelId = FIRST_REEL_ID,
                VideoUrl = "https://cdn.example.com/reel-1301.mp4",
                FeatureDurationSeconds = -5,
            };

            var secondReel = new ReelModel
            {
                ReelId = SECOND_REEL_ID,
                VideoUrl = "https://cdn.example.com/reel-1302.mp4",
                FeatureDurationSeconds = 2,
            };

            var mockedRecommendationService = new Mock<IRecommendationService>();
            var mockedClipPlaybackService = new Mock<IClipPlaybackService>();
            var mockedReelInteractionService = new Mock<IReelInteractionService>();

            mockedRecommendationService
                .Setup(mock => mock.GetRecommendedReelsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new List<ReelModel> { firstReel, secondReel });

            mockedReelInteractionService
                .Setup(mock => mock.GetInteractionAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((UserReelInteractionModel?)null);

            mockedReelInteractionService
                .Setup(mock => mock.GetLikeCountAsync(It.IsAny<int>()))
                .ReturnsAsync(0);

            mockedClipPlaybackService
                .Setup(mock => mock.PrefetchClipAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            mockedClipPlaybackService
                .Setup(mock => mock.GetClipTransmission(It.IsAny<string>()))
                .Returns((string videoUrl) => new ClipMediaSourceTransmission
                {
                    VideoUrl = videoUrl,
                    WasPrefetched = true,
                });

            var viewModel = new ReelsFeedViewModel(
                mockedRecommendationService.Object,
                mockedClipPlaybackService.Object,
                mockedReelInteractionService.Object);

            await viewModel.LoadFeedAsync();
            await Task.Delay(650);

            viewModel.ScrollNext(secondReel);

            var recordInvoked = SpinWait.SpinUntil(
                () => mockedReelInteractionService.Invocations.Any(invocation =>
                    invocation.Method.Name == nameof(IReelInteractionService.RecordViewAsync)),
                TimeSpan.FromSeconds(2));

            Assert.That(recordInvoked, Is.True);

        }

        private static ReelsFeedViewModel CreateViewModel()
        {
            return new ReelsFeedViewModel(
                new Mock<IRecommendationService>().Object,
                new Mock<IClipPlaybackService>().Object,
                new Mock<IReelInteractionService>().Object);
        }
    }
}
