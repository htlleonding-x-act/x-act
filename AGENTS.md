# AGENTS.md

This file provides guidance to AI Agents when working with code in this repository.

## Overview

X-ACT is a location-based multiplayer game (Scotland Yard-style) with a .NET 10 backend and Flutter mobile client in a monorepo.

## Commands

All commands can be run via `just` (see `justfile` at root):

```
just backend               # run backend on port 5200
just frontend [url]        # run Flutter app (default API: http://localhost:5200)
just apk [url]             # build APK
just db-update             # apply EF Core migrations
```

**Backend** (from `backend/`):
```
dotnet build XActBackend.slnx
dotnet run --project XActBackend/XActBackend.csproj
dotnet test XActBackend.Test/XActBackend.Test.csproj          # unit tests
dotnet test XActBackend.TestInt/XActBackend.TestInt.csproj    # integration tests
pwsh ./ManageMigration.ps1                                    # manage EF Core migrations
```

**Frontend** (from `xact_frontend/`):
```
flutter run --dart-define=API_BASE_URL=<url>
flutter test
flutter build apk --dart-define=API_BASE_URL=<url>
flutter build web
```

## Architecture

### Backend (`backend/`)

Four projects with strict layering:

- **XActBackend** — ASP.NET Core Web API. Controllers translate HTTP → service calls. SignalR hub at `/hubs/game-session` for real-time events. `Program.cs` / `Setup.cs` wire DI, middleware, CORS, and JSON config (NodaTime).
- **XActBackend.Core** — Domain and business logic. Services return `OneOf<T, TError>` union types instead of throwing exceptions. Key services: `GameSessionService`, `TeamService`, `UserService`, `GameSessionSnapshotService`.
- **XActBackend.Persistence** — EF Core 10 + PostgreSQL. Repository and transaction abstractions. Models: `GameSession`, `Team`, `TeamMember`, `LocationLog`, `GeofencePoint`, `PowerUpUsage`. Migrations in `Migrations/`.
- **XActBackend.Shared** — NodaTime JSON config, date/time extensions.
- **XActBackend.Test** / **XActBackend.TestInt** — xUnit unit and integration tests. Prefer the smallest test layer that proves the behavior changed.

### Frontend (`xact_frontend/lib/`)

- `api/` — `ApiService` singleton (HTTP + SignalR); split into `_http`, `_session`, `_data` partials. `api_config.dart` reads `API_BASE_URL` from `--dart-define`.
- `services/` — `AppSession` (session state), `RealtimeService` (SignalR event stream).
- `screens/` — screen-level widgets organized by feature (`start/`, `lobby/`, `team/`).
- `widgets/` — reusable UI components.

## Key Conventions

**Error handling**: Never replace `OneOf` result unions with exceptions. Not-found and domain failures are always modeled as union variants.

**Async**: All backend service methods use `ValueTask<T>`.

**Date/time**: Use NodaTime (`Instant`, etc.) throughout the backend. Do not introduce `DateTime`.

**Validation**: Use FluentValidation for request validation; keep it in the Web API layer.

**HTTPS**: Do not add HTTPS middleware — TLS is terminated by a reverse proxy in front of the backend.

**CORS**: `ClientOrigin` is required configuration; validate origin changes against `Setup.cs`.

**Build settings**: `net10.0`, nullable enabled, warnings as errors, implicit usings on (see `backend/Directory.Build.props`).

## Reference

- Entity relationship diagram: `backend/erd.puml`
- Database migration script: `backend/ManageMigration.ps1`
- Seed/import workflow: `backend/XActBackend.Importer/import.sh`
