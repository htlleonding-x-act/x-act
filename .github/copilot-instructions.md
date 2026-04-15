# X-ACT Project Guidelines

## Code Style

- Keep .NET code consistent with [backend/Directory.Build.props](backend/Directory.Build.props): `net10.0`, nullable enabled, warnings as errors, analyzers enabled, and implicit usings on.
- Favor the existing backend patterns in [backend/XActBackend/Controllers/](backend/XActBackend/Controllers/) and [backend/XActBackend.Core/](backend/XActBackend.Core/): `OneOf` result types, `ValueTask`-based async APIs, and FluentValidation for request validation.
- Use NodaTime for date and time handling across the backend; do not introduce `DateTime`-based shortcuts where the codebase already uses `Instant` and related types.
- Keep Flutter code aligned with the existing app structure in [xact_frontend/lib/](xact_frontend/lib/) and avoid introducing unrelated framework patterns.

## Architecture

- The repo is a monorepo with a .NET backend in [backend/](backend/) and a Flutter client in [xact_frontend/](xact_frontend/).
- Backend responsibilities are split across Web API in [backend/XActBackend/](backend/XActBackend/), domain logic in [backend/XActBackend.Core/](backend/XActBackend.Core/), persistence in [backend/XActBackend.Persistence/](backend/XActBackend.Persistence/), and shared helpers in [backend/XActBackend.Shared/](backend/XActBackend.Shared/).
- Integration tests live in [backend/XActBackend.TestInt/](backend/XActBackend.TestInt/) and unit tests live in [backend/XActBackend.Test/](backend/XActBackend.Test/); prefer the smallest test layer that proves the behavior you changed.
- Domain and API flow is intentionally explicit: controllers translate HTTP requests to service calls, services return result unions, and persistence stays behind repository and transaction abstractions.

## Build and Test

- Backend build from [backend/](backend/): `dotnet build XActBackend.slnx`.
- Backend unit tests: `dotnet test XActBackend.Test/XActBackend.Test.csproj`.
- Backend integration tests: `dotnet test XActBackend.TestInt/XActBackend.TestInt.csproj`.
- Backend run target: `dotnet run --project XActBackend/XActBackend.csproj`.
- Frontend build and verification from [xact_frontend/](xact_frontend/): `flutter build`, `flutter test`, and `flutter run`.
- Use [backend/ManageMigration.ps1](backend/ManageMigration.ps1) for EF Core migrations and [backend/XActBackend.Importer/import.sh](backend/XActBackend.Importer/import.sh) for seed/import workflows.

## Conventions

- Do not replace result-based domain handling with exceptions; the backend consistently models not-found and domain failures as union results.
- Keep JSON and date/time configuration aligned with [backend/XActBackend/Program.cs](backend/XActBackend/Program.cs) and [backend/XActBackend/appsettings.json](backend/XActBackend/appsettings.json).
- Do not add HTTPS middleware to the app; the backend expects TLS termination to happen in front of it.
- Treat `ClientOrigin` as required configuration for CORS; changes that affect origins should be validated against [backend/XActBackend/Setup.cs](backend/XActBackend/Setup.cs).
- Prefer linking to existing docs and diagrams such as [backend/erd.puml](backend/erd.puml) and [README.md](README.md) instead of duplicating repository guidance here.
