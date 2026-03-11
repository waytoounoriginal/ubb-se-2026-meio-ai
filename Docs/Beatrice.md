### 1. Formal Requirements
*   **Requirement 1:** The system must allow an authenticated user to select a previously uploaded reel for editing.
*   **Requirement 2:** The system must provide functionality for the user to define crop dimensions (x/y coordinates, width, and height) for the selected reel.
*   **Requirement 3:** The system must allow the user to select a background music track from an available library and apply it to their reel.
*   **Requirement 4:** The system must persist the edited reel state (crop metadata via `CropDataJson` and selected music track via `BackgroundMusicId`) by updating the existing `Reel` row in the shared table.
*   **Owner:** Beatrice
*   **Cross-Team Dependencies:**
    *   **Alex:** Alex owns the `Reel` table schema — Beatrice reads/writes crop and music columns on existing `Reel` rows uploaded by Alex's feature.
    *   **Tudor:** Edited reels (with crop + music) will appear in Tudor's reels feed — coordinate on `ReelModel` and `CropDataJson`/`BackgroundMusicId` columns.

### 2. Diagram Blueprint
*   **Use Case Diagram Additions:**
    *   *Actor:* Authenticated User
    *   *Use Cases:* `Select Uploaded Reel`, `Crop Reel Video`, `Add Background Music`, `Save Reel Edits`
*   **Database Schema Additions:**
    *   *(This feature does NOT create new tables. It reads/writes to the shared `Reel` table and reads from the shared `MusicTrack` table.)*
    *   **Shared Table: `Reel`** — Uses existing columns: `CropDataJson` (JSON — stores x, y, width, height) and `BackgroundMusicId` (FK → MusicTrack, nullable). Updates `LastEditedAt` on save.
    *   **Shared Table: `MusicTrack`** (MusicTrackId PK, TrackName VARCHAR, AudioUrl VARCHAR, DurationSeconds INT) — Read-only source of available background music.
*   **Class Diagram (MVVM) Additions:**
    *   *Models:* `ReelModel` (shared), `MusicTrackModel`, `VideoEditMetadata` (local DTO for in-progress edits)
    *   *Views:* `ReelGalleryView` (for choosing the reel to edit), `ReelEditorView` (main crop/music workspace), `MusicSelectionDialogView` (library pop-up)
    *   *ViewModels:* `ReelGalleryViewModel`, `ReelEditorViewModel`, `MusicSelectionDialogViewModel`
    *   *Utils/Services:* `IVideoProcessingService` (for handling media crop/merge), `IAudioLibraryService` (to fetch available music from `MusicTrack` table)

### 3. Project Management Tasks (Max 30-Minutes Each)

**Database & Models**
*   **Task:** Define `MusicTrack` Table Schema
    *   **Description:** Design and create the database migration for the `MusicTrack` table with columns: `MusicTrackId` (PK), `TrackName` (VARCHAR), `AudioUrl` (VARCHAR), `DurationSeconds` (INT). Define constraints (NOT NULL on TrackName, AudioUrl). Max 30 mins effort.
*   **Task:** Create `MusicTrackModel` Data Class
    *   **Description:** Define the Model class for the `MusicTrack` table: `MusicTrackId`, `TrackName`, `AudioUrl`, `DurationSeconds`. Max 30 mins effort.
*   **Task:** Create `ReelModel` Data Class
    *   **Description:** Define the Model class mirroring the shared `Reel` table: `ReelId`, `MovieId`, `CreatorUserId`, `VideoUrl`, `Title`, `CropDataJson`, `BackgroundMusicId`, `CreatedAt`, `LastEditedAt`. This is the same shared model used by all reel features. Max 30 mins effort.
*   **Task:** Create `VideoEditMetadata` Wrapper Model
    *   **Description:** Create a local data model `VideoEditMetadata` to temporarily hold the user's ongoing edits (current crop coordinates, tentative music selection) before saving. Max 30 mins effort.
*   **Task:** Implement Reel Update Repository Method (Crop + Music)
    *   **Description:** Write the repository method to update an existing `Reel` row's `CropDataJson`, `BackgroundMusicId`, and `LastEditedAt` columns. Max 30 mins effort.
*   **Task:** Implement Music Track Listing Query
    *   **Description:** Write the repository method to fetch all available `MusicTrack` rows for display in the music selection dialog. Max 30 mins effort.
*   **Task:** Implement User Reels Query Method
    *   **Description:** Write the repository method to fetch all `Reel` rows where `CreatorUserId` matches the current user, for display in the reel gallery. Max 30 mins effort.

**Backend Services & ViewModels**
*   **Task:** Scaffold `ReelGalleryViewModel`
    *   **Description:** Create the ViewModel for the reel gallery screen. Define an `ObservableCollection<ReelModel>` for the user's reels and a `LoadReelsCommand` that fetches from the repository. Max 30 mins effort.
*   **Task:** Scaffold `ReelEditorViewModel`
    *   **Description:** Create the ViewModel class. Define observable properties for `SelectedReel`, `CurrentCropCoordinates`, and `SelectedMusicTrack`. Max 30 mins effort.
*   **Task:** Create Save Edits Command in `ReelEditorViewModel`
    *   **Description:** Implement a `SaveEditsCommand` that collects `CropDataJson` and `BackgroundMusicId`, calls the repository update method, and calls `IVideoProcessingService` for processing. Max 30 mins effort.
*   **Task:** Define `IVideoProcessingService` Contract
    *   **Description:** Design the interface with method signatures for `ApplyCrop()` and `MergeAudioTrack()`. Max 30 mins effort.
*   **Task:** Define `IAudioLibraryService` Contract
    *   **Description:** Design the interface with a `GetAvailableTracksAsync()` method returning a list of `MusicTrackModel`. Max 30 mins effort.
*   **Task:** Scaffold `MusicSelectionDialogViewModel`
    *   **Description:** Create the ViewModel. Inject `IAudioLibraryService` and create an observable list of `MusicTrackModel` to populate the dialog. Max 30 mins effort.
*   **Task:** Create `SelectTrackCommand`
    *   **Description:** Inside `MusicSelectionDialogViewModel`, implement a command that captures the user's selected track and passes the ID back to `ReelEditorViewModel`. Max 30 mins effort.

**GUI (Views)**
*   **Task:** Mockup `ReelGalleryView` Layout
    *   **Description:** Design the layout for `ReelGalleryView` displaying a grid of the user's existing reels (filtered to user-uploaded only) ready for editing. Max 30 mins effort.
*   **Task:** Mockup `ReelEditorView` — Media Player Area
    *   **Description:** Construct the center layout for `ReelEditorView` holding the video preview playback component. Max 30 mins effort.
*   **Task:** Mockup `ReelEditorView` — Cropping Interface
    *   **Description:** Add UI specifications (sliders or bounded rectangle overlay) for the cropping experience. Bind to ViewModel crop coordinates. Max 30 mins effort.
*   **Task:** Mockup `ReelEditorView` — Music Action Bar
    *   **Description:** Design a bottom-action-bar with "Add Music" button and label for the currently active track name. Max 30 mins effort.
*   **Task:** Mockup `MusicSelectionDialogView` Layout
    *   **Description:** Design a pop-up modal with a scrollable list of tracks and a "Confirm" button. Bind to ViewModel's track list. Max 30 mins effort.
