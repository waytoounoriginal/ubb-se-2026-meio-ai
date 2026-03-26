using Microsoft.Extensions.DependencyInjection;
using ubb_se_2026_meio_ai.Features.ReelsFeed.Services;
using ubb_se_2026_meio_ai.Features.ReelsFeed.Repositories;
using Microsoft.UI.Xaml;
using ubb_se_2026_meio_ai.Core.Database;
using ubb_se_2026_meio_ai.Features.ReelsUpload.ViewModels;
using ubb_se_2026_meio_ai.Features.TrailerScraping.ViewModels;
using ubb_se_2026_meio_ai.Features.TrailerScraping.Services;
using ubb_se_2026_meio_ai.Features.ReelsEditing.ViewModels;
using ubb_se_2026_meio_ai.Features.MovieSwipe.ViewModels;
using ubb_se_2026_meio_ai.Features.MovieSwipe.Services;
using ubb_se_2026_meio_ai.Features.MovieTournament.ViewModels;
using ubb_se_2026_meio_ai.Features.PersonalityMatch.ViewModels;
using ubb_se_2026_meio_ai.Features.PersonalityMatch.Services;
using ubb_se_2026_meio_ai.Features.ReelsFeed.ViewModels;
using ubb_se_2026_meio_ai.Features.MovieTournament.Services;
using System.Diagnostics;
using System.IO;

namespace ubb_se_2026_meio_ai
{
    /// <summary>
    /// Application entry point. Configures the DI container and initialises the database.
    /// </summary>
    public partial class App : Application
    {
        // ⚠️ PASTE YOUR YOUTUBE API KEY BELOW — CLEAR BEFORE COMMITTING TO GITHUB ⚠️
        private const string YouTubeApiKey = "AIzaSyA035aofA1kYjUovkGKoS9qy8kCmTz-Ue4";

        private static readonly string CrashLogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MeioAI", "crash.log");

        /// <summary>
        /// Global service provider — use <c>App.Services.GetRequiredService&lt;T&gt;()</c>
        /// from Page code-behinds to resolve registered types.
        /// </summary>
        public static IServiceProvider Services { get; private set; } = null!;

        private Window? m_window;

