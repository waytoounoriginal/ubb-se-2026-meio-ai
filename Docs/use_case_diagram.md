# Use Case Diagram — Movie & Personality Application

## Actors

| Actor | Description |
|---|---|
| **Authenticated User** | A logged-in user who interacts with reels, swipes movies, runs tournaments, and finds personality matches |
| **Automated System (Scraper)** | A background service that scrapes external sources for movie trailer content |

## Diagram

```mermaid
graph LR
    %% ─── Actors ───
    User(("👤 Authenticated User"))
    Scraper(("🤖 Automated System<br/>(Scraper)"))

    %% ═══════════════════════════════════
    %% REEL UPLOAD (Alex)
    %% ═══════════════════════════════════
    subgraph ReelUpload["📤 Reel Upload — Alex"]
        UC_Upload["Upload Movie Reel"]
        UC_Validate["Validate Video Format"]
        UC_Associate["Associate Reel with Movie"]
    end

    User --- UC_Upload
    UC_Upload -.->|"«include»"| UC_Validate
    UC_Upload -.->|"«extend»"| UC_Associate

    %% ═══════════════════════════════════
    %% REEL EDITING (Beatrice)
    %% ═══════════════════════════════════
    subgraph ReelEditing["✂️ Reel Editing — Beatrice"]
        UC_EditReel["Edit Uploaded Reel"]
        UC_SelectReel["Select Uploaded Reel"]
        UC_Crop["Crop Reel Video"]
        UC_AddMusic["Add Background Music"]
        UC_SaveEdits["Save Reel Edits"]
    end

    User --- UC_EditReel
    UC_EditReel -.->|"«include»"| UC_SelectReel
    UC_EditReel -.->|"«extend»"| UC_Crop
    UC_EditReel -.->|"«extend»"| UC_AddMusic
    UC_EditReel -.->|"«include»"| UC_SaveEdits

    %% ═══════════════════════════════════
    %% TRAILER SCRAPING (Andrei)
    %% ═══════════════════════════════════
    subgraph TrailerScraping["🔍 Trailer Scraping — Andrei"]
        UC_Scrape["Scrape Movie Trailer Content"]
        UC_StoreScraped["Store Scraped Trailers as Reels"]
    end

    Scraper --- UC_Scrape
    UC_Scrape -.->|"«include»"| UC_StoreScraped

    %% ═══════════════════════════════════
    %% REELS FEED (Tudor)
    %% ═══════════════════════════════════
    subgraph ReelsFeed["📱 Reels Feed — Tudor"]
        UC_Browse["Browse Reels Feed"]
        UC_Scroll["Scroll to Next/Previous Clip"]
        UC_Like["Like a Reel"]
        UC_RecordView["Record View Interaction"]
        UC_UpdateProfile["Update Engagement Profile"]
        UC_BoostPref["Boost Movie Preference on Like"]
        UC_PersonalFeed["Generate Personalized Feed"]
    end

    User --- UC_Browse
    User --- UC_Scroll
    User --- UC_Like
    UC_Browse -.->|"«include»"| UC_RecordView
    UC_RecordView -.->|"«include»"| UC_UpdateProfile
    UC_Like -.->|"«extend»"| UC_BoostPref
    UC_Browse -.->|"«include»"| UC_PersonalFeed

    %% ═══════════════════════════════════
    %% MOVIE SWIPE (Bogdan)
    %% ═══════════════════════════════════
    subgraph MovieSwipe["👆 Movie Swipe — Bogdan"]
        UC_Swipe["Swipe on Movie Card"]
        UC_UpdatePref["Update Movie Preference Score"]
        UC_LoadCards["Load Next Movie Cards"]
    end

    User --- UC_Swipe
    UC_Swipe -.->|"«include»"| UC_UpdatePref
    UC_Swipe -.->|"«extend»"| UC_LoadCards

    %% ═══════════════════════════════════
    %% MOVIE TOURNAMENT (Gabi)
    %% ═══════════════════════════════════
    subgraph MovieTournament["🏆 Movie Tournament — Gabi"]
        UC_StartTourney["Start Movie Tournament"]
        UC_SelectWinner["Select Movie Pair Winner"]
        UC_ViewResult["View Tournament Result"]
        UC_BoostWinner["Boost Winner Preference Score"]
    end

    User --- UC_StartTourney
    User --- UC_SelectWinner
    User --- UC_ViewResult
    UC_ViewResult -.->|"«includes»"| UC_BoostWinner

    %% ═══════════════════════════════════
    %% PERSONALITY MATCHING (Madi)
    %% ═══════════════════════════════════
    subgraph PersonalityMatching["💜 Personality Matching — Madi"]
        UC_FindMatches["Find Personality Matches"]
        UC_ViewDetail["View Matched User Details"]
    end

    User --- UC_FindMatches
    UC_FindMatches -.->|"«extend»"| UC_ViewDetail

    %% ─── Styles ───
    classDef actor fill:#4a90d9,stroke:#2c5282,color:#fff,font-weight:bold
    classDef upload fill:#48bb78,stroke:#276749,color:#fff
    classDef editing fill:#ed8936,stroke:#c05621,color:#fff
    classDef scraping fill:#a0aec0,stroke:#4a5568,color:#fff
    classDef feed fill:#667eea,stroke:#434190,color:#fff
    classDef swipe fill:#f56565,stroke:#c53030,color:#fff
    classDef tournament fill:#ecc94b,stroke:#b7791f,color:#2d3748
    classDef matching fill:#d53f8c,stroke:#97266d,color:#fff

    class User,Scraper actor
    class UC_Upload,UC_Validate,UC_Associate upload
    class UC_EditReel,UC_SelectReel,UC_Crop,UC_AddMusic,UC_SaveEdits editing
    class UC_Scrape,UC_StoreScraped scraping
    class UC_Browse,UC_Scroll,UC_Like,UC_RecordView,UC_UpdateProfile,UC_BoostPref,UC_PersonalFeed feed
    class UC_Swipe,UC_UpdatePref,UC_LoadCards swipe
    class UC_StartTourney,UC_SelectWinner,UC_ViewResult,UC_BoostWinner tournament
    class UC_FindMatches,UC_ViewDetail matching
```

