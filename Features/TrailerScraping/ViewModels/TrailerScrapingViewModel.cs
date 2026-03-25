using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.TrailerScraping.Services;

namespace ubb_se_2026_meio_ai.Features.TrailerScraping.ViewModels
{
    /// <summary>
    /// ViewModel for the Trailer Scraping admin dashboard.
    /// Handles movie autocomplete, scrape execution, and log display.
    /// Owner: Andrei
    /// </summary>
    public partial class TrailerScrapingViewModel : ObservableObject
    {
        private readonly VideoIngestionService _ingestionService;
        private readonly IScrapeJobRepository _repository;

        public TrailerScrapingViewModel(
            VideoIngestionService ingestionService,
            IScrapeJobRepository repository)
        {
            _ingestionService = ingestionService;
            _repository = repository;
        }

        // ── Stats bar ────────────────────────────────────────────────────

        [ObservableProperty]
        private int _totalMovies;

        [ObservableProperty]
        private int _totalReels;

        [ObservableProperty]
        private int _totalJobs;

        [ObservableProperty]
        private int _runningJobs;

        [ObservableProperty]
        private int _completedJobs;

        [ObservableProperty]
        private int _failedJobs;

        // ── Movie Autocomplete ──────────────────────────────────────────

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private MovieCardModel? _selectedMovie;

        [ObservableProperty]
        private bool _noMovieFound;

        [ObservableProperty]
        private int _maxResults = 5;

        [ObservableProperty]
        private bool _isScraping;

        [ObservableProperty]
        private string _statusText = "Idle";

        public ObservableCollection<MovieCardModel> SuggestedMovies { get; } = new();

        public List<int> MaxResultsOptions { get; } = new() { 5, 10, 15, 25, 50 };

        // ── Logs ────────────────────────────────────────────────────────

        public ObservableCollection<ScrapeJobLogModel> LogEntries { get; } = new();

        // ── Commands ────────────────────────────────────────────────────

        /// <summary>
        /// Called when the user types in the AutoSuggestBox.
        /// Queries the Movie table for case-insensitive matches.
        /// </summary>
        [RelayCommand]
        private async Task SearchMoviesAsync(string query)
        {
            SearchText = query;
            SelectedMovie = null;
            NoMovieFound = false;

            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                SuggestedMovies.Clear();
                return;
            }

            try
            {
                IList<MovieCardModel> matches = await _repository.SearchMoviesByNameAsync(query);
                SuggestedMovies.Clear();
                foreach (var m in matches)
                {
                    SuggestedMovies.Add(m);
                }

                NoMovieFound = SuggestedMovies.Count == 0;
            }
            catch
            {
                SuggestedMovies.Clear();
            }
        }

        /// <summary>
        /// Called when the user picks a movie from the dropdown.
        /// </summary>
        public void SelectMovie(MovieCardModel movie)
        {
            SelectedMovie = movie;
            SearchText = movie.Title;
            NoMovieFound = false;
            StartScrapeCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand(CanExecute = nameof(CanStartScrape))]
        private async Task StartScrapeAsync()
        {
            if (SelectedMovie is null)
            {
                return;
            }

            IsScraping = true;
            StatusText = "Scraping...";
            StartScrapeCommand.NotifyCanExecuteChanged();

            try
            {
                await _ingestionService.RunScrapeJobAsync(
                    SelectedMovie,
                    MaxResults,
                    onLogEntry: async logEntry =>
                    {
                        // Dispatch to UI thread
                        Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread()?.TryEnqueue(() =>
                        {
                            LogEntries.Insert(0, logEntry);
                        });
                        await Task.CompletedTask;
                    });
            }
            catch
            {
                // Errors are already logged inside RunScrapeJobAsync
            }
            finally
            {
                IsScraping = false;
                StatusText = "Idle";
                StartScrapeCommand.NotifyCanExecuteChanged();
                await RefreshAsync();
            }
        }

        // ── Table viewers ────────────────────────────────────────────

        public ObservableCollection<MovieCardModel> MovieTableItems { get; } = new();
        public ObservableCollection<ReelModel> ReelTableItems { get; } = new();

        private bool CanStartScrape() => !IsScraping && SelectedMovie is not null;

        [RelayCommand]
        private async Task RefreshAsync()
        {
            try
            {
                DashboardStatsModel stats = await _repository.GetDashboardStatsAsync();
                TotalMovies     = stats.TotalMovies;
                TotalReels      = stats.TotalReels;
                TotalJobs       = stats.TotalJobs;
                RunningJobs     = stats.RunningJobs;
                CompletedJobs   = stats.CompletedJobs;
                FailedJobs      = stats.FailedJobs;

                IList<ScrapeJobLogModel> logs = await _repository.GetAllLogsAsync();
                LogEntries.Clear();
                foreach (var log in logs)
                {
                    LogEntries.Add(log);
                }

                // Table viewers
                IList<MovieCardModel> movies = await _repository.GetAllMoviesAsync();
                MovieTableItems.Clear();
                foreach (var m in movies)
                {
                    MovieTableItems.Add(m);
                }

                IList<ReelModel> reels = await _repository.GetAllReelsAsync();
                ReelTableItems.Clear();
                foreach (var r in reels)
                {
                    ReelTableItems.Add(r);
                }
            }
            catch
            {
                // Database may not be available during development
            }
        }

        /// <summary>
        /// Called by the Page when it loads to populate initial data.
        /// </summary>
        public async Task InitializeAsync()
        {
            await RefreshAsync();
        }
    }
}
