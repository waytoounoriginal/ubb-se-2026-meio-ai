# Class Diagram — Gabi (Movie Tournament)

```mermaid
classDiagram
    direction TB

    %% ── Models ──
    class MovieModel {
        +int MovieId
        +string Title
        +string PosterUrl
    }

    class UserMoviePreferenceModel {
        +int UserMoviePreferenceId
        +int UserId
        +int MovieId
        +float Score
        +DateTime LastModified
    }

    class TournamentState {
        <<in-memory>>
        +List~Matchup~ PendingMatches
        +List~Matchup~ CompletedMatches
        +int CurrentRound
    }

    class Matchup {
        <<in-memory>>
        +MovieModel MovieA
        +MovieModel MovieB
        +int? WinnerId
    }

    TournamentState --> "many" Matchup : contains
    Matchup --> "2" MovieModel : references

    %% ── Services ──
    class TournamentLogicService {
        +GenerateBracket(List~MovieModel~) TournamentState
        +AdvanceWinner(TournamentState, int winnerId) TournamentState
    }

    class MovieRepository {
        +GetRandomMoviesAsync(int count) List~MovieModel~
    }

    class PreferenceRepository {
        +BoostWinnerScoreAsync(int userId, int movieId) void
    }

    %% ── ViewModels ──
    class TournamentSetupViewModel {
        +int PoolSize
        +ICommand StartTournamentCommand
    }

    class TournamentMatchViewModel {
        +MovieModel MovieOptionA
        +MovieModel MovieOptionB
        +ICommand SelectMovieCommand
    }

    class TournamentResultViewModel {
        +MovieModel Winner
        +bool ScoreBoosted
    }

    %% ── Views ──
    class TournamentSetupView {
        <<View>>
    }

    class TournamentMatchView {
        <<View>>
    }

    class TournamentResultView {
        <<View>>
    }

    %% ── Relationships ──
    TournamentSetupView --> TournamentSetupViewModel : DataContext
    TournamentMatchView --> TournamentMatchViewModel : DataContext
    TournamentResultView --> TournamentResultViewModel : DataContext

    TournamentSetupViewModel --> MovieRepository : fetches random movies
    TournamentMatchViewModel --> TournamentLogicService : advances bracket
    TournamentMatchViewModel --> TournamentState : reads/updates
    TournamentResultViewModel --> PreferenceRepository : boosts winner score
    TournamentLogicService --> TournamentState : creates/mutates
    PreferenceRepository --> UserMoviePreferenceModel : upserts
```
