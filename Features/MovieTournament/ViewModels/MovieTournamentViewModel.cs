using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using ubb_se_2026_meio_ai.Features.MovieTournament.Services;
using ubb_se_2026_meio_ai.Core.Models;
using static System.Net.WebRequestMethods;

namespace ubb_se_2026_meio_ai.Features.MovieTournament.ViewModels
{

    public partial class MovieTournamentViewModel : ObservableObject
    {
        private readonly ITournamentLogicService _tournamentService;
        private readonly IMovieTournamentRepository _repository;
        private readonly int _currentUserId = 1; 

        [ObservableProperty]
        private string _pageTitle = "Movie Tournament";

        // View States: 0 = Setup, 1 = Match, 2 = Winner
        [ObservableProperty]
        private int _currentViewState = 0;

        
        [ObservableProperty]
        private int _poolSize = 0;

        [ObservableProperty]
        private int _maxPoolSize = 4;

        [ObservableProperty]
        private string _setupErrorMessage = string.Empty;

        [ObservableProperty] private string? _bg1;
        [ObservableProperty] private string? _bg2;
        [ObservableProperty] private string? _bg3;
        [ObservableProperty] private string? _bg4;

        // Second View state ( movieA vs movieB )
        [ObservableProperty]
        private MovieCardModel? _movieOptionA;

        [ObservableProperty]
        private MovieCardModel? _movieOptionB;

        [ObservableProperty]
        private int _roundNumber;

        [ObservableProperty]
        private string _roundDisplay = string.Empty;

        // Winner view state
        [ObservableProperty]
        private MovieCardModel? _winnerMovie;

        public MovieTournamentViewModel(
            ITournamentLogicService tournamentService,
            IMovieTournamentRepository repository)
        {
            _tournamentService = tournamentService;
            _repository = repository;

            if (_tournamentService.IsTournamentActive)
            {
                UpdateCurrentMatchDisplay();
                CurrentViewState = 1; 
            }
            else if (_tournamentService.IsTournamentComplete())
            {
                WinnerMovie = _tournamentService.GetFinalWinner();
                CurrentViewState = 2; 
            }
            else
            {
                
                LoadMaxPoolSizeAsync();
            }
        }

        private async void LoadMaxPoolSizeAsync()
        {
            try
            {
                await Task.Yield(); // Ensure we don't throw synchronously from the constructor
                MaxPoolSize = await _repository.GetTournamentPoolSizeAsync(_currentUserId);
                
                
                var bgMovies = await _repository.GetTournamentPoolAsync(_currentUserId, 4);
                if (bgMovies.Count >= 4)
                {
                    Bg1 = bgMovies[0].PosterUrl;
                    Bg2 = bgMovies[1].PosterUrl;
                    Bg3 = bgMovies[2].PosterUrl;
                    Bg4 = bgMovies[3].PosterUrl;
                }
                else
                {
                    Bg1 = new String("https://image.tmdb.org/t/p/w500/3bhkrj58Vtu7enYsRolD1fZdja1.jpg");
                    Bg2 = new String("https://media.themoviedb.org/t/p/w600_and_h900_face/qJ2tW6WMUDux911r6m7haRef0WH.jpg");
                    Bg3 = new String("https://media.themoviedb.org/t/p/w600_and_h900_face/q2qXg4OmJgm0qGaBYLdXzP8nHPy.jpg");
                    Bg4 = new String("https://media.themoviedb.org/t/p/w600_and_h900_face/nrmXQ0zcZUL8jFLrakWc90IR8z9.jpg");


                }    
            }
            catch (Exception ex)
            {
                SetupErrorMessage = "Error loading pool size: " + ex.Message;
            }
        }

        [RelayCommand]
        private async Task StartTournamentAsync()
        {
            if (PoolSize < 4)
            {
                SetupErrorMessage = "Pool size must be at least 4.\nIf you don't have enough, go like some movies!";
                return;
            }
            
            if (PoolSize > MaxPoolSize)
            {
                SetupErrorMessage = $"Pool size cannot exceed {MaxPoolSize}.";
                return;
            }

            SetupErrorMessage = string.Empty;

            try
            {
                await _tournamentService.StartTournamentAsync(_currentUserId, PoolSize);
                UpdateCurrentMatchDisplay();
                CurrentViewState = 1; 
            }
            catch (Exception ex)
            {
                SetupErrorMessage = "Failed to start tournament: " + ex.Message;
            }
        }

        [RelayCommand]
        private async Task SelectMovieAsync(int movieId)
        {
            await _tournamentService.AdvanceWinnerAsync(_currentUserId, movieId);

            if (_tournamentService.IsTournamentComplete())
            {
                WinnerMovie = _tournamentService.GetFinalWinner();
                CurrentViewState = 2; 
            }
            else
            {
                UpdateCurrentMatchDisplay();
            }
        }

        [RelayCommand]
        private void ResetTournament()
        {
            _tournamentService.ResetTournament();
            CurrentViewState = 0; 
            LoadMaxPoolSizeAsync(); 
        }

        private void UpdateCurrentMatchDisplay()
        {
            var match = _tournamentService.GetCurrentMatch();
            if (match != null)
            {
                MovieOptionA = match.MovieA;
                MovieOptionB = match.MovieB;
                RoundNumber = _tournamentService.CurrentState.CurrentRound;
                RoundDisplay = $"Round {RoundNumber}";
            }
        }

        public ImageSource? GetImageSource(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            try
            {
                return new BitmapImage(new Uri(url));
            }
            catch
            {
                return null;
            }
        }
    }
}
