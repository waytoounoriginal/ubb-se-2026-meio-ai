using NUnit.Framework;
using ubb_se_2026_meio_ai.Features.ReelsFeed.Services;

namespace UnitTests.ReelsFeed
{
    [TestFixture]
    public class ClipPlaybackServiceTests
    {
        [Test]
        public async Task PrefetchClipAsync_validUrlThenGetClipTransmission_returnsPrefetchedTransmission()
        {
            const string VIDEO_URL = "https://cdn.example.com/reel-101.mp4";

            var service = new ClipPlaybackService();

            await service.PrefetchClipAsync(VIDEO_URL);
            var transmission = service.GetClipTransmission(VIDEO_URL);

            Assert.That(transmission.VideoUrl, Is.EqualTo(VIDEO_URL));
            Assert.That(transmission.WasPrefetched, Is.True);
        }

        [Test]
        public void GetClipTransmission_urlNotPrefetched_returnsNotPrefetchedTransmission()
        {
            const string VIDEO_URL = "https://cdn.example.com/reel-102.mp4";

            var service = new ClipPlaybackService();

            var transmission = service.GetClipTransmission(VIDEO_URL);

            Assert.That(transmission.VideoUrl, Is.EqualTo(VIDEO_URL));
            Assert.That(transmission.WasPrefetched, Is.False);
        }

        [Test]
        public async Task GetClipTransmission_urlConsumedOnce_returnsPrefetchedOnlyOnFirstRequest()
        {
            const string VIDEO_URL = "https://cdn.example.com/reel-103.mp4";

            var service = new ClipPlaybackService();

            await service.PrefetchClipAsync(VIDEO_URL);

            var firstTransmission = service.GetClipTransmission(VIDEO_URL);
            var secondTransmission = service.GetClipTransmission(VIDEO_URL);

            Assert.That(firstTransmission.WasPrefetched, Is.True);
            Assert.That(secondTransmission.WasPrefetched, Is.False);
        }

        [Test]
        public async Task PrefetchClipAsync_sameUrlDifferentCaseThenGetClipTransmission_returnsPrefetchedTransmission()
        {
            const string UPPERCASE_URL = "HTTPS://CDN.EXAMPLE.COM/REEL-104.MP4";
            const string LOWERCASE_URL = "https://cdn.example.com/reel-104.mp4";

            var service = new ClipPlaybackService();

            await service.PrefetchClipAsync(UPPERCASE_URL);
            var transmission = service.GetClipTransmission(LOWERCASE_URL);

            Assert.That(transmission.VideoUrl, Is.EqualTo(LOWERCASE_URL));
            Assert.That(transmission.WasPrefetched, Is.True);
        }

        [Test]
        public async Task PrefetchClipAsync_invalidUrlThenGetClipTransmission_returnsNotPrefetchedTransmission()
        {
            const string INVALID_URL = "   ";

            var service = new ClipPlaybackService();

            await service.PrefetchClipAsync(INVALID_URL);
            var transmission = service.GetClipTransmission(INVALID_URL);

            Assert.That(transmission.VideoUrl, Is.EqualTo(INVALID_URL));
            Assert.That(transmission.WasPrefetched, Is.False);
        }

    }
}
