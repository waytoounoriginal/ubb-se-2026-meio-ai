# Agent Implementation Plan — ReelsEditing Feature (Beatrice)

## Project Context

- **Project:** `ubb-se-2026-meio-ai` — WinUI 3 / .NET 8 desktop app
- **Project root:** `C:\Users\cretu\Desktop\Semester4\ISS\ubb-se-2026-meio-ai\`
- **Namespace root:** `ubb_se_2026_meio_ai`
- **Pattern:** MVVM with `CommunityToolkit.Mvvm` (source-gen via `[ObservableProperty]`, `[RelayCommand]`)
- **DI:** `App.Services` (Microsoft.Extensions.DependencyInjection), resolved in page constructors via `App.Services.GetRequiredService<T>()`
- **DB:** Raw `Microsoft.Data.SqlClient` against `(localdb)\MSSQLLocalDB` / `MeioAiDb` — **no ORM**

## What Already Exists (DO NOT DELETE OR MODIFY)

| Path | Status |
|---|---|
| `Features/ReelsEditing/Services/IVideoProcessingService.cs` | ✅ Exists — keep as-is |
| `Features/ReelsEditing/Services/IAudioLibraryService.cs` | ✅ Exists — keep as-is |
| `Features/ReelsEditing/ViewModels/ReelsEditingViewModel.cs` | ⚠️ Stub — will be **replaced** |
| `Features/ReelsEditing/Views/ReelsEditingPage.xaml` | ⚠️ Stub — will be **replaced** |
| `Features/ReelsEditing/Views/ReelsEditingPage.xaml.cs` | ⚠️ Stub — will be **replaced** |
| `Core/Models/ReelModel.cs` | ✅ Shared — read-only |
| `Core/Models/MusicTrackModel.cs` | ✅ Shared — read-only |
| `Core/Database/SqlConnectionFactory.cs` | ✅ Shared — use as-is |

## Reference Implementation (Style Guide)

Use **ReelsUpload** as a style reference for page patterns:
- `Features/ReelsUpload/Views/ReelsUploadPage.xaml` — XAML Page structure
- `Features/ReelsUpload/Views/ReelsUploadPage.xaml.cs` — code-behind pattern
- `Features/ReelsUpload/ViewModels/ReelsUploadViewModel.cs` — ViewModel base pattern

**Pattern rules:**
- ViewModels inherit `ObservableObject` (partial class)
- Observable properties use `[ObservableProperty]` on private backing fields
- Commands use `[RelayCommand]` on async methods
- Pages resolve ViewModel from DI in constructor: `ViewModel = App.Services.GetRequiredService<TViewModel>();`
- Pages use `x:Bind ViewModel.Property, Mode=OneWay` in XAML

---

## Beatrice's Spec

From `Docs/Beatrice.md` and `Docs/class_diagram_Beatrice.md`:

**Requirements:**
1. User can select a previously uploaded reel from a gallery
2. User can define crop dimensions (CropX, CropY, CropWidth, CropHeight)
3. User can select background music from the MusicTrack table
4. On save: update `Reel.CropDataJson`, `Reel.BackgroundMusicId`, `Reel.LastEditedAt` in DB

**Class Diagram defines:**
- Models: `ReelModel` (shared), `MusicTrackModel` (shared), `VideoEditMetadata` (DTO — new)
- Services: `IVideoProcessingService`, `IAudioLibraryService`, `ReelRepository` 
- ViewModels: `ReelGalleryViewModel`, `ReelEditorViewModel`, `MusicSelectionDialogViewModel`
- Views: `ReelGalleryView`, `ReelEditorView`, `MusicSelectionDialogView`

---

## Files to Create / Modify

### 1. NEW — `VideoEditMetadata.cs` (DTO)
**Path:** `Features/ReelsEditing/Models/VideoEditMetadata.cs`

```csharp
namespace ubb_se_2026_meio_ai.Features.ReelsEditing.Models
{
    /// <summary>
    /// Local DTO holding in-progress crop + music edits before saving.
    /// Owner: Beatrice
    /// </summary>
    public class VideoEditMetadata
    {
        public int CropX { get; set; }
        public int CropY { get; set; }
        public int CropWidth { get; set; } = 1920;
        public int CropHeight { get; set; } = 1080;
        public int? SelectedMusicTrackId { get; set; }

