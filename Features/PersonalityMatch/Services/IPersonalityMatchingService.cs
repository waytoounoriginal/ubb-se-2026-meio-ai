using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Features.PersonalityMatch.Services
{
    /// <summary>
    /// Compares user profiles to find personality/taste overlap.
    /// Owner: Madi
    /// </summary>
    public interface IPersonalityMatchingService
    {
        /// <summary>
        /// Returns the top N users whose movie preferences most closely match the given user.
        /// </summary>
        Task<IList<(UserProfileModel Profile, double OverlapScore)>> GetTopMatchesAsync(int userId, int count);
    }
}
