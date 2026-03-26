### 1. Formal Requirements
*   **Requirement 1:** The system must present an authenticated user with a vertically-scrollable, full-screen "Reels" feed of short movie clips, similar to Instagram Reels or TikTok.
*   **Requirement 2:** The system must auto-play the currently visible clip when a user lands on it.
*   **Requirement 3:** The system must allow the user to "like" the currently displayed clip by tapping a heart icon, toggling the liked state on and off.
*   **Requirement 4:** The system must allow the user to vertically swipe/scroll to navigate to the next or previous clip, with smooth snap-to-clip scrolling behavior.
*   **Requirement 5:** The system must persistently record every user interaction (likes, watch durations per clip) to the shared `UserReelInteraction` table, mapped to the authenticated user's ID and the specific reel.
*   **Requirement 6:** The system must build and maintain the user's `UserProfile` engagement metrics (total likes, total watch time, average watch time, clips viewed, like-to-view ratio).
*   **Requirement 7:** The system must utilize a recommendation algorithm that consumes the user's `UserMoviePreference` scores to serve the most relevant clips in the feed; for new users with no preference data, it falls back to globally trending reels (most-liked in the last 7 days) or most recent.
*   **Requirement 8:** The system must pre-fetch nearby clips (buffering ±2 clips around the current position) to ensure a seamless, lag-free scrolling experience.
*   **Requirement 9:** The system must display contextual metadata overlaid on each clip (movie title, genre pill badge, caption, and a playback progress bar) using a semi-transparent gradient at the bottom, without obstructing the primary video content.
*   **Requirement 10:** User flow: The authenticated user opens the Reels Feed screen, and the first clip immediately begins auto-playing. The user can watch the clip, tap the heart icon to "like" it, or scroll vertically to seamlessly transition to the next/previous clip.
*   **Requirement 11:** If the user closes the application, the exact watch progress (the seconds watched on the currently playing clip if it hasn't been synchronized yet) is discarded. Any previously completed view aggregations and likes are already persisted to the database and will not be lost.
*   **Owner:** Tudor
*   **Cross-Team Dependencies:**
    *   **Alex:** Alex owns the `Reel` table schema — Tudor reads from it to populate the feed. Uploaded reels are the primary feed content.
    *   **Andrei:** Andrei writes scraped trailers to the `Reel` table — these also appear in Tudor's feed.
    *   **Beatrice:** Beatrice edits reels (crop + music) — Tudor's feed displays the edited versions.
    *   **Bogdan:** Bogdan owns the `UserMoviePreference` table and the shared `UserMoviePreferenceModel`. Tudor also writes to it on reel likes (boost +1.5) — reuse the same upsert logic.
    *   **Madi:** Madi reads from `UserProfile` (which Tudor owns) to display matched users' engagement stats.

---

### 2. Diagram Blueprint

*   **Use Case Diagram Additions:**
    *   **Actor:** Authenticated User
    *   **Use Cases:**
        *   `Browse Reels Feed` — User opens the Reels screen and views the auto-playing clip feed.
        *   `Scroll to Next/Previous Clip` — User vertically swipes to navigate between clips.
        *   `Like a Reel` — User taps the like button to mark/unmark a clip as liked.
        *   `Record View Interaction` — System tracks watch duration and timestamps automatically.
        *   `Update Engagement Profile` — System aggregates interaction data into `UserProfile`.
        *   `Boost Movie Preference on Like` — System updates `UserMoviePreference` score when a reel is liked.
        *   `Generate Personalized Feed` — System uses the recommendation algorithm to order clips.

*   **Database Schema Additions:**
    *   *(This feature does NOT create new tables. It reads/writes to the shared tables defined in the unified schema.)*
    *   **Shared Table: `Reel`** — Source of clips displayed in the feed (both uploaded and scraped).
    *   **Shared Table: `UserReelInteraction`** (InteractionId BIGINT IDENTITY PK, UserId INT NOT NULL, ReelId INT NOT NULL FK→Reel, IsLiked BIT NOT NULL DEFAULT 0, WatchDurationSec FLOAT NOT NULL DEFAULT 0, WatchPercentage FLOAT NOT NULL DEFAULT 0, ViewedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), UNIQUE(UserId, ReelId)) — Stores every view/like event.
    *   **Shared Table: `UserProfile`** (UserProfileId INT IDENTITY PK, UserId INT NOT NULL UNIQUE, TotalLikes INT DEFAULT 0, TotalWatchTimeSec BIGINT DEFAULT 0, AvgWatchTimeSec FLOAT DEFAULT 0, TotalClipsViewed INT DEFAULT 0, LikeToViewRatio FLOAT DEFAULT 0, LastUpdated DATETIME2 DEFAULT SYSUTCDATETIME()) — Cached engagement metrics.
    *   **Shared Table: `UserMoviePreference`** — Score is boosted when a user likes a reel linked to a movie.

