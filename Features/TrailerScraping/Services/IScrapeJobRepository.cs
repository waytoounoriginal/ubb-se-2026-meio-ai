using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Features.TrailerScraping.Services
{
    /// <summary>
    /// Dashboard statistics returned by the repository.
    /// </summary>
    public class DashboardStatsModel
    {
        public int TotalMovies { get; set; }
        public int TotalReels { get; set; }
        public int TotalJobs { get; set; }
        public int RunningJobs { get; set; }
        public int CompletedJobs { get; set; }
        public int FailedJobs { get; set; }
    }

    /// <summary>
    /// Repository for managing ScrapeJob and ScrapeJobLog records.
    /// Owner: Andrei
    /// </summary>
    public interface IScrapeJobRepository
    {
        /// <summary>
        /// Creates a new scrape job and returns its auto-generated ID.
        /// </summary>
        Task<int> CreateJobAsync(ScrapeJobModel job);

        /// <summary>
        /// Updates an existing scrape job (status, counts, completion time, error).
        /// </summary>
        Task UpdateJobAsync(ScrapeJobModel job);

        /// <summary>
        /// Appends a log entry to a scrape job.
        /// </summary>
        Task AddLogEntryAsync(ScrapeJobLogModel log);

        /// <summary>
        /// Retrieves all scrape jobs ordered by most recent first.
        /// </summary>
        Task<IList<ScrapeJobModel>> GetAllJobsAsync();

        /// <summary>
        /// Retrieves log entries for a specific job ordered by timestamp.
        /// </summary>
        Task<IList<ScrapeJobLogModel>> GetLogsForJobAsync(int jobId);

        /// <summary>
        /// Retrieves all log entries across all jobs, most recent first.
        /// </summary>
        Task<IList<ScrapeJobLogModel>> GetAllLogsAsync();

        /// <summary>
        /// Returns aggregated dashboard statistics.
        /// </summary>
        Task<DashboardStatsModel> GetDashboardStatsAsync();

        /// <summary>
        /// Searches movies by partial title match (case-insensitive) for autocomplete.
        /// </summary>
        Task<IList<MovieCardModel>> SearchMoviesByNameAsync(string partialName);

        /// <summary>
        /// Checks whether a movie with the given title already exists.
        /// Returns the MovieId if found, null otherwise.
        /// </summary>
        Task<int?> FindMovieByTitleAsync(string title);



        /// <summary>
        /// Checks whether a reel with the given VideoUrl already exists.
        /// </summary>
        Task<bool> ReelExistsByVideoUrlAsync(string videoUrl);

        /// <summary>
        /// Inserts a new Reel row with Source = 'scraped'.
        /// </summary>
        Task<int> InsertScrapedReelAsync(ReelModel reel);

        /// <summary>
        /// Retrieves all movies from the Movie table.
        /// </summary>
        Task<IList<MovieCardModel>> GetAllMoviesAsync();

        /// <summary>
        /// Retrieves all reels from the Reel table.
        /// </summary>
        Task<IList<ReelModel>> GetAllReelsAsync();
    }
}