        public string ToCropDataJson()
        {
            return $"{{\"x\":{CropX},\"y\":{CropY},\"width\":{CropWidth},\"height\":{CropHeight}}}";
        }
    }
}
```

---

### 2. NEW — `ReelRepository.cs`
**Path:** `Features/ReelsEditing/Services/ReelRepository.cs`

Implement a concrete class with raw SQL. Inject `ISqlConnectionFactory` from `Core.Database`.

```csharp
using Microsoft.Data.SqlClient;
using ubb_se_2026_meio_ai.Core.Database;
using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Features.ReelsEditing.Services
{
    public class ReelRepository
    {
        private readonly ISqlConnectionFactory _db;
        public ReelRepository(ISqlConnectionFactory db) => _db = db;

        // Returns all reels where CreatorUserId = userId
        public async Task<IList<ReelModel>> GetUserReelsAsync(int userId)
        {
            const string sql = "SELECT ReelId, MovieId, CreatorUserId, VideoUrl, ThumbnailUrl, Title, Caption, BackgroundMusicId, CropDataJson, Source, CreatedAt, LastEditedAt FROM Reel WHERE CreatorUserId = @UserId ORDER BY CreatedAt DESC";
            var result = new List<ReelModel>();
            await using var conn = await _db.CreateConnectionAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@UserId", userId);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new ReelModel
                {
                    ReelId = reader.GetInt32(0),
                    MovieId = reader.GetInt32(1),
                    CreatorUserId = reader.GetInt32(2),
                    VideoUrl = reader.GetString(3),
                    ThumbnailUrl = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                    Title = reader.GetString(5),
                    Caption = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                    BackgroundMusicId = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                    CropDataJson = reader.IsDBNull(8) ? null : reader.GetString(8),
                    Source = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                    CreatedAt = reader.GetDateTime(10),
                    LastEditedAt = reader.IsDBNull(11) ? null : reader.GetDateTime(11),
                });
            }
            return result;
        }

        // Updates CropDataJson, BackgroundMusicId, LastEditedAt for a reel
        public async Task UpdateReelEditsAsync(int reelId, string cropDataJson, int? musicId)
        {
            const string sql = "UPDATE Reel SET CropDataJson = @Crop, BackgroundMusicId = @MusicId, LastEditedAt = SYSUTCDATETIME() WHERE ReelId = @ReelId";
            await using var conn = await _db.CreateConnectionAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Crop", cropDataJson);
            cmd.Parameters.AddWithValue("@MusicId", (object?)musicId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ReelId", reelId);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
```

---

### 3. NEW — `AudioLibraryService.cs`
**Path:** `Features/ReelsEditing/Services/AudioLibraryService.cs`

Implements `IAudioLibraryService`. Reads from `MusicTrack` table.

```csharp
using Microsoft.Data.SqlClient;
using ubb_se_2026_meio_ai.Core.Database;
using ubb_se_2026_meio_ai.Core.Models;

namespace ubb_se_2026_meio_ai.Features.ReelsEditing.Services
{
    public class AudioLibraryService : IAudioLibraryService
    {
        private readonly ISqlConnectionFactory _db;
        public AudioLibraryService(ISqlConnectionFactory db) => _db = db;

        public async Task<IList<MusicTrackModel>> GetAllTracksAsync()
        {
            const string sql = "SELECT MusicTrackId, TrackName, AudioUrl, DurationSeconds FROM MusicTrack ORDER BY TrackName";
            var result = new List<MusicTrackModel>();
            await using var conn = await _db.CreateConnectionAsync();
            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new MusicTrackModel
                {
                    MusicTrackId = reader.GetInt32(0),
                    TrackName = reader.GetString(1),
                    AudioUrl = reader.GetString(2),
                    DurationSeconds = (float)reader.GetDouble(3),
                });
            }
            return result;
        }

        public async Task<MusicTrackModel?> GetTrackByIdAsync(int musicTrackId)
        {
            const string sql = "SELECT MusicTrackId, TrackName, AudioUrl, DurationSeconds FROM MusicTrack WHERE MusicTrackId = @Id";
            await using var conn = await _db.CreateConnectionAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", musicTrackId);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new MusicTrackModel
                {
                    MusicTrackId = reader.GetInt32(0),
                    TrackName = reader.GetString(1),
                    AudioUrl = reader.GetString(2),
                    DurationSeconds = (float)reader.GetDouble(3),
                };
            }
            return null;
        }
    }
}
```

---

### 4. NEW — `VideoProcessingService.cs`
**Path:** `Features/ReelsEditing/Services/VideoProcessingService.cs`

Implements `IVideoProcessingService`. Since actual ffmpeg processing is complex, implement it as a **metadata-only** stub that logs what it would do (consistent with the project's current level of implementation). Do not call ffmpeg — just return the original path and note the edit in a comment. 

```csharp
namespace ubb_se_2026_meio_ai.Features.ReelsEditing.Services
{
    /// <summary>
    /// Stub implementation of IVideoProcessingService.
    /// Records the requested edit operations; actual ffmpeg processing can be
    /// wired in later. Returns the original videoPath unchanged.
    /// Owner: Beatrice
    /// </summary>
    public class VideoProcessingService : IVideoProcessingService
    {
        public Task<string> ApplyCropAsync(string videoPath, string cropDataJson)
        {
            // Future: invoke ffmpeg to crop the video file
            // For now: editing metadata is persisted to DB; the path is unchanged
            return Task.FromResult(videoPath);
        }

