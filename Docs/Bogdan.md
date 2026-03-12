### 1. Formal Requirements

*   **Requirement 1:** The system must show the authenticated user a stack of movie cards (poster + title), sourced from the external `Movie` table, filtering out movies the user has already swiped on.
    *   **Verified by:** Unit test confirming the card queue only contains unswiped movies with correct poster/title.
*   **Requirement 2:** The user can swipe right to "like" or left to "skip" a movie card.
    *   **Verified by:** UI test confirming a drag beyond 30 % of card width triggers the correct action.
*   **Requirement 3:** Each swipe updates the user's score in `UserMoviePreference`: **right-swipe → +1.0**, **left-swipe → −0.5**. If no row exists, one is created at **0.0** before applying the delta.
    *   **Verified by:** Unit test asserting exact delta values; integration test confirming upsert for first-time swipes.
*   **Requirement 4:** All score changes are persisted immediately after each swipe.
    *   **Verified by:** Integration test confirming the DB write completes within the same request cycle.
*   **Owner:** Bogdan
*   **Cross-Team Dependencies:**
    *   **External Group:** Reads from the other group's `Movie` table for card data.
    *   **Gabi:** Also writes to `UserMoviePreference` (tournament winner boost +2.0) — share the same upsert logic.
    *   **Tudor:** Also writes to `UserMoviePreference` (reel-like boost +1.5) — share the `UserMoviePreferenceModel`.
    *   **Madi:** Reads `UserMoviePreference` scores as input for personality matching.

---

### 2. Diagram Blueprint

*   **Use Case Diagram Additions:**
    *   **Actor:** Authenticated User
    *   **Use Cases:** `Swipe on Movie Card`, `Update Movie Preference Score`, `Load Next Movie Cards`

*   **Database Schema Additions:**
    *   *(No new tables — reads from external `Movie`, writes to shared `UserMoviePreference`.)*
    *   **Shared Table: `UserMoviePreference`** (`UserId` FK, `MovieId` FK, `Score` FLOAT, `LastModified` DATETIME)

        | Action      | Score Delta |
        |-------------|-------------|
        | Right-swipe | +1.0        |
        | Left-swipe  | −0.5        |

        If no row exists → create at 0.0, then apply delta.

*   **Class Diagram (MVVM) Additions:**
    *   *Models:* `MovieCardModel` (projection from Movie), `UserMoviePreferenceModel`
    *   *Views:* `MovieSwipeView`
    *   *ViewModels:* `MovieSwipeViewModel`
    *   *Services:* `ISwipeService` / `SwipeService`, `IPreferenceRepository` / `PreferenceRepository`

---

### 3. Project Management Tasks (Max 30-Minutes Each)

**Database & Models**

| # | Task | Description |
|---|------|-------------|
| 1 | Define `UserMoviePreference` table schema | Create the DB migration: `UserMoviePreferenceId` PK, `UserId` FK, `MovieId` FK, `Score` FLOAT, `LastModified` DATETIME. Add UNIQUE(`UserId`, `MovieId`). |
| 2 | Create `MovieCardModel` & `UserMoviePreferenceModel` | `MovieCardModel` — read-only projection (`MovieId`, `Title`, `PosterUrl`, `PrimaryGenre`). `UserMoviePreferenceModel` — mirrors the table; this is the canonical shared class used by Gabi, Tudor, and Madi. |
| 3 | Implement `IPreferenceRepository` & `PreferenceRepository` | Methods: `UpsertPreferenceAsync(model)`, `GetUnswipedMoviesAsync(userId) → List<MovieCardModel>`. The unswiped query joins `Movie` LEFT JOIN `UserMoviePreference` and returns only un-matched rows. |

**Backend Services & ViewModels**

| # | Task | Description |
|---|------|-------------|
| 4 | Implement `ISwipeService` & `SwipeService` | Methods: `UpdatePreferenceScoreAsync(userId, movieId, isLiked)` and `GetUnswipedMoviesAsync(userId, count)`. Like → +1.0, dislike → −0.5. Upserts via the repository. |
| 5 | Scaffold `MovieSwipeViewModel` | Observable properties: `CurrentCard`, `CardQueue`, `IsLoading`. Inherits `ViewModelBase`. Includes try-catch around all service calls. |
| 6 | Implement swipe commands (right & left) | Two relay commands that call the swipe service with `isLiked = true / false`, then advance the card queue. |
| 7 | Implement card queue auto-refill | Keep 5 cards buffered. When ≤ 2 remain, request more from `ISwipeService.GetUnswipedMoviesAsync()`. |

**GUI (Views)**

| # | Task | Description |
|---|------|-------------|
| 8 | Create `MovieSwipeView` layout & card component | Full-screen centred container. Card design: image 70 %, title/genre 30 %, rounded corners, subtle shadow. |
| 9 | Wire data binding | Bind top card to `CurrentCard`, card queue to the collection, loading spinner to `IsLoading`. |
| 10 | Implement swipe gestures & drag-threshold logic | Touch-drag listeners on the card. If drag > 30 % of card width → fire the matching command on release. |
| 11 | Bind "Like" / "Nope" overlays | Green "LIKE" / Red "NOPE" text overlays whose opacity scales with drag distance. |
