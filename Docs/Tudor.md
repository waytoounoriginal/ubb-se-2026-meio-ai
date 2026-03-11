### 1. Formal Requirements
*   **Requirement 1:** The system must present an authenticated user with a vertically-scrollable, full-screen "Reels" feed of short movie clips, similar to Instagram Reels or TikTok.
*   **Requirement 2:** The system must auto-play the currently visible clip when a user lands on it, and pause it when the user scrolls away.
*   **Requirement 3:** The system must allow the user to "like" the currently displayed clip by tapping a heart icon, toggling the liked state on and off.
*   **Requirement 4:** The system must allow the user to vertically swipe/scroll to navigate to the next or previous clip, with smooth snap-to-clip scrolling behavior.
*   **Requirement 5:** The system must persistently record every user interaction (likes, watch durations per clip, scroll-aways) to the shared `UserReelInteraction` table, mapped to the authenticated user's ID and the specific reel.
*   **Requirement 6:** The system must build and maintain the user's `UserProfile` engagement metrics (total likes, total watch time, average watch time, clips viewed, like-to-view ratio).
*   **Requirement 7:** The system must utilize a recommendation algorithm that consumes the user's engagement profile to serve the most relevant clips in the feed.
*   **Requirement 8:** When a user likes a reel that is linked to a movie, the system must also boost that movie's score in the shared `UserMoviePreference` table.
*   **Requirement 9:** The system must pre-fetch upcoming clips (buffering the next N clips) to ensure a seamless, lag-free scrolling experience.
*   **Requirement 10:** The system must display contextual metadata overlaid on each clip (movie title, genre tag, clip duration) without obstructing the primary video content.
*   **Owner:** Tudor
*   **Cross-Team Dependencies:**
    *   **Alex:** Alex owns the `Reel` table schema — Tudor reads from it to populate the feed. Uploaded reels are the primary feed content.
    *   **Andrei:** Andrei writes scraped trailers to the `Reel` table — these also appear in Tudor's feed.
    *   **Beatrice:** Beatrice edits reels (crop + music) — Tudor's feed displays the edited versions.
    *   **Bogdan:** Bogdan owns the `UserMoviePreference` table schema — Tudor also writes to it when a reel is liked (boosting the linked movie's score).
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
    *   **Shared Table: `UserReelInteraction`** (InteractionId PK, UserId FK, ReelId FK, IsLiked BOOLEAN, WatchDurationSec INT, WatchPercentage FLOAT, ViewedAt DATETIME) — Stores every view/like event.
    *   **Shared Table: `UserProfile`** (UserProfileId PK, UserId FK UNIQUE, TotalLikes, TotalWatchTimeSec, AvgWatchTimeSec, TotalClipsViewed, LikeToViewRatio, LastUpdated) — Cached engagement metrics.
    *   **Shared Table: `UserMoviePreference`** — Score is boosted when a user likes a reel linked to a movie.

*   **Class Diagram (MVVM) Additions:**
    *   *Models:* `ReelModel`, `UserReelInteractionModel`, `UserProfileModel`
    *   *Views:* `ReelsFeedView` (the full-screen scrollable feed), `ReelItemView` (single clip card with video player + overlay)
    *   *ViewModels:* `ReelsFeedViewModel` (manages clip queue, scroll state, prefetching, like toggling), `UserProfileViewModel` (exposes engagement metrics)
    *   *Utils/Services:*
        *   `ClipPlaybackService` — Manages video player lifecycle (play, pause, buffer)
        *   `ReelInteractionService` — Records likes, view durations to `UserReelInteraction` and updates `UserMoviePreference`
        *   `EngagementProfileService` — Computes/recalculates the user's `UserProfile` from raw interaction data
        *   `RecommendationService` — Consumes the engagement profile and returns an ordered list of recommended reels

---

### 3. Project Management Tasks (Max 30-Minutes Each)

**Database & Models**

*   **Task:** Define `UserReelInteraction` Table Schema
    *   **Description:** Design and create the database migration for the `UserReelInteraction` table with columns: `InteractionId` (PK), `UserId` (FK → User), `ReelId` (FK → Reel), `IsLiked` (BOOLEAN, default false), `WatchDurationSec` (INT, nullable), `WatchPercentage` (FLOAT, nullable), `ViewedAt` (DATETIME). Define all constraints and foreign keys. Max 30 mins effort.

*   **Task:** Define `UserProfile` Table Schema
    *   **Description:** Design and create the database migration for the `UserProfile` table with columns: `UserProfileId` (PK), `UserId` (FK → User, UNIQUE), `TotalLikes` (INT), `TotalWatchTimeSec` (BIGINT), `AvgWatchTimeSec` (FLOAT), `TotalClipsViewed` (INT), `LikeToViewRatio` (FLOAT), `LastUpdated` (DATETIME). Max 30 mins effort.

*   **Task:** Create `ReelModel` Data Class
    *   **Description:** Define the Model class `ReelModel` mirroring the shared `Reel` table. Properties: `ReelId`, `MovieId`, `CreatorUserId`, `VideoUrl`, `ThumbnailUrl`, `Title`, `Caption`, `DurationSeconds`, `Source`, `CreatedAt`. Max 30 mins effort.

*   **Task:** Create `UserReelInteractionModel` Data Class
    *   **Description:** Define the Model class `UserReelInteractionModel` with properties: `InteractionId`, `UserId`, `ReelId`, `IsLiked`, `WatchDurationSec`, `WatchPercentage`, `ViewedAt`. Max 30 mins effort.

*   **Task:** Create `UserProfileModel` Data Class
    *   **Description:** Define the Model class `UserProfileModel` with properties: `UserProfileId`, `UserId`, `TotalLikes`, `TotalWatchTimeSec`, `AvgWatchTimeSec`, `TotalClipsViewed`, `LikeToViewRatio`, `LastUpdated`. Max 30 mins effort.

*   **Task:** Create `UserMoviePreferenceModel` Data Class
    *   **Description:** Define the Model class mirroring the shared `UserMoviePreference` table: `UserMoviePreferenceId`, `UserId`, `MovieId`, `Score`, `LastModified`. Needed for cross-updating preference scores on reel likes. Max 30 mins effort.

*   **Task:** Design Interaction Insertion Query
    *   **Description:** Plan the query/ORM mapping for inserting a new `UserReelInteraction` record. Handle upsert for repeat views of the same reel. Max 30 mins effort.

*   **Task:** Design Engagement Profile Aggregation Query
    *   **Description:** Plan the aggregation query on `UserReelInteraction` for a given `UserId`: `COUNT(*)`, `SUM(WatchDurationSec)`, `AVG(WatchDurationSec)`, count of `IsLiked = true`, and `LikeToViewRatio`. Max 30 mins effort.

*   **Task:** Implement Preference Score Boost on Reel Like
    *   **Description:** Write a repository method that upserts a `UserMoviePreference` row when a reel linked to a movie is liked — boosting the associated movie's Score. Max 30 mins effort.

---

**Backend Services & ViewModels**

*   **Task:** Define `IReelInteractionService` Interface
    *   **Description:** Create the service interface with methods: `RecordViewAsync(userId, reelId, watchDuration, watchPercentage)`, `ToggleLikeAsync(userId, reelId)`, `GetInteractionAsync(userId, reelId)`. Max 30 mins effort.

*   **Task:** Implement `ReelInteractionService` — `RecordViewAsync`
    *   **Description:** Implement the concrete method to create a new `UserReelInteraction` DB record with watch duration and percentage. Max 30 mins effort.

*   **Task:** Implement `ReelInteractionService` — `ToggleLikeAsync`
    *   **Description:** Implement like toggling. Flip `IsLiked` on existing interaction, or create a new one with `IsLiked = true`. Also call the preference service to boost the associated movie's score in `UserMoviePreference`. Max 30 mins effort.

*   **Task:** Define `IEngagementProfileService` Interface
    *   **Description:** Create the service interface with methods: `RecalculateProfileAsync(userId)` and `GetProfileAsync(userId)` returning `UserProfileModel`. Max 30 mins effort.

*   **Task:** Implement `EngagementProfileService` — Profile Computation
    *   **Description:** Implement `RecalculateProfileAsync`. Fetch all `UserReelInteraction` rows for the user, compute aggregate stats, persist to `UserProfile`. Max 30 mins effort.

*   **Task:** Define `IRecommendationService` Interface
    *   **Description:** Create the service interface with method: `GetRecommendedReelsAsync(userId, count)` returning a list of `ReelModel`. Max 30 mins effort.

*   **Task:** Implement `RecommendationService` — Basic Algorithm
    *   **Description:** Fetch the user's `UserProfile` and `UserMoviePreference` top scores, query the `Reel` table excluding already-viewed reels, score remaining reels by genre/movie match. Return top N by score. Max 30 mins effort.

*   **Task:** Implement `RecommendationService` — Cold Start Fallback
    *   **Description:** Handle new users with no engagement data. Serve a default list of popular/trending reels (globally most-liked from the last 7 days). Max 30 mins effort.

*   **Task:** Scaffold `ReelsFeedViewModel`
    *   **Description:** Create the ViewModel class. Define observable properties: `CurrentReel` (ReelModel), `ReelQueue` (ObservableCollection), `IsLoading` (bool), `IsCurrentReelLiked` (bool). Max 30 mins effort.

*   **Task:** Implement `ReelsFeedViewModel` — Load Initial Feed
    *   **Description:** Create `LoadFeedCommand` that calls `IRecommendationService`, populates `ReelQueue`, sets `CurrentReel` to the first item. Max 30 mins effort.

*   **Task:** Implement `ReelsFeedViewModel` — Scroll Next Logic
    *   **Description:** Implement `ScrollNextCommand`. Advance `CurrentReel` to next in queue. When queue has ≤ 2 remaining, trigger background fetch for more. Max 30 mins effort.

*   **Task:** Implement `ReelsFeedViewModel` — Scroll Previous Logic
    *   **Description:** Implement `ScrollPreviousCommand`. Maintain a small history stack (last 5 clips) for backward navigation. Max 30 mins effort.

*   **Task:** Implement `ReelsFeedViewModel` — Like Toggle Command
    *   **Description:** Create `ToggleLikeCommand`. Call `IReelInteractionService.ToggleLikeAsync()`, flip `IsCurrentReelLiked`, trigger heart animation flag. Max 30 mins effort.

*   **Task:** Implement `ReelsFeedViewModel` — Watch Time Tracking
    *   **Description:** Add a timer that starts when a reel becomes `CurrentReel` and stops on scroll-away. Call `RecordViewAsync()` with elapsed seconds and watch percentage. Max 30 mins effort.

*   **Task:** Implement `ReelsFeedViewModel` — Prefetch Buffer
    *   **Description:** When user watches reel N, signal `ClipPlaybackService` to buffer reel N+1 and N+2. Expose `PrefetchStatus` property. Max 30 mins effort.

*   **Task:** Define `IClipPlaybackService` Interface
    *   **Description:** Define interface with methods: `PlayClip(videoUrl)`, `PauseClip()`, `ResumeClip()`, `GetElapsedSeconds()`, `PrefetchClip(videoUrl)`. Max 30 mins effort.

*   **Task:** Implement `ClipPlaybackService` Concrete Class
    *   **Description:** Implement wrapping the platform's native video player API. Wire play, pause, and prefetch operations. Max 30 mins effort.

*   **Task:** Implement ViewModel Error Handling
    *   **Description:** Add try-catch wrappers around all service calls. On failure, set `ErrorMessage` property and log. Ensure feed stays navigable. Max 30 mins effort.

---

**GUI (Views)**

*   **Task:** Create `ReelsFeedView` Skeleton Layout
    *   **Description:** Create the base UI layout. Full-screen container (immersive mode), single vertical snap-scroll container, black background. Max 30 mins effort.

*   **Task:** Design `ReelItemView` — Video Player Container
    *   **Description:** Design a single clip layout filling 100% viewport. Video player stretches edge-to-edge, scales to "cover". Max 30 mins effort.

*   **Task:** Design `ReelItemView` — Metadata Overlay
    *   **Description:** Add semi-transparent gradient overlay at bottom. Place movie title (bold, white) and genre tag (pill badge) bottom-left. Max 30 mins effort.

*   **Task:** Design `ReelItemView` — Like Button & Animation
    *   **Description:** Place heart icon button on right side. Outline when unliked, filled red when liked. Scale-bounce animation on tap. Max 30 mins effort.

*   **Task:** Implement Double-Tap-to-Like Gesture
    *   **Description:** Add double-tap gesture recognizer. On double-tap, trigger `ToggleLikeCommand` and play a heart burst animation at tap point. Max 30 mins effort.

*   **Task:** Design `ReelItemView` — Progress Bar
    *   **Description:** Add thin horizontal progress bar at bottom showing playback progress. Bind to playback position. Max 30 mins effort.

*   **Task:** Implement Vertical Snap-Scroll Behavior
    *   **Description:** Configure snap-to-page scrolling. When a new clip snaps into view, fire `ScrollNextCommand` or `ScrollPreviousCommand`. Max 30 mins effort.

*   **Task:** Wire Data Binding — Feed View to ViewModel
    *   **Description:** Bind scroll container item source to `ReelQueue`, visible clip to `CurrentReel`, like button to `IsCurrentReelLiked`. Max 30 mins effort.

*   **Task:** Implement Loading Spinner
    *   **Description:** Add centered circular loader displayed when `IsLoading` is true. Hide when clips are ready. Max 30 mins effort.

*   **Task:** Design Error State UI
    *   **Description:** Create error layout (icon + message + "Retry" button) displayed when `ErrorMessage` is non-empty. Bind Retry to `LoadFeedCommand`. Max 30 mins effort.

*   **Task:** Design Empty State UI
    *   **Description:** Create empty/cold-start layout ("No clips yet" illustration + message) shown when feed returns zero clips. Max 30 mins effort.