        public Task<string> MergeAudioAsync(string videoPath, int musicTrackId, double startOffsetSec)
        {
            // Future: invoke ffmpeg to mix audio into video
            return Task.FromResult(videoPath);
        }
    }
}
```

---

### 5. NEW — `ReelGalleryViewModel.cs`
**Path:** `Features/ReelsEditing/ViewModels/ReelGalleryViewModel.cs`

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.ReelsEditing.Services;

namespace ubb_se_2026_meio_ai.Features.ReelsEditing.ViewModels
{
    public partial class ReelGalleryViewModel : ObservableObject
    {
        private readonly ReelRepository _reelRepository;

        // Hard-coded to UserId=1 (same as the rest of the app)
        private const int CurrentUserId = 1;

        [ObservableProperty]
        private ObservableCollection<ReelModel> _userReels = new();

        [ObservableProperty]
        private ReelModel? _selectedReel;

        [ObservableProperty]
        private string _statusMessage = "Select a reel to edit.";

        public ReelGalleryViewModel(ReelRepository reelRepository)
        {
            _reelRepository = reelRepository;
        }

        [RelayCommand]
        private async Task LoadReelsAsync()
        {
            StatusMessage = "Loading reels...";
            try
            {
                var reels = await _reelRepository.GetUserReelsAsync(CurrentUserId);
                UserReels.Clear();
                foreach (var reel in reels)
                    UserReels.Add(reel);
                StatusMessage = UserReels.Count > 0
                    ? $"{UserReels.Count} reel(s) found."
                    : "No reels uploaded yet. Upload a reel first.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading reels: {ex.Message}";
            }
        }
    }
}
```

---

### 6. NEW — `MusicSelectionDialogViewModel.cs`
**Path:** `Features/ReelsEditing/ViewModels/MusicSelectionDialogViewModel.cs`

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.ReelsEditing.Services;

namespace ubb_se_2026_meio_ai.Features.ReelsEditing.ViewModels
{
    public partial class MusicSelectionDialogViewModel : ObservableObject
    {
        private readonly IAudioLibraryService _audioLibrary;

        [ObservableProperty]
        private ObservableCollection<MusicTrackModel> _availableTracks = new();

        [ObservableProperty]
        private MusicTrackModel? _selectedTrack;

        public MusicSelectionDialogViewModel(IAudioLibraryService audioLibrary)
        {
            _audioLibrary = audioLibrary;
        }

        [RelayCommand]
        private async Task LoadTracksAsync()
        {
            var tracks = await _audioLibrary.GetAllTracksAsync();
            AvailableTracks.Clear();
            foreach (var t in tracks)
                AvailableTracks.Add(t);
        }

