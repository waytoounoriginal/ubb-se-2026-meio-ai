# Tudor's Features — Implementation vs. Diagrams

A detailed comparison between the UML class diagram, DB diagram, and requirement docs
versus the current codebase.

---

## 1. Models

### ReelModel

| Aspect | Diagram | Code | Delta |
|--------|---------|------|-------|
| `DurationSeconds` (int) | Present | Renamed to `FeatureDurationSeconds`, type `double` | **Renamed + type change** |
| `IsLiked`, `LikeCount` | Not present | Added as `[ObservableProperty]` | **Added** (client-side UI state) |
| Base class | Plain data class | Extends `ObservableObject` | **Changed** (needed for runtime binding) |

### UserReelInteractionModel

| Aspect | Diagram | Code | Delta |
|--------|---------|------|-------|
| `InteractionId` type | `int` | `long` | **Type change** (DB uses BIGINT) |
| `WatchDurationSec` type | `int?` (nullable) | `double` (non-nullable) | **Type change** |
| `WatchPercentage` type | `float?` (nullable) | `double` (non-nullable) | **Type change** |

### UserProfileModel

| Aspect | Diagram | Code | Delta |
|--------|---------|------|-------|
| `AvgWatchTimeSec` type | `float` | `double` | **Type change** |
| `LikeToViewRatio` type | `float` | `double` | **Type change** |

### UserMoviePreferenceModel

| Aspect | Diagram | Code | Delta |
|--------|---------|------|-------|
| `Score` type | `float` | `double` | **Type change** |
| `ChangeFromPreviousValue` | Not present | `int?` property + DB column | **Added** (not in any diagram) |

---

## 2. Database Schema

The `DatabaseInitializer.cs` CREATE TABLE statements match the `dbdiagram_io.md` schema with these exceptions:

| Table.Column | Diagram | Code | Delta |
|--------------|---------|------|-------|
| `Reel.FeatureDurationSeconds` | Named `DurationSeconds` | Named `FeatureDurationSeconds` | **Renamed** |
| `UserReelInteraction.WatchDurationSec` | nullable | `NOT NULL DEFAULT 0` | **Nullability changed** |
| `UserReelInteraction.WatchPercentage` | nullable | `NOT NULL DEFAULT 0` | **Nullability changed** |
| `UserMoviePreference.ChangeFromPreviousValue` | Not present | `INT NULL` column exists | **Added** |

---

## 3. Service Interfaces

### IReelInteractionService

| Method | Diagram Signature | Code Signature | Delta |
|--------|------------------|----------------|-------|
| `RecordViewAsync` | `(int, int, int, float) → void` | `(int, int, double, double) → Task` | **Parameter types + async** |
| `ToggleLikeAsync` | `(int, int) → void` | `(int, int) → Task` | **Async** |
| `GetInteractionAsync` | `(int, int) → UserReelInteractionModel` | `(int, int) → Task<UserReelInteractionModel?>` | **Async + nullable** |
| `GetLikeCountAsync` | **Not in diagram** | `(int) → Task<int>` | **Added** |

### IEngagementProfileService

| Method | Diagram Signature | Code Signature | Delta |
|--------|------------------|----------------|-------|
| `RecalculateProfileAsync` | `(int) → void` | **Renamed** to `RefreshProfileAsync`, `(int) → Task` | **Renamed + async** |
| `GetProfileAsync` | `(int) → UserProfileModel` | `(int) → Task<UserProfileModel?>` | **Async + nullable** |

### IRecommendationService

| Method | Diagram Signature | Code Signature | Delta |
|--------|------------------|----------------|-------|
| `GetRecommendedReelsAsync` | `(int, int) → List<ReelModel>` | `(int, int) → Task<IList<ReelModel>>` | **Async + interface return type** |

### IClipPlaybackService

| Method | Diagram | Code | Delta |
|--------|---------|------|-------|
| `PlayClip(string)` | Present | Renamed `PlayAsync(string) → Task` | **Renamed + async** |
| `PauseClip()` | Present | Renamed `PauseAsync() → Task` | **Renamed + async** |
| `ResumeClip()` | Present | **Not implemented** | **Missing** |
| `GetElapsedSeconds()` | Present | **Not implemented** | **Missing** |
| `PrefetchClip(string)` | Present | Renamed `PrefetchClipAsync(string) → Task` | **Renamed + async** |
| `SeekAsync(double)` | Not in diagram | Present | **Added** |
| `IsPlaying` property | Not in diagram | Present | **Added** |

---

## 4. Repository Layer — Implemented

The UML class diagram defines three repository classes. All three are now implemented with interfaces + concrete classes in `Features/ReelsFeed/Repositories/`:

| Repository | Diagram Methods | Code Status |
|------------|----------------|-------------|
| `InteractionRepository` | `InsertInteractionAsync`, `UpsertInteractionAsync` | **Implemented** — also includes `ToggleLikeAsync`, `UpdateViewDataAsync`, `GetInteractionAsync`, `GetLikeCountAsync`, `GetReelMovieIdAsync` (extra methods beyond diagram) |
| `ProfileRepository` | `UpsertProfileAsync` | **Implemented** — also includes `GetProfileAsync` |
| `PreferenceRepository` | `BoostPreferenceOnLikeAsync` | **Implemented** — +1.5 boost upsert on `UserMoviePreference` |

