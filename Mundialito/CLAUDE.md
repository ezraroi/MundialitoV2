# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Mundialito is a football tournament betting web app. It runs as a single ASP.NET Core 8 (.NET 8) backend that serves both a REST API and an AngularJS SPA. The frontend is compiled by Gulp from `Client/` into `wwwroot/`.

## Commands

### Backend (run from `Mundialito/`)
```bash
# Run the app
dotnet run

# Build
dotnet build

# Run all tests (from repo root or Tests/)
dotnet test ../Tests/Tests.csproj

# Run a single test
dotnet test ../Tests/Tests.csproj --filter "FullyQualifiedName~GameDateTimeTests"

# Add a migration
dotnet ef migrations add <MigrationName>

# Apply migrations
dotnet ef database update
```

### Frontend (run from `Mundialito/`)
```bash
# Install deps (first time)
npm install

# Build frontend (compile + minify + cache-bust)
npx gulp

# The default gulp task: clean → build CSS themes → compress lib/app JS → copy HTML → cache-bust index
```

After running `gulp`, the hashed filenames in `wwwroot/js/` and `wwwroot/lib/` are automatically written back into `Views/Home/Index.cshtml` by the cache-bust tasks.

**IMPORTANT — always commit these together after every gulp run:**
```bash
git add Mundialito/wwwroot/ Mundialito/Views/Home/Index.cshtml
```
`Views/Home/Index.cshtml`, the new hashed files in `wwwroot/js/`, and the deleted old hashed files must be committed atomically. If `Index.cshtml` is left out, production will request a JS file that no longer exists and the app will fail to load.

## Architecture

### Backend layers
- **Controllers/** — thin HTTP layer; each maps directly to a resource (Games, Bets, Teams, Stadiums, Players, GeneralBets, Users, Account, Stats)
- **DAL/** — Entity Framework Core repositories + `MundialitoDbContext`. One repository interface + implementation per entity. `GenericRepository<T>` is the base.
- **Logic/** — business rules: `BetValidator` (validates new/update bets against game state and time), `BetsResolver` (scores bets after a game result is entered), `TableBuilder` (builds the leaderboard), `GeneralBetsService`, `TournamentTimesUtils`
- **Auth/** — JWT-based auth + Google OAuth (`GoogleAuthService`, `AuthService`, `TokenService`)
- **Configuration/Config.cs** — maps `App:` section in `appsettings.json`
- **Mail/** — Azure Communication Services email sender
- **Migrations/** — single initial EF migration; add new ones with `dotnet ef migrations add`

### Database
- Uses **PostgreSQL** in all environments (via Npgsql). The connection string lives in `ConnectionStrings:App` (blank in dev; set via environment or `appsettings.Development.json`).
- `MundialitoDbContext` stores: Games, Teams, Stadiums, Players, Bets, GeneralBets, ActionLogs, UserFollows, and ASP.NET Identity tables.
- `DatabaseInitilaizer.Seed()` runs on startup: creates the admin user if no users exist, creates tournament data (teams/stadiums/players/games) if no teams exist by dynamically loading a class named `App:TournamentDBCreatorName` from `DAL/TournamentCreators/`.

### Scoring system
A `Bet` on a `Game` can earn points across four dimensions (controlled by `GameType`):
- **Mark** (1X2): 3 pts (Groups) / 4 pts (Knockouts)
- **Result** (exact score): 2 pts (Groups) / 3 pts (Knockouts)
- **Cards mark**: 1 pt
- **Corners mark**: 1 pt
- **Bingo bonus** (exact score + correct mark): +2 pts

`GeneralBet` (tournament winner + golden boot player) resolves to 12 pts each.

### Frontend
AngularJS 1.x SPA served from `wwwroot/`. Source lives in `Client/`:
- `Client/src/app.js` — module definition and `$routeProvider` config
- `Client/src/Constants.js` — API base URL and route constants
- `Client/src/<Feature>/` — controllers, services, templates per feature (Bets, Games, Teams, Stadiums, Players, Users, Dashboard, GeneralBets)
- `Client/lib/` — third-party JS (Angular, Bootstrap, ui-grid, etc.)
- `Client/css/` — all CSS including two Bootstrap themes (cerulean, spacelab)

The active theme is set via `App:Theme` in `appsettings.json`. Gulp builds `app-cerulean.css` and `app-space-lab.css`; the frontend loads the one matching the configured theme.

### Configuration keys (appsettings.json → `App:`)
| Key | Purpose |
|---|---|
| `TournamentDBCreatorName` | Class name in `DAL/TournamentCreators/` used to seed tournament data |
| `TournamentStartDate` / `TournamentEndDate` | Parsed by `TournamentTimesUtils` to compute general bet close time |
| `MonkeyUserName` | Placeholder user for unowned bets |
| `Theme` | Bootstrap theme (`cerulean` or `spacelab`) |
| `SendBetMail` | Toggle email notifications on bet events |
| `PrivateKeyProtection` | Hides other users' bet details before game closes |
| `LinkAddress` | Base URL used in email links |
| `GoogleClientId/Secret` | Google OAuth credentials |

## Tests

Tests live in `../Tests/` (relative to `Mundialito/`). The test project uses **NUnit**. Currently covers `GameDateTime` UTC conversion logic and a placeholder test. Run with `dotnet test` from the solution root or the `Tests/` directory.
