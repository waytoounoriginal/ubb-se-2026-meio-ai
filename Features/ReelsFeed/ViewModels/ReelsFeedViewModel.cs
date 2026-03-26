using CommunityToolkit.Mvvm.ComponentModel;

namespace ubb_se_2026_meio_ai.Features.ReelsFeed.ViewModels
{
    /// <summary>
    /// ViewModel for the Reels Feed page.
    /// Owner: Tudor
    /// </summary>
    public partial class ReelsFeedViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _pageTitle = "Reels Feed";

        [ObservableProperty]
        private string _statusMessage = "Scroll to discover reels.";
    }
}
