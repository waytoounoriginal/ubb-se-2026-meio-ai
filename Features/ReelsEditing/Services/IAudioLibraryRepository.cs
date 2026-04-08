namespace ubb_se_2026_meio_ai.Features.ReelsEditing.Services
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using ubb_se_2026_meio_ai.Core.Models;

    /// <summary>
    /// Defines the contract for interacting with the audio library.
    /// </summary>
    public interface IAudioLibraryRepository
    {
        /// <summary>
        /// Retrieves all available music tracks.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of music tracks.</returns>
        Task<IList<MusicTrackModel>> GetAllTracksAsync();

        /// <summary>
        /// Retrieves a specific music track by its unique identifier.
        /// </summary>
        /// <param name="musicTrackId">The unique identifier of the music track to retrieve.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the music track if found; otherwise, null.</returns>
        Task<MusicTrackModel?> GetTrackByIdAsync(int musicTrackId);
    }
}