using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ubb_se_2026_meio_ai.Features.MovieTournament.ViewModels;

namespace ubb_se_2026_meio_ai.Features.MovieTournament.Views
{
    public sealed partial class MovieTournamentPage : Page
    {
        public MovieTournamentViewModel ViewModel { get; }

        public MovieTournamentPage()
        {
            // This gets the ViewModel from the App's dependency injection container
            ViewModel = App.Services.GetRequiredService<MovieTournamentViewModel>();
            this.InitializeComponent();
        }

        /// <summary>
        /// This helper function is used by the XAML {x:Bind} to decide which 
        /// part of the UI to show based on the CurrentViewState.
        /// </summary>
        public Visibility IsState(int currentState, int targetState)
        {
            return currentState == targetState ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
