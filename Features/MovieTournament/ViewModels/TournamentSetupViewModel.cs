using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using ubb_se_2026_meio_ai.Features.MovieTournament.Services;
using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Features.MovieTournament.ViewModels
{
    public partial class TournamentSetupViewModel : ObservableObject
    {
        private readonly ITournamentLogicService _tournamentService;
        private readonly IMovieTournamentRepository _repository;
        private readonly int _currentUserId = 1;

        [ObservableProperty]
        private int _poolSize = 4;

        [ObservableProperty]
        private int _maxPoolSize = 4;

        [ObservableProperty]
        private string _setupErrorMessage = string.Empty;

        [ObservableProperty] private string? _bg1;
        [ObservableProperty] private string? _bg2;
        [ObservableProperty] private string? _bg3;
        [ObservableProperty] private string? _bg4;

        /// <summary>Raised when the tournament starts — nav to Match page.</summary>
        public event EventHandler? TournamentStarted;

        public TournamentSetupViewModel(
            ITournamentLogicService tournamentService,
            IMovieTournamentRepository repository)
        {
            _tournamentService = tournamentService;
            _repository = repository;
            LoadDataAsync();
        }

        private async void LoadDataAsync()
        {
            try
            {
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
                    // Fallback posters
                    Bg1 = "https://image.tmdb.org/t/p/w500/3bhkrj58Vtu7enYsRolD1fZdja1.jpg";
                    Bg2 = "https://media.themoviedb.org/t/p/w600_and_h900_face/qJ2tW6WMUDux911r6m7haRef0WH.jpg";
                    Bg3 = "https://media.themoviedb.org/t/p/w600_and_h900_face/q2qXg4OmJgm0qGaBYLdXzP8nHPy.jpg";
                    Bg4 = "https://media.themoviedb.org/t/p/w600_and_h900_face/nrmXQ0zcZUL8jFLrakWc90IR8z9.jpg";
                }
            }
            catch (Exception ex)
            {
                SetupErrorMessage = "Error loading: " + ex.Message;
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
                TournamentStarted?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                SetupErrorMessage = "Failed to start tournament: " + ex.Message;
            }
        }

        public ImageSource? GetImageSource(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            try { return new BitmapImage(new Uri(url)); }
            catch { return null; }
        }
    }
}
