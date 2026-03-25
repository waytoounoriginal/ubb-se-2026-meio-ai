using Microsoft.Extensions.DependencyInjection;
using ubb_se_2026_meio_ai.Features.ReelsFeed.Services;
using ubb_se_2026_meio_ai.Features.ReelsFeed.Repositories;
using Microsoft.UI.Xaml;
using ubb_se_2026_meio_ai.Core.Database;
using ubb_se_2026_meio_ai.Features.ReelsUpload.ViewModels;
using ubb_se_2026_meio_ai.Features.TrailerScraping.ViewModels;
using ubb_se_2026_meio_ai.Features.ReelsEditing.ViewModels;
using ubb_se_2026_meio_ai.Features.MovieSwipe.ViewModels;
using ubb_se_2026_meio_ai.Features.MovieSwipe.Services;
using ubb_se_2026_meio_ai.Features.MovieTournament.ViewModels;
using ubb_se_2026_meio_ai.Features.PersonalityMatch.ViewModels;
using ubb_se_2026_meio_ai.Features.ReelsFeed.ViewModels;
using ubb_se_2026_meio_ai.Features.MovieTournament.Services;

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

            // Build the DI container
            var services = new ServiceCollection();
            ConfigureServices(services);
            Services = services.BuildServiceProvider();
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

            m_window = new MainWindow();
            m_window.Activate();
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
            // TODO (Alex):      services.AddTransient<IVideoStorageService, VideoStorageService>();
            // TODO (Andrei):    services.AddTransient<IWebScraperService, WebScraperService>();
            //                   services.AddTransient<IVideoIngestionService, VideoIngestionService>();
            // TODO (Beatrice):  services.AddTransient<IVideoProcessingService, VideoProcessingService>();
            //                   services.AddTransient<IAudioLibraryService, AudioLibraryService>();
            // TODO (Bogdan):    services.AddTransient<ISwipeService, SwipeService>();
            //                   services.AddTransient<IPreferenceRepository, PreferenceRepository>();
            // TODO (Gabi):
            services.AddTransient<ITournamentLogicService, TournamentLogicService>();
            services.AddTransient<IMovieTournamentRepository, MovieTournamentRepository>();
            // ── Bogdan (Movie Swipe) ──
            services.AddTransient<ISwipeService, SwipeService>();
            services.AddTransient<IPreferenceRepository, PreferenceRepository>();
            services.AddTransient<IMovieCardFeedService, MovieCardFeedService>();
            // TODO (Gabi):      services.AddTransient<ITournamentLogicService, TournamentLogicService>();
            //                   services.AddTransient<IMovieTournamentRepository, MovieTournamentRepository>();
            // TODO (Madi):      services.AddTransient<IPersonalityMatchingService, PersonalityMatchingService>();
            // Tudor – Repositories
            services.AddTransient<IInteractionRepository, InteractionRepository>();
            services.AddTransient<IProfileRepository, ProfileRepository>();
            services.AddTransient<IPreferenceRepository, PreferenceRepository>();

            // Tudor – Services
            services.AddTransient<IReelInteractionService, ReelInteractionService>();
            services.AddTransient<IEngagementProfileService, EngagementProfileService>();
            services.AddTransient<IRecommendationService, RecommendationService>();
            services.AddTransient<IClipPlaybackService, ClipPlaybackService>();
        }
    }
}
