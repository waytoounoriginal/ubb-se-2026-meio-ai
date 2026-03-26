using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.MovieSwipe.Services;

namespace ubb_se_2026_meio_ai.Features.MovieSwipe.ViewModels
{
    /// <summary>
    /// ViewModel for the Movie Swipe (Tinder-style) page.
    /// Manages the card queue, swipe commands, and auto-refill logic.
    /// Owner: Bogdan
    /// </summary>
    public partial class MovieSwipeViewModel : ObservableObject
    {
        private const int BufferSize = 5;
        private const int RefillThreshold = 2;
        private const int DefaultUserId = 1; // Hardcoded until auth is implemented

        private readonly ISwipeService _swipeService;
        private bool _isRefilling;

        public MovieSwipeViewModel(ISwipeService swipeService)
        {
            _swipeService = swipeService;
            CardQueue = new ObservableCollection<MovieCardModel>();

            // Fire-and-forget initial load (exceptions caught inside)
            _ = LoadInitialCardsAsync();
        }

        /// <summary>The top card currently displayed to the user.</summary>
        [ObservableProperty]
        private MovieCardModel? _currentCard;

        /// <summary>Buffered upcoming cards.</summary>
        public ObservableCollection<MovieCardModel> CardQueue { get; }

        /// <summary>True while fetching cards from the service.</summary>
        [ObservableProperty]
        private bool _isLoading;

        /// <summary>True when no unswiped movies remain.</summary>
        [ObservableProperty]
        private bool _isAllCaughtUp;

        /// <summary>Status/error message shown to the user.</summary>
        [ObservableProperty]
        private string _statusMessage = "Swipe right to like, left to skip.";

        /// <summary>
        /// Loads the initial batch of cards into the queue.
        /// </summary>
        private async Task LoadInitialCardsAsync()
        {
            try
            {
                IsLoading = true;
                IsAllCaughtUp = false;

                var movies = await _swipeService.GetUnswipedMoviesAsync(DefaultUserId, BufferSize);

                CardQueue.Clear();
                foreach (var movie in movies)
                {
                    CardQueue.Add(movie);
                }

                AdvanceToNextCard();

                if (CardQueue.Count == 0 && CurrentCard == null)
                {
                    IsAllCaughtUp = true;
                    StatusMessage = "All caught up! No more movies to swipe.";
                }
            }
            catch (Exception)
            {
                StatusMessage = "Could not load movies. Please try again later.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Right-swipe: like the current movie (+1.0 score).
        /// </summary>
        [RelayCommand]
        private async Task SwipeRightAsync()
        {
            await ProcessSwipeAsync(isLiked: true);
        }

        /// <summary>
        /// Left-swipe: skip the current movie (−0.5 score).
        /// </summary>
        [RelayCommand]
        private async Task SwipeLeftAsync()
        {
            await ProcessSwipeAsync(isLiked: false);
        }

        /// <summary>
        /// Processes a completed swipe: persists the preference and advances the card queue.
        /// </summary>
        private async Task ProcessSwipeAsync(bool isLiked)
        {
            if (CurrentCard == null)
            {
                return;
            }

            var swipedCard = CurrentCard;

            // Advance immediately for responsive UI
            AdvanceToNextCard();

            // Start persistence but don't block showing the next card.
            Task persistTask = _swipeService.UpdatePreferenceScoreAsync(DefaultUserId, swipedCard.MovieId, isLiked);

            // Refill as soon as possible so the next card appears even if DB write is slow.
            await TryRefillQueueAsync(swipedCard.MovieId);

            try
            {
                await persistTask;
            }
            catch (Exception)
            {
                // Preference update failed — the swipe is still consumed to avoid
                // showing the same card again. The score will be missing but no crash.
            }
        }

        /// <summary>
        /// Pops the next card from the queue into <see cref="CurrentCard"/>.
        /// Sets <see cref="IsAllCaughtUp"/> when the queue is empty.
        /// </summary>
        private void AdvanceToNextCard()
        {
            if (CardQueue.Count > 0)
            {
                CurrentCard = CardQueue[0];
                CardQueue.RemoveAt(0);
                IsAllCaughtUp = false;
            }
            else
            {
                CurrentCard = null;
                IsAllCaughtUp = true;
                StatusMessage = "All caught up! No more movies to swipe.";
            }
        }

        /// <summary>
        /// Fetches more cards when the queue drops to ≤ <see cref="RefillThreshold"/>.
        /// Prevents concurrent refill requests.
        /// </summary>
        private async Task TryRefillQueueAsync(int? recentlySwipedMovieId = null)
        {
            // Guard: don't refill if already refilling or above threshold
            if (_isRefilling || CardQueue.Count > RefillThreshold)
            {
                return;
            }

            _isRefilling = true;

            try
            {
                var newMovies = await _swipeService.GetUnswipedMoviesAsync(DefaultUserId, BufferSize);

                var existingIds = new HashSet<int>(CardQueue.Select(m => m.MovieId));
                if (CurrentCard != null)
                {
                    existingIds.Add(CurrentCard.MovieId);
                }

                foreach (var movie in newMovies)
                {
                    if (recentlySwipedMovieId.HasValue && movie.MovieId == recentlySwipedMovieId.Value)
                    {
                        continue;
                    }

                    if (existingIds.Contains(movie.MovieId))
                    {
                        continue;
                    }

                    CardQueue.Add(movie);
                    existingIds.Add(movie.MovieId);
                }

                // If CurrentCard was set to null because queue was empty,
                // but we just got new movies, advance again.
                if (CurrentCard == null && CardQueue.Count > 0)
                {
                    IsAllCaughtUp = false;
                    StatusMessage = "Swipe right to like, left to skip.";
                    AdvanceToNextCard();
                }
            }
            catch (Exception)
            {
                // Silently fail — user can keep swiping remaining cards
            }
            finally
            {
                _isRefilling = false;
            }
        }
    }
}