*   **Class Diagram (MVVM) Additions:**
    *   *Models:* `ReelModel` (extends `ObservableObject`, includes observable `IsLiked`/`LikeCount` client-side state), `UserReelInteractionModel`, `UserProfileModel`
    *   *Views:* `ReelsFeedPage` (full-screen FlipView-based scrollable feed with loading/error/empty states), `ReelItemView` (single clip card with `MediaPlayerElement`, like button with bounce animation, double-tap heart burst, bottom gradient overlay with metadata and progress bar)
    *   *ViewModels:* `ReelsFeedViewModel` (manages `ReelQueue`, `CurrentReel`, scroll commands, watch-time tracking via `Stopwatch`, prefetching), `UserProfileViewModel` (loads and exposes engagement metrics with loading/error state)
    *   *Repositories:*
        *   `IInteractionRepository` / `InteractionRepository` — CRUD for `UserReelInteraction` (upsert, toggle like, update view data, get like count, get reel movie ID)
        *   `IProfileRepository` / `ProfileRepository` — Get/upsert `UserProfile`
        *   `IPreferenceRepository` / `PreferenceRepository` — Boosts `UserMoviePreference.Score` by +1.5 on reel like
    *   *Services:*
        *   `IClipPlaybackService` / `ClipPlaybackService` — Manages video player lifecycle (`PlayAsync`, `PauseAsync`, `ResumeAsync`, `SeekAsync`, `PrefetchClipAsync`, `GetMediaSource`); caches `MediaSource` objects; implements `IDisposable`; exposes `IsPlaying` property
        *   `IReelInteractionService` / `ReelInteractionService` — Orchestrates like toggling (with preference boost on like), view recording, like count retrieval; depends on `IInteractionRepository` and `IPreferenceRepository`
        *   `IEngagementProfileService` / `EngagementProfileService` — Aggregates raw `UserReelInteraction` data via SQL into `UserProfile` metrics (`RefreshProfileAsync`); depends on `IProfileRepository` and `ISqlConnectionFactory`
        *   `IRecommendationService` / `RecommendationService` — Two-path algorithm: warm (personalized by `UserMoviePreference` scores with recency tiebreaker) and cold-start (trending/recent reels); depends on `ISqlConnectionFactory`

---

### 3. Project Management Tasks (Max 30-Minutes Each)

**Database & Models**

*   **Task 1:** Define Database Schemas (`UserReelInteraction` + `UserProfile`)
    *   **Description:** Design and create the database schemas in `DatabaseInitializer.cs` for both Tudor-owned tables. `UserReelInteraction`: `InteractionId` (BIGINT IDENTITY PK), `UserId` (INT NOT NULL), `ReelId` (INT NOT NULL, FK → Reel), `IsLiked` (BIT NOT NULL DEFAULT 0), `WatchDurationSec` (FLOAT NOT NULL DEFAULT 0), `WatchPercentage` (FLOAT NOT NULL DEFAULT 0), `ViewedAt` (DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()), with a UNIQUE constraint on (`UserId`, `ReelId`). `UserProfile`: `UserProfileId` (INT IDENTITY PK), `UserId` (INT NOT NULL UNIQUE), `TotalLikes` (INT DEFAULT 0), `TotalWatchTimeSec` (BIGINT DEFAULT 0), `AvgWatchTimeSec` (FLOAT DEFAULT 0), `TotalClipsViewed` (INT DEFAULT 0), `LikeToViewRatio` (FLOAT DEFAULT 0), `LastUpdated` (DATETIME2 DEFAULT SYSUTCDATETIME()). Seed `UserId = 1` into `UserProfile`. Max 30 mins effort.

*   **Task 2:** Create Data Model Classes (`UserReelInteractionModel` + `UserProfileModel`)
    *   **Description:** Define the `UserReelInteractionModel` (properties: `InteractionId: long`, `UserId: int`, `ReelId: int`, `IsLiked: bool`, `WatchDurationSec: double`, `WatchPercentage: double`, `ViewedAt: DateTime`) and `UserProfileModel` (properties: `UserProfileId: int`, `UserId: int`, `TotalLikes: int`, `TotalWatchTimeSec: long`, `AvgWatchTimeSec: double`, `TotalClipsViewed: int`, `LikeToViewRatio: double`, `LastUpdated: DateTime`). Extend the shared `ReelModel` (from **Alex**) with observable client-side properties `IsLiked: bool` and `LikeCount: int`, and add `Genre: string?` and rename `DurationSeconds` to `FeatureDurationSeconds: double`. Max 30 mins effort.

