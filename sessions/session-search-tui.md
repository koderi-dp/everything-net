# Everything.Net Search TUI Session Handoff

## Scope

This document captures the current state of the interactive `Everything.Net.Search` finder so a new session can continue without re-discovering the design and implementation decisions from scratch.

## Current UI Direction

- The interactive finder has been migrated from `Spectre.Console` live rendering to `Spectre.Tui`.
- The design goal is a terminal UI with:
  - a simple inline query line at the top
  - a left results pane with filename as the primary line and full path as the secondary line
  - a right preview pane with content first and compact metadata below
  - a dark base background with accented borders, but no panel fill background
- The current visual language was tuned toward a softer, warmer palette:
  - accent headers
  - pink search prompt
  - muted secondary text
  - active pane border highlight
  - selected row highlight
- The hard-coded dark gray background fill has been removed; the TUI now uses the terminal default background instead.

## Key Architectural Decisions

### 1. Search Backend Model

- The interactive finder does not keep an arbitrarily large in-memory result viewport.
- Instead it uses Everything's native paging/windowing model.
- The current page size is dynamic:
  - `SearchCliOptions.Limit` is updated to match how many result items fit in the results pane
  - `Offset` is used to fetch the current page from Everything
- Results are paged from the backend when moving beyond the current page bounds.

### 2. Result Item Layout

- Results are rendered as multi-line items in `FinderTuiRenderer`.
- Current layout:
  - line 1: row number + filename
  - line 2: full parent path
  - optional line 3 when details are enabled
- Path is shown in full and naturally clipped by the viewport width.

### 3. Preview Pane Layout

- Preview pane has three bands:
  - compact header
  - scrollable content body
  - compact metadata footer
- Metadata footer currently shows:
  - type
  - size
  - extension
  - modified date
- The preview pane no longer mixes a panel background with terminal black; pane content is back on the base background.
- Preview lines can now carry multiple styled spans, which enables syntax-highlighted code preview without changing the scroll model.

### 4. Focus Model

- A real pane focus model has been added.
- `FinderViewState` now tracks focus via `FinderPaneFocus`.
- `Tab` switches focus between `Results` and `Preview`.
- Active pane border is visually highlighted.

## Current Keybindings

### Global

- `Tab`: switch focus between results and preview
- `Esc`: exit
- `F1`: toggle help
- `F2`: toggle details
- `F3`: cycle sort
- `F4`: toggle regex
- `F5`: refresh search
- `F6`: open the selected file or folder with the default shell action
- `F7`: reveal the selected item in Windows Explorer

### Results Focus

- `Up` / `Down`: move selection within the current page
- `PageUp` / `PageDown`: fetch previous/next page
- Reaching above the first item or below the last item also pages through Everything results

### Preview Focus

- `Up` / `Down`: scroll preview by one line
- `PageUp` / `PageDown`: scroll preview by one page
- `Home` / `End`: jump preview to start/end

### Filter / Query Shortcuts

- Typing updates the query
- `Backspace`: delete
- `Ctrl+L`: clear query
- `Ctrl+F`: files filter
- `Ctrl+A`: all filter
- `Ctrl+O`: folders filter
- `Ctrl+P`: toggle match-path
- `Ctrl+W`: toggle whole-word
- `Ctrl+C`: toggle case
- `Ctrl+R`: toggle regex

## Important Implementation Notes

### Syntax Highlighting

- `FinderPreviewContent` now carries `FinderPreviewLine` and `FinderPreviewSpan` objects instead of raw string lines.
- `SyntaxHighlightingService` uses `TextMateSharp` with extension-based grammar lookup from `TextMateSharp.Grammars`.
- The current token-to-style mapping uses a compact warm palette for comments, strings, numbers, keywords, types, function/member names, and punctuation/operators.
- If grammar lookup or tokenization fails, preview rendering falls back to plain text spans.

### Size Availability

- The list design now depends on size being available even when details are off.
- `SearchQueryFactory` was changed so interactive queries always request:
  - `FileName`
  - `Path`
  - `Extension`
  - `Size`
- `DateModified` is still only requested when `ShowDetails` is enabled.

### Preview Scroll State

- `PreviewScrollOffset` is preserved in state and clamped against visible preview lines.
- Selection changes reset preview scroll to `0`.

### Dynamic Result Capacity

- `FinderTuiRenderer.VisibleResultRows` reflects how many items fit, not raw terminal rows.
- This matters because result items are multi-line.
- `LiveFinderSession` updates `Options.Limit` to match that visible capacity and triggers a refetch when needed.

## Files Touched In This Session

- `src/Everything.Net.Search/Models/FinderViewState.cs`
- `src/Everything.Net.Search/Models/FinderPaneFocus.cs`
- `src/Everything.Net.Search/Models/FinderPreviewContent.cs`
- `src/Everything.Net.Search/Models/FinderPreviewLine.cs`
- `src/Everything.Net.Search/Models/FinderPreviewSpan.cs`
- `src/Everything.Net.Search/Rendering/FinderTuiRenderer.cs`
- `src/Everything.Net.Search/Rendering/SearchConsoleRenderer.cs`
- `src/Everything.Net.Search/Services/FinderPreviewService.cs`
- `src/Everything.Net.Search/Services/SyntaxHighlightingService.cs`
- `src/Everything.Net.Search/Services/LiveFinderSession.cs`
- `src/Everything.Net.Search/Services/SearchQueryFactory.cs`
- `src/Everything.Net.Search/Everything.Net.Search.csproj`
- `src/Directory.Packages.props`

## Current Known State

- The interactive finder builds successfully.
- If `Everything.Net.Search` is currently running, normal builds to `bin/Debug` can fail because the output DLL or EXE is locked.
- A reliable verification workaround used during this session was:

```powershell
dotnet build Everything.Net.Search\Everything.Net.Search.csproj /p:UseAppHost=false /p:OutDir=C:\Source\projects\everything-net\src\artifacts\verify\
```

## Recommended Next Steps

- Refine the current color palette further if needed, but preserve the no-panel-fill decision.
- Consider making the footer less verbose now that focus is visible through the active border.
- Consider reducing the top metadata density if the query line should dominate more strongly.
- If preview metadata still feels noisy, reduce it to a single short line or make it conditional on file type.

## Where To Resume

If a new session continues from here, start by opening:

- `src/Everything.Net.Search/Rendering/FinderTuiRenderer.cs`
- `src/Everything.Net.Search/Services/LiveFinderSession.cs`
- `src/Everything.Net.Search/Models/FinderViewState.cs`
- `src/Everything.Net.Search/Models/FinderPaneFocus.cs`

Those files contain nearly all of the current interaction, rendering, focus, paging, and preview behavior.

## Session Status

- Reopened on 2026-04-07 and adopted as the active session note.
- Updated `FinderTuiRenderer` so the base TUI background uses `Color.Default` instead of a custom dark RGB fill.
- Added a first-pass syntax-highlighted preview path using `TextMateSharp` and `TextMateSharp.Grammars`.
- Updated the preview renderer to support multi-span lines while preserving the current preview scrolling behavior.
- Added TUI actions to open the selected item or reveal it in Explorer.
- Verification build succeeded via the `src/artifacts/verify` output path.
