using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ubb_se_2026_meio_ai.Features.MovieTournament.Services;

namespace ubb_se_2026_meio_ai.Features.MovieTournament.Views
{
    /// <summary>
    /// Host page for the movie tournament feature.
    /// On load, navigates the inner frame to the appropriate sub-page
    /// based on the current tournament state.
    /// </summary>
    public sealed partial class MovieTournamentPage : Page
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MovieTournamentPage"/> class
        /// and registers the loaded event handler.
        /// </summary>
        public MovieTournamentPage()
        {
            this.InitializeComponent();
            this.Loaded += this.OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var tournamentLogicService = App.Services.GetRequiredService<ITournamentLogicService>();

            if (tournamentLogicService.IsTournamentActive)
            {
                this.TournamentFrame.Navigate(typeof(TournamentMatchPage));
            }
            else if (tournamentLogicService.IsTournamentComplete())
            {
                this.TournamentFrame.Navigate(typeof(TournamentWinnerPage));
            }
            else
            {
                this.TournamentFrame.Navigate(typeof(TournamentSetupPage));
            }
        }
    }
}