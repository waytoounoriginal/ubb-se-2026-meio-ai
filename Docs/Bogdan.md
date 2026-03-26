### 1. Formal Requirements (Movie tinder)

*   **Requirement 1:** The system must show the authenticated user a stack of movie cards (poster + title + genre), sourced from the external `Movie` table, prioritising unswiped movies, and then previously swiped movies.
    *   **Verified by:** Unit test confirming the card queue correctly prioritizes unswiped movies.
*   **Requirement 2:** The user can swipe right to "like" or left to "skip" a movie card.
    *   **Verified by:** UI test confirming a drag beyond 30 % of card width triggers the correct action.
*   **Requirement 3:** Each swipe updates the user's score in `UserMoviePreference`: **right-swipe → +1.0**, **left-swipe → −0.5**. If no row exists, one is created at **0.0** before applying the delta (atomic `MERGE` statement).
    *   **Verified by:** Unit test asserting exact delta values; integration test confirming upsert for first-time swipes.
*   **Requirement 4:** All score changes are persisted immediately after each swipe. Persistence runs concurrently with the card-advance animation so the UI stays responsive.
    *   **Verified by:** Integration test confirming the DB write completes within the same request cycle.
*   **Requirement 5 (User Flow):** The logged-in user navigates to the "Discover Movies" screen, where a stack of movie cards is loaded automatically. The top card shows the movie's poster and title. The user drags the card right to like or left to skip — a green "LIKE" or red "NOPE" overlay fades in proportionally to the drag distance. Once the drag passes 30 % of the card width and the user releases, the swipe is confirmed: the card animates off-screen (fly-off + fade-out, 250 ms, cubic ease-in), the preference score is updated in the background, and the next card rises to the top. When the queue drops to ≤ 2 cards, an infinite stream of movies is fetched automatically (buffer size = 5). The user can continue swiping infinitely unless the database is entirely empty, at which point a "No movies found" message is displayed.
    *   **Verified by:** End-to-end UI walkthrough test covering: card load → drag → overlay opacity → release → card animation → score persistence → queue refill.
*   **Requirement 6:** If the user closes the application mid-swipe (before releasing the card), the in-progress swipe is discarded — only fully completed swipes are persisted. No data is lost for previously completed swipes. If the pointer capture is lost during a drag, the card snaps back to center.
    *   **Verified by:** Test confirming that killing the app during a drag does not create a partial `UserMoviePreference` row.
*   **Owner:** Bogdan
*   **Cross-Team Dependencies:**
    *   **External Group:** Reads from the other group's `Movie` table for card data.
    *   **Gabi:** Also writes to `UserMoviePreference` (tournament winner boost +2.0) — share the same upsert logic.
    *   **Tudor:** Also writes to `UserMoviePreference` (reel-like boost +1.5) — share the `UserMoviePreferenceModel`.
    *   **Madi:** Reads `UserMoviePreference` scores as input for personality matching (`GetAllPreferencesExceptUserAsync`).

---

### 2. Diagram Blueprint

*   **Use Case Diagram Additions:**
    *   **Actor:** Authenticated User
    *   **Use Cases:** `Swipe on Movie Card`, `Update Movie Preference Score`, `Load Next Movie Cards`

*   **Database Schema Additions:**
    *   *(No new tables — reads from external `Movie`, writes to shared `UserMoviePreference`.)*
    *   **Shared Table: `UserMoviePreference`** (`UserMoviePreferenceId` PK IDENTITY, `UserId` INT NOT NULL, `MovieId` INT NOT NULL, `Score` FLOAT DEFAULT 0, `LastModified` DATETIME2 DEFAULT SYSUTCDATETIME(), `ChangeFromPreviousValue` INT NULL, UNIQUE(`UserId`, `MovieId`))

        | Action      | Score Delta |
        |-------------|-------------|
        | Right-swipe | +1.0        |
        | Left-swipe  | −0.5        |

        If no row exists → create at 0.0, then apply delta (SQL `MERGE`).

*   **Class Diagram (MVVM) Additions:**
    *   *Models:* `MovieCardModel` (projection from Movie — `MovieId`, `Title`, `PosterUrl`, `PrimaryGenre`, `Genre` alias, `ReleaseYear`, `Synopsis`, `ToString()`), `UserMoviePreferenceModel` (`UserMoviePreferenceId`, `UserId`, `MovieId`, `Score`, `LastModified`, `ChangeFromPreviousValue`)
    *   *Views:* `MovieSwipeView` (main swipe page with drag gesture handling), `SwipeResultSummaryView` (empty-state "All caught up!" page)
    *   *ViewModels:* `MovieSwipeViewModel` (inherits `ObservableObject` from CommunityToolkit.Mvvm, uses `[ObservableProperty]` and `[RelayCommand]` source generators)
    *   *Services:* `ISwipeService` / `SwipeService` (business logic — delta calculation + delegation), `IMovieCardFeedService` / `MovieCardFeedService` (feed fetching wrapper), `IPreferenceRepository` / `PreferenceRepository` (ADO.NET data access via `ISqlConnectionFactory`)

