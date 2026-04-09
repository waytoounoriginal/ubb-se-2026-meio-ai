using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.ObjectModel;
using ubb_se_2026_meio_ai.Core.Database;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Core.Platform;
using ubb_se_2026_meio_ai.Features.ReelsUpload.Services;
using ubb_se_2026_meio_ai.Core.Services;

namespace ubb_se_2026_meio_ai.Features.ReelsUpload.ViewModels
{
    /// <summary>
    /// ViewModel for the Reels Upload page.
    /// Owner: Alex
    /// </summary>
    public partial class ReelsUploadViewModel : ObservableObject
    {
        private readonly IAppWindowContext _appWindowContext;
        private readonly IVideoStorageService _videoStorageService;
        private readonly IMovieService _movieService;

        const string untitledName = "Untitled Reel";

        public ReelsUploadViewModel(
            IAppWindowContext appWindowContext,
            IVideoStorageService videoStorageService,
            IMovieService movieService)
        {
            _appWindowContext = appWindowContext;
            _videoStorageService = videoStorageService;
            _movieService = movieService;
            SuggestedMovies = new ObservableCollection<MovieCardModel>();
        }

        public ObservableCollection<MovieCardModel> SuggestedMovies { get; }

        [ObservableProperty]
        private string _pageTitle = "Reels Upload";

        [ObservableProperty]
        private string _statusMessage = "Ready to upload.";

        // TODO: Replace with actual authenticated user ID later
        private const int _currentUserID = 1;

        [ObservableProperty]
        private string _reelTitle = string.Empty;

        [ObservableProperty]
        private string _reelCaption = string.Empty;

        [ObservableProperty]
        private MovieCardModel? _linkedMovie;

        [ObservableProperty]
        private string _localVideoFilePath = string.Empty;

        const string videoFileExtension = ".mp4";

        [RelayCommand]
        private async Task SelectVideoFileAsync()
        {
            var filePicker = new Windows.Storage.Pickers.FileOpenPicker();
            filePicker.FileTypeFilter.Add(videoFileExtension);

            var windowHandle = _appWindowContext.GetMainWindowHandle();
            WinRT.Interop.InitializeWithWindow.Initialize(filePicker, windowHandle);

            var selectedMovieFile = await filePicker.PickSingleFileAsync();
            if (selectedMovieFile != null)
            {
                LocalVideoFilePath = selectedMovieFile.Path;
            }
        }

        [RelayCommand]
        private async Task UploadReelAsync()
        {
            if (string.IsNullOrWhiteSpace(LocalVideoFilePath))
            {
                StatusMessage = "Please select a video first!";
                return;
            }

            if (string.IsNullOrWhiteSpace(ReelTitle))
            {
                StatusMessage = "Please enter a title for the reel!";
                return;
            }

            if (LinkedMovie == null)
            {
                StatusMessage = "Please link a movie to the reel!";
                return;
            }

            _appWindowContext.TryEnqueueOnUiThread(() => StatusMessage = "Validating video format...");

            try
            {
                bool isValid = await _videoStorageService.ValidateVideoAsync(LocalVideoFilePath);
                if (!isValid)
                {
                    _appWindowContext.TryEnqueueOnUiThread(() => StatusMessage = "Invalid file! Must be a non-empty MP4 file\nno longer than 60 seconds.");
                    return;
                }

                _appWindowContext.TryEnqueueOnUiThread(() => StatusMessage = "Uploading to Blob Storage & saving metadata...");

                var request = new Models.ReelUploadRequest
                {
                    LocalFilePath = LocalVideoFilePath,
                    Title = ReelTitle,
                    Caption = ReelCaption ?? string.Empty,
                    UploaderUserId = _currentUserID,
                    MovieId = LinkedMovie.MovieId
                };

                var savedReel = await _videoStorageService.UploadVideoAsync(request);

                _appWindowContext.TryEnqueueOnUiThread(() =>
                {
                    StatusMessage = $"Success! Reel uploaded with ID {savedReel.ReelId}.";
                    LocalVideoFilePath = string.Empty;
                    ReelTitle = string.Empty;
                    ReelCaption = string.Empty;
                    LinkedMovie = null;
                });
            }
            catch (Exception ex)
            {
                string errorMessage = $"Upload Failed: {ex.Message}";
                _appWindowContext.TryEnqueueOnUiThread(() => StatusMessage = errorMessage);
            }
        }

        [RelayCommand]
        private void SelectMovie(MovieCardModel movieToSelect)
        {
            LinkedMovie = movieToSelect;
        }

        [RelayCommand]
        private async Task SearchMovieAsync(string partialMovieName)
        {
            if (string.IsNullOrWhiteSpace(partialMovieName))
            {
                _appWindowContext.TryEnqueueOnUiThread(() => SuggestedMovies.Clear());
                return;
            }

            try
            {
                // Ask the service for the data
                var searchResults = await _movieService.SearchTop10MoviesAsync(partialMovieName);

                // Update UI
                _appWindowContext.TryEnqueueOnUiThread(() =>
                {
                    SuggestedMovies.Clear();
                    foreach (var movie in searchResults)
                    {
                        SuggestedMovies.Add(movie);
                    }
                });
            }
            catch (Exception ex)
            {
                _appWindowContext.TryEnqueueOnUiThread(() => StatusMessage = $"Search Error: {ex.Message}");
            }
        }
    }
}