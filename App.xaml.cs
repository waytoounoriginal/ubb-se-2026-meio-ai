using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using ubb_se_2026_meio_ai.Core.Database;
using ubb_se_2026_meio_ai.Features.ReelsUpload.ViewModels;
using ubb_se_2026_meio_ai.Features.TrailerScraping.ViewModels;
using ubb_se_2026_meio_ai.Features.ReelsEditing.ViewModels;
using ubb_se_2026_meio_ai.Features.MovieSwipe.ViewModels;
using ubb_se_2026_meio_ai.Features.MovieTournament.ViewModels;
using ubb_se_2026_meio_ai.Features.PersonalityMatch.ViewModels;
using ubb_se_2026_meio_ai.Features.ReelsFeed.ViewModels;

namespace ubb_se_2026_meio_ai
{
    /// <summary>
    /// Application entry point. Configures the DI container and initialises the database.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Global service provider — use <c>App.Services.GetRequiredService&lt;T&gt;()</c>
        /// from Page code-behinds to resolve registered types.
        /// </summary>
        public static IServiceProvider Services { get; private set; } = null!;

        private Window? m_window;

        public App()
        {
            this.InitializeComponent();

<<<<<<< HEAD
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
=======
            // Load configuration from appsettings.json
            var builder = new ConfigurationBuilder()
                .SetBasePath(System.AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            var configuration = builder.Build();

            // Build the DI container
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            ConfigureServices(services, configuration);
            Services = services.BuildServiceProvider();
>>>>>>> parent of b6fd163 (Merge branch 'dev' into feature/reels-upload)
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
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

            MainWindow = new MainWindow();
            MainWindow.Activate();
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

            // ── ViewModels (one per feature page) ────────────────────────
            services.AddTransient<ReelsUploadViewModel>();
            services.AddTransient<TrailerScrapingViewModel>();
            services.AddTransient<ReelsEditingViewModel>();
            services.AddTransient<MovieSwipeViewModel>();
            services.AddTransient<MovieTournamentViewModel>();
            services.AddTransient<PersonalityMatchViewModel>();
            services.AddTransient<ReelsFeedViewModel>();

            // ── Feature Services ─────────────────────────────────────────
<<<<<<< HEAD
            // TODO (Alex):      services.AddTransient<IVideoStorageService, VideoStorageService>();
            // ── Beatrice (Reels Editing) ──
            services.AddTransient<Features.ReelsEditing.Services.ReelRepository>();
            services.AddTransient<Features.ReelsEditing.Services.IVideoProcessingService,
                                  Features.ReelsEditing.Services.VideoProcessingService>();
            services.AddTransient<Features.ReelsEditing.Services.IAudioLibraryService,
                                  Features.ReelsEditing.Services.AudioLibraryService>();
=======
            services.AddTransient<ubb_se_2026_meio_ai.Features.ReelsUpload.Services.IVideoStorageService, ubb_se_2026_meio_ai.Features.ReelsUpload.Services.VideoStorageService>();
            // TODO (Andrei):    services.AddTransient<IWebScraperService, WebScraperService>();
            //                   services.AddTransient<IVideoIngestionService, VideoIngestionService>();
            // TODO (Beatrice):  services.AddTransient<IVideoProcessingService, VideoProcessingService>();
            //                   services.AddTransient<IAudioLibraryService, AudioLibraryService>();
>>>>>>> parent of b6fd163 (Merge branch 'dev' into feature/reels-upload)
            // TODO (Bogdan):    services.AddTransient<ISwipeService, SwipeService>();
            //                   services.AddTransient<IPreferenceRepository, PreferenceRepository>();
            // TODO (Gabi):      services.AddTransient<ITournamentLogicService, TournamentLogicService>();
            //                   services.AddTransient<IMovieTournamentRepository, MovieTournamentRepository>();
            // TODO (Madi):      services.AddTransient<IPersonalityMatchingService, PersonalityMatchingService>();
            // TODO (Tudor):     services.AddTransient<IReelInteractionService, ReelInteractionService>();
            //                   services.AddTransient<IEngagementProfileService, EngagementProfileService>();
            //                   services.AddTransient<IRecommendationService, RecommendationService>();
            //                   services.AddTransient<IClipPlaybackService, ClipPlaybackService>();
        }
    }
}
