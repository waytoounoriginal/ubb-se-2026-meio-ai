using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using ubb_se_2026_meio_ai.Features.TrailerScraping.ViewModels;

namespace ubb_se_2026_meio_ai.Features.TrailerScraping.Views
{
    public sealed partial class TrailerScrapingPage : Page
    {
        public TrailerScrapingViewModel ViewModel { get; }

        public TrailerScrapingPage()
        {
            ViewModel = App.Services.GetRequiredService<TrailerScrapingViewModel>();
            this.InitializeComponent();
        }
    }
}
