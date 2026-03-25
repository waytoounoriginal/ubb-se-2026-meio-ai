using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Features.ReelsFeed.Services
{
    /// <summary>
    /// Records user interactions (likes, views) with reels.
    /// Owner: Tudor
    /// </summary>
    public interface IReelInteractionService
    {
        Task ToggleLikeAsync(int userId, int reelId);
        Task RecordViewAsync(int userId, int reelId, double watchDurationSec, double watchPercentage);
        Task<UserReelInteractionModel?> GetInteractionAsync(int userId, int reelId);
        Task<int> GetLikeCountAsync(int reelId);
    }
}
