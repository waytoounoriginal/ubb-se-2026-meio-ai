using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using ubb_se_2026_meio_ai.Features.ReelsFeed.ViewModels;

namespace ubb_se_2026_meio_ai.Features.ReelsFeed.Views
{
    public sealed partial class ReelsFeedPage : Page
    {
        public ReelsFeedViewModel ViewModel { get; }

        public ReelsFeedPage()
        {
            ViewModel = App.Services.GetRequiredService<ReelsFeedViewModel>();
            this.InitializeComponent();
        }
    }
}