        [RelayCommand]
        private void SelectTrack(MusicTrackModel track)
        {
            SelectedTrack = track;
        }
    }
}
```

---

### 7. REPLACE — `ReelsEditingViewModel.cs`
**Path:** `Features/ReelsEditing/ViewModels/ReelsEditingViewModel.cs`

Replace the existing stub entirely:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ubb_se_2026_meio_ai.Core.Models;
using ubb_se_2026_meio_ai.Features.ReelsEditing.Models;
using ubb_se_2026_meio_ai.Features.ReelsEditing.Services;

namespace ubb_se_2026_meio_ai.Features.ReelsEditing.ViewModels
{
    /// <summary>
    /// ViewModel for the main Reel Editor workspace.
    /// Owner: Beatrice
    /// </summary>
    public partial class ReelsEditingViewModel : ObservableObject
    {
        private readonly ReelRepository _reelRepository;
        private readonly IVideoProcessingService _videoProcessing;

        [ObservableProperty]
        private ReelModel? _selectedReel;

        [ObservableProperty]
        private VideoEditMetadata _currentEdits = new();

        [ObservableProperty]
        private MusicTrackModel? _selectedMusicTrack;

        [ObservableProperty]
        private string _statusMessage = "Select a reel to edit.";

        [ObservableProperty]
        private bool _isSaving;

        public ReelsEditingViewModel(
            ReelRepository reelRepository,
            IVideoProcessingService videoProcessing)
        {
            _reelRepository = reelRepository;
            _videoProcessing = videoProcessing;
        }

        public void LoadReel(ReelModel reel)
        {
            SelectedReel = reel;
            // Pre-populate crop if saved previously
            CurrentEdits = new VideoEditMetadata();
            SelectedMusicTrack = null;
            StatusMessage = $"Editing: {reel.Title}";
        }

        public void ApplyMusicSelection(MusicTrackModel track)
        {
            SelectedMusicTrack = track;
            CurrentEdits.SelectedMusicTrackId = track.MusicTrackId;
            StatusMessage = $"Music selected: {track.TrackName}";
        }

        [RelayCommand]
        private async Task SaveEditsAsync()
        {
            if (SelectedReel == null)
            {
                StatusMessage = "No reel selected.";
                return;
            }

            IsSaving = true;
            StatusMessage = "Saving edits...";
            try
            {
                string cropJson = CurrentEdits.ToCropDataJson();
                int? musicId = CurrentEdits.SelectedMusicTrackId;

                // Apply processing (stub — returns original path)
                await _videoProcessing.ApplyCropAsync(SelectedReel.VideoUrl, cropJson);
                if (musicId.HasValue)
                    await _videoProcessing.MergeAudioAsync(SelectedReel.VideoUrl, musicId.Value, 0);

                // Persist to DB
                await _reelRepository.UpdateReelEditsAsync(SelectedReel.ReelId, cropJson, musicId);

                StatusMessage = "Edits saved successfully!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Save failed: {ex.Message}";
            }
            finally
            {
                IsSaving = false;
            }
        }

        [RelayCommand]
        private void ResetEdits()
        {
            CurrentEdits = new VideoEditMetadata();
            SelectedMusicTrack = null;
            StatusMessage = "Edits reset.";
        }
    }
}
```

---

### 8. REPLACE — `ReelsEditingPage.xaml`
**Path:** `Features/ReelsEditing/Views/ReelsEditingPage.xaml`

The page is a two-panel layout:
- **Left panel:** Reel gallery list (scrollable) — uses `ReelGalleryViewModel`
- **Right panel:** Editor workspace with crop sliders, music section, and save button — uses `ReelsEditingViewModel`

The page should follow the same XAML structure as `ReelsUploadPage.xaml`:
- `x:Class` = `ubb_se_2026_meio_ai.Features.ReelsEditing.Views.ReelsEditingPage`
- `Background="{ThemeResource ApplicationPageBackgroundThemeBrush}"`
- Bind to `ViewModel.Property` using `x:Bind`

**XAML layout structure to implement:**

```xml
<?xml version="1.0" encoding="utf-8"?>
<Page
    x:Class="ubb_se_2026_meio_ai.Features.ReelsEditing.Views.ReelsEditingPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    mc:Ignorable="d"
    Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">

    <Grid Padding="24" ColumnSpacing="24">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="280"/>
            <ColumnDefinition Width="*"/>
        </Grid.ColumnDefinitions>

        <!-- LEFT: Reel Gallery -->
        <StackPanel Grid.Column="0" Spacing="12">
            <TextBlock Text="Your Reels" Style="{StaticResource SubtitleTextBlockStyle}"/>
            <Button Content="Load Reels" Command="{x:Bind GalleryViewModel.LoadReelsCommand}" HorizontalAlignment="Stretch"/>
            <TextBlock Text="{x:Bind GalleryViewModel.StatusMessage, Mode=OneWay}"
                       Style="{StaticResource CaptionTextBlockStyle}"
                       TextWrapping="Wrap"/>
            <ListView ItemsSource="{x:Bind GalleryViewModel.UserReels, Mode=OneWay}"
                      SelectedItem="{x:Bind GalleryViewModel.SelectedReel, Mode=TwoWay}"
                      SelectionChanged="ReelListView_SelectionChanged"
                      MaxHeight="500">
                <ListView.ItemTemplate>
                    <DataTemplate>
                        <StackPanel Padding="8,4">
                            <TextBlock Text="{Binding Title}" FontWeight="SemiBold" TextTrimming="CharacterEllipsis"/>
                            <TextBlock Text="{Binding Source}" Style="{StaticResource CaptionTextBlockStyle}"
                                       Foreground="{ThemeResource SystemControlForegroundBaseMediumBrush}"/>
                        </StackPanel>
                    </DataTemplate>
                </ListView.ItemTemplate>
            </ListView>
        </StackPanel>

        <!-- RIGHT: Editor Workspace -->
        <ScrollViewer Grid.Column="1">
            <StackPanel Spacing="20">
                <TextBlock Text="Reel Editor" Style="{StaticResource TitleTextBlockStyle}"/>
                <TextBlock Text="{x:Bind ViewModel.StatusMessage, Mode=OneWay}"
                           Style="{StaticResource SubtitleTextBlockStyle}"
                           Foreground="{ThemeResource SystemControlForegroundBaseMediumBrush}"
                           TextWrapping="Wrap"/>

                <!-- Crop Section -->
                <Border Background="{ThemeResource CardBackgroundFillColorDefaultBrush}"
                        CornerRadius="8" Padding="16">
                    <StackPanel Spacing="12">
                        <TextBlock Text="Crop Settings" FontWeight="SemiBold"/>

                        <StackPanel Spacing="4">
                            <TextBlock Text="Crop X"/>
                            <Slider Minimum="0" Maximum="1920" StepFrequency="1"
                                    Value="{x:Bind ViewModel.CurrentEdits.CropX, Mode=TwoWay}"/>
                        </StackPanel>
                        <StackPanel Spacing="4">
                            <TextBlock Text="Crop Y"/>
                            <Slider Minimum="0" Maximum="1080" StepFrequency="1"
                                    Value="{x:Bind ViewModel.CurrentEdits.CropY, Mode=TwoWay}"/>
                        </StackPanel>
                        <StackPanel Spacing="4">
                            <TextBlock Text="Width"/>
                            <Slider Minimum="100" Maximum="1920" StepFrequency="1"
                                    Value="{x:Bind ViewModel.CurrentEdits.CropWidth, Mode=TwoWay}"/>
                        </StackPanel>
                        <StackPanel Spacing="4">
                            <TextBlock Text="Height"/>
                            <Slider Minimum="100" Maximum="1080" StepFrequency="1"
                                    Value="{x:Bind ViewModel.CurrentEdits.CropHeight, Mode=TwoWay}"/>
                        </StackPanel>
                    </StackPanel>
                </Border>

                <!-- Music Section -->
                <Border Background="{ThemeResource CardBackgroundFillColorDefaultBrush}"
                        CornerRadius="8" Padding="16">
                    <StackPanel Spacing="8">
                        <TextBlock Text="Background Music" FontWeight="SemiBold"/>
                        <TextBlock Text="{x:Bind ViewModel.SelectedMusicTrack.TrackName, Mode=OneWay, FallbackValue='No music selected'}"
                                   Foreground="{ThemeResource SystemControlForegroundBaseMediumBrush}"/>
                        <Button Content="Choose Music..." Click="ChooseMusicButton_Click"/>
                    </StackPanel>
                </Border>

                <!-- Action Buttons -->
                <StackPanel Orientation="Horizontal" Spacing="12">
                    <Button Content="Save Edits"
                            Command="{x:Bind ViewModel.SaveEditsCommand}"
                            IsEnabled="{x:Bind ViewModel.IsSaving, Mode=OneWay, Converter={StaticResource BoolNegationConverter}}"
                            Style="{StaticResource AccentButtonStyle}"/>
                    <Button Content="Reset" Command="{x:Bind ViewModel.ResetEditsCommand}"/>
                </StackPanel>

                <!-- Owner tag -->
                <TextBlock Text="Owner: Beatrice"
                           Style="{StaticResource CaptionTextBlockStyle}"
                           Foreground="{ThemeResource SystemControlForegroundBaseMediumLowBrush}"/>
            </StackPanel>
        </ScrollViewer>
    </Grid>
</Page>
```

