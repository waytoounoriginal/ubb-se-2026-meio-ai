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
                .Setup(x => x.GetRecommendedReelsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new List<ReelModel> { firstReel, prefetchedReel, nextReel });

            mockedReelInteractionService
                .Setup(x => x.GetInteractionAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((UserReelInteractionModel?)null);

            mockedReelInteractionService
                .Setup(x => x.GetLikeCountAsync(It.IsAny<int>()))
                .ReturnsAsync(0);

            mockedClipPlaybackService
                .Setup(x => x.PrefetchClipAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            mockedClipPlaybackService
                .Setup(x => x.GetClipTransmission(It.IsAny<string>()))
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

            Assert.That(playbackItem, Is.Not.Null);
            Assert.That(getTransmissionCallsAfter, Is.EqualTo(getTransmissionCallsBefore));
        }

        [Test]
        public void BuildPlaybackItem_validUrlWhenNotPrefetched_createsPlaybackItem()
        {
            const string VIDEO_URL = "https://cdn.example.com/reel-910.mp4";

            var mockedRecommendationService = new Mock<IRecommendationService>();
            var mockedClipPlaybackService = new Mock<IClipPlaybackService>();
            var mockedReelInteractionService = new Mock<IReelInteractionService>();

            mockedClipPlaybackService
                .Setup(x => x.GetClipTransmission(VIDEO_URL))
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
            mockedClipPlaybackService.Verify(x => x.GetClipTransmission(VIDEO_URL), Times.Once);
        }

        [Test]
        public void BuildPlaybackItem_invalidUrl_returnsNull()
        {
            const string INVALID_URL = "not a valid url";

            var mockedRecommendationService = new Mock<IRecommendationService>();
            var mockedClipPlaybackService = new Mock<IClipPlaybackService>();
            var mockedReelInteractionService = new Mock<IReelInteractionService>();

            mockedClipPlaybackService
                .Setup(x => x.GetClipTransmission(INVALID_URL))
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
            mockedClipPlaybackService.Verify(x => x.GetClipTransmission(INVALID_URL), Times.Once);
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
                .Setup(x => x.GetRecommendedReelsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new List<ReelModel> { reel });

            mockedReelInteractionService
                .Setup(x => x.GetInteractionAsync(It.IsAny<int>(), REEL_ID))
                .ReturnsAsync((UserReelInteractionModel?)null);

            mockedReelInteractionService
                .Setup(x => x.GetLikeCountAsync(REEL_ID))
                .ReturnsAsync(0);

            var viewModel = new ReelsFeedViewModel(
                mockedRecommendationService.Object,
                mockedClipPlaybackService.Object,
                mockedReelInteractionService.Object);

            Assert.That(viewModel.LoadFeedCommand.CanExecute(null), Is.True);

            await viewModel.LoadFeedCommand.ExecuteAsync(null);

            Assert.That(viewModel.CurrentReel, Is.Not.Null);
            Assert.That(viewModel.CurrentReel!.ReelId, Is.EqualTo(REEL_ID));
            mockedRecommendationService.Verify(x => x.GetRecommendedReelsAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Once);
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
                .Setup(x => x.GetRecommendedReelsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new List<ReelModel> { firstReel, secondReel });

            mockedReelInteractionService
                .Setup(x => x.GetInteractionAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((UserReelInteractionModel?)null);

            mockedReelInteractionService
                .Setup(x => x.GetLikeCountAsync(It.IsAny<int>()))
                .ReturnsAsync(0);

            mockedClipPlaybackService
                .Setup(x => x.PrefetchClipAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            mockedClipPlaybackService
                .Setup(x => x.GetClipTransmission(It.IsAny<string>()))
                .Returns((string videoUrl) => new ClipMediaSourceTransmission { VideoUrl = videoUrl, WasPrefetched = true });

            var viewModel = new ReelsFeedViewModel(
                mockedRecommendationService.Object,
                mockedClipPlaybackService.Object,
                mockedReelInteractionService.Object);

            await viewModel.LoadFeedCommand.ExecuteAsync(null);

            viewModel.ScrollNextCommand.Execute(secondReel);

            Assert.That(viewModel.CurrentReel, Is.SameAs(secondReel));
            mockedClipPlaybackService.Verify(x => x.PrefetchClipAsync(It.IsAny<string>()), Times.AtLeastOnce);
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
                .Setup(x => x.GetRecommendedReelsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new List<ReelModel> { firstReel, secondReel });

            mockedReelInteractionService
                .Setup(x => x.GetInteractionAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((UserReelInteractionModel?)null);

            mockedReelInteractionService
                .Setup(x => x.GetLikeCountAsync(It.IsAny<int>()))
                .ReturnsAsync(0);

            mockedClipPlaybackService
                .Setup(x => x.PrefetchClipAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            mockedClipPlaybackService
                .Setup(x => x.GetClipTransmission(It.IsAny<string>()))
                .Returns((string videoUrl) => new ClipMediaSourceTransmission { VideoUrl = videoUrl, WasPrefetched = true });

            var viewModel = new ReelsFeedViewModel(
                mockedRecommendationService.Object,
                mockedClipPlaybackService.Object,
                mockedReelInteractionService.Object);

            await viewModel.LoadFeedCommand.ExecuteAsync(null);

            viewModel.ScrollPreviousCommand.Execute(secondReel);

            Assert.That(viewModel.CurrentReel, Is.SameAs(secondReel));
            mockedClipPlaybackService.Verify(x => x.PrefetchClipAsync(It.IsAny<string>()), Times.AtLeastOnce);
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
                .Setup(x => x.GetRecommendedReelsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new List<ReelModel> { reel });

            mockedReelInteractionService
                .Setup(x => x.GetInteractionAsync(It.IsAny<int>(), REEL_ID))
                .ReturnsAsync(new UserReelInteractionModel { IsLiked = true });

            mockedReelInteractionService
                .Setup(x => x.GetLikeCountAsync(REEL_ID))
                .ReturnsAsync(LIKE_COUNT);

            var viewModel = new ReelsFeedViewModel(
                mockedRecommendationService.Object,
                mockedClipPlaybackService.Object,
                mockedReelInteractionService.Object);

            await viewModel.LoadFeedAsync();

            Assert.That(viewModel.IsLoading, Is.False);
            Assert.That(viewModel.ErrorMessage, Is.Null);
            Assert.That(viewModel.IsEmpty, Is.False);
            Assert.That(viewModel.StatusMessage, Is.Empty);
            Assert.That(viewModel.ReelQueue.Count, Is.EqualTo(1));
            Assert.That(viewModel.CurrentReel, Is.Not.Null);
            Assert.That(viewModel.CurrentReel!.ReelId, Is.EqualTo(REEL_ID));
            Assert.That(viewModel.CurrentReel.IsLiked, Is.True);
            Assert.That(viewModel.CurrentReel.LikeCount, Is.EqualTo(LIKE_COUNT));

            mockedReelInteractionService.Verify(x => x.GetInteractionAsync(It.IsAny<int>(), REEL_ID), Times.Once);
            mockedReelInteractionService.Verify(x => x.GetLikeCountAsync(REEL_ID), Times.Once);
        }

        [Test]
        public async Task LoadFeedAsync_noRecommendations_setsIsEmptyTrueAndNoCurrentReel()
        {
            var mockedRecommendationService = new Mock<IRecommendationService>();
            var mockedClipPlaybackService = new Mock<IClipPlaybackService>();
            var mockedReelInteractionService = new Mock<IReelInteractionService>();

            mockedRecommendationService
                .Setup(x => x.GetRecommendedReelsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new List<ReelModel>());

            var viewModel = new ReelsFeedViewModel(
                mockedRecommendationService.Object,
                mockedClipPlaybackService.Object,
                mockedReelInteractionService.Object);

            await viewModel.LoadFeedAsync();

            Assert.That(viewModel.IsLoading, Is.False);
            Assert.That(viewModel.ErrorMessage, Is.Null);
            Assert.That(viewModel.IsEmpty, Is.True);
            Assert.That(viewModel.CurrentReel, Is.Null);
            Assert.That(viewModel.ReelQueue.Count, Is.EqualTo(0));
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
                .Setup(x => x.GetRecommendedReelsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new List<ReelModel> { reel });

            mockedReelInteractionService
                .Setup(x => x.GetInteractionAsync(It.IsAny<int>(), REEL_ID))
                .ThrowsAsync(new InvalidOperationException("interaction load failed"));

            var viewModel = new ReelsFeedViewModel(
                mockedRecommendationService.Object,
                mockedClipPlaybackService.Object,
                mockedReelInteractionService.Object);

            await viewModel.LoadFeedAsync();

            Assert.That(viewModel.IsLoading, Is.False);
            Assert.That(viewModel.ErrorMessage, Is.Null);
            Assert.That(viewModel.CurrentReel, Is.Not.Null);
            Assert.That(viewModel.CurrentReel!.IsLiked, Is.False);
            Assert.That(viewModel.CurrentReel.LikeCount, Is.EqualTo(0));

            mockedReelInteractionService.Verify(x => x.GetLikeCountAsync(It.IsAny<int>()), Times.Never);
        }

        [Test]
        public async Task LoadFeedAsync_recommendationServiceThrows_setsErrorMessage()
        {
            const string ERROR_TEXT = "boom";

            var mockedRecommendationService = new Mock<IRecommendationService>();
            var mockedClipPlaybackService = new Mock<IClipPlaybackService>();
            var mockedReelInteractionService = new Mock<IReelInteractionService>();

            mockedRecommendationService
                .Setup(x => x.GetRecommendedReelsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new InvalidOperationException(ERROR_TEXT));

            var viewModel = new ReelsFeedViewModel(
                mockedRecommendationService.Object,
                mockedClipPlaybackService.Object,
                mockedReelInteractionService.Object);

            await viewModel.LoadFeedAsync();

            Assert.That(viewModel.IsLoading, Is.False);
            Assert.That(viewModel.ReelQueue.Count, Is.EqualTo(0));
            Assert.That(viewModel.ErrorMessage, Is.Not.Null);
            Assert.That(viewModel.ErrorMessage, Does.Contain(ERROR_TEXT));
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
                .SetupSequence(x => x.GetRecommendedReelsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new InvalidOperationException("load failed"))
                .ReturnsAsync(new List<ReelModel> { reel });

            mockedReelInteractionService
                .Setup(x => x.GetInteractionAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((UserReelInteractionModel?)null);

            mockedReelInteractionService
                .Setup(x => x.GetLikeCountAsync(It.IsAny<int>()))
                .ReturnsAsync(0);

            var viewModel = new ReelsFeedViewModel(
                mockedRecommendationService.Object,
                mockedClipPlaybackService.Object,
                mockedReelInteractionService.Object);

            Assert.That(viewModel.HasError, Is.False);

            await viewModel.LoadFeedAsync();

            Assert.That(viewModel.HasError, Is.True);
            Assert.That(viewModel.ErrorMessage, Is.Not.Null);

            await viewModel.LoadFeedAsync();

            Assert.That(viewModel.HasError, Is.False);
            Assert.That(viewModel.ErrorMessage, Is.Null);
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
                x => x.RecordViewAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<double>(), It.IsAny<double>()),
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
                .Setup(x => x.GetRecommendedReelsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new List<ReelModel> { firstReel, secondReel });

            mockedReelInteractionService
                .Setup(x => x.GetInteractionAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((UserReelInteractionModel?)null);

            mockedReelInteractionService
                .Setup(x => x.GetLikeCountAsync(It.IsAny<int>()))
                .ReturnsAsync(0);

            mockedClipPlaybackService
                .Setup(x => x.PrefetchClipAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            mockedClipPlaybackService
                .Setup(x => x.GetClipTransmission(It.IsAny<string>()))
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

            mockedReelInteractionService.Verify(
                x => x.RecordViewAsync(
                    1,
                    FIRST_REEL_ID,
                    It.Is<double>(watchSeconds => watchSeconds >= 0.5),
                    It.Is<double>(watchPercentage => watchPercentage > 0 && watchPercentage <= 100)),
                Times.Once);
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
                .Setup(x => x.GetRecommendedReelsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new List<ReelModel> { firstReel, secondReel });

            mockedReelInteractionService
                .Setup(x => x.GetInteractionAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((UserReelInteractionModel?)null);

            mockedReelInteractionService
                .Setup(x => x.GetLikeCountAsync(It.IsAny<int>()))
                .ReturnsAsync(0);

            mockedClipPlaybackService
                .Setup(x => x.PrefetchClipAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            mockedClipPlaybackService
                .Setup(x => x.GetClipTransmission(It.IsAny<string>()))
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

            mockedReelInteractionService.Verify(
                x => x.RecordViewAsync(
                    1,
                    FIRST_REEL_ID,
                    It.Is<double>(watchSeconds => watchSeconds >= 0.5),
                    0),
                Times.Once);
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
                .Setup(x => x.GetRecommendedReelsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new List<ReelModel> { firstReel, secondReel });

            mockedReelInteractionService
                .Setup(x => x.GetInteractionAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((UserReelInteractionModel?)null);

            mockedReelInteractionService
                .Setup(x => x.GetLikeCountAsync(It.IsAny<int>()))
                .ReturnsAsync(0);

            mockedClipPlaybackService
                .Setup(x => x.PrefetchClipAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            mockedClipPlaybackService
                .Setup(x => x.GetClipTransmission(It.IsAny<string>()))
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

            mockedReelInteractionService.Verify(
                x => x.RecordViewAsync(
                    1,
                    FIRST_REEL_ID,
                    It.Is<double>(watchSeconds => watchSeconds >= 0.5),
                    0),
                Times.Once);
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
