### 1. Formal Requirements
*   **Requirement 1:** The system must present an authenticated user with a vertically-scrollable, full-screen "Reels" feed of short movie clips, similar in mechanic to Instagram Reels or TikTok.
*   **Requirement 2:** The system must auto-play the currently visible movie clip when a user lands on it, and pause it when the user scrolls away.
*   **Requirement 3:** The system must allow the user to "like" the currently displayed movie clip by tapping a clearly visible like button (heart icon), toggling the liked state on and off.
*   **Requirement 4:** The system must allow the user to vertically swipe/scroll to navigate to the next or previous movie clip in the feed, with smooth snap-to-clip scrolling behavior.
*   **Requirement 5:** The system must persistently record every user interaction (likes, view durations per clip, scroll-aways) to a long-term database, mapped to the authenticated user's ID and the specific clip.
*   **Requirement 6:** The system must build and maintain a user engagement profile aggregating metrics such as: total liked clips, total watch time, average watch time per clip, most-watched genres, and like-to-view ratio.
*   **Requirement 7:** The system must utilize a recommendation algorithm that consumes the user's engagement profile metrics to determine and serve the most relevant movie clips in the feed, prioritizing clips aligned with the user's demonstrated preferences.
*   **Requirement 8:** The system must pre-fetch upcoming clips (buffering the next N clips) to ensure a seamless, lag-free scrolling experience.
*   **Requirement 9:** The system must display contextual metadata overlaid on each clip (movie title, genre tag, clip duration) without obstructing the primary video content.
*   **Owner:** Tudor
*   **Cross-Team Dependencies:**
    *   **UI Team:** Responsible for implementing the full-screen vertical snap-scroll feed, video playback controls, like button animations, and metadata overlay design.
    *   **Backend Logic Team:** Responsible for the recommendation algorithm that analyzes engagement metrics and determines clip ordering; also responsible for view-tracking and engagement profile computation logic.
    *   **Database Team:** Responsible for designing and maintaining the schema for clip metadata, user interaction logs (views, likes, watch durations), and the computed user engagement profile.

---

### 2. Diagram Blueprint

*   **Use Case Diagram Additions:**
    *   **Actor:** Authenticated User
    *   **Use Cases:**
        *   `Browse Movie Reels Feed` — User opens the Reels screen and views the auto-playing clip feed.
        *   `Scroll to Next/Previous Clip` — User vertically swipes to navigate between clips.
        *   `Like a Movie Clip` — User taps the like button to mark/unmark a clip as liked.
        *   `Record Clip View Interaction` — System tracks watch duration, completion %, and timestamps automatically.
        *   `Compute User Engagement Profile` — System aggregates interaction data into a user profile.
        *   `Generate Personalized Feed` — System uses the recommendation algorithm to order clips for the user.

*   **Database Schema Additions:**

    *   **Table: `MovieClip`**
        *   `ClipId` (PK, INT/UUID)
        *   `MovieId` (FK → Movie)
        *   `VideoUrl` (VARCHAR) — URL/path to the clip media file
        *   `ThumbnailUrl` (VARCHAR) — Preview image URL
        *   `DurationSeconds` (INT) — Length of the clip
        *   `GenreId` (FK → Genre)
        *   `Title` (VARCHAR) — Display title for the clip
        *   `CreatedAt` (DATETIME)

    *   **Table: `ClipInteraction`**
        *   `InteractionId` (PK, INT/UUID)
        *   `UserId` (FK → User)
        *   `ClipId` (FK → MovieClip)
        *   `IsLiked` (BOOLEAN) — Whether user has liked this clip
        *   `WatchDurationSeconds` (INT) — How many seconds the user actually watched
        *   `WatchPercentage` (FLOAT) — Fraction of clip watched (0.0–1.0)
        *   `ViewedAt` (DATETIME) — Timestamp of when this view event occurred

    *   **Table: `UserEngagementProfile`**
        *   `ProfileId` (PK, INT/UUID)
        *   `UserId` (FK → User, UNIQUE)
        *   `TotalLikes` (INT)
        *   `TotalWatchTimeSeconds` (BIGINT)
        *   `AvgWatchTimeSeconds` (FLOAT)
        *   `TotalClipsViewed` (INT)
        *   `LikeToViewRatio` (FLOAT)
        *   `TopGenresJson` (TEXT/JSON) — Ranked list of most-engaged genres with weights
        *   `LastUpdated` (DATETIME)

    *   **Relationships:**
        *   `ClipInteraction.UserId` → `User.UserId` (Many-to-One)
        *   `ClipInteraction.ClipId` → `MovieClip.ClipId` (Many-to-One)
        *   `MovieClip.MovieId` → `Movie.MovieId` (Many-to-One)
        *   `MovieClip.GenreId` → `Genre.GenreId` (Many-to-One)
        *   `UserEngagementProfile.UserId` → `User.UserId` (One-to-One)

