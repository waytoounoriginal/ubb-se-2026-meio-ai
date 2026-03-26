namespace ubb_se_2026_meio_ai.Features.ReelsUpload.Services
{
    /// <summary>
    /// Handles uploading and validating video files for Reels.
    /// Owner: Alex
    /// </summary>
    public interface IVideoStorageService
    {
        /// <summary>
        /// Uploads a video file from the local disk and returns the stored URL / path.
        /// </summary>
        Task<string> UploadVideoAsync(string localFilePath, int movieId, int creatorUserId);

        /// <summary>
        /// Validates that a video file meets size, duration, and format requirements.
        /// </summary>
        Task<bool> ValidateVideoAsync(string localFilePath);
    }
}
