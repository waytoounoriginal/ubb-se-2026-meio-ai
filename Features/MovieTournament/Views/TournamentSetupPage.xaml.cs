using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using ubb_se_2026_meio_ai.Features.MovieTournament.ViewModels;

namespace ubb_se_2026_meio_ai.Features.MovieTournament.Views
{
    public sealed partial class TournamentSetupPage : Page
    {
        public TournamentSetupViewModel ViewModel { get; }

        public TournamentSetupPage()
        {
            ViewModel = App.Services.GetRequiredService<TournamentSetupViewModel>();
            this.InitializeComponent();

            ViewModel.TournamentStarted += (_, _) =>
                Frame.Navigate(typeof(TournamentMatchPage));
        }
    }
}
