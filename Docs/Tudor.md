### 1. Formal Requirements
*   **Requirement 1:** The system must present an authenticated user with a vertically-scrollable, full-screen "Reels" feed of short movie clips, similar to Instagram Reels or TikTok.
*   **Requirement 2:** The system must auto-play the currently visible clip when a user lands on it.
*   **Requirement 3:** The system must allow the user to "like" the currently displayed clip by tapping a heart icon, toggling the liked state on and off.
*   **Requirement 4:** The system must allow the user to vertically swipe/scroll to navigate to the next or previous clip, with smooth snap-to-clip scrolling behavior.
*   **Requirement 5:** The system must persistently record every user interaction (likes, watch durations per clip) to the shared `UserReelInteraction` table, mapped to the authenticated user's ID and the specific reel.
*   **Requirement 6:** The system must build and maintain the user's `UserProfile` engagement metrics (total likes, total watch time, average watch time, clips viewed, like-to-view ratio).
*   **Requirement 7:** The system must utilize a recommendation algorithm that consumes the user's engagement profile to serve the most relevant clips in the feed.
*   **Requirement 8:** The system must pre-fetch upcoming clips (buffering the next 3 clips) to ensure a seamless, lag-free scrolling experience.
*   **Requirement 9:** The system must display contextual metadata overlaid on each clip (movie title, genre tag, clip duration) without obstructing the primary video content.
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

*   **Task 1:** Define Database Schemas (`UserReelInteraction` + `UserProfile`)
    *   **Description:** Design and create the database migrations for both Tudor-owned tables. `UserReelInteraction`: `InteractionId` (PK), `UserId` (FK → User), `ReelId` (FK → Reel), `IsLiked` (BOOLEAN, default false), `WatchDurationSec` (INT, nullable), `WatchPercentage` (FLOAT, nullable), `ViewedAt` (DATETIME), with a UNIQUE constraint on (`UserId`, `ReelId`). `UserProfile`: `UserProfileId` (PK), `UserId` (FK → User, UNIQUE), `TotalLikes` (INT), `TotalWatchTimeSec` (BIGINT), `AvgWatchTimeSec` (FLOAT), `TotalClipsViewed` (INT), `LikeToViewRatio` (FLOAT), `LastUpdated` (DATETIME). Define all constraints and foreign keys for both tables. Max 30 mins effort.

*   **Task 2:** Create Data Model Classes (`UserReelInteractionModel` + `UserProfileModel`)
    *   **Description:** Define the `UserReelInteractionModel` (properties: `InteractionId`, `UserId`, `ReelId`, `IsLiked`, `WatchDurationSec`, `WatchPercentage`, `ViewedAt`) and `UserProfileModel` (properties: `UserProfileId`, `UserId`, `TotalLikes`, `TotalWatchTimeSec`, `AvgWatchTimeSec`, `TotalClipsViewed`, `LikeToViewRatio`, `LastUpdated`). Also integrate the shared `ReelModel` (from **Alex**) and `UserMoviePreferenceModel` (from **Bogdan**) into the project references. Max 30 mins effort.

*   **Task 3:** Implement Data Access Queries & Preference Boost
    *   **Description:** Write the repository methods for: (a) inserting/upserting a `UserReelInteraction` record on repeat views, (b) aggregating engagement stats from `UserReelInteraction` for a given user (`COUNT(*)`, `SUM(WatchDurationSec)`, `AVG(WatchDurationSec)`, `IsLiked` count, `LikeToViewRatio`), and (c) upserting a `UserMoviePreference` row to boost the associated movie's Score when a reel linked to a movie is liked. Max 30 mins effort.

---

**Backend Services & ViewModels**

*   **Task 4:** Define & Implement `ReelInteractionService`
    *   **Description:** Create the `IReelInteractionService` interface (`RecordViewAsync`, `ToggleLikeAsync`, `GetInteractionAsync`) and its concrete implementation. `RecordViewAsync` creates a new `UserReelInteraction` DB record with watch duration and percentage. `ToggleLikeAsync` flips `IsLiked` on an existing interaction (or creates a new one with `IsLiked = true`) and calls the preference boost method to update `UserMoviePreference`. Max 30 mins effort.

*   **Task 5:** Define & Implement `EngagementProfileService`
    *   **Description:** Create the `IEngagementProfileService` interface (`RecalculateProfileAsync`, `GetProfileAsync`) and its concrete implementation. `RecalculateProfileAsync` fetches all `UserReelInteraction` rows for a user, computes aggregate stats (total likes, total/avg watch time, clips viewed, like-to-view ratio), and persists the result to the `UserProfile` table. Max 30 mins effort.

