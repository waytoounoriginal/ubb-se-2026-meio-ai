using CommunityToolkit.Mvvm.ComponentModel;

namespace ubb_se_2026_meio_ai.Features.ReelsEditing.ViewModels
{
    /// <summary>
    /// ViewModel for the Reels Editing page.
    /// Owner: Beatrice
    /// </summary>
    public partial class ReelsEditingViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _pageTitle = "Reels Editing";

        [ObservableProperty]
        private string _statusMessage = "Select a reel to edit.";
    }
}
