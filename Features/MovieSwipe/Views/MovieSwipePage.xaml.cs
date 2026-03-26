using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using ubb_se_2026_meio_ai.Features.MovieSwipe.ViewModels;

namespace ubb_se_2026_meio_ai.Features.MovieSwipe.Views
{
    public sealed partial class MovieSwipePage : Page
    {
        public MovieSwipeViewModel ViewModel { get; }

        public MovieSwipePage()
        {
            ViewModel = App.Services.GetRequiredService<MovieSwipeViewModel>();
            this.InitializeComponent();
        }
    }
}
