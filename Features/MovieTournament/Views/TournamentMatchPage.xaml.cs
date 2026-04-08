using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using ubb_se_2026_meio_ai.Features.MovieTournament.ViewModels;

namespace ubb_se_2026_meio_ai.Features.MovieTournament.Views
{
    /// <summary>
    /// Code-behind for the tournament match page.
    /// Handles pointer-based hover animations on the movie buttons
    /// and wires up view model navigation events.
    /// </summary>
    public sealed partial class TournamentMatchPage : Page
    {
        private const double HoverScale = 1.05;
        private const double NormalScale = 1.0;
        private const double ScaleAnimationDurationMilliseconds = 150;

        /// <summary>
        /// Initializes a new instance of the <see cref="TournamentMatchPage"/> class,
        /// resolves the view model, and subscribes to navigation events.
        /// </summary>
        public TournamentMatchPage()
        {
            this.ViewModel = App.Services.GetRequiredService<TournamentMatchViewModel>();
            this.InitializeComponent();

            this.ViewModel.TournamentComplete += (_, _) =>
                this.Frame.Navigate(typeof(TournamentWinnerPage));

            this.ViewModel.NavigateBack += (_, _) =>
                this.Frame.Navigate(typeof(TournamentSetupPage));
        }

        /// <summary>
        /// Gets the view model that drives the match display and winner selection.
        /// </summary>
        public TournamentMatchViewModel ViewModel { get; }

        private void OnMoviePointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Button button && button.RenderTransform is ScaleTransform scaleTransform)
            {
                this.AnimateScale(scaleTransform, HoverScale);
            }
        }

        private void OnMoviePointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Button button && button.RenderTransform is ScaleTransform scaleTransform)
            {
                this.AnimateScale(scaleTransform, NormalScale);
            }
        }

        private void AnimateScale(ScaleTransform scaleTransform, double targetScale)
        {
            var animationDuration = TimeSpan.FromMilliseconds(ScaleAnimationDurationMilliseconds);

            var scaleXAnimation = new DoubleAnimation { To = targetScale, Duration = animationDuration };
            Storyboard.SetTarget(scaleXAnimation, scaleTransform);
            Storyboard.SetTargetProperty(scaleXAnimation, "ScaleX");

            var scaleYAnimation = new DoubleAnimation { To = targetScale, Duration = animationDuration };
            Storyboard.SetTarget(scaleYAnimation, scaleTransform);
            Storyboard.SetTargetProperty(scaleYAnimation, "ScaleY");

            var storyboard = new Storyboard();
            storyboard.Children.Add(scaleXAnimation);
            storyboard.Children.Add(scaleYAnimation);
            storyboard.Begin();
        }
    }
}