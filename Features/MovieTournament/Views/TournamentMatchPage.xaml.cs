using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using ubb_se_2026_meio_ai.Features.MovieTournament.ViewModels;

namespace ubb_se_2026_meio_ai.Features.MovieTournament.Views
{
    public sealed partial class TournamentMatchPage : Page
    {
        public TournamentMatchViewModel ViewModel { get; }

        public TournamentMatchPage()
        {
            ViewModel = App.Services.GetRequiredService<TournamentMatchViewModel>();
            this.InitializeComponent();

            ViewModel.TournamentComplete += (_, _) =>
                Frame.Navigate(typeof(TournamentWinnerPage));

            ViewModel.NavigateBack += (_, _) =>
                Frame.Navigate(typeof(TournamentSetupPage));
        }

        private void MoviePointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Button btn && btn.RenderTransform is ScaleTransform st)
                AnimateScale(st, 1.05);
        }

        private void MoviePointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Button btn && btn.RenderTransform is ScaleTransform st)
                AnimateScale(st, 1.0);
        }

        private void AnimateScale(ScaleTransform st, double targetScale)
        {
            var storyboard = new Storyboard();

            var animX = new DoubleAnimation { To = targetScale, Duration = TimeSpan.FromMilliseconds(150) };
            Storyboard.SetTarget(animX, st);
            Storyboard.SetTargetProperty(animX, "ScaleX");

            var animY = new DoubleAnimation { To = targetScale, Duration = TimeSpan.FromMilliseconds(150) };
            Storyboard.SetTarget(animY, st);
            Storyboard.SetTargetProperty(animY, "ScaleY");

            storyboard.Children.Add(animX);
            storyboard.Children.Add(animY);
            storyboard.Begin();
        }
    }
}