*   **Class Diagram (MVVM) Additions:**
    *   *Models:* `MovieClipModel`, `ClipInteractionModel`, `UserEngagementProfileModel`, `GenreWeightModel`
    *   *Views:* `ReelsFeedView` (the full-screen scrollable clip feed), `ReelClipItemView` (a single clip card within the feed, containing video player + overlay)
    *   *ViewModels:* `ReelsFeedViewModel` (manages the clip queue, scroll state, prefetching, and like toggling), `UserEngagementProfileViewModel` (exposes the computed engagement metrics)
    *   *Utils/Services:*
        *   `ClipPlaybackService` — Manages video player lifecycle (play, pause, buffer)
        *   `ClipInteractionService` — Records likes, view durations, and scroll events to DB
        *   `EngagementProfileService` — Computes/recalculates the user's engagement profile from raw interaction data
        *   `RecommendationService` — Consumes the engagement profile and returns an ordered list of recommended `MovieClipModel` objects for the feed

---

### 3. Project Management Tasks (Max 30-Minutes Each)

**Database & Models**

*   **Task:** Define `MovieClip` Table Schema
    *   **Description:** Design the database table schema for `MovieClip`. Define columns: `ClipId` (PK), `MovieId` (FK), `VideoUrl`, `ThumbnailUrl`, `DurationSeconds`, `GenreId` (FK), `Title`, and `CreatedAt`. Specify data types, constraints (NOT NULL, defaults), and foreign key relationships to existing `Movie` and `Genre` tables. Max 30 mins effort.

*   **Task:** Define `ClipInteraction` Table Schema
    *   **Description:** Design the database table schema for `ClipInteraction`. Define columns: `InteractionId` (PK), `UserId` (FK), `ClipId` (FK), `IsLiked` (BOOLEAN, default false), `WatchDurationSeconds`, `WatchPercentage`, `ViewedAt`. Add a composite unique constraint on (`UserId`, `ClipId`, `ViewedAt`) to distinguish repeat views. Max 30 mins effort.

*   **Task:** Define `UserEngagementProfile` Table Schema
    *   **Description:** Design the table schema for `UserEngagementProfile`. Columns: `ProfileId` (PK), `UserId` (FK, UNIQUE), `TotalLikes`, `TotalWatchTimeSeconds`, `AvgWatchTimeSeconds`, `TotalClipsViewed`, `LikeToViewRatio`, `TopGenresJson` (JSON blob of ranked genres with weights), `LastUpdated`. Max 30 mins effort.

*   **Task:** Create `MovieClipModel` Data Class
    *   **Description:** Define the plain Model class `MovieClipModel` mirroring the `MovieClip` database table. Properties: `ClipId`, `MovieId`, `VideoUrl`, `ThumbnailUrl`, `DurationSeconds`, `GenreId`, `Title`, `CreatedAt`. Add constructors/getters/setters as per language conventions. Max 30 mins effort.

*   **Task:** Create `ClipInteractionModel` Data Class
    *   **Description:** Define the Model class `ClipInteractionModel` with properties: `InteractionId`, `UserId`, `ClipId`, `IsLiked`, `WatchDurationSeconds`, `WatchPercentage`, `ViewedAt`. Max 30 mins effort.

*   **Task:** Create `UserEngagementProfileModel` Data Class
    *   **Description:** Define the Model class `UserEngagementProfileModel` with properties: `ProfileId`, `UserId`, `TotalLikes`, `TotalWatchTimeSeconds`, `AvgWatchTimeSeconds`, `TotalClipsViewed`, `LikeToViewRatio`, `TopGenres` (list of `GenreWeightModel`), `LastUpdated`. Max 30 mins effort.

