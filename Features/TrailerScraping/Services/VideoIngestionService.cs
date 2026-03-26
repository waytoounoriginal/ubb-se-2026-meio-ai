using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Features.TrailerScraping.Services
{
    /// <summary>
    /// Orchestrates the full scrape flow:
    /// 1. Create a ScrapeJob
    /// 2. Search YouTube for trailers of a selected movie
    /// 3. For each result: download MP4, create Reel (skip duplicates)
    /// 4. Update job status to completed/failed
    /// 
    /// Implements <see cref="IVideoIngestionService"/>.
    /// Owner: Andrei
    /// </summary>
    public class VideoIngestionService : IVideoIngestionService
    {
        private readonly YouTubeScraperService _scraper;
        private readonly IScrapeJobRepository _repository;
        private readonly VideoDownloadService _downloader;

        public VideoIngestionService(
            YouTubeScraperService scraper,
            IScrapeJobRepository repository,
            VideoDownloadService downloader)
        {
            _scraper = scraper;
            _repository = repository;
            _downloader = downloader;
        }

        /// <inheritdoc />
        public async Task<string> IngestVideoFromUrlAsync(string trailerUrl, int movieId)
        {
            bool exists = await _repository.ReelExistsByVideoUrlAsync(trailerUrl);
            if (exists)
            {
                return string.Empty; // duplicate
            }

            // Download as MP4 — YouTube URLs are not directly playable
            string? localPath = await _downloader.DownloadVideoAsMp4Async(trailerUrl, maxDurationSeconds: 60);

            if (string.IsNullOrEmpty(localPath))
            {
                return string.Empty; // download failed, skip
            }

            var reel = new ReelModel
            {
                MovieId = movieId,
                CreatorUserId = 1, // UserId 1 (so it appears in Reels Editing)
                VideoUrl = localPath,
                Title = "Scraped Trailer",
                Caption = string.Empty,
                ThumbnailUrl = string.Empty,
                Source = "scraped",
                CreatedAt = DateTime.UtcNow,
            };

            int reelId = await _repository.InsertScrapedReelAsync(reel);
            return reelId.ToString();
        }

        /// <summary>
        /// Runs a full scrape job for a specific movie from the Movie table.
        /// Searches YouTube for trailers, downloads them as MP4, and inserts Reels
        /// linked to the given MovieId.
        /// </summary>
        /// <param name="movie">The movie selected from the Movie table.</param>
        /// <param name="maxResults">Maximum number of YouTube results to fetch.</param>
        /// <param name="onLogEntry">Optional callback for live UI log updates.</param>
        /// <returns>The completed <see cref="ScrapeJobModel"/>.</returns>
        public async Task<ScrapeJobModel> RunScrapeJobAsync(
            MovieCardModel movie,
            int maxResults,
            Func<ScrapeJobLogModel, Task>? onLogEntry = null)
        {
            // Build search query from the movie title
            string searchQuery = $"{movie.Title} official trailer";

            // 1. Create job record
            var job = new ScrapeJobModel
            {
                SearchQuery = searchQuery,
                MaxResults = maxResults,
                Status = "running",
                StartedAt = DateTime.UtcNow,
            };
            job.ScrapeJobId = await _repository.CreateJobAsync(job);

            async Task LogAsync(string level, string message)
            {
                var logEntry = new ScrapeJobLogModel
                {
                    ScrapeJobId = job.ScrapeJobId,
                    Level = level,
                    Message = message,
                    Timestamp = DateTime.UtcNow,
                };
                await _repository.AddLogEntryAsync(logEntry);
                if (onLogEntry is not null)
                {
                    await onLogEntry(logEntry);
                }
            }

            try
            {
                await LogAsync("Info", $"Scraping trailers for movie: \"{movie.Title}\" (ID {movie.MovieId})");
                await LogAsync("Info", $"YouTube query: \"{searchQuery}\" (max {maxResults} results)");

                // 2. Search YouTube
                IList<ScrapedVideoResult> results = await _scraper.SearchVideosAsync(searchQuery, maxResults);
                await LogAsync("Info", $"YouTube returned {results.Count} result(s)");

                int reelsCreated = 0;

                // 3. Process each result
                foreach (var video in results)
                {
                    try
                    {
                        // Check for duplicate reel
                        bool reelExists = await _repository.ReelExistsByVideoUrlAsync(video.VideoUrl);
                        if (reelExists)
                        {
                            await LogAsync("Warn", $"Reel already exists for: {video.VideoUrl}");
                            continue;
                        }

                        // Download video as MP4 (max 60 seconds)
                        await LogAsync("Info", $"Downloading MP4: \"{video.Title}\"...");
                        string? localMp4Path = await _downloader.DownloadVideoAsMp4Async(video.VideoUrl, maxDurationSeconds: 60);

                        // Skip this video if download failed
                        if (string.IsNullOrEmpty(localMp4Path))
                        {
                            string reason = _downloader.LastError ?? "unknown error";
                            await LogAsync("Error", $"MP4 download failed: {reason}");
                            await LogAsync("Warn", $"Skipping \"{video.Title}\" — no playable MP4 available.");
                            continue;
                        }

                        string videoUrl = localMp4Path;

                        // Insert Reel linked to the selected movie
                        var reel = new ReelModel
                        {
                            MovieId = movie.MovieId,          // FK to the selected movie
                            CreatorUserId = 1,                 // UserId 1 (so it appears in Reels Editing)
                            VideoUrl = videoUrl,
                            ThumbnailUrl = video.ThumbnailUrl,
                            Title = video.Title,
                            Caption = $"Trailer for \"{movie.Title}\" — {video.ChannelTitle} | {video.VideoUrl}",
                            Source = "scraped",
                            CreatedAt = DateTime.UtcNow,
                        };

                        int reelId = await _repository.InsertScrapedReelAsync(reel);
                        reelsCreated++;

                        string format = !string.IsNullOrEmpty(localMp4Path) ? "MP4" : "YouTube URL";
                        await LogAsync("Info", $"Created reel (ID {reelId}) for \"{movie.Title}\" [{format}]");
                    }
                    catch (Exception ex)
                    {
                        await LogAsync("Error", $"Failed to process \"{video.Title}\": {ex.Message}");
                    }
                }

                // 4. Complete job
                job.MoviesFound = 1; // We scraped for a single movie
                job.ReelsCreated = reelsCreated;
                job.Status = "completed";
                job.CompletedAt = DateTime.UtcNow;
                await _repository.UpdateJobAsync(job);

                await LogAsync("Info", $"Job completed — {reelsCreated} reel(s) created for \"{movie.Title}\"");
            }
            catch (Exception ex)
            {
                job.Status = "failed";
                job.CompletedAt = DateTime.UtcNow;
                job.ErrorMessage = ex.Message;
                await _repository.UpdateJobAsync(job);

                try { await LogAsync("Error", $"Job failed: {ex.Message}"); }
                catch { /* best effort logging */ }
            }

            return job;
        }
    }
}
