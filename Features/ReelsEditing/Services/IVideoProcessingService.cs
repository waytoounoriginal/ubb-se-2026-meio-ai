namespace ubb_se_2026_meio_ai.Features.ReelsEditing.Services
{
    /// <summary>
    /// Handles video cropping, trimming, and overlay operations.
    /// Owner: Beatrice
    /// </summary>
    public interface IVideoProcessingService
    {
        Task<string> ApplyCropAsync(string videoPath, string cropDataJson);
        Task<string> MergeAudioAsync(
            string videoPath,
            int musicTrackId,
            double startOffsetSec,
            double musicDurationSec,
            double musicVolumePercent);
    }
}