## Use Case Summary by Feature

### 📤 Reel Upload (Alex)
| Use Case | Description |
|---|---|
| Upload Movie Reel | User selects and uploads a short video file |
| Validate Video Format | System checks format (MP4) and duration (< 60s) |
| Associate Reel with Movie | User optionally links the reel to a movie |

### ✂️ Reel Editing (Beatrice)
| Use Case | Description |
|---|---|
| Edit Uploaded Reel | User initiates the editing workflow for a reel |
| Select Uploaded Reel | System prompts user to pick a previously uploaded reel |
| Crop Reel Video | User optionally defines crop dimensions (x, y, width, height) |
| Add Background Music | User optionally selects a music track from the library |
| Save Reel Edits | System persists crop metadata and music selection |

### 🔍 Trailer Scraping (Andrei)
| Use Case | Description |
|---|---|
| Scrape Movie Trailer Content | Background service scrapes external sources for trailers |
| Store Scraped Trailers as Reels | Scraped videos are inserted into the Reel table |

### 📱 Reels Feed (Tudor)
| Use Case | Description |
|---|---|
| Browse Reels Feed | User views a full-screen, vertically-scrollable clip feed |
| Scroll to Next/Previous Clip | User swipes vertically with snap-to-clip behavior |
| Like a Reel | User taps/double-taps heart to toggle like state |
| Record View Interaction | System tracks watch duration and timestamps |
| Update Engagement Profile | System aggregates interaction data into UserProfile |
| Boost Movie Preference on Like | System boosts UserMoviePreference score on reel like |
| Generate Personalized Feed | Recommendation algorithm orders clips by relevance |

### 👆 Movie Swipe (Bogdan)
| Use Case | Description |
|---|---|
| Swipe on Movie Card | User swipes right (like) or left (dislike) on movie cards |
| Update Movie Preference Score | System boosts/lowers score in UserMoviePreference |
| Load Next Movie Cards | System auto-refills the card queue with unswiped movies conditionally when low |

### 🏆 Movie Tournament (Gabi)
| Use Case | Description |
|---|---|
| Start Movie Tournament | User inputs pool size and begins a bracket tournament |
| Select Movie Pair Winner | User picks a winner in each head-to-head matchup |
| View Tournament Result | System displays the final winning movie |
| Boost Winner Preference Score | System boosts the winner's score in UserMoviePreference upon final match completion |

### 💜 Personality Matching (Madi)
| Use Case | Description |
|---|---|
| Find Personality Matches | User initiates matching based on movie preferences and system displays top 10 compatible users |
| View Matched User Details | User views a matched user's engagement stats and top preferences |
