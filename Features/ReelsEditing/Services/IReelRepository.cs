namespace ubb_se_2026_meio_ai.Features.ReelsEditing.Services
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using ubb_se_2026_meio_ai.Core.Models;

    /// <summary>
    /// Defines the contract for interacting with the reel repository.
    /// </summary>
    public interface IReelRepository
    {
        /// <summary>
        /// Retrieves all reels associated with a specific user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of reels.</returns>
        Task<IList<ReelModel>> GetUserReelsAsync(int userId);

        /// <summary>
        /// Retrieves a specific reel by its unique identifier.
        /// </summary>
        /// <param name="reelId">The unique identifier of the reel.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the reel if found; otherwise, null.</returns>
        Task<ReelModel?> GetReelByIdAsync(int reelId);

        /// <summary>
        /// Updates the editing metadata and video URL for a specific reel.
        /// </summary>
        /// <param name="reelId">The unique identifier of the reel to update.</param>
        /// <param name="cropDataJson">The JSON string containing the crop metadata.</param>
        /// <param name="backgroundMusicId">The unique identifier of the background music track, if any.</param>
        /// <param name="videoUrl">The updated URL or path to the video file.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the number of affected rows.</returns>
        Task<int> UpdateReelEditsAsync(int reelId, string cropDataJson, int? backgroundMusicId, string videoUrl);

        /// <summary>
        /// Deletes a specific reel from the repository.
        /// </summary>
        /// <param name="reelId">The unique identifier of the reel to delete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DeleteReelAsync(int reelId);
    }
}