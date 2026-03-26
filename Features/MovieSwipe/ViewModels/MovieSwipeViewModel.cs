using CommunityToolkit.Mvvm.ComponentModel;

namespace ubb_se_2026_meio_ai.Features.MovieSwipe.ViewModels
{
    /// <summary>
    /// ViewModel for the Movie Swipe page.
    /// Owner: Bogdan
    /// </summary>
    public partial class MovieSwipeViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _pageTitle = "Movie Swipe";

        [ObservableProperty]
        private string _statusMessage = "Swipe right to like, left to skip.";
    }
}