---

### 3. Project Management Tasks (Max 30-Minutes Each)

**Database & Models**

| # | Task | Description |
|---|------|-------------|
| 1 | Define `UserMoviePreference` table schema | Create the DB migration: `UserMoviePreferenceId` PK IDENTITY, `UserId` INT NOT NULL, `MovieId` INT NOT NULL, `Score` FLOAT DEFAULT 0, `LastModified` DATETIME2 DEFAULT SYSUTCDATETIME(), `ChangeFromPreviousValue` INT NULL. Add UNIQUE(`UserId`, `MovieId`). |
| 2 | Create `MovieCardModel` & `UserMoviePreferenceModel` | `MovieCardModel` — read-only projection (`MovieId`, `Title`, `PosterUrl`, `PrimaryGenre`, `Genre` alias property, `ReleaseYear`, `Synopsis`, `ToString()` override). `UserMoviePreferenceModel` — mirrors the table including `ChangeFromPreviousValue`; this is the canonical shared class used by Gabi, Tudor, and Madi. |
| 3 | Implement `IPreferenceRepository` & `PreferenceRepository` | Methods: `GetPreferenceAsync(userId, movieId)`, `UpsertPreferenceAsync(model)` (SQL `MERGE` for atomic upsert), `GetMovieFeedAsync(userId, count)` (LEFT JOIN Movie ↔ UserMoviePreference, prioritizes unswiped then older swiped limits), `GetUnswipedMovieIdsAsync(userId)`, `GetAllPreferencesExceptUserAsync(excludeUserId)`. Uses `ISqlConnectionFactory` for ADO.NET connections. |

**Backend Services & ViewModels**

| # | Task | Description |
|---|------|-------------|
| 4 | Implement `ISwipeService` & `SwipeService` | Methods: `UpdatePreferenceScoreAsync(userId, movieId, isLiked)` and `GetMovieFeedAsync(userId, count)`. Like → +1.0 (`LikeDelta` const), dislike → −0.5 (`SkipDelta` const). Upserts via the repository. Depends on `IPreferenceRepository` only. |
| 5 | Implement `IMovieCardFeedService` & `MovieCardFeedService` | Method: `FetchMovieFeedAsync(userId, count)`. Thin wrapper delegating to `IPreferenceRepository.GetMovieFeedAsync`. |
| 6 | Scaffold `MovieSwipeViewModel` | Observable properties: `CurrentCard`, `CardQueue` (`ObservableCollection`), `IsLoading`, `IsAllCaughtUp`, `StatusMessage`. Inherits `ObservableObject` (CommunityToolkit.Mvvm). Constants: `BufferSize = 5`, `RefillThreshold = 2`, `DefaultUserId = 1`. Includes try-catch around all service calls. |
| 7 | Implement swipe commands (right & left) | Two `[RelayCommand]` async methods (`SwipeRightAsync`, `SwipeLeftAsync`) that call the swipe service with `isLiked = true / false` via shared `ProcessSwipeAsync`. Card advance happens immediately; persistence runs concurrently. |
| 8 | Implement card queue auto-refill | Keep 5 cards buffered. When ≤ 2 remain, request more from `ISwipeService.GetMovieFeedAsync()`. Deduplicates against current card + existing queue. Guards against concurrent refill with `_isRefilling` flag. |

**GUI (Views)**

| # | Task | Description |
|---|------|-------------|
| 9  | Create `MovieSwipeView` layout & card component | Full-screen centred container (340 × 500). Card design: image 70 %, title/genre 30 %, rounded corners (`CornerRadius="16"`), themed background. Includes `ProgressRing` loading spinner, "All caught up!" empty-state `StackPanel`. |
| 10 | Wire data binding | Bind card content via `PropertyChanged` handler (manual code-behind binding to `TitleText`, `GenreText`, `PosterImage`). Bind loading spinner to `IsLoading`, empty state to `IsAllCaughtUp`, status bar to `StatusMessage`. |
| 11 | Implement swipe gestures & drag-threshold logic | Pointer event listeners (`PointerPressed/Moved/Released/CaptureLost`) on the card. Horizontal drag tracked via `CompositeTransform.TranslateX`. If drag > 30 % of card width → fire the matching command on release. Includes rotation (max ±15°) proportional to drag distance. |
| 12 | Bind "Like" / "Nope" overlays | Green "LIKE" (`#00C853`) / Red "NOPE" (`#FF1744`) text overlays whose opacity scales with drag distance relative to threshold. Reset to 0 on snap-back. |
| 13 | Implement fly-off animation | `Storyboard` with `DoubleAnimation` for `TranslateX` (±600 px) and `Opacity` (fade to 0), 250 ms with `CubicEase` EaseIn. On completion, resets card position and fires ViewModel swipe command. |
| 14 | Create `SwipeResultSummaryView` | Separate page shown when all movies are swiped. Displays emoji icon, "All caught up!" header, and descriptive message. Binds to `MovieSwipeViewModel` via DI. |
