### 1. Formal Requirements
*   **Requirement 1:** The system must allow the user to input a valid numeric value greater than 2 to determine the initial pool size for the movie tournament.
*   **Requirement 2:** The system must fetch a random set of movies (matching the pool size) from the external Movie table and pair them for head-to-head matchups.
*   **Requirement 3:** The system must present the user with a side-by-side selection of two movies, displaying each movie's title and poster.
*   **Requirement 4:** The system must advance the user's selected movie from each pair to the next round, discarding the unselected movie.
*   **Requirement 5:** The system must repeatedly present winning movies in pairs until only one final winner remains.
*   **Requirement 6:** The system must boost the winning movie's score in the shared `UserMoviePreference` table, associating the result with the current user's profile.
*   **Owner:** Gabi
*   **Cross-Team Dependencies:**
    *   **External Group:** Depends on the other group's `Movie` table for fetching the tournament movie pool.
    *   **Bogdan:** Bogdan owns the `UserMoviePreference` table schema — Gabi also writes to it (boosting winner score). Coordinate on shared upsert logic.
    *   **Madi:** Madi's personality matching reads from `UserMoviePreference` — tournament winners influence matching results.

### 2. Diagram Blueprint
*   **Use Case Diagram Additions:**
    *   *Actor:* Authenticated User
    *   *Use Cases:* `Start Movie Tournament`, `Select Movie Pair Winner`, `View Tournament Result`, `Boost Winner Preference Score`
*   **Database Schema Additions:**
    *   *(This feature does NOT create new tables. It reads from the external `Movie` table and writes to the shared `UserMoviePreference` table.)*
    *   **External Table: `Movie`** — Read-only source of movie pool for tournament generation.
    *   **Shared Table: `UserMoviePreference`** — The tournament winner's Score is boosted for the current user. If no row exists, one is created with an initial boosted score.
    *   *(Note: Tournament bracket state — `TournamentState`, `Matchup` — is managed entirely in-memory and is NOT persisted to the database.)*
*   **Class Diagram (MVVM) Additions:**
    *   *Models:* `MovieModel` (projection from external Movie table), `TournamentState` (in-memory), `Matchup` (in-memory)
    *   *Views:* `TournamentSetupView` (pool size input), `TournamentMatchView` (pairwise selection), `TournamentResultView` (winner display)
    *   *ViewModels:* `TournamentSetupViewModel`, `TournamentMatchViewModel`, `TournamentResultViewModel`
    *   *Utils/Services:* `TournamentLogicService` (handles bracket generation and advancement), `MovieRepository` (fetches random movies from external table)

### 3. Project Management Tasks (Max 30-Minutes Each)

**Database & Models**
*   **Task:** Create `MovieModel` Data Class
    *   **Description:** Define the Model class representing a movie card: `MovieId`, `Title`, `PosterUrl`. This is a read-only projection from the external Movie table. Max 30 mins effort.
*   **Task:** Create `UserMoviePreferenceModel` Data Class
    *   **Description:** Define the Model class mirroring the shared `UserMoviePreference` table: `UserMoviePreferenceId`, `UserId`, `MovieId`, `Score`, `LastModified`. This is the same shared model used by swipe, matching, and reel-like features. Max 30 mins effort.
*   **Task:** Create `TournamentState` In-Memory Model
    *   **Description:** Create a model class representing the current state of a tournament: list of pending matches, completed matches, current round number. Not persisted. Max 30 mins effort.
*   **Task:** Create `Matchup` In-Memory Model
    *   **Description:** Define a lightweight model holding `MovieA`, `MovieB`, and `WinnerId`. Used to track individual tournament pairings in memory. Max 30 mins effort.
*   **Task:** Implement Preference Score Boost for Winner
    *   **Description:** Write a repository method that upserts a `UserMoviePreference` row for the (UserId, winning MovieId) pair, boosting the Score significantly. Max 30 mins effort.

**Backend Services & ViewModels**
*   **Task:** Implement `MovieRepository` — Fetch Random Movies
    *   **Description:** Create a repository method that queries the external Movie table and returns a random subset of N movies for tournament pool generation. Max 30 mins effort.
*   **Task:** Implement Initial Bracket Generation Logic
    *   **Description:** Write an algorithm in `TournamentLogicService` that takes a list of N movies and constructs initial pairs. Handle odd numbers (e.g., give one movie a bye to the next round). Max 30 mins effort.
*   **Task:** Implement Match Advancement Logic
    *   **Description:** Add a method to `TournamentLogicService` that accepts a winner, updates the `TournamentState`, and generates the next round of matchups. Max 30 mins effort.
*   **Task:** Create `TournamentSetupViewModel`
    *   **Description:** Implement the ViewModel with an integer property for pool size (validate > 2) and a command to initialize the tournament with random movies. Max 30 mins effort.
*   **Task:** Create `TournamentMatchViewModel`
    *   **Description:** Implement the ViewModel with `MovieOptionA` and `MovieOptionB` properties. Add `SelectMovieCommand` triggered when user clicks a side. Max 30 mins effort.
*   **Task:** Connect MatchViewModel to Tournament Logic
    *   **Description:** Wire `SelectMovieCommand` to call `TournamentLogicService`, then update `MovieA`/`MovieB` to the next pair. When the final winner is decided, navigate to result screen and boost the score. Max 30 mins effort.

**GUI (Views)**
*   **Task:** Lay out `TournamentSetupView` UI
    *   **Description:** Create the skeleton layout with a number input field (or slider) and a "Start" button. Bind to `TournamentSetupViewModel`. Max 30 mins effort.
*   **Task:** Lay out `TournamentMatchView` Split Screen
    *   **Description:** Design the split-screen layout with two distinct clickable halves (left vs right). Max 30 mins effort.
*   **Task:** Bind Posters and Titles in `TournamentMatchView`
    *   **Description:** Populate the split areas with Image components for posters and Text for titles. Bind to ViewModel properties. Max 30 mins effort.
*   **Task:** Lay out `TournamentResultView`
    *   **Description:** Create the "Winner" screen displaying the winning movie's poster in large format, with visual confirmation that the preference score was boosted. Max 30 mins effort.