*   **Task 3:** Implement Repository Layer (`IInteractionRepository`, `IProfileRepository`, `IPreferenceRepository`)
    *   **Description:** Implement three repository interfaces and their concrete classes: (a) `IInteractionRepository` / `InteractionRepository` with methods `InsertInteractionAsync`, `UpsertInteractionAsync`, `ToggleLikeAsync`, `UpdateViewDataAsync`, `GetInteractionAsync`, `GetLikeCountAsync`, `GetReelMovieIdAsync` — all using raw parameterized SQL against `UserReelInteraction`. (b) `IProfileRepository` / `ProfileRepository` with `GetProfileAsync` and `UpsertProfileAsync` (IF EXISTS update/ELSE insert pattern). (c) `IPreferenceRepository` / `PreferenceRepository` with `BoostPreferenceOnLikeAsync(userId, movieId)` that upserts +1.5 to `UserMoviePreference.Score`. Max 30 mins effort.

---

**Backend Services & ViewModels**

*   **Task 4:** Define & Implement `ReelInteractionService`
    *   **Description:** Create the `IReelInteractionService` interface with methods: `ToggleLikeAsync(int userId, int reelId)`, `RecordViewAsync(int userId, int reelId, double watchDurationSec, double watchPercentage)`, `GetInteractionAsync(int userId, int reelId) → UserReelInteractionModel?`, `GetLikeCountAsync(int reelId) → int`. Implement `ReelInteractionService` depending on `IInteractionRepository` and `IPreferenceRepository`. `ToggleLikeAsync` delegates to `InteractionRepository.ToggleLikeAsync`, then looks up the reel's `MovieId` via `GetReelMovieIdAsync` and calls `PreferenceRepository.BoostPreferenceOnLikeAsync` to boost `UserMoviePreference` by +1.5. `RecordViewAsync` delegates to `UpdateViewDataAsync`. Max 30 mins effort.

*   **Task 5:** Define & Implement `EngagementProfileService`
    *   **Description:** Create the `IEngagementProfileService` interface with methods: `GetProfileAsync(int userId) → UserProfileModel?`, `RefreshProfileAsync(int userId)`. Implement `EngagementProfileService` depending on `IProfileRepository` and `ISqlConnectionFactory`. `RefreshProfileAsync` runs a raw SQL aggregation query against `UserReelInteraction` (total likes, total/avg watch time, clips viewed, like-to-view ratio) and persists the result via `IProfileRepository.UpsertProfileAsync`. Max 30 mins effort.

*   **Task 6:** Define & Implement `RecommendationService` (Algorithm + Cold Start)
    *   **Description:** Create the `IRecommendationService` interface with method: `GetRecommendedReelsAsync(int userId, int count) → IList<ReelModel>`. Implement `RecommendationService` depending on `ISqlConnectionFactory`. Two-path algorithm: (1) **Warm path** — if the user has `UserMoviePreference` rows, query unwatched reels via LEFT JOIN on `UserMoviePreference` and `Movie`, scoring by preference score with recency tiebreaker. (2) **Cold-start path** — no preferences → serve globally trending reels (most-liked in last 7 days), falling back to most recent. Genre is retrieved from the `Movie` table via JOIN (not stored on `Reel`). Max 30 mins effort.

*   **Task 7:** Define & Implement `ClipPlaybackService`
    *   **Description:** Create the `IClipPlaybackService` interface with methods: `PlayAsync(string videoUrl)`, `PauseAsync()`, `ResumeAsync()`, `SeekAsync(double positionSeconds)`, `GetElapsedSeconds() → double`, `PrefetchClipAsync(string videoUrl)`, and property `IsPlaying: bool`. Implement `ClipPlaybackService` (also implements `IDisposable`) using a `Dictionary<string, MediaSource>` cache for prefetched clips and a `Stopwatch` for elapsed time. Provide a `GetMediaSource(string videoUrl) → MediaSource` method for views to retrieve cached sources. Max 30 mins effort.

*   **Task 8:** Scaffold `ReelsFeedViewModel` & Load Initial Feed
    *   **Description:** Create the `ReelsFeedViewModel` class (extends `ObservableObject` via CommunityToolkit.Mvvm) with observable properties: `PageTitle` (string, default "Reels Feed"), `StatusMessage` (string), `IsLoading` (bool), `ErrorMessage` (string?), `IsEmpty` (bool), `CurrentReel` (ReelModel?). Expose `ReelQueue` (ObservableCollection<ReelModel>) and computed `HasError` property. Implement `LoadFeedAsync` (RelayCommand) that calls `IRecommendationService.GetRecommendedReelsAsync(MockUserId: 1, count: 10)`, populates `ReelQueue`, loads like data for each reel, and sets `CurrentReel` to the first item. Max 30 mins effort.

