namespace UnitTests.ReelsEditing
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Moq;
    using NUnit.Framework;
    using ubb_se_2026_meio_ai.Core.Models;
    using ubb_se_2026_meio_ai.Features.ReelsEditing.Services;
    using ubb_se_2026_meio_ai.Features.ReelsEditing.ViewModels;

    /// <summary>
    /// Unit tests for the <see cref="MusicSelectionDialogViewModel"/> class.
    /// </summary>
    [TestFixture]
    public class MusicSelectionDialogViewModelTests
    {
        /// <summary>
        /// Tests that executing the load tracks command populates the available tracks collection.
        /// </summary>
        /// <returns>A task that represents the asynchronous test operation.</returns>
        [Test]
        public async Task LoadTracksCommand_TracksExistInLibrary_SetsAvailableTracksCollection()
        {
            var mockedAudioLibrary = new Mock<IAudioLibraryRepository>();

            var expectedTracks = new List<MusicTrackModel>
            {
                new MusicTrackModel { MusicTrackId = 1, TrackName = "Lofi Chill" },
                new MusicTrackModel { MusicTrackId = 2, TrackName = "Upbeat Pop" },
            };

            mockedAudioLibrary
                .Setup(library => library.GetAllTracksAsync())
                .ReturnsAsync(expectedTracks);

            var viewModel = new MusicSelectionDialogViewModel(mockedAudioLibrary.Object);

            await viewModel.LoadTracksCommand.ExecuteAsync(null);

            Assert.That(viewModel.AvailableTracks, Is.Not.Null);
        }

        [Test]
        public async Task LoadTracksCommand_TracksExistInLibrary_SetsExpectedTrackCount()
        {
            var mockedAudioLibrary = new Mock<IAudioLibraryRepository>();
            var expectedTracks = new List<MusicTrackModel>
            {
                new MusicTrackModel { MusicTrackId = 1, TrackName = "Lofi Chill" },
                new MusicTrackModel { MusicTrackId = 2, TrackName = "Upbeat Pop" },
            };

            mockedAudioLibrary.Setup(library => library.GetAllTracksAsync()).ReturnsAsync(expectedTracks);
            var viewModel = new MusicSelectionDialogViewModel(mockedAudioLibrary.Object);

            await viewModel.LoadTracksCommand.ExecuteAsync(null);

            Assert.That(viewModel.AvailableTracks.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task LoadTracksCommand_TracksExistInLibrary_SetsExpectedFirstTrackName()
        {
            var mockedAudioLibrary = new Mock<IAudioLibraryRepository>();
            var expectedTracks = new List<MusicTrackModel>
            {
                new MusicTrackModel { MusicTrackId = 1, TrackName = "Lofi Chill" },
                new MusicTrackModel { MusicTrackId = 2, TrackName = "Upbeat Pop" },
            };

            mockedAudioLibrary.Setup(library => library.GetAllTracksAsync()).ReturnsAsync(expectedTracks);
            var viewModel = new MusicSelectionDialogViewModel(mockedAudioLibrary.Object);

            await viewModel.LoadTracksCommand.ExecuteAsync(null);

            Assert.That(viewModel.AvailableTracks[0].TrackName, Is.EqualTo("Lofi Chill"));
        }

        [Test]
        public async Task LoadTracksCommand_TracksExistInLibrary_CallsRepositoryOnce()
        {
            var mockedAudioLibrary = new Mock<IAudioLibraryRepository>();
            var expectedTracks = new List<MusicTrackModel>
            {
                new MusicTrackModel { MusicTrackId = 1, TrackName = "Lofi Chill" },
                new MusicTrackModel { MusicTrackId = 2, TrackName = "Upbeat Pop" },
            };

            mockedAudioLibrary.Setup(library => library.GetAllTracksAsync()).ReturnsAsync(expectedTracks);
            var viewModel = new MusicSelectionDialogViewModel(mockedAudioLibrary.Object);

            await viewModel.LoadTracksCommand.ExecuteAsync(null);

            mockedAudioLibrary.Verify(library => library.GetAllTracksAsync(), Times.Once);
        }

        /// <summary>
        /// Tests that executing the select track command sets the selected track property.
        /// </summary>
        [Test]
        public void SelectTrackCommand_ValidTrackProvided_SetsSelectedTrack()
        {
            var mockedAudioLibrary = new Mock<IAudioLibraryRepository>();
            var viewModel = new MusicSelectionDialogViewModel(mockedAudioLibrary.Object);

            var chosenTrack = new MusicTrackModel { MusicTrackId = 99, TrackName = "Epic Soundtrack" };

            viewModel.SelectTrackCommand.Execute(chosenTrack);

            Assert.That(viewModel.SelectedTrack, Is.Not.Null);
        }

        [Test]
        public void SelectTrackCommand_ValidTrackProvided_SetsSelectedTrackId()
        {
            var mockedAudioLibrary = new Mock<IAudioLibraryRepository>();
            var viewModel = new MusicSelectionDialogViewModel(mockedAudioLibrary.Object);

            var chosenTrack = new MusicTrackModel { MusicTrackId = 99, TrackName = "Epic Soundtrack" };
            viewModel.SelectTrackCommand.Execute(chosenTrack);

            Assert.That(viewModel.SelectedTrack!.MusicTrackId, Is.EqualTo(99));
        }

        [Test]
        public void SelectTrackCommand_ValidTrackProvided_SetsSelectedTrackName()
        {
            var mockedAudioLibrary = new Mock<IAudioLibraryRepository>();
            var viewModel = new MusicSelectionDialogViewModel(mockedAudioLibrary.Object);

            var chosenTrack = new MusicTrackModel { MusicTrackId = 99, TrackName = "Epic Soundtrack" };
            viewModel.SelectTrackCommand.Execute(chosenTrack);

            Assert.That(viewModel.SelectedTrack.TrackName, Is.EqualTo("Epic Soundtrack"));
        }
    }
}
