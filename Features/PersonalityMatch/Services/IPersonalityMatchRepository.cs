using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Features.PersonalityMatch.Services
{
    /// <summary>
    /// Data access contract for the Personality Match feature.
    /// Reads from the shared UserMoviePreference and UserProfile tables.
    /// Owner: Madi
    /// </summary>
    public interface IPersonalityMatchRepository
    {
        /// <summary>
        /// Returns every user's preferences grouped by UserId, excluding the given user.
        /// </summary>
        Task<Dictionary<int, List<UserMoviePreferenceModel>>> GetAllPreferencesExceptUserAsync(int excludeUserId);

        /// <summary>
        /// Returns the current user's movie preferences.
        /// </summary>
        Task<List<UserMoviePreferenceModel>> GetCurrentUserPreferencesAsync(int userId);

        /// <summary>
        /// Returns the engagement profile for a single user, or null if none exists.
        /// </summary>
        Task<UserProfileModel?> GetUserProfileAsync(int userId);

        /// <summary>
        /// Returns up to <paramref name="count"/> random distinct user IDs
        /// that have at least one preference row, excluding <paramref name="excludeUserId"/>.
        /// </summary>
        Task<List<int>> GetRandomUserIdsAsync(int excludeUserId, int count);

        /// <summary>
        /// Returns the username for a given UserId from the User table,
        /// or a fallback string if not found.
        /// </summary>
        Task<string> GetUsernameAsync(int userId);
    }
}
