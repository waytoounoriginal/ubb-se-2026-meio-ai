using Microsoft.UI.Xaml.Controls;
using ubb_se_2026_meio_ai.Features.MovieSwipe.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ubb_se_2026_meio_ai.Features.MovieSwipe.Views
{
    public sealed partial class SwipeResultSummaryView : Page
    {
        public MovieSwipeViewModel ViewModel { get; }

        public SwipeResultSummaryView()
        {
            this.InitializeComponent();
            ViewModel = App.Services.GetRequiredService<MovieSwipeViewModel>();
            this.DataContext = ViewModel;
        }
    }
}
