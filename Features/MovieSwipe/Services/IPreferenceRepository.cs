using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Features.MovieSwipe.Services
{
    /// <summary>
    /// Defines the data access contract for movie preferences and feed retrieval.
    /// </summary>
    public interface IPreferenceRepository
    {
        /// <summary> Retrieves a specific user's preference for a given movie. </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="movieId">The unique identifier of the movie.</param>
        /// <returns>A task representing the asynchronous operation, containing the preference model or null.</returns>
        Task<UserMoviePreferenceModel?> GetPreferenceAsync(int userId, int movieId);

        /// <summary> Inserts a new preference or updates an existing one using an atomic MERGE operation. </summary>
        /// <param name="preference">The preference data to persist.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpsertPreferenceAsync(UserMoviePreferenceModel preference);

        /// <summary> Fetches all movie preferences from the database, excluding a specific user. </summary>
        /// <param name="excludeUserId">The user ID to be filtered out of the results.</param>
        /// <returns>A dictionary where keys are user IDs and values are lists of their preferences.</returns>
        Task<Dictionary<int, List<UserMoviePreferenceModel>>> GetAllPreferencesExceptUserAsync(int excludeUserId);

        /// <summary> Retrieves a list of movie IDs that the specified user has not yet swiped on. </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>A list of IDs for movies without a corresponding preference entry.</returns>
        Task<List<int>> GetUnswipedMovieIdsAsync(int userId);

        /// <summary> Fetches a set of movie cards to be displayed in the swipe feed. </summary>
        /// <param name="userId">The user for whom the feed is generated.</param>
        /// <param name="count">The maximum number of cards to retrieve.</param>
        /// <returns>A list of movie card models.</returns>
        Task<List<MovieCardModel>> GetMovieFeedAsync(int userId, int count);
    }
}