using System.Threading.Tasks;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.ReelsUpload.Models;

namespace ubb_se_2026_meio_ai.Features.ReelsUpload.Services
{
    /// <summary>
    /// Handles uploading and validating video files for Reels.
    /// Owner: Alex
    /// </summary>
    public interface IVideoStorageService
    {
        /// <summary>
        /// Uploads a video file from the local disk, inserts it into the database, and returns the stored ReelModel.
        /// </summary>
        Task<ReelModel> UploadVideoAsync(ReelUploadRequest request);

        /// <summary>
        /// Validates that a video file meets size, duration, and format requirements.
        /// </summary>
        Task<bool> ValidateVideoAsync(string localFilePath);
    }
}
