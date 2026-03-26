using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using ubb_se_2026_meio_ai.Features.MovieTournament.ViewModels;

namespace ubb_se_2026_meio_ai.Features.MovieTournament.Views
{
    public sealed partial class MovieTournamentPage : Page
    {
        public MovieTournamentViewModel ViewModel { get; }

        public MovieTournamentPage()
        {
            ViewModel = App.Services.GetRequiredService<MovieTournamentViewModel>();
            this.InitializeComponent();
        }
    }
}
