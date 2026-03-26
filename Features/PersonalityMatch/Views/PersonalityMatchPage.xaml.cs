using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using ubb_se_2026_meio_ai.Features.PersonalityMatch.ViewModels;

namespace ubb_se_2026_meio_ai.Features.PersonalityMatch.Views
{
    public sealed partial class PersonalityMatchPage : Page
    {
        public PersonalityMatchViewModel ViewModel { get; }

        public PersonalityMatchPage()
        {
            ViewModel = App.Services.GetRequiredService<PersonalityMatchViewModel>();
            this.InitializeComponent();
        }
    }
}
