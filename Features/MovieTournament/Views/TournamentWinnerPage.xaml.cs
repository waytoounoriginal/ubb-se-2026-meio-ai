using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using ubb_se_2026_meio_ai.Features.MovieTournament.ViewModels;

namespace ubb_se_2026_meio_ai.Features.MovieTournament.Views
{
    public sealed partial class TournamentWinnerPage : Page
    {
        public TournamentWinnerViewModel ViewModel { get; }

        public TournamentWinnerPage()
        {
            ViewModel = App.Services.GetRequiredService<TournamentWinnerViewModel>();
            this.InitializeComponent();

            ViewModel.NavigateToSetup += (_, _) =>
                Frame.Navigate(typeof(TournamentSetupPage));
        }
    }
}