> [!IMPORTANT]
> The XAML uses a `BoolNegationConverter`. You must add this converter to `App.xaml` resources, OR simplify the IsEnabled binding to just not use a converter (set `IsEnabled="True"` and handle it in the ViewModel). The simplest approach: skip the converter, just bind `IsEnabled` to a computed property `CanSave` in the ViewModel.

---

### 9. REPLACE — `ReelsEditingPage.xaml.cs`
**Path:** `Features/ReelsEditing/Views/ReelsEditingPage.xaml.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using ubb_se_2026_meio_ai.Features.ReelsEditing.ViewModels;

namespace ubb_se_2026_meio_ai.Features.ReelsEditing.Views
{
    public sealed partial class ReelsEditingPage : Page
    {
        public ReelsEditingViewModel ViewModel { get; }
        public ReelGalleryViewModel GalleryViewModel { get; }
        public MusicSelectionDialogViewModel MusicDialogViewModel { get; }

        public ReelsEditingPage()
        {
            ViewModel = App.Services.GetRequiredService<ReelsEditingViewModel>();
            GalleryViewModel = App.Services.GetRequiredService<ReelGalleryViewModel>();
            MusicDialogViewModel = App.Services.GetRequiredService<MusicSelectionDialogViewModel>();
            this.InitializeComponent();
        }

        private void ReelListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GalleryViewModel.SelectedReel != null)
                ViewModel.LoadReel(GalleryViewModel.SelectedReel);
        }

        private async void ChooseMusicButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            await MusicDialogViewModel.LoadTracksCommand.ExecuteAsync(null);

            var dialog = new ContentDialog
            {
                Title = "Choose Background Music",
                CloseButtonText = "Cancel",
                PrimaryButtonText = "Confirm",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot,
            };

            var listView = new ListView
            {
                ItemsSource = MusicDialogViewModel.AvailableTracks,
                Height = 300,
            };
            listView.ItemTemplate = (Microsoft.UI.Xaml.DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(
                "<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">" +
                "<TextBlock Text=\"{Binding TrackName}\" Padding=\"8,4\"/>" +
                "</DataTemplate>");

            listView.SelectionChanged += (s, args) =>
            {
                if (listView.SelectedItem is ubb_se_2026_meio_ai.Core.Models.MusicTrackModel track)
                    MusicDialogViewModel.SelectedTrack = track;
            };

            dialog.Content = listView;

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && MusicDialogViewModel.SelectedTrack != null)
                ViewModel.ApplyMusicSelection(MusicDialogViewModel.SelectedTrack);
        }
    }
}
```

---

### 10. MODIFY — `App.xaml.cs`
**Path:** `App.xaml.cs`

In the `ConfigureServices` method, register the new services. The existing `ReelsEditingViewModel` registration must be updated. Add after the existing `services.AddTransient<ReelsEditingViewModel>()` line (around line 169):

