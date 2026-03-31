# MEIO AI

A WinUI 3 desktop application built with .NET 8 and the Windows App SDK.

## Prerequisites

- **Visual Studio 2022/2026** (version 17.8 or later)
- **.NET 8 SDK**
- **Windows App SDK** workload
- **SQL Server** (LocalDB or a full instance) for the database

## Setup in Visual Studio

1. **Clone the repository**
   ```
   git clone https://github.com/your-org/ubb-se-2026-meio-ai.git
   ```
2. **Open the solution** — double-click `ubb-se-2026-meio-ai.sln`.
3. **Install workloads** — if prompted, install the **.NET Desktop Development** and **Windows application development** workloads via the Visual Studio Installer.
4. **Restore NuGet packages** — Visual Studio restores them automatically on build, or run `dotnet restore` from the solution directory.
5. **Set the target platform** — select **x64** (or x86 / ARM64) from the platform dropdown in the toolbar.
6. **Build & Run** — press **F5** or click the green play button. The app is packaged with MSIX.

> **Optional:** Some features (Reels Editing) require `ffmpeg.exe` and `ffprobe.exe` in a `tools/ffmpeg/` folder at the project root. Download them from [ffmpeg.org](https://ffmpeg.org/download.html) and place them there.

## Project Structure

```
├── Core/                   # Shared foundation
│   ├── Converters/         # XAML value converters
│   ├── Database/           # SQL connection factory & DB initializer
│   ├── Models/             # Domain models (Movie, Reel, User, etc.)
│   └── Services/           # Core service interfaces (navigation, dialogs)
│
├── Features/               # Feature modules (each follows MVVM)
│   ├── MovieSwipe/
│   ├── MovieTournament/
│   ├── PersonalityMatch/
│   ├── ReelsEditing/
│   ├── ReelsFeed/
│   ├── ReelsUpload/
│   └── TrailerScraping/
│
├── Assets/                 # App icons and splash screens
├── Assignment1/            # Initial assignment deliverables (diagrams, report)
├── Docs/                   # Per-member documentation and diagrams
└── Properties/             # Launch settings and publish profiles
```

Each feature folder contains its own `Views/`, `ViewModels/`, `Services/`, and optionally `Models/` or `Repositories/` subfolders.

## Documentation

- **Assignment report & requirements** — `Assignment1/Report.pdf`, `Assignment1/Requirements.pdf`
- **Database diagram** — `Assignment1/DB_Diagram_PNG.png` (source: `Docs/dbdiagram_io.md`)
- **UML class diagram** — `Assignment1/uml_class_diagram.pdf` (source: `Docs/uml_class_diagram.md`)
- **Use-case diagram** — `Assignment1/UseCase_Diagram.png` (source: `Docs/use_case_diagram.md`)
- **Per-member docs & class diagrams** — see the individual `.md` files in `Docs/`

## Key Dependencies

| Package | Purpose |
|---|---|
| Microsoft.WindowsAppSDK | WinUI 3 framework |
| CommunityToolkit.Mvvm | MVVM helpers and source generators |
| Microsoft.Data.SqlClient | SQL Server database access |
| Google.Apis.YouTube.v3 | YouTube Data API integration |
| YoutubeDLSharp | Video downloading for trailer scraping |
| Microsoft.Extensions.DependencyInjection | DI container |
