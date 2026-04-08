namespace ubb_se_2026_meio_ai.Features.TrailerScraping.Services
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using ubb_se_2026_meio_ai.Core.Models;

    /// <summary>
    /// Repository for managing ScrapeJob and ScrapeJobLog records.
    /// Owner: Andrei.
    /// </summary>
    public interface IScrapeJobRepository
    {
        /// <summary>
        /// Creates a new scrape job and returns its auto-generated ID.
        /// </summary>
        /// <param name="job">The scrape job model to insert.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the auto-generated job ID.</returns>
        Task<int> CreateJobAsync(ScrapeJobModel job);

        /// <summary>
        /// Updates an existing scrape job (status, counts, completion time, error).
        /// </summary>
        /// <param name="job">The scrape job model to update.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task UpdateJobAsync(ScrapeJobModel job);

        /// <summary>
        /// Appends a log entry to a scrape job.
        /// </summary>
        /// <param name="log">The log entry model to insert.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task AddLogEntryAsync(ScrapeJobLogModel log);

        /// <summary>
        /// Retrieves all scrape jobs ordered by most recent first.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of scrape jobs.</returns>
        Task<IList<ScrapeJobModel>> GetAllJobsAsync();

        /// <summary>
        /// Retrieves log entries for a specific job ordered by timestamp.
        /// </summary>
        /// <param name="jobId">The unique identifier of the scrape job.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of log entries for the job.</returns>
        Task<IList<ScrapeJobLogModel>> GetLogsForJobAsync(int jobId);

        /// <summary>
        /// Retrieves all log entries across all jobs, most recent first.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of all log entries.</returns>
        Task<IList<ScrapeJobLogModel>> GetAllLogsAsync();

        /// <summary>
        /// Returns aggregated dashboard statistics.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the dashboard statistics.</returns>
        Task<DashboardStatsModel> GetDashboardStatsAsync();

        /// <summary>
        /// Searches movies by partial title match (case-insensitive) for autocomplete.
        /// </summary>
        /// <param name="partialName">The partial name or title to search for.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of matching movies.</returns>
        Task<IList<MovieCardModel>> SearchMoviesByNameAsync(string partialName);

        /// <summary>
        /// Checks whether a movie with the given title already exists.
        /// </summary>
        /// <param name="title">The exact title of the movie.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the MovieId if found, null otherwise.</returns>
        Task<int?> FindMovieByTitleAsync(string title);

        /// <summary>
        /// Checks whether a reel with the given VideoUrl already exists.
        /// </summary>
        /// <param name="videoUrl">The video URL to check for duplicates.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is true if the reel exists, false otherwise.</returns>
        Task<bool> ReelExistsByVideoUrlAsync(string videoUrl);

        /// <summary>
        /// Inserts a new Reel row with Source = 'scraped'.
        /// </summary>
        /// <param name="reel">The reel model to insert.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the auto-generated reel ID.</returns>
        Task<int> InsertScrapedReelAsync(ReelModel reel);

        /// <summary>
        /// Retrieves all movies from the Movie table.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of all movies.</returns>
        Task<IList<MovieCardModel>> GetAllMoviesAsync();

        /// <summary>
        /// Retrieves all reels from the Reel table.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of all reels.</returns>
        Task<IList<ReelModel>> GetAllReelsAsync();
    }
}