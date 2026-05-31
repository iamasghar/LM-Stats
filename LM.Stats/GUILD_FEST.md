# GuildFest Feature Notes

## Goal
Add a monthly GuildFest import and reporting flow that reuses the current report-upload UX, stores each event in the database, and lets us track performance against a configurable guild target.

## Source File
- CSV reviewed: `D:\LM\New folder\2026-05-11 10.29 GUILD_FESTIVAL @DR.csv`
- Shape is simple and consistent:
  - `Name`
  - `Completed`
  - `Total`
  - `Score`
  - `Completed Bonus`

## What We Observed
- The file is a monthly event snapshot, not a weekly report.
- It is best treated as a separate event type from the current weekly Hunt/Kills stats.
- The CSV already gives enough data to build leaderboard, target compliance, and near-miss views.

## Decisions We Made
### 1. Upload UX
- Use a popup/modal similar to the existing report upload flow.
- User uploads one GuildFest CSV file.
- Validate before import, then save to DB.

### 2. Event Goal
- Use a configurable monthly target in the database.
- Default target discussed: `4000` points.
- Store the goal in config, but also snapshot it per event so history does not change if config changes later.

### 3. Identity Matching
We need to resolve each CSV row to a stable user id before saving or analyzing it.

Match order:
1. Hunt table current name match.
2. Kill table current name match.
3. Kill table old name match.

If a row cannot be matched confidently, store it as unresolved and surface it in the UI.

### 4. Data Storage
Suggested structure:
- `GuildFestEvent`
  - month / date range
  - file name / upload metadata
  - goal points snapshot
  - created timestamp
- `GuildFestEntry`
  - raw name from CSV
  - resolved `UserId` when found
  - resolved display name
  - score
  - completed count
  - total count
  - bonus completed flag
  - percent of goal
  - match status / confidence

### 5. Status Logic
Reuse the existing Green / Yellow / Red style thinking:
- Green: target met and bonus completed
- Yellow: partial success or near target
- Red: under target
- New / Unmatched: row not resolved to a user

## Best Use of the Data After Upload
### Core Monthly Views
- Month selector.
- Summary cards:
  - total players
  - players meeting target
  - players with bonus completed
  - unresolved matches
  - average score
  - near misses
- Leaderboard table sorted by score.
- Target/compliance view using the configured goal.

### Useful Visualizations
- Monthly score distribution.
- Target attainment counts.
- Top performers.
- Near-miss group close to the monthly goal.
- Player trend history across months.

### Useful Follow-Up Questions the Data Can Answer
- Who is consistently above target?
- Who is just missing target each month?
- Who is improving month over month?
- Which names still need alias cleanup?
- How does GuildFest performance compare with weekly Hunt/Kills behavior?

## Integration Direction
- Reuse the existing report upload modal pattern in `Views/Shared/_Layout.cshtml` and `wwwroot/js/site.js`.
- Add a new GuildFest controller/service flow rather than mixing it into weekly report import.
- Reuse current naming and zone/status conventions where possible.
- Keep it monthly and separate from the weekly report pipeline.

## Deferred Until Later
- Database schema changes.
- Upload endpoint implementation.
- Name-to-user resolution logic.
- GuildFest dashboard UI.
- Trend charts and export support.

## Short Version
GuildFest should become a monthly, configurable-target event with one-file upload, stable identity matching, unresolved-row handling, and a dashboard focused on target compliance, leaderboard ranking, and month-to-month trends.