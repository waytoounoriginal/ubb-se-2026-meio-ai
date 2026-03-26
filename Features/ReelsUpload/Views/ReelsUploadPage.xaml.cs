using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using ubb_se_2026_meio_ai.Features.ReelsUpload.ViewModels;

namespace ubb_se_2026_meio_ai.Features.ReelsUpload.Views
{
    /// <summary>
    /// Placeholder page for the Reels Upload feature (Alex).
    /// </summary>
    public sealed partial class ReelsUploadPage : Page
    {
        public ReelsUploadViewModel ViewModel { get; }

        public ReelsUploadPage()
        {
            ViewModel = App.Services.GetRequiredService<ReelsUploadViewModel>();
            this.InitializeComponent();
        }
    }
}
