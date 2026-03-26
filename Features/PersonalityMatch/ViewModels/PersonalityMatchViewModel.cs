using CommunityToolkit.Mvvm.ComponentModel;

namespace ubb_se_2026_meio_ai.Features.PersonalityMatch.ViewModels
{
    /// <summary>
    /// ViewModel for the Personality Match page.
    /// Owner: Madi
    /// </summary>
    public partial class PersonalityMatchViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _pageTitle = "Personality Match";

        [ObservableProperty]
        private string _statusMessage = "Discover users with similar taste.";
    }
}
