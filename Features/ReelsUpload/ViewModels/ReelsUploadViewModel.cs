using CommunityToolkit.Mvvm.ComponentModel;

namespace ubb_se_2026_meio_ai.Features.ReelsUpload.ViewModels
{
    /// <summary>
    /// ViewModel for the Reels Upload page.
    /// Owner: Alex
    /// </summary>
    public partial class ReelsUploadViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _pageTitle = "Reels Upload";

        [ObservableProperty]
        private string _statusMessage = "Ready to upload.";
    }
}
