# LM-Stats Project - Development Session Notes

## Project Overview
- **Stack**: ASP.NET Core (.NET 8), Razor Views, EF Core
- **Branch**: Excel-sheet-integration-v2
- **Repo**: https://github.com/iamasghar/LM-Stats
- **Key files**:
  - `Controllers/ReportController.cs`
  - `Views/Report/Index.cshtml` (main weekly report)
  - `Views/Report/PlayerExplorer.cshtml` (single player view)
  - `Views/Shared/_Layout.cshtml`
  - `Data/Models/StatsSummary.cs`
  - `wwwroot/css/site.css`, `wwwroot/js/site.js`
- **Client libs**: Bootstrap 5, jQuery, Chart.js, html2canvas, jsPDF, jspdf-autotable, TomSelect

---

## Issues Fixed & Changes Made

### 1. Player Explorer - All Weeks History Not Showing
- **Fix**: `ReportController.GetPlayerDetails` changed to resolve player by normalized name -> get `UserId` -> fetch ALL rows for that `UserId` ordered by week.

### 2. Player Explorer - Nav Link in Header
- **Fix**: Added `Player Explorer` nav link to `Views/Shared/_Layout.cshtml`.

### 3. Main Report - Export Button Not Visible
- **Root cause**: `showTopPerformers()` was called in `displayReport()` but was NEVER DEFINED anywhere. This caused a JS `ReferenceError` that stopped execution before `exportGroup` was shown and before `renderDataInsights` ran.
- **Fix**: Added `showTopPerformers(topPerformers, ignoredSet)` function definition that renders Top Kills / Hunt / EDM / Purchase leaderboard cards into `#topPerformers` div.

### 4. Main Report - Top Stats Cards Not Showing
- **Root cause**: Same as above - JS error halted execution before `renderDataInsights()` was called.
- **Fix**: Same as above.

### 5. Player Explorer - Oversized Chart Breaking Page
- **Fix**: Wrapped canvas in a constrained div (height: 320px, max-height: 320px). Set chart `responsive: true, maintainAspectRatio: false`. Reduced chart series to only Might, Kills, Hunting.

### 6. Player Explorer - Searchable Dropdown + Clear Button
- **Fix**: Integrated TomSelect library for searchable dropdown. Added Clear button that calls `clearPlayerSelection()`. Added `hydrateNames()` to pre-load all player names from `/Report/SearchPlayers?term=&take=2000`.

### 7. Player Explorer - PDF Export Was 43MB / Half Columns
- **Fix**: Replaced html2canvas-based PDF with `jsPDF autoTable` (table-based). Summary table + weekly history table rendered as proper PDF tables. Much smaller file size.

### 8. Main Report - PDF roundedRect Error
- **Fix**: Corrected legend `roundedRect` call - was passing wrong size arguments.

### 9. JS Syntax Error in PlayerExplorer
- **Fix**: Fixed `if playerSelector)` -> `if (playerSelector)` in `initializePlayerSelector()`.

### 10. Duplicate renderDataInsights Function
- **Fix**: Removed duplicate bottom definition via terminal regex replacement.

### 11. New Header Filter Options (In Progress)
- Added two new checkboxes to "Report Headers" dropdown:
  - **History Graph**: toggles sparkline SVG line under history blocks in the History column
  - **History Indicators**: toggles trend arrows in all report columns (Kills, EDM, Troops, Hunt, Purchase)
- Updated `getColumnVisibility()` to include `historyGraph` and `historyIndicators`
- `createHistoryCells` conditionally calls `createHistorySparkline` based on `col.historyGraph`
- `formatDiff` updated to conditionally render arrow span based on `col.historyIndicators`
- **Still needed**: Apply `col.historyIndicators` to `formatDiff2`, edm, troopsLost, purchase inline renders

### 12. PDF Arrow Rendering Issue (Pending)
- **Problem**: `innerText` extracts Unicode arrows from HTML entities (`&uarr;` -> `↑` etc.), but jsPDF Helvetica font cannot render Unicode arrows -> garbled/missing characters in PDF.
- **Fix needed**: In PDF row cell extraction, replace arrows:
  ```
  cell.innerText.replace(/\n/g,' ').replace(/↑/g,'^').replace(/↓/g,'v').replace(/→/g,'>').trim()
  ```

### 13. Stats Cards (dataInsights) Missing from Exports (Pending)
- **JPG fix needed**: Clone `#dataInsights` and insert into `exportRoot` after subtitle, before table clone.
- **PDF fix needed**: Add stats summary text section (Total Players, Total Kills, Total Hunt, G/Y/R counts) above the autoTable.

---

## Current State of Key Files

### `Views/Report/Index.cshtml`
- Export group hidden by default, shown after `displayReport()` completes
- `displayReport()` -> calls `showTopPerformers()`, `renderDataInsights()`, shows export button
- `renderDataInsights()`: renders 4 stat cards (Total Players, Total Kills, Total Hunt Pts, G/Y/R)
- `showTopPerformers()`: renders top performers leaderboard cards per selected category
- `generatePDF()`: jsPDF autoTable with color-coded rows, history heatmap boxes drawn manually, legend + top performers cards
- `generateJPG()`: html2canvas of offscreen div containing title + table + legend + top performers
- New `col.historyGraph` and `col.historyIndicators` flags in `getColumnVisibility()` (partially applied)

