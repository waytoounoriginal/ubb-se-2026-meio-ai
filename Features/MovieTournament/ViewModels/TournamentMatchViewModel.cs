using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using ubb_se_2026_meio_ai.Features.MovieTournament.Services;
using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Features.MovieTournament.ViewModels
{
    public partial class TournamentMatchViewModel : ObservableObject
    {
        private readonly ITournamentLogicService _tournamentService;
        private readonly int _currentUserId = 1;

        [ObservableProperty]
        private MovieCardModel? _movieOptionA;

        [ObservableProperty]
        private MovieCardModel? _movieOptionB;

        [ObservableProperty]
        private int _roundNumber;

        [ObservableProperty]
        private string _roundDisplay = string.Empty;

        /// <summary>Raised when the user picks a winner and tournament is complete — nav to Winner page.</summary>
        public event EventHandler? TournamentComplete;

        /// <summary>Raised when the user clicks Back — nav back to Setup page.</summary>
        public event EventHandler? NavigateBack;

        public TournamentMatchViewModel(ITournamentLogicService tournamentService)
        {
            _tournamentService = tournamentService;
            RefreshCurrentMatch();
        }

        public void RefreshCurrentMatch()
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

        [RelayCommand]
        private async Task SelectMovieAsync(int movieId)
        {
            await _tournamentService.AdvanceWinnerAsync(_currentUserId, movieId);

            if (_tournamentService.IsTournamentComplete())
            {
                TournamentComplete?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                RefreshCurrentMatch();
            }
        }

        [RelayCommand]
        private void GoBack()
        {
            _tournamentService.ResetTournament();
            NavigateBack?.Invoke(this, EventArgs.Empty);
        }

        public ImageSource? GetImageSource(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            try { return new BitmapImage(new Uri(url)); }
            catch { return null; }
        }
    }
}