*   **Task 6:** Define & Implement `RecommendationService` (Algorithm + Cold Start)
    *   **Description:** Create the `IRecommendationService` interface (`GetRecommendedReelsAsync(userId, count)`) and its concrete implementation. The basic algorithm fetches the user's `UserProfile` and top `UserMoviePreference` scores, queries the `Reel` table excluding already-viewed reels, scores remaining reels by genre/movie match, and returns the top 10 reels. Includes a cold-start fallback for new users with no engagement data — serve globally popular/trending reels (most-liked from the last 7 days). Max 30 mins effort.

*   **Task 7:** Define & Implement `ClipPlaybackService`
    *   **Description:** Create the `IClipPlaybackService` interface (`PlayClip(videoUrl)`, `PauseClip()`, `ResumeClip()`, `GetElapsedSeconds()`, `PrefetchClip(videoUrl)`) and its concrete implementation wrapping the platform's native video player API. Wire play, pause, and prefetch operations. Max 30 mins effort.

*   **Task 8:** Scaffold `ReelsFeedViewModel` & Load Initial Feed
    *   **Description:** Create the `ReelsFeedViewModel` class with observable properties: `CurrentReel` (ReelModel), `ReelQueue` (ObservableCollection), `IsLoading` (bool), `IsCurrentReelLiked` (bool), `ErrorMessage` (string). Implement `LoadFeedCommand` that calls `IRecommendationService`, populates `ReelQueue`, and sets `CurrentReel` to the first item. Max 30 mins effort.

*   **Task 9:** Implement `ReelsFeedViewModel` — Navigation & Interaction Commands
    *   **Description:** Implement `ScrollNextCommand` (advance `CurrentReel` to next in queue, trigger background fetch when ≤ 2 remaining), `ScrollPreviousCommand` (maintain a history stack of last 5 clips for backward navigation), and `ToggleLikeCommand` (call `IReelInteractionService.ToggleLikeAsync()`, flip `IsCurrentReelLiked`, trigger heart animation flag). Max 30 mins effort.

*   **Task 10:** Implement `ReelsFeedViewModel` — Watch Tracking, Prefetch & Error Handling
    *   **Description:** Add a timer that starts when a reel becomes `CurrentReel` and stops on scroll-away, calling `RecordViewAsync()` with elapsed seconds and watch percentage. Implement prefetching: when user watches the current reel, signal `ClipPlaybackService` to buffer the next 3 upcoming reels (expose `PrefetchStatus`). Add try-catch wrappers around all service calls — on failure, set `ErrorMessage` and log; ensure feed stays navigable. Max 30 mins effort.

---

**GUI (Views)**

*   **Task 11:** Create `ReelsFeedView` Layout & `ReelItemView` Video Container
    *   **Description:** Create the `ReelsFeedView` base UI layout: full-screen container (immersive mode), single vertical snap-scroll container, black background. Design the `ReelItemView` single clip layout filling 100% viewport with a video player that stretches edge-to-edge and scales to "cover". Max 30 mins effort.

*   **Task 12:** Design `ReelItemView` — Overlays, Like Button & Gestures
    *   **Description:** Add a semi-transparent gradient overlay at bottom with movie title (bold, white) and genre tag (pill badge) bottom-left. Place a heart icon button on the right side (outline when unliked, filled red when liked, scale-bounce animation on tap). Add a double-tap gesture recognizer that triggers `ToggleLikeCommand` and plays a heart burst animation at the tap point. Add a thin horizontal progress bar at bottom bound to playback position. Max 30 mins effort.

*   **Task 13:** Implement Snap-Scroll Behavior & Data Binding
    *   **Description:** Configure snap-to-page vertical scrolling — when a new clip snaps into view, fire `ScrollNextCommand` or `ScrollPreviousCommand`. Wire all data bindings: scroll container item source to `ReelQueue`, visible clip to `CurrentReel`, like button to `IsCurrentReelLiked`. Max 30 mins effort.

*   **Task 14:** Design Loading & Error State UI
    *   **Description:** Add a centered circular loading spinner displayed when `IsLoading` is true (hidden when clips are ready). Create an error layout (icon + message + "Retry" button) displayed when `ErrorMessage` is non-empty, with Retry bound to `LoadFeedCommand`. Max 30 mins effort.

*   **Task 15:** Design Empty State UI & `UserProfileViewModel`
    *   **Description:** Create an empty/cold-start layout ("No clips yet" illustration + message) shown when the feed returns zero clips. Scaffold the `UserProfileViewModel` that exposes the user's engagement metrics via `IEngagementProfileService` (read by **Madi** for matched user details). Max 30 mins effort.
