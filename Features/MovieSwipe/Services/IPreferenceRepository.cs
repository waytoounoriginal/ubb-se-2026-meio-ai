using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Features.MovieSwipe.Services
{
    /// <summary>
    /// Data access for user-movie preference records.
    /// Owner: Bogdan
    /// </summary>
    public interface IPreferenceRepository
    {
        /// <summary>
        /// Returns the preference row for a specific user-movie pair, or null if none exists.
        /// </summary>
        Task<UserMoviePreferenceModel?> GetPreferenceAsync(int userId, int movieId);

        /// <summary>
        /// Inserts a new preference row or updates the existing one (score += delta).
        /// If no row exists, one is created at score 0 before applying the delta.
        /// </summary>
        Task UpsertPreferenceAsync(UserMoviePreferenceModel preference);

        /// <summary>
        /// Returns all preference records grouped by userId, excluding the specified user.
        /// Used by Madi's personality matching.
        /// </summary>
        Task<Dictionary<int, List<UserMoviePreferenceModel>>> GetAllPreferencesExceptUserAsync(int excludeUserId);

        /// <summary>
        /// Returns the MovieIds that the user has already swiped on.
        /// </summary>
        Task<List<int>> GetUnswipedMovieIdsAsync(int userId);

        /// <summary>
        /// Returns a feed of movies from the external Movie table, prioritising unswiped movies,
        /// then previously swiped movies starting with those swiped longest ago.
        /// </summary>
        Task<List<MovieCardModel>> GetMovieFeedAsync(int userId, int count);
    }
}
