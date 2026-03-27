using Microsoft.UI.Xaml.Controls;
using ubb_se_2026_meio_ai.Features.MovieTournament.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ubb_se_2026_meio_ai.Features.MovieTournament.Views
{
    public sealed partial class MovieTournamentPage : Page
    {
        public MovieTournamentPage()
        {
            this.InitializeComponent();
            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var tournamentService = App.Services.GetRequiredService<ITournamentLogicService>();

            // Restore the correct sub-page based on the current tournament state
            if (tournamentService.IsTournamentActive)
                TournamentFrame.Navigate(typeof(TournamentMatchPage));
            else if (tournamentService.IsTournamentComplete())
                TournamentFrame.Navigate(typeof(TournamentWinnerPage));
            else
                TournamentFrame.Navigate(typeof(TournamentSetupPage));
        }
    }
}
