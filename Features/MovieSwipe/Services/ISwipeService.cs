using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Features.MovieSwipe.Services
{
    /// <summary>
    /// Manages Tinder-like swiping logic for movies.
    /// Owner: Bogdan
    /// </summary>
    public interface ISwipeService
    {
        /// <summary>
        /// Updates the preference score for a user-movie pair.
        /// Like (right swipe) → +1.0, Skip (left swipe) → −0.5.
        /// </summary>
        Task UpdatePreferenceScoreAsync(int userId, int movieId, bool isLiked);

        /// <summary>
        /// Returns a batch of movies for the card queue (infinite feed).
        /// </summary>
        Task<List<MovieCardModel>> GetMovieFeedAsync(int userId, int count);
    }
}
