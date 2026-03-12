### 1. Formal Requirements
*   **Requirement 1:** The application allows the user to input a valid numeric value greater than 2 and smaller than the total number of liked movies to determine the initial pool size for the movie tournament.The system must fetch a random set of movies (matching the pool size) from the external Movie table and pair them for head-to-head matchups.
*   **Requirement 2:** The system must present the user with a side-by-side selection of two movies, displaying each movie's title and poster.The system must advance the user's selected movie from each pair to the next round, discarding the unselected movie.
*   **Requirement 3:** The system must repeatedly present winning movies in pairs until only one final winner remains.
*   **Requirement 4:** The system must boost the winning movie's score in the shared and present to the user a victory view with the results.
*   **Requirement 5:** Tournament progress is maintained during active sessions, allowing users to navigate away and return without interruption. However if the user closes  the application, it will result in a reset of all current tournament data.
 `UserMoviePreference` table, associating the result with the current user's profile.
*   **Owner:** Gabi
*   **Cross-Team Dependencies:**
    *   **External Group:** Depends on Lucas's team's  `Movie` table for fetching the tournament movie pool.
    *   **Bogdan:** Bogdan owns the `UserMoviePreference` table schema — Gabi also writes to it (boosting winner score). Coordinate on shared upsert logic.


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
    *   *Models:* `MovieModel` (projection from external Movie table),`UserMoviePreferenceModel`,`Matchup`, `TournamentState` (in-memory), `Matchup` (in-memory)
    *   *Views:* `TournamentSetupView` (pool size input), `TournamentMatchView` (pairwise selection), `TournamentResultView` (winner display)
    *   *ViewModels:* `TournamentSetupViewModel`, `TournamentMatchViewModel`, `TournamentResultViewModel`
    *   *Utils/Services:* `TournamentLogicService` (handles bracket generation and advancement), `MovieTournamentRepository` (fetches random movies from external table)

### 3. Project Management Tasks (Max 30-Minutes Each)

**Database & Models**
*   **Task:** Create `MovieModel` and  `UserMoviePreferenceModel` Data Class
    *   **Description:** Define the Model class representing a movie card: `MovieId`, `Title`, `PosterUrl`. Use the `UserMoviePreferenceModel` class created by **Bogdan**. It mirrors the shared `UserMoviePreference` table.
*   **Task:** Create `TournamentState` In-Memory Model
    *   **Description:** Create a model class representing the current state of a tournament: list of pending matches, completed matches, current round number. Not persisted.
*   **Task:** Create `Matchup` In-Memory Model
    *   **Description:** Define a  model holding `MovieA`, `MovieB`, and `WinnerId`. Used to track individual tournament pairings in memory. 
*   **Task:** Implement Preference Score Boost for Winner
    *   **Description:** Write a repository method that updates a `UserMoviePreference` row for the (UserId, winning MovieId) pair, boosting the Score by a significatn amount

**Backend Services & ViewModels**
*   **Task:** Implement `MovieRepository` — Fetch Random Movies
    *   **Description:** Create a repository method that queries the external Movie table and returns a random subset of N movies for tournament pool generation. 
*   **Task:** Implement Initial Bracket Generation Logic
    *   **Description:** Write an algorithm in `TournamentLogicService` that takes a list of N movies and constructs initial pairs. Handle odd numbers (e.g., give one movie a bye to the next round).
*   **Task:** Implement Match Advancement Logic
    *   **Description:** Add a method to `TournamentLogicService` that accepts a winner, updates the `TournamentState`, and generates the next round of matchups.
*   **Task:** Create `TournamentSetupViewModel`
    *   **Description:** Implement the ViewModel with an integer property for pool size (validate > 2) and a command to initialize the tournament with random movies. 
*   **Task:** Create `TournamentMatchViewModel`
    *   **Description:** Implement the ViewModel with `MovieOptionA` and `MovieOptionB` properties. Add `SelectMovieCommand` triggered when user clicks a side. 
*   **Task:** Connect MatchViewModel to Tournament Logic
    *   **Description:** Wire `SelectMovieCommand` to call `TournamentLogicService`, then update `MovieA`/`MovieB` to the next pair. When the final winner is decided, navigate to result screen and boost the score.

**GUI (Views)**
*   **Task:** Lay out `TournamentSetupView` UI
    *   **Description:** Create a layout with where the user can select the number of movies through a slider or manually input it. Bind to `TournamentSetupViewModel`. 
*   **Task:** Lay out `TournamentMatchView` 
    *   **Description:** Design the split-screen layout with two distinct clickable options (left vs right).Populate the split areas with image components and text for titles. Bind to the `TournamentMatchViewModel`.
*   **Task:** Lay out `TournamentResultView`
    *   **Description:** Create the "Winner" screen displaying the winning movie's poster in large format, with visual confirmation that the preference score was boosted.
