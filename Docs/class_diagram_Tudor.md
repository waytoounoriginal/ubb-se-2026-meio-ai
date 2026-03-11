# Class Diagram — Tudor (Reels Feed)

```mermaid
classDiagram
    direction TB

    %% ── Models ──
    class ReelModel {
        +int ReelId
        +int? MovieId
        +int? CreatorUserId
        +string VideoUrl
        +string ThumbnailUrl
        +string Title
        +string Caption
        +int DurationSeconds
        +string Source
        +DateTime CreatedAt
    }

    class UserReelInteractionModel {
        +int InteractionId
        +int UserId
        +int ReelId
        +bool IsLiked
        +int? WatchDurationSec
        +float? WatchPercentage
        +DateTime ViewedAt
    }

    class UserProfileModel {
        +int UserProfileId
        +int UserId
        +int TotalLikes
        +long TotalWatchTimeSec
        +float AvgWatchTimeSec
        +int TotalClipsViewed
        +float LikeToViewRatio
        +DateTime LastUpdated
    }

    class UserMoviePreferenceModel {
        +int UserMoviePreferenceId
        +int UserId
        +int MovieId
        +float Score
        +DateTime LastModified
    }

    %% ── Services ──
    class IReelInteractionService {
        <<interface>>
        +RecordViewAsync(int userId, int reelId, int watchDuration, float watchPct) void
        +ToggleLikeAsync(int userId, int reelId) void
        +GetInteractionAsync(int userId, int reelId) UserReelInteractionModel
    }

    class IEngagementProfileService {
        <<interface>>
        +RecalculateProfileAsync(int userId) void
        +GetProfileAsync(int userId) UserProfileModel
    }

    class IRecommendationService {
        <<interface>>
        +GetRecommendedReelsAsync(int userId, int count) List~ReelModel~
    }

    class IClipPlaybackService {
        <<interface>>
        +PlayClip(string videoUrl) void
        +PauseClip() void
        +ResumeClip() void
        +GetElapsedSeconds() int
        +PrefetchClip(string videoUrl) void
    }

    class InteractionRepository {
        +InsertInteractionAsync(UserReelInteractionModel) void
        +UpsertInteractionAsync(int userId, int reelId) void
    }

    class ProfileRepository {
        +UpsertProfileAsync(UserProfileModel) void
    }

    class PreferenceRepository {
        +BoostPreferenceOnLikeAsync(int userId, int movieId) void
    }

    %% ── ViewModels ──
    class ReelsFeedViewModel {
        +ReelModel CurrentReel
        +ObservableCollection~ReelModel~ ReelQueue
        +bool IsLoading
        +bool IsCurrentReelLiked
        +string? ErrorMessage
        +ICommand LoadFeedCommand
        +ICommand ScrollNextCommand
        +ICommand ScrollPreviousCommand
        +ICommand ToggleLikeCommand
    }

    class UserProfileViewModel {
        +UserProfileModel Profile
    }

    %% ── Views ──
    class ReelsFeedView {
        <<View>>
    }

    class ReelItemView {
        <<View>>
    }

    %% ── Relationships ──
    ReelsFeedView --> ReelsFeedViewModel : DataContext
    ReelItemView --> ReelsFeedViewModel : DataContext

    ReelsFeedViewModel --> IRecommendationService : fetches feed
    ReelsFeedViewModel --> IReelInteractionService : records views & likes
    ReelsFeedViewModel --> IClipPlaybackService : controls playback
    UserProfileViewModel --> IEngagementProfileService : reads profile

    IReelInteractionService --> InteractionRepository : persists interactions
    IReelInteractionService --> PreferenceRepository : boosts on like
    IReelInteractionService --> UserReelInteractionModel : creates
    IEngagementProfileService --> ProfileRepository : updates profile
    IEngagementProfileService --> UserProfileModel : computes
    IRecommendationService --> ReelModel : returns
    PreferenceRepository --> UserMoviePreferenceModel : upserts
```
