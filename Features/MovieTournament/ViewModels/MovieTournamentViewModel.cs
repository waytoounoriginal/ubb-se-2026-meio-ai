using CommunityToolkit.Mvvm.ComponentModel;

namespace ubb_se_2026_meio_ai.Features.MovieTournament.ViewModels
{
    /// <summary>
    /// ViewModel for the Movie Tournament page.
    /// Owner: Gabi
    /// </summary>
    public partial class MovieTournamentViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _pageTitle = "Movie Tournament";

        [ObservableProperty]
        private string _statusMessage = "Generate a bracket to begin.";
    }
}
