using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.SqlClient;
using ubb_se_2026_meio_ai.Core.Database;
using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Features.ReelsUpload.ViewModels
{
    /// <summary>
    /// ViewModel for the Reels Upload page.
    /// Owner: Alex
    /// </summary>
    public partial class ReelsUploadViewModel : ObservableObject
    {
        private readonly ISqlConnectionFactory _connectionFactory;
        private readonly ubb_se_2026_meio_ai.Features.ReelsUpload.Services.IVideoStorageService _videoStorageService;

        public ReelsUploadViewModel(
            ISqlConnectionFactory connectionFactory, 
            ubb_se_2026_meio_ai.Features.ReelsUpload.Services.IVideoStorageService videoStorageService)
        {
            _connectionFactory = connectionFactory;
            _videoStorageService = videoStorageService;
            SuggestedMovies = new ObservableCollection<MovieCardModel>();
        }

        public ObservableCollection<MovieCardModel> SuggestedMovies { get; }

        [ObservableProperty]
        private string _pageTitle = "Reels Upload";

        [ObservableProperty]
        private string _statusMessage = "Ready to upload.";

        // Reel attributes
        int currentUserID = 1;

        [ObservableProperty]
        private string _reelTitle = string.Empty;

        [ObservableProperty]
        private string _reelCaption = string.Empty;

        [ObservableProperty]
        private Core.Models.MovieCardModel? _linkedMovie;

        [ObservableProperty]
        private string _localVideoFilePath = string.Empty;

        // Button 1 click: let the user browse their PC for a video
        [RelayCommand]
        private async Task SelectVideoFileAsync()
        {
            var filePicker = new Windows.Storage.Pickers.FileOpenPicker();
            string videoFileExtension = ".mp4";
            filePicker.FileTypeFilter.Add(videoFileExtension);

            // In WinUI 3 Desktop apps, the file picker needs to know WHICH window it belongs to!
            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(filePicker, windowHandle);

            var selectedMovieFile = await filePicker.PickSingleFileAsync();
            if (selectedMovieFile != null)
            {
                LocalVideoFilePath = selectedMovieFile.Path;
            }
        }

        // Button 3 click: upload everything to the database and blob storage
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

            var dispatcherQueue = App.MainWindow.DispatcherQueue;

            dispatcherQueue.TryEnqueue(() => StatusMessage = "Validating video format...");

            try
            {
                bool isValid = await _videoStorageService.ValidateVideoAsync(LocalVideoFilePath);
                if (!isValid)
                {
                    dispatcherQueue.TryEnqueue(() => StatusMessage = "Invalid file! Must be a non-empty MP4 file\nno longer than 60 seconds.");
                    return;
                }

                dispatcherQueue.TryEnqueue(() => StatusMessage = "Uploading to Blob Storage & saving metadata...");

                var request = new ubb_se_2026_meio_ai.Features.ReelsUpload.Models.ReelUploadRequest
                {
                    LocalFilePath = LocalVideoFilePath,
                    Title = string.IsNullOrWhiteSpace(ReelTitle) ? "Untitled Reel" : ReelTitle,
                    Caption = ReelCaption ?? string.Empty,
                    UploaderUserId = currentUserID,
                    MovieId = LinkedMovie?.MovieId
                };

                var savedReel = await _videoStorageService.UploadVideoAsync(request);

                dispatcherQueue.TryEnqueue(() => 
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
                dispatcherQueue.TryEnqueue(() =>
                {
                    StatusMessage = errorMessage;
                });
            }
        }

        // AutoSuggestBox click: select the movie
        [RelayCommand]
        private void SelectMovie(MovieCardModel movieToSelect)
        {
            LinkedMovie = movieToSelect;
        }

        // AutoSuggestBox 1 type: search for a movie to link to the reel
        [RelayCommand]
        private async Task SearchMovieAsync(string partialMovieName)
        {
            if (string.IsNullOrWhiteSpace(partialMovieName))
            {
                // Safely clear UI on the main thread
                App.MainWindow.DispatcherQueue.TryEnqueue(() => 
                {
                    SuggestedMovies.Clear();
                });
                return;
            }

            try
            {
                await using var connection = await _connectionFactory.CreateConnectionAsync();
                
                string sqlInstruction = "SELECT TOP 10 MovieId, Title, PosterUrl, PrimaryGenre, ReleaseYear, Description FROM Movie WHERE Title LIKE @SearchTerm";
                await using var sqlCommand = new SqlCommand(sqlInstruction, connection);

                string searchParameter = "@SearchTerm";
                string searchedText = $"%{partialMovieName}%";
                sqlCommand.Parameters.AddWithValue(searchParameter, searchedText);

                await using var sqlCommandOutputReader = await sqlCommand.ExecuteReaderAsync();
                
                var newMovieResults = new System.Collections.Generic.List<MovieCardModel>();
                while (await sqlCommandOutputReader.ReadAsync())
                {
                    newMovieResults.Add(new MovieCardModel
                    {
                        MovieId = sqlCommandOutputReader.GetInt32(sqlCommandOutputReader.GetOrdinal("MovieId")),
                        Title = sqlCommandOutputReader.GetString(sqlCommandOutputReader.GetOrdinal("Title")),
                        PosterUrl = sqlCommandOutputReader.IsDBNull(sqlCommandOutputReader.GetOrdinal("PosterUrl")) ? "" : sqlCommandOutputReader.GetString(sqlCommandOutputReader.GetOrdinal("PosterUrl")),
                        PrimaryGenre = sqlCommandOutputReader.IsDBNull(sqlCommandOutputReader.GetOrdinal("PrimaryGenre")) ? "" : sqlCommandOutputReader.GetString(sqlCommandOutputReader.GetOrdinal("PrimaryGenre")),
                        ReleaseYear = sqlCommandOutputReader.IsDBNull(sqlCommandOutputReader.GetOrdinal("ReleaseYear")) ? 0 : sqlCommandOutputReader.GetInt32(sqlCommandOutputReader.GetOrdinal("ReleaseYear")),
                        Synopsis = sqlCommandOutputReader.IsDBNull(sqlCommandOutputReader.GetOrdinal("Description")) ? "" : sqlCommandOutputReader.GetString(sqlCommandOutputReader.GetOrdinal("Description"))
                    });
                }

                // 2. Only modify the UI collection once we have all the data safely!
                App.MainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    SuggestedMovies.Clear();
                    foreach (var movie in newMovieResults)
                    {
                        SuggestedMovies.Add(movie);
                    }
                });
            }
            catch (Exception ex)
            {
                // If a background database exception occurs (like Table Doesn't Exist), 
                // setting an [ObservableProperty] from the wrong thread causes an instant FailFast crash (0xffffffff).
                // We must marshal the error message update to the UI Thread!
                string errorMessage = $"DB Note: {ex.Message}";
                App.MainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    StatusMessage = errorMessage;
                });
            }
        }
    }
}