*   **Task:** Create `GenreWeightModel` Data Class
    *   **Description:** Define a lightweight data class `GenreWeightModel` with two properties: `GenreName` (string) and `Weight` (float/double representing the user's affinity score for that genre). This is used inside `UserEngagementProfileModel.TopGenres`. Max 30 mins effort.

*   **Task:** Design Clip Interaction Insertion Query Structure
    *   **Description:** Plan the query/ORM mapping for inserting a new record into `ClipInteraction`. Handle the upsert case where the user re-views a clip they already interacted with (update `IsLiked`, `WatchDurationSeconds`, `WatchPercentage`). Document the query contract. Max 30 mins effort.

*   **Task:** Design Engagement Profile Aggregation Query Structure
    *   **Description:** Plan the aggregation query that reads from `ClipInteraction` for a given `UserId`, computing `COUNT(*)` (total views), `SUM(WatchDurationSeconds)`, `AVG(WatchDurationSeconds)`, count of `IsLiked = true`, and `GROUP BY GenreId ORDER BY count DESC LIMIT 10` for top genres. Document the query logic, not the code. Max 30 mins effort.

---

**Backend Services & ViewModels**

*   **Task:** Define `IClipInteractionService` Interface
    *   **Description:** Create the service interface with method signatures: `RecordViewAsync(userId, clipId, watchDuration, watchPercentage)`, `ToggleLikeAsync(userId, clipId)`, and `GetInteractionAsync(userId, clipId)` returning a `ClipInteractionModel`. Max 30 mins effort.

*   **Task:** Implement `ClipInteractionService` — `RecordViewAsync` Method
    *   **Description:** Implement the concrete `RecordViewAsync` method inside `ClipInteractionService`. It should create a new `ClipInteraction` DB record with the watch duration and percentage. On error, log and rethrow. Max 30 mins effort.

*   **Task:** Implement `ClipInteractionService` — `ToggleLikeAsync` Method
    *   **Description:** Implement `ToggleLikeAsync`. Look up the existing interaction record for (userId, clipId); if it exists, flip `IsLiked`. If no interaction exists yet, create one with `IsLiked = true`. Persist to database. Max 30 mins effort.

*   **Task:** Define `IEngagementProfileService` Interface
    *   **Description:** Create the service interface with method: `RecalculateProfileAsync(userId)` returning a `UserEngagementProfileModel`, and `GetProfileAsync(userId)`. Max 30 mins effort.

*   **Task:** Implement `EngagementProfileService` — Profile Computation Logic
    *   **Description:** Implement `RecalculateProfileAsync`. Fetch all `ClipInteraction` rows for the user, aggregate total likes, total watch time, average watch time, clip count, like-to-view ratio, and top 10 genres by interaction count. Persist the result to `UserEngagementProfile`. Max 30 mins effort.

*   **Task:** Define `IRecommendationService` Interface
    *   **Description:** Create the service interface with method: `GetRecommendedClipsAsync(userId, count)` returning a list of `MovieClipModel` ordered by relevance. Max 30 mins effort.

*   **Task:** Implement `RecommendationService` — Basic Recommendation Algorithm
    *   **Description:** Implement a first-pass recommendation algorithm: fetch the user's `UserEngagementProfile`, extract their top genre weights, query `MovieClip` table excluding already-viewed clips, and score remaining clips by genre weight match. Return the top N clips sorted by descending score. Max 30 mins effort.

*   **Task:** Implement `RecommendationService` — Cold Start / Fallback Logic
    *   **Description:** Handle the case where a new user has no engagement profile yet. Serve a default curated list of popular/trending clips (e.g., globally most-liked clips from the last 7 days). Max 30 mins effort.

*   **Task:** Scaffold `ReelsFeedViewModel`
    *   **Description:** Create the `ReelsFeedViewModel` class inheriting from base ViewModel. Define observable properties: `CurrentClip` (MovieClipModel), `ClipQueue` (ObservableCollection of MovieClipModel), `IsLoading` (bool), `IsCurrentClipLiked` (bool). Set up `INotifyPropertyChanged`. Max 30 mins effort.

*   **Task:** Implement `ReelsFeedViewModel` — Load Initial Feed Command
    *   **Description:** Create a `LoadFeedCommand` that calls `IRecommendationService.GetRecommendedClipsAsync()`, populates `ClipQueue` with the results, and sets `CurrentClip` to the first item. Handle loading states. Max 30 mins effort.

*   **Task:** Implement `ReelsFeedViewModel` — Scroll to Next Clip Logic
    *   **Description:** Implement `ScrollNextCommand`. Advance `CurrentClip` to the next item in `ClipQueue`. When the queue has ≤ 2 remaining clips, automatically trigger a background fetch for more recommended clips and append them. Max 30 mins effort.

*   **Task:** Implement `ReelsFeedViewModel` — Scroll to Previous Clip Logic
    *   **Description:** Implement `ScrollPreviousCommand`. Navigate back to the previously viewed clip. Maintain a small history stack (last 5 clips) for backward navigation. Max 30 mins effort.

*   **Task:** Implement `ReelsFeedViewModel` — Like Toggle Command
    *   **Description:** Create a `ToggleLikeCommand`. When fired, call `IClipInteractionService.ToggleLikeAsync()` for the current clip, flip `IsCurrentClipLiked` property, and trigger a heart animation flag. Max 30 mins effort.

*   **Task:** Implement `ReelsFeedViewModel` — Watch Time Tracking Logic
    *   **Description:** Add a timer/stopwatch mechanism that starts when a clip becomes `CurrentClip` and stops when the user scrolls away. On scroll-away, call `IClipInteractionService.RecordViewAsync()` with the elapsed seconds and calculated watch percentage. Max 30 mins effort.

*   **Task:** Implement `ReelsFeedViewModel` — Prefetch Buffer Management
    *   **Description:** Implement background prefetch logic: when user is watching clip N, signal `ClipPlaybackService` to begin buffering clip N+1 and N+2. Expose a `PrefetchStatus` property for UI loading indicators. Max 30 mins effort.

*   **Task:** Define `IClipPlaybackService` Interface
    *   **Description:** Define the service interface with methods: `PlayClip(videoUrl)`, `PauseClip()`, `ResumeClip()`, `GetElapsedSeconds()`, `PrefetchClip(videoUrl)`. This abstracts the video player lifecycle. Max 30 mins effort.

*   **Task:** Implement `ClipPlaybackService` Concrete Class
    *   **Description:** Implement the concrete `ClipPlaybackService` wrapping the platform's native video player API. Wire `PlayClip` to load and auto-play the video URL; `PauseClip` to halt playback; `PrefetchClip` to download/buffer without playing. Max 30 mins effort.

*   **Task:** Implement ViewModel Global Error Handling
    *   **Description:** Add try-catch wrappers around all service calls in `ReelsFeedViewModel`. On failure, set an `ErrorMessage` observable property and log the exception. Ensure the feed remains navigable even if a single interaction record fails to save. Max 30 mins effort.

---

**GUI (Views)**

*   **Task:** Create `ReelsFeedView` Skeleton Layout
    *   **Description:** Create the base UI layout file for the Reels screen. Define a full-screen container with no navigation bar (immersive mode). Add a single vertical-scroll/snap container that will host clip items. Set background to black. Max 30 mins effort.

*   **Task:** Design `ReelClipItemView` — Video Player Container
    *   **Description:** Design a single clip item layout that fills 100% of the viewport. Place a video player element that stretches edge-to-edge. Ensure the video scales to "cover" (no letterboxing). Max 30 mins effort.

*   **Task:** Design `ReelClipItemView` — Metadata Overlay (Title & Genre)
    *   **Description:** Add a semi-transparent gradient overlay at the bottom of the clip item. Place the movie title (bold, white, large font) and genre tag (smaller, white, pill-shaped badge) over this gradient, positioned bottom-left. Max 30 mins effort.

*   **Task:** Design `ReelClipItemView` — Like Button & Animation
    *   **Description:** Place a heart icon button on the right side of the clip (vertically centered or bottom-right). When unliked, show an outline heart; when liked, show a filled red heart. Add a scale-bounce micro-animation on tap. Max 30 mins effort.

*   **Task:** Implement Double-Tap-to-Like Gesture on Clip
    *   **Description:** Add a double-tap gesture recognizer to the `ReelClipItemView`. On double-tap, trigger the ViewModel's `ToggleLikeCommand` and play a large, brief heart burst animation at the tap point (like Instagram). Max 30 mins effort.

*   **Task:** Design `ReelClipItemView` — Progress Bar
    *   **Description:** Add a thin horizontal progress bar at the very bottom of each clip item showing playback progress (elapsed / total). Use a subtle white or accent-colored bar. Bind its value to the playback position from the ViewModel. Max 30 mins effort.

*   **Task:** Implement Vertical Snap-Scroll Behavior
    *   **Description:** Configure the scroll container in `ReelsFeedView` to snap each child clip item to the full viewport on scroll end (snap-to-page scrolling). When a new clip snaps into view, fire the ViewModel's `ScrollNextCommand` or `ScrollPreviousCommand` accordingly. Max 30 mins effort.

*   **Task:** Wire Data Binding — `ReelsFeedView` to `ReelsFeedViewModel`
    *   **Description:** Connect the `ReelsFeedView` to its `ReelsFeedViewModel`. Bind the scroll container's item source to `ClipQueue`. Bind the currently visible clip to `CurrentClip`. Bind the like button state to `IsCurrentClipLiked`. Max 30 mins effort.

*   **Task:** Implement Loading Spinner for Feed
    *   **Description:** Add a centered circular loading indicator that displays when `IsLoading` is true in the ViewModel (e.g., initial load and when fetching more clips). Hide it when clips are ready. Max 30 mins effort.

*   **Task:** Design Error State UI for Feed
    *   **Description:** Create an error state layout (centered icon + "Something went wrong" message + "Retry" button) that displays when `ErrorMessage` is non-empty in the ViewModel. Bind the Retry button to `LoadFeedCommand`. Max 30 mins effort.

*   **Task:** Design Empty State UI for Feed
    *   **Description:** Create an empty/cold-start state layout (illustration + "No clips yet" message) shown when the feed returns zero clips. Provide a friendly prompt encouraging the user to explore other parts of the app. Max 30 mins effort.
