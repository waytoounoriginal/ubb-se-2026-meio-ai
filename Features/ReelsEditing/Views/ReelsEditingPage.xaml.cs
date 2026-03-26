using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using ubb_se_2026_meio_ai.Features.ReelsEditing.ViewModels;

namespace ubb_se_2026_meio_ai.Features.ReelsEditing.Views
{
    public sealed partial class ReelsEditingPage : Page
    {
        public ReelsEditingViewModel ViewModel { get; }

        public ReelsEditingPage()
        {
            ViewModel = App.Services.GetRequiredService<ReelsEditingViewModel>();
            this.InitializeComponent();
        }
    }
}