```csharp
// ── Beatrice (Reels Editing) ──
services.AddTransient<ReelsEditingViewModel>();       // replaces existing stub registration
services.AddTransient<ReelGalleryViewModel>();
services.AddTransient<MusicSelectionDialogViewModel>();
services.AddTransient<ubb_se_2026_meio_ai.Features.ReelsEditing.Services.ReelRepository>();
services.AddTransient<ubb_se_2026_meio_ai.Features.ReelsEditing.Services.IAudioLibraryService,
                      ubb_se_2026_meio_ai.Features.ReelsEditing.Services.AudioLibraryService>();
services.AddTransient<ubb_se_2026_meio_ai.Features.ReelsEditing.Services.IVideoProcessingService,
                      ubb_se_2026_meio_ai.Features.ReelsEditing.Services.VideoProcessingService>();
```

> [!IMPORTANT]
> Remove the existing `services.AddTransient<ReelsEditingViewModel>();` line at line 169 and replace it with the block above to avoid duplicate registrations.

---

## File Summary

| File | Action |
|---|---|
| `Features/ReelsEditing/Models/VideoEditMetadata.cs` | 🆕 Create |
| `Features/ReelsEditing/Services/ReelRepository.cs` | 🆕 Create |
| `Features/ReelsEditing/Services/AudioLibraryService.cs` | 🆕 Create |
| `Features/ReelsEditing/Services/VideoProcessingService.cs` | 🆕 Create |
| `Features/ReelsEditing/ViewModels/ReelGalleryViewModel.cs` | 🆕 Create |
| `Features/ReelsEditing/ViewModels/MusicSelectionDialogViewModel.cs` | 🆕 Create |
| `Features/ReelsEditing/ViewModels/ReelsEditingViewModel.cs` | ✏️ Replace stub |
| `Features/ReelsEditing/Views/ReelsEditingPage.xaml` | ✏️ Replace stub |
| `Features/ReelsEditing/Views/ReelsEditingPage.xaml.cs` | ✏️ Replace stub |
| `App.xaml.cs` | ✏️ Update DI registrations |
| `Features/ReelsEditing/Services/IVideoProcessingService.cs` | ✅ Keep unchanged |
| `Features/ReelsEditing/Services/IAudioLibraryService.cs` | ✅ Keep unchanged |

---

## Verification Steps

### Step 1 — Build
In Visual Studio 2022, set platform to **x64** (not AnyCPU) and press **Ctrl+Shift+B**.  
Expected: **0 errors**. Nullable warnings in other features are acceptable.

### Step 2 — Run the app (F5)
The app should launch showing the navigation sidebar. Click **"Reels Editing"**.  
Expected: The two-panel editor page loads without crashing.

### Step 3 — Test Load Reels
Click **"Load Reels"** button.  
Expected: Either shows "No reels uploaded yet." (if DB is empty) or shows a list of reels from `UserId=1`.

### Step 4 — Test Select Reel
If reels exist, click one in the list.  
Expected: The status message changes to "Editing: [reel title]".

### Step 5 — Test Crop Sliders
Move the crop sliders.  
Expected: Values update (no crash).

### Step 6 — Test Music Dialog
Click "Choose Music…".  
Expected: A ContentDialog opens. If `MusicTrack` table is empty → list is empty. If populated → tracks are listed.  
To seed a test music track, run in SSMS or sqlcmd:
```sql
USE MeioAiDb;
INSERT INTO MusicTrack (TrackName, AudioUrl, DurationSeconds) VALUES ('Test Track', 'https://example.com/test.mp3', 180.0);
```
Then click "Choose Music…" again — "Test Track" should appear. Select it and confirm. The editor should show "Music selected: Test Track".

### Step 7 — Test Save
With a reel selected, click **"Save Edits"**.  
Expected: Status shows "Saving edits…" then "Edits saved successfully!".  
Verify in DB:
```sql
SELECT ReelId, CropDataJson, BackgroundMusicId, LastEditedAt FROM Reel WHERE ReelId = <id>;
```
`CropDataJson` should contain JSON with x/y/width/height values. `LastEditedAt` should be a recent timestamp.