### `Views/Report/PlayerExplorer.cshtml`
- TomSelect searchable dropdown with Clear button
- `loadPlayerDetails()` fetches from `/Report/GetPlayerDetails?name=...`
- Chart limited to Might/Kills/Hunting, constrained height
- `exportPlayerPdf()`: jsPDF autoTable (summary + history tables)
- `exportPlayerJpg()`: html2canvas

### `Controllers/ReportController.cs`
- `GetPlayerDetails(string name)`: finds latest `UserId` for normalized name, returns full history
- `SearchPlayers(string term, int take)`: for TomSelect hydration
- `GenerateReport(string week)`: main report data

---

## Pending / TODO
- [ ] Apply `col.historyIndicators` to `formatDiff2`, edm, troopsLost, purchase columns in `formatRow`
- [ ] Fix PDF Unicode arrow characters (replace with ASCII in cell text extraction)
- [ ] Include `#dataInsights` cards clone in JPG export
- [ ] Include stats summary text in PDF export before table
- [ ] Test full PDF/JPG export flow end to end after all fixes

---

## Key Lessons / Gotchas
- The JS `showTopPerformers` missing definition was the single root cause of both "no export button" AND "no stats cards" symptoms on the main report page
- Large inline scripts in Razor views are regression-prone; duplicate function declarations silently override earlier ones
- jsPDF Helvetica does not support Unicode arrows - must use ASCII equivalents (^, v, >) in PDF cell text
- `innerText` on a DOM cell decodes HTML entities to Unicode before jsPDF reads them
- TomSelect must be destroyed and re-initialized (`playerSelector.destroy()`) before re-creating to avoid duplicate instances

---

## 2026-05-12 Upload + Rollback Implementation Context (Mandatory Notes)

### Scope Delivered
- Replaced import-from-directory workflow with popup file upload workflow.
- Added support for BOTH Hunt and Kills uploads in CSV/XLSX formats.
- Added server-side validation flow before import submission.
- Added rollback/delete uploaded report workflow in navbar with DB-loaded report list and confirmation.
- Kept weekly report refresh in sync after successful import/delete.

### Files Changed
- `Services/ExcelStatsService.cs`
- `Controllers/HomeController.cs`
- `Controllers/ReportController.cs`
- `Views/Shared/_Layout.cshtml`
- `wwwroot/js/site.js`
- `Views/Report/Index.cshtml`

### Backend Changes
#### 1) Stream-based file processing (CSV + XLSX)
- Added uploaded-file parse/validate support in `ExcelStatsService`.
- Added validation methods for each file type:
  - `ValidateHuntFileAsync(IFormFile)`
  - `ValidateKillsFileAsync(IFormFile)`
- Added file-to-model parse methods:
  - `ReadHuntsFromFileAsync(IFormFile)`
  - `ReadKillsFromFileAsync(IFormFile)`
- Added required header groups for Hunt/Kills with synonyms.

#### 2) CSV parsing robustness
- Replaced custom simplistic CSV parser with `TextFieldParser`.
- Added delimiter auto-detection (comma/semicolon/tab).
- Supports quoted values correctly and prevents partial imports.

#### 3) Date validation tolerance for hunt timestamps
- Validation no longer hard-fails on non-critical odd datetime strings in Hunt file.
- Suggested range still uses valid parsed dates when available.

#### 4) Import/validate endpoints
- Added `POST /Home/ValidateImportFiles`.
- Added `POST /Home/ImportFromFiles` (file-based import path).
- Added duplicate date-range protection before save.
- Explicitly parses posted `fromDate`/`toDate` as `yyyy-MM-dd`.

#### 5) TypeLoadException hardening
- Replaced anonymous JSON response objects in import/validate actions with concrete response classes.
- This avoids runtime anonymous-type load mismatch after iterative edits/hot reload.

#### 6) Rollback/delete APIs
- Added `GET /Report/GetUploadedReports` to populate rollback dropdown.
- Added `POST /Report/DeleteUploadedReport` for hard-delete selected upload.
- Delete is transactional and logs successful/failure operations.

### UI/UX Changes
#### 1) Import popup layout
- Popup now contains:
  - Hunt file input (.csv/.xlsx)
  - Kills file input (.csv/.xlsx)
  - Validation status region (inside popup)
  - Date range control
  - Read-only unique identifier
- Upload action stays in popup on errors (state persists).

#### 2) Date range behavior (latest requested behavior)
- Restored single date-range control (not separate from/to fields).
- Date picker now:
  - auto-applies once both dates are selected (`autoApply: true`)
  - opens upward in modal (`drops: 'up'`) to avoid bottom clipping
  - uses modal parent (`parentEl: '#importModal'`)

#### 3) Upload button behavior (latest requested behavior)
- Added strict in-flight guard `isImportSubmitting` in `site.js`.
- Prevents double-click and duplicate submit while request is running.
- Button shows spinner/loading text while uploading.
- Button re-enables only after request completes.

#### 4) Rollback UI
- Added navbar action: reset/delete uploaded report.
- Added modal with DB-loaded report dropdown and explicit confirmation flow.

### Report Page Sync
- Added global `refreshAvailableWeeks` function in `Views/Report/Index.cshtml`.
- Called after successful import/delete so week dropdown updates immediately.

### Important Runtime Note
- Build failures seen during session were due to file lock by running process (`LM.Stats.exe` / dll), not compile errors in changed files.
- Stopping running app process and rebuilding resolved lock-related failures.

### Remaining Known Non-blocking Warnings
- Project has existing nullable warnings in older code paths (not newly introduced by this change set).
- No functional blocker for import/rollback workflows from these warnings.

