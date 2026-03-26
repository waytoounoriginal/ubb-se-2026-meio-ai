using CommunityToolkit.Mvvm.ComponentModel;

namespace ubb_se_2026_meio_ai.Features.TrailerScraping.ViewModels
{
    /// <summary>
    /// ViewModel for the Trailer Scraping page.
    /// Owner: Andrei
    /// </summary>
    public partial class TrailerScrapingViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _pageTitle = "Trailer Scraping";

        [ObservableProperty]
        private string _statusMessage = "Background scraper idle.";
    }
}
