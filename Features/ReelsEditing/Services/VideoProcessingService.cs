namespace ubb_se_2026_meio_ai.Features.ReelsEditing.Services
{
    /// <summary>
    /// Stub implementation of IVideoProcessingService.
    /// Records the requested edit operations; actual ffmpeg processing can be
    /// wired in later. Returns the original videoPath unchanged.
    /// Owner: Beatrice
    /// </summary>
    public class VideoProcessingService : IVideoProcessingService
    {
        public Task<string> ApplyCropAsync(string videoPath, string cropDataJson)
        {
            // Future: invoke ffmpeg to crop the video file
            // For now: editing metadata is persisted to DB; the path is unchanged
            return Task.FromResult(videoPath);
        }

        public Task<string> MergeAudioAsync(string videoPath, int musicTrackId, double startOffsetSec)
        {
            // Future: invoke ffmpeg to mix audio into video
            return Task.FromResult(videoPath);
        }
    }
}
