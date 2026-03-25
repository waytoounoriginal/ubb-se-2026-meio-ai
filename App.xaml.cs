using Microsoft.Extensions.Configuration;
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

        public static Window MainWindow { get; private set; } = null!;

        public App()
        {
            this.InitializeComponent();

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
        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // ── Core / Database ──────────────────────────────────────────
            string connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? "Server=localhost;Database=MeioAiDb;Trusted_Connection=True;TrustServerCertificate=True;";
                
            services.AddSingleton<ISqlConnectionFactory>(new SqlConnectionFactory(connectionString));
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
            services.AddTransient<ubb_se_2026_meio_ai.Features.ReelsUpload.Services.IVideoStorageService, ubb_se_2026_meio_ai.Features.ReelsUpload.Services.VideoStorageService>();
            // TODO (Andrei):    services.AddTransient<IWebScraperService, WebScraperService>();
            //                   services.AddTransient<IVideoIngestionService, VideoIngestionService>();
            // TODO (Beatrice):  services.AddTransient<IVideoProcessingService, VideoProcessingService>();
            //                   services.AddTransient<IAudioLibraryService, AudioLibraryService>();
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
