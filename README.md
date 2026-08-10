# 2DCore

2DCore is a cross-platform 2D scene editor written in C# with Avalonia UI. The main editor targets Windows, Linux and macOS from one `net9.0` project.

## Requirements

- .NET 9 SDK or newer.
- Windows 10/11, a desktop Linux distribution, or macOS.
- On Debian/Ubuntu Linux, Avalonia may require: `libx11-6 libice6 libsm6 libfontconfig1`.

## Run

```bash
git clone https://github.com/Teliar/2DCore.git
cd 2DCore
dotnet run
```

The root `2DCore.csproj` is the Avalonia application and does not use a Windows-specific target framework.

Build the complete solution and run tests:

```bash
dotnet build 2DCore.slnx
dotnet test tests/TwoDCore.Tests/TwoDCore.Tests.csproj
```

Create a framework-dependent Linux build from any supported development OS:

```bash
dotnet publish 2DCore.csproj -c Release -r linux-x64 --self-contained false
```

## Architecture

```text
2DCore/
├── 2DCore.csproj                    # Cross-platform Avalonia entry point
├── 2DCore.slnx
├── EditorAssets/                    # Icons and editor resources
├── src/
│   ├── TwoDCore.Core/               # Scene model, hierarchy, audio math, history
│   ├── TwoDCore.Persistence/        # DTOs, legacy-compatible JSON mapping, project I/O
│   └── TwoDCore.Editor/             # Avalonia views, controls and editor state
├── tests/
│   └── TwoDCore.Tests/              # Scene graph and persistence compatibility tests
└── legacy/
    └── TwoDCore.Editor.WinForms/    # Temporary Windows-only reference implementation
```

Dependency direction:

```text
Avalonia Editor → Persistence → Core
Avalonia Editor ─────────────→ Core
```

`Core` contains no WinForms, Avalonia, file-dialog or JSON dependencies. `Persistence` does not reference the UI.

## Current editor features

- Scene hierarchy with folders and protected drag-to-reparent behavior.
- Shape, image, folder, global sound, sound trigger and spatial sound objects.
- Custom-drawn zoomable and pannable viewport.
- Type-aware Inspector with volume and transparency sliders.
- Spatial sound radius and attenuation visualization.
- New/Open/Save/Save As with `.2dproj` and `.2dscene` v1 compatibility.
- Undo, redo, copy, paste, duplicate and delete.
- Cross-platform file and asset pickers.

## Project formats

- `.2dproj` stores project metadata and the start scene path.
- `.2dscene` stores object hierarchy and component data as readable JSON.

The persistence tests include a legacy v1 scene fixture to prevent accidental format breakage.

## Keyboard shortcuts

| Shortcut | Action |
|---|---|
| `Ctrl+N` | New project |
| `Ctrl+O` | Open project or scene |
| `Ctrl+S` | Save |
| `Ctrl+Shift+S` | Save As |
| `Ctrl+Z` | Undo |
| `Ctrl+Shift+Z` / `Ctrl+Y` | Redo |
| `Ctrl+C` / `Ctrl+V` | Copy / Paste |
| `Ctrl+D` | Duplicate |
| `Delete` | Delete selected object |

## Legacy WinForms editor

The previous implementation remains temporarily available as a migration reference:

```powershell
dotnet run --project legacy/TwoDCore.Editor.WinForms/TwoDCore.Editor.WinForms.csproj
```

It is not part of the cross-platform solution and will be removed after the Avalonia editor reaches complete feature parity.