Services now delegate persistence to repositories. `EngagementProfileService` still runs the aggregation query directly (business logic), then passes the result to `ProfileRepository.UpsertProfileAsync`. `RecommendationService` still queries the DB directly (no repository in diagram).

---

## 5. ViewModel

### ReelsFeedViewModel

| Aspect | Diagram | Code | Delta |
|--------|---------|------|-------|
| Base class | `ViewModelBase` (abstract) | `ObservableObject` (CommunityToolkit.Mvvm) | **Changed** |
| `IsCurrentReelLiked` property | Present | **Removed** (like state moved to `ReelModel.IsLiked`) | **Removed** |
| `ToggleLikeCommand` | Present | **Removed** (like toggle handled in `ReelItemView` code-behind) | **Moved to View** |
| `PageTitle` property | Not in diagram | Present (`"Reels Feed"`) | **Added** |
| `StatusMessage` property | Not in diagram | Present | **Added** |
| Watch tracking timer | Required (Task 10) | **Not implemented** | **Missing** |
| `RecordViewAsync` integration | Required (Task 10) | Never called from ViewModel or View | **Missing** |

### UserProfileViewModel

| Aspect | Diagram | Code | Delta |
|--------|---------|------|-------|
| Entire class | Present with `Profile` property | **Not implemented** | **Missing** |

---

## 6. Views

### Naming

| Diagram Name | Code Name | Delta |
|--------------|-----------|-------|
| `ReelsFeedView` | `ReelsFeedPage` | **Renamed** (WinUI Page convention) |
| `ReelItemView` | `ReelItemView` (UserControl) | Matches |

### ReelItemView — UI Features

| Feature (from Task 12) | Status |
|------------------------|--------|
| Video player filling viewport, UniformToFill | **Implemented** |
| Semi-transparent gradient overlay at bottom | **Implemented** |
| Movie title (bold, white) | **Implemented** |
| Genre tag (pill badge) | **Not implemented** — only Caption shown |
| Heart icon button (outline/filled red) | **Implemented** |
| Scale-bounce animation on like tap | **Not implemented** |
| Double-tap gesture to toggle like | **Not implemented** — single click only |
| Heart burst animation at tap point | **Not implemented** |
| Horizontal progress bar bound to playback | **Not implemented** |

### ReelsFeedPage — UI Features

| Feature | Status |
|---------|--------|
| Full-screen, black background | **Implemented** |
| Vertical snap-to-clip scrolling | **Implemented** (via FlipView) |
| Loading spinner | **Implemented** (ProgressRing) |
| Error message display | **Implemented** (TextBlock) |
| Empty/cold-start layout ("No clips yet") | **Not implemented** — uses StatusMessage text only |
| Retry button bound to LoadFeedCommand | **Not implemented** |

---

## 7. Behavioural / Functional Gaps

### Preference Boost on Like (+1.5)

**Docs say:** "Tudor also writes to `UserMoviePreference` on reel likes (boost +1.5) — reuse the same upsert logic."

**Code:** **Implemented.** `ReelInteractionService.ToggleLikeAsync` checks the previous like state and, on an unliked→liked transition, calls `PreferenceRepository.BoostPreferenceOnLikeAsync(userId, movieId)` which upserts +1.5 into `UserMoviePreference`.

### Watch Duration Tracking

**Docs say (Task 10):** "Add a timer that starts when a reel becomes CurrentReel and stops on scroll-away, calling RecordViewAsync() with elapsed seconds and watch percentage."

**Code:** `RecordViewAsync` exists in the service and is fully functional, but **is never called** from any ViewModel or View. No timer or stopwatch is implemented.

### ClipPlaybackService Underutilized

The actual video playback (play, pause, seek) is handled directly in `ReelItemView.xaml.cs` via `MediaPlayerElement.MediaPlayer`. The `ClipPlaybackService` is only used for prefetch caching and an `IsPlaying` flag — it does not control playback. The diagram intended it to be the central playback controller.

---

## 8. Summary

### What matches the diagrams
- Database table schemas (structurally correct)
- Recommendation algorithm (warm path + cold-start fallback)
- Engagement profile aggregation logic
- Core interaction upsert logic (ToggleLike, RecordView, GetInteraction)
- Repository layer (InteractionRepository, ProfileRepository, PreferenceRepository) with interfaces
- Preference boost on like (+1.5 to UserMoviePreference) via PreferenceRepository
- DI registrations for all repositories and services
- Basic feed UI (FlipView, snap-scroll, loading, error, metadata overlay)
- Like button with correct visual states (outline/filled red)

### What diverges from the diagrams
- All `float` types → `double` in C# models
- Repository layer has extra methods beyond diagram (e.g. `GetLikeCountAsync`, `GetReelMovieIdAsync`)
- `ViewModelBase` replaced by `ObservableObject` (CommunityToolkit)
- View renamed `ReelsFeedView` → `ReelsFeedPage`
- Like toggle moved from ViewModel command to View code-behind
- `IClipPlaybackService` methods renamed with Async suffix
- `IEngagementProfileService.RecalculateProfileAsync` → `RefreshProfileAsync`

### What is missing entirely
- **Watch duration tracking timer** — `RecordViewAsync` is never called
- **`UserProfileViewModel`** — not implemented
- **`IClipPlaybackService.ResumeClip()`** and **`GetElapsedSeconds()`**
- **Genre tag pill badge** on reel overlay
- **Double-tap gesture**, **heart burst animation**, **scale-bounce animation**
- **Playback progress bar**
- **Empty state / retry UI**