*   **Task 9:** Implement `ReelsFeedViewModel` — Navigation Commands
    *   **Description:** Implement `ScrollNext(ReelModel newCurrent)` and `ScrollPrevious(ReelModel newCurrent)` as RelayCommands. Both flush the current reel's watch data before switching, set the new `CurrentReel`, and call `PrefetchNearby` for ±2 surrounding clips. Like toggling is handled directly in `ReelItemView` code-behind (not the ViewModel) via `IReelInteractionService.ToggleLikeAsync()` with optimistic UI updates and rollback on failure. Max 30 mins effort.

*   **Task 10:** Implement `ReelsFeedViewModel` — Watch Tracking, Prefetch & Error Handling
    *   **Description:** Add a `Stopwatch` that starts when a reel becomes `CurrentReel` and stops on scroll-away via `FlushWatchData()`, calling `RecordViewAsync()` with elapsed seconds and calculated watch percentage (elapsed / reel duration). Implement `OnNavigatingAway()` to flush the final reel's data on page unload. Implement `PrefetchNearby(currentIndex)` to signal `ClipPlaybackService.PrefetchClipAsync` for ±2 surrounding reels. Add try-catch wrappers around all service calls — on failure, set `ErrorMessage` (which triggers `HasError` computed property); ensure feed stays navigable. Max 30 mins effort.

---

**GUI (Views)**

*   **Task 11:** Create `ReelsFeedPage` Layout & `ReelItemView` Video Container
    *   **Description:** Create the `ReelsFeedPage` base UI layout: full-screen black background with a `FlipView` using a vertical `VirtualizingStackPanel` for snap-scroll behavior. `ItemsSource` bound to `ReelQueue`, `SelectedItem` bound to `CurrentReel` (TwoWay). `SelectionChanged` triggers `ScrollNext` and playback orchestration via `TriggerPlaybackForCurrent()` (iterates realized containers, plays current clip, pauses others using `FindVisualChild<T>()` helper). Design the `ReelItemView` filling 100% viewport with a `MediaPlayerElement` (`AreTransportControlsEnabled="False"`, `Stretch="UniformToFill"`). Max 30 mins effort.

*   **Task 12:** Design `ReelItemView` — Overlays, Like Button & Gestures
    *   **Description:** Add a semi-transparent black gradient overlay at the bottom with: movie title (bold, white, 22pt), genre pill badge (semi-transparent red, 12pt, visibility bound to non-null genre), caption (white, 14pt), and a 3px playback progress bar (white foreground, semi-transparent background) updated via a 250ms `DispatcherTimer` reading `MediaPlayer.PlaybackSession.Position`. Place a heart icon button on the right side (outline white when unliked, filled red when liked) with `ScaleTransform` bounce animation (1.0 → 1.4 → 1.0, 300ms). Add a double-tap gesture recognizer that only likes (never unlikes) and plays a large centered heart burst animation (opacity 0→1→0, scale 0.5→1.3→1.5, 600ms). Like toggle calls `IReelInteractionService.ToggleLikeAsync()` with optimistic UI and rollback on failure. Max 30 mins effort.

*   **Task 13:** Implement Snap-Scroll Behavior, Data Binding & Media Lifecycle
    *   **Description:** Configure the `FlipView.SelectionChanged` event to fire `ScrollNext`/`ScrollPrevious` commands and orchestrate playback. Wire data bindings: `FlipView.ItemsSource` → `ReelQueue`, `FlipView.SelectedItem` → `CurrentReel`, per-item like button state bound to `ReelModel.IsLiked` and `ReelModel.LikeCount`. Implement `ReelItemView.OnReelChanged()` DependencyProperty callback: disposes previous `MediaPlayer`, sets new `MediaSource` from `ClipPlaybackService.GetMediaSource()`, hooks `MediaEnded`, and subscribes to `PropertyChanged` for like state. Implement `DisposeCurrentPlayer()` and `DisposeMediaPlayer()` for COM object cleanup. Set static `ReelItemView.IsAppClosing` flag from `MainWindow.Closed`. Max 30 mins effort.

*   **Task 14:** Design Loading & Error State UI
    *   **Description:** Add a centered circular loading spinner displayed when `IsLoading` is true (hidden when clips are ready). Create an error layout (icon + message + "Retry" button) displayed when `ErrorMessage` is non-empty, with Retry bound to `LoadFeedCommand`. Max 30 mins effort.

*   **Task 15:** Design Empty State UI & `UserProfileViewModel`
    *   **Description:** Create an empty/cold-start layout (icon + "No clips yet" message + "Refresh" button) shown when `IsEmpty` is true. Scaffold the `UserProfileViewModel` (extends `ObservableObject`) with observable properties: `Profile` (UserProfileModel?), `IsLoading` (bool), `ErrorMessage` (string?). Implement `LoadProfileAsync(int userId)` as a RelayCommand that calls `RefreshProfileAsync` then `GetProfileAsync` to populate `Profile` (read by **Madi** for matched user details). Max 30 mins effort.
