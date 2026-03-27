using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using ubb_se_2026_meio_ai.Features.MovieTournament.Services;
using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Features.MovieTournament.ViewModels
{
    public partial class TournamentWinnerViewModel : ObservableObject
    {
        private readonly ITournamentLogicService _tournamentService;

        [ObservableProperty]
        private MovieCardModel? _winnerMovie;

        /// <summary>Raised when user wants to start a new tournament — nav back to Setup.</summary>
        public event EventHandler? NavigateToSetup;

        public TournamentWinnerViewModel(ITournamentLogicService tournamentService)
        {
            _tournamentService = tournamentService;

            if (_tournamentService.IsTournamentComplete())
                WinnerMovie = _tournamentService.GetFinalWinner();
        }

        [RelayCommand]
        private void StartAnotherTournament()
        {
            _tournamentService.ResetTournament();
            NavigateToSetup?.Invoke(this, EventArgs.Empty);
        }

        public ImageSource? GetImageSource(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            try { return new BitmapImage(new Uri(url)); }
            catch { return null; }
        }
    }
}