        public App()
        {
            // Log first-chance exceptions to help diagnose native crashes (0xc000027b).
            AppDomain.CurrentDomain.FirstChanceException += (sender, e) =>
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(CrashLogPath)!);
                    File.AppendAllText(CrashLogPath,
                        $"[{DateTime.Now:HH:mm:ss.fff}] FirstChance: {e.Exception.GetType().Name}: {e.Exception.Message}\n" +
                        $"  {e.Exception.StackTrace?.Split('\n').FirstOrDefault()?.Trim()}\n");
                }
                catch { /* logging must never crash */ }
            };

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                try
                {
                    var ex = e.ExceptionObject as Exception;
                    Directory.CreateDirectory(Path.GetDirectoryName(CrashLogPath)!);
                    File.AppendAllText(CrashLogPath,
                        $"[{DateTime.Now:HH:mm:ss.fff}] UNHANDLED (CLR): {ex?.GetType().Name}: {ex?.Message}\n{ex?.StackTrace}\n\n");
                }
                catch { }
            };

            this.InitializeComponent();

            // Suppress COM teardown exceptions during app close.
            // WinUI 3 MediaPlayer fires callbacks on background threads that
            // race with window destruction — these are harmless but otherwise
            // show an "unhandled Win32 exception" dialog.
            this.UnhandledException += (sender, e) =>
            {
                try
                {
                    File.AppendAllText(CrashLogPath,
                        $"[{DateTime.Now:HH:mm:ss.fff}] WinUI UnhandledException: {e.Exception.GetType().Name}: {e.Exception.Message}\n{e.Exception.StackTrace}\n\n");
                }
                catch { }
                e.Handled = true;
            };

            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                e.SetObserved();
            };

            // Build the DI container
            try
            {
                var services = new ServiceCollection();
                ConfigureServices(services);
                Services = services.BuildServiceProvider();
            }
            catch (Exception ex)
            {
                File.AppendAllText(CrashLogPath,
                    $"[{DateTime.Now:HH:mm:ss.fff}] DI CONTAINER FAILED: {ex}\n\n");
                throw;
            }
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            try
            {
                // Ensure shared tables exist before any feature code runs.
                var dbInit = Services.GetRequiredService<DatabaseInitializer>();
                try
                {
                    await dbInit.CreateTablesIfNotExistAsync();
                }
                catch
                {
                    // Database may not be available during development — continue anyway.
                }

                m_window = new MainWindow();
                m_window.Activate();
            }
            catch (Exception ex)
            {
                try
                {
                    File.AppendAllText(CrashLogPath,
                        $"[{DateTime.Now:HH:mm:ss.fff}] OnLaunched FAILED: {ex}\n\n");
                }
                catch { }
            }
        }

        /// <summary>
        /// Resolves the YouTube API key: prefers the compiled-in constant,
        /// falls back to the YOUTUBE_API_KEY environment variable.
        /// </summary>
        private static string ResolveYouTubeApiKey()
        {
            if (!string.IsNullOrWhiteSpace(YouTubeApiKey))
            {
                return YouTubeApiKey;
            }

            return Environment.GetEnvironmentVariable("YOUTUBE_API_KEY") ?? string.Empty;
        }

        /// <summary>
        /// Register all shared infrastructure and per-feature ViewModels.
        /// Feature developers: register your concrete service implementations here
        /// when they are ready.
        /// </summary>
        private static void ConfigureServices(IServiceCollection services)
        {
            // ── Core / Database ──────────────────────────────────────────
            services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
            services.AddTransient<DatabaseInitializer>();

            // ── Andrei — Trailer Scraping Services ───────────────────────
            string apiKey = ResolveYouTubeApiKey();
            services.AddSingleton(new YouTubeScraperService(apiKey));
            services.AddSingleton<IScrapeJobRepository, ScrapeJobRepository>();
            services.AddSingleton<VideoDownloadService>();
            services.AddTransient<VideoIngestionService>();

            // ── ViewModels (one per feature page) ────────────────────────
            services.AddTransient<ReelsUploadViewModel>();
            services.AddTransient<TrailerScrapingViewModel>();
            services.AddTransient<ReelsEditingViewModel>();
            services.AddTransient<MovieSwipeViewModel>();
            services.AddTransient<MovieTournamentViewModel>();
            services.AddTransient<PersonalityMatchViewModel>();
            services.AddTransient<ReelsFeedViewModel>();
            services.AddTransient<UserProfileViewModel>();

            // ── Feature Services ─────────────────────────────────────────
            // TODO (Alex):      services.AddTransient<IVideoStorageService, VideoStorageService>();
            // TODO (Beatrice):  services.AddTransient<IVideoProcessingService, VideoProcessingService>();
            //                   services.AddTransient<IAudioLibraryService, AudioLibraryService>();
            // TODO (Bogdan):    services.AddTransient<ISwipeService, SwipeService>();
            //                   services.AddTransient<IPreferenceRepository, PreferenceRepository>();
            // TODO (Gabi):
            services.AddTransient<ITournamentLogicService, TournamentLogicService>();
            services.AddTransient<IMovieTournamentRepository, MovieTournamentRepository>();
            // ── Bogdan (Movie Swipe) ──
            services.AddTransient<ISwipeService, SwipeService>();
            services.AddTransient<Features.MovieSwipe.Services.IPreferenceRepository, Features.MovieSwipe.Services.PreferenceRepository>();
            services.AddTransient<IMovieCardFeedService, MovieCardFeedService>();
            // TODO (Gabi):      services.AddTransient<ITournamentLogicService, TournamentLogicService>();
            //                   services.AddTransient<IMovieTournamentRepository, MovieTournamentRepository>();
            // ── Madi (Personality Match) ──
            services.AddTransient<IPersonalityMatchRepository, PersonalityMatchRepository>();
            services.AddTransient<IPersonalityMatchingService, PersonalityMatchingService>();
            services.AddTransient<MatchedUserDetailViewModel>();
            // Tudor – Repositories
            services.AddTransient<IInteractionRepository, InteractionRepository>();
            services.AddTransient<IProfileRepository, ProfileRepository>();
            services.AddTransient<Features.ReelsFeed.Repositories.IPreferenceRepository, Features.ReelsFeed.Repositories.PreferenceRepository>();

            // Tudor – Services
            services.AddTransient<IReelInteractionService, ReelInteractionService>();
            services.AddTransient<IEngagementProfileService, EngagementProfileService>();
            services.AddTransient<IRecommendationService, RecommendationService>();
            services.AddSingleton<IClipPlaybackService, ClipPlaybackService>();
        }
    }
}
