using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using ubb_se_2026_meio_ai.Core.Database;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.ReelsUpload.Models;
using ubb_se_2026_meio_ai.Features.ReelsUpload.Repository;

namespace ubb_se_2026_meio_ai.Features.ReelsUpload.Services
{
    /// <summary>
    /// Concrete implementation of IVideoStorageService.
    /// Owner: Alex
    /// </summary>
    public class VideoStorageService : IVideoStorageService
    {
        private readonly IVideoStorageRepository _memoryRepository;

        // Simulating a blob storage directory inside the AppData folder for local development
        private readonly string _blobStorageDirectory;

        const string videoFileExtension = ".mp4", emptyURL = "", uploadSource = "upload";

        const int nullId = 0;

        const double maximumReelDurationSeconds = 60.0;

        public VideoStorageService(IVideoStorageRepository memoryRepository)
        {
            _memoryRepository = memoryRepository;

            _blobStorageDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MeioAI",
                "Videos");

            if (!Directory.Exists(_blobStorageDirectory))
            {
                Directory.CreateDirectory(_blobStorageDirectory);
            }
        }

        public async Task<bool> ValidateVideoAsync(string localFilePath)
        {
            if (string.IsNullOrWhiteSpace(localFilePath) || !File.Exists(localFilePath))
                return false;

            var fileExtension = Path.GetExtension(localFilePath).ToLowerInvariant();
            if (fileExtension != videoFileExtension)
            {
                return false;
            }

            try
            {
                var storageFile = await Windows.Storage.StorageFile.GetFileFromPathAsync(localFilePath);
                var videoProperties = await storageFile.Properties.GetVideoPropertiesAsync();

                if (videoProperties.Duration.TotalSeconds > maximumReelDurationSeconds)
                {
                    return false; // Video is too long
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        public async Task<ReelModel> UploadVideoAsync(ReelUploadRequest request)
        {
            if (!File.Exists(request.LocalFilePath))
                throw new FileNotFoundException("The selected video file could not be found.", request.LocalFilePath);

            // "Upload" to Blob Storage
            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(request.LocalFilePath);
            string destinationBlobPath = Path.Combine(_blobStorageDirectory, fileName);
            await Task.Run(() => File.Copy(request.LocalFilePath, destinationBlobPath, overwrite: true));

            // Compute TRUE duration natively
            double computedDurationSeconds = 0;
            try
            {
                var storageFile = await Windows.Storage.StorageFile.GetFileFromPathAsync(request.LocalFilePath);
                var videoProps = await storageFile.Properties.GetVideoPropertiesAsync();
                computedDurationSeconds = videoProps.Duration.TotalSeconds;
            }
            catch
            {
                computedDurationSeconds = 15.0; // Fallback
            }

            // Prepare the model with the data we know
            var newReel = new ReelModel
            {
                MovieId = request.MovieId ?? nullId,
                CreatorUserId = request.UploaderUserId,
                VideoUrl = destinationBlobPath,
                ThumbnailUrl = emptyURL,

                Title = request.Title,
                Caption = request.Caption,

                FeatureDurationSeconds = computedDurationSeconds,
                Source = uploadSource
            };

            return await _memoryRepository.InsertReelAsync(newReel);
        }
    }
}