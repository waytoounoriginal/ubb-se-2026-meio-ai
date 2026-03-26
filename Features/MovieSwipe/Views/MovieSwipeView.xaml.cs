using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using ubb_se_2026_meio_ai.Features.MovieSwipe.ViewModels;
using Windows.Foundation;

namespace ubb_se_2026_meio_ai.Features.MovieSwipe.Views
{
    /// <summary>
    /// Code-behind for the Movie Swipe page.
    /// Handles drag gestures, overlay opacity, and card fly-off animations.
    /// Owner: Bogdan
    /// </summary>
    public sealed partial class MovieSwipeView : Page
    {
        /// <summary>Fraction of card width the drag must exceed to confirm a swipe.</summary>
        private const double SwipeThresholdFraction = 0.30;

        /// <summary>Distance in pixels the card flies off-screen.</summary>
        private const double FlyOffDistance = 600;

        /// <summary>Duration of the fly-off animation in milliseconds.</summary>
        private const int FlyOffDurationMs = 250;

        private bool _isDragging;
        private Point _dragStartPoint;
        private uint _activePointerId;

        public MovieSwipeViewModel ViewModel { get; }

        public MovieSwipeView()
        {
            ViewModel = App.Services.GetRequiredService<MovieSwipeViewModel>();
            this.InitializeComponent();

            // Bind card content whenever CurrentCard changes
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            UpdateCardContent();
            UpdateCardVisibility();
        }

        // ─── Data Binding Helpers ────────────────────────────────────────

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.CurrentCard))
            {
                UpdateCardContent();
                UpdateCardVisibility();
            }
            else if (e.PropertyName == nameof(ViewModel.IsAllCaughtUp) || e.PropertyName == nameof(ViewModel.IsLoading))
            {
                UpdateCardVisibility();
            }
        }

        private void UpdateCardContent()
        {
            var card = ViewModel.CurrentCard;
            if (card != null)
            {
                TitleText.Text = card.Title;
                GenreText.Text = card.PrimaryGenre;

                if (!string.IsNullOrEmpty(card.PosterUrl))
                {
                    try
                    {
                        PosterImage.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(card.PosterUrl));
                    }
                    catch
                    {
                        PosterImage.Source = null;
                    }
                }
                else
                {
                    PosterImage.Source = null;
                }
            }
        }

        private void UpdateCardVisibility()
        {
            bool hasCard = ViewModel.CurrentCard != null && !ViewModel.IsLoading && !ViewModel.IsAllCaughtUp;
            MovieCard.Visibility = hasCard ? Visibility.Visible : Visibility.Collapsed;
        }

        // ─── Drag / Swipe Gesture Handling ───────────────────────────────

        private void MovieCard_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (_isDragging)
            {
                return;
            }

            _isDragging = true;
            _activePointerId = e.Pointer.PointerId;
            _dragStartPoint = e.GetCurrentPoint(CardContainer).Position;

            // Capture the pointer so we get move/release even if it leaves the card
            MovieCard.CapturePointer(e.Pointer);
            e.Handled = true;
        }

        private void MovieCard_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_isDragging || e.Pointer.PointerId != _activePointerId)
            {
                return;
            }

            Point currentPoint = e.GetCurrentPoint(CardContainer).Position;
            double deltaX = currentPoint.X - _dragStartPoint.X;

            // Move the card horizontally
            CardTransform.TranslateX = deltaX;

            // Slight rotation for a natural feel (max ±15°)
            double cardWidth = MovieCard.ActualWidth > 0 ? MovieCard.ActualWidth : 340;
            double rotationDeg = (deltaX / cardWidth) * 15.0;
            CardTransform.Rotation = rotationDeg;

            // Update overlay opacities proportional to the drag distance
            double progress = Math.Abs(deltaX) / (cardWidth * SwipeThresholdFraction);
            progress = Math.Min(progress, 1.0);

            if (deltaX > 0)
            {
                // Dragging right → show "LIKE"
                LikeOverlay.Opacity = progress;
                NopeOverlay.Opacity = 0;
            }
            else if (deltaX < 0)
            {
                // Dragging left → show "NOPE"
                NopeOverlay.Opacity = progress;
                LikeOverlay.Opacity = 0;
            }
            else
            {
                LikeOverlay.Opacity = 0;
                NopeOverlay.Opacity = 0;
            }

            e.Handled = true;
        }

        private void MovieCard_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (!_isDragging || e.Pointer.PointerId != _activePointerId)
            {
                return;
            }

            // Finalize first; releasing capture can raise PointerCaptureLost.
            // If we released first, capture-lost could cancel a valid swipe.
            FinalizeSwipe();
            MovieCard.ReleasePointerCapture(e.Pointer);
            e.Handled = true;
        }

        private void MovieCard_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            // If user lifts finger outside the card or system steals capture
            if (_isDragging && e.Pointer.PointerId == _activePointerId)
            {
                // Treat as cancelled — snap back (Requirement 6: mid-swipe discard)
                ResetCardPosition();
                _isDragging = false;
            }
        }

        /// <summary>
        /// Determines whether the drag exceeded the threshold and triggers the swipe command
        /// or snaps the card back to center.
        /// </summary>
        private void FinalizeSwipe()
        {
            double deltaX = CardTransform.TranslateX;
            double cardWidth = MovieCard.ActualWidth > 0 ? MovieCard.ActualWidth : 340;
            double threshold = cardWidth * SwipeThresholdFraction;

            if (Math.Abs(deltaX) >= threshold)
            {
                bool isLiked = deltaX > 0;
                AnimateCardOffScreen(isLiked);
            }
            else
            {
                // Didn't cross threshold — snap back (no swipe recorded)
                ResetCardPosition();
            }

            _isDragging = false;
        }

        /// <summary>
        /// Animates the card flying off to the left or right, then triggers the swipe command.
        /// </summary>
        private void AnimateCardOffScreen(bool isLiked)
        {
            double targetX = isLiked ? FlyOffDistance : -FlyOffDistance;

            var storyboard = new Storyboard();

            // Translate animation
            var translateAnim = new DoubleAnimation
            {
                To = targetX,
                Duration = new Duration(TimeSpan.FromMilliseconds(FlyOffDurationMs)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn },
            };
            Storyboard.SetTarget(translateAnim, CardTransform);
            Storyboard.SetTargetProperty(translateAnim, "TranslateX");
            storyboard.Children.Add(translateAnim);

            // Fade out
            var opacityAnim = new DoubleAnimation
            {
                To = 0,
                Duration = new Duration(TimeSpan.FromMilliseconds(FlyOffDurationMs)),
            };
            Storyboard.SetTarget(opacityAnim, MovieCard);
            Storyboard.SetTargetProperty(opacityAnim, "Opacity");
            storyboard.Children.Add(opacityAnim);

            storyboard.Completed += (s, e) =>
            {
                // Reset card transform for the next card
                ResetCardPosition();
                MovieCard.Opacity = 1;

                // Fire the swipe command
                if (isLiked)
                {
                    ViewModel.SwipeRightCommand.Execute(null);
                }
                else
                {
                    ViewModel.SwipeLeftCommand.Execute(null);
                }
            };

            storyboard.Begin();
        }

        /// <summary>
        /// Snaps the card back to its original position with a short animation.
        /// </summary>
        private void ResetCardPosition()
        {
            CardTransform.TranslateX = 0;
            CardTransform.TranslateY = 0;
            CardTransform.Rotation = 0;
            LikeOverlay.Opacity = 0;
            NopeOverlay.Opacity = 0;
        }
    }
}
