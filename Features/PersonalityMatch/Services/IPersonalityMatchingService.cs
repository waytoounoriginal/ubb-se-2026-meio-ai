using ubb_se_2026_meio_ai.Features.PersonalityMatch.Models;

namespace ubb_se_2026_meio_ai.Features.PersonalityMatch.Services
{
    /// <summary>
    /// Compares user profiles to find personality/taste overlap.
    /// Owner: Madi
    /// </summary>
    public interface IPersonalityMatchingService
    {
        /// <summary>
        /// Returns the top <paramref name="count"/> users whose movie preferences
        /// most closely match the given user, each with a 0–100% score.
        /// </summary>
        Task<List<MatchResult>> GetTopMatchesAsync(int userId, int count);

        /// <summary>
        /// Returns up to <paramref name="count"/> random users as a fallback
        /// when no meaningful matches are found.
        /// </summary>
        Task<List<MatchResult>> GetRandomUsersAsync(int userId, int count);
    }
}
