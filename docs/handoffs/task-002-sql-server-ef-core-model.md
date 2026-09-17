# Task 002 — SQL Server and EF Core data model

- Date: 2026-09-18
- Status: Complete
- Roadmap entry: [Task 002](../../ROADMAP.md#task-002--sql-server-and-ef-core-data-model)

## Objective

Persist AssetPulse's approved small domain in a reproducible local SQL Server 2022 environment. The verified acceptance criteria are a fresh migration, relational constraints, repeatable non-destructive development seed data, and data that survives a container restart.

## What changed

- Added a loopback-only SQL Server 2022 CU22 Compose service, named volume, readiness check, safe `.env.example` placeholder, and a local `dotnet-ef` 8.0.31 tool manifest.
- Added EF Core 8.0.31 SQL Server persistence: `AssetPulseDbContext`, Assets, Alarms, AssetEvents, string-backed enums, entity configurations, and the `InitialDataModel` migration.
- Added required lengths and trimmed/nonblank checks; uppercase unique asset codes; nullable `decimal(10,2)` readings with physical-range checks; UTC `datetimeoffset` timestamps; explicit restrictive foreign keys; and the approved query indexes.
- Added the explicit Development-only `--seed-development-data` command. It migrates then inserts five deterministic demo assets once without overwriting later edits; normal API startup never migrates or seeds.
- Added a model test plus an isolated real-SQL Server test that creates a guarded disposable `AssetPulse_Task002Tests_<guid>` database, migrates it, checks indexes/foreign keys/normalized-code constraints, and confirms repeatable seed behavior.
- Updated the README with Docker, secret, migration, seed, test-isolation, cleanup, and persistence-restart instructions.
- Added repository-root VS Code workspace settings so the frontend uses its installed TypeScript 5.9.3 instead of VS Code's bundled TypeScript 6.x. No TypeScript upgrade or `rootDir` workaround was added.

No REST resource contracts, CRUD operations, UI work, dashboard behavior, or Task 003+ functionality was implemented.

## Important files

- [compose.yaml](../../compose.yaml): SQL Server 2022 CU22 service bound to `127.0.0.1:1433`, health check, and named volume.
- [AssetPulseDbContext.cs](../../backend/src/AssetPulse.Api/Data/AssetPulseDbContext.cs): persistence boundary.
- [Configurations](../../backend/src/AssetPulse.Api/Data/Configurations): entity mappings, relationships, constraints, and indexes.
- [InitialDataModel migration](../../backend/src/AssetPulse.Api/Data/Migrations/20260917233924_InitialDataModel.cs): initial SQL Server schema.
- [DevelopmentDataSeeder.cs](../../backend/src/AssetPulse.Api/Data/DevelopmentDataSeeder.cs): explicit repeatable seed data.
- [SqlServerPersistenceTests.cs](../../backend/tests/AssetPulse.Api.Tests/SqlServerPersistenceTests.cs): SQL Server-backed migration and seed coverage.
- [VS Code workspace settings](../../.vscode/settings.json): select `frontend/node_modules/typescript/lib` for a repository-root workspace.
- [README.md](../../README.md): local setup and guarded integration-test contract.

## Architecture and design decisions

- `Asset` has many `Alarm` and `AssetEvent` records. Deletes are restrictive to preserve alarm and status history.
- Asset health is deliberately not stored. It remains derived from unresolved alarms: Critical wins over Warning; Acknowledged remains unresolved.
- Enums are stored as bounded strings with database check constraints. Timestamps use `DateTimeOffset`/`datetimeoffset`, and measurements retain null for unavailable values.
- Initial indexes are the architecture-approved unique `AssetCode`, `Alarm(AssetId, Status)`, `Alarm(Status, Severity)`, and `AssetEvent(AssetId, Timestamp)` only.
- Seed data spans Healthy/Warning/Critical derived states, every alarm state, null readings, locations/types, and status-history events. The marker check avoids both duplicates and overwrites.
- Integration cleanup can only drop a generated database whose name starts with `AssetPulse_Task002Tests_`; it never resets the development `AssetPulse` database.

## Commands and tests executed

Commands ran from `E:\asset-pulse` unless noted otherwise. Password values used for local verification were not stored or committed.

| Command / working directory | Result |
| --- | --- |
| `git fetch origin --prune`; `git switch main`; `git pull --ff-only origin main` | Passed; main advanced to merged Task 001 commit `5298af2`. |
| `git diff --exit-code main task/001-application-scaffold`; `git branch -D task/001-application-scaffold` | Passed; branch content matched main and the safely merged local branch was removed. |
| `dotnet restore backend/AssetPulse.sln` | Passed. |
| `dotnet tool restore` | Passed; restored `dotnet-ef` 8.0.31. |
| `dotnet build backend/AssetPulse.sln --no-restore` | Passed; 0 warnings, 0 errors. |
| `docker compose config --quiet` | Passed. |
| `docker compose up -d --wait sqlserver` | Passed; SQL Server readiness check reported healthy. |
| `dotnet ef database update --project backend/src/AssetPulse.Api --startup-project backend/src/AssetPulse.Api` | Passed against an empty `AssetPulse` database; applied `20260917233924_InitialDataModel`. |
| `dotnet run --project backend/src/AssetPulse.Api -- --seed-development-data` twice | Passed; the second invocation found the seed marker and made no changes. |
| `dotnet test backend/AssetPulse.sln --no-build` with `ASSET_PULSE_TEST_CONNECTION` | Passed; 3 of 3 tests, including real SQL Server migration, relationship, constraint, derived-status, and repeat-seed checks. |
| SQL count query before and after `docker compose restart sqlserver` | Passed; persisted `Assets=5`, `Alarms=5`, `AssetEvents=11` both times. |
| `dotnet format backend/AssetPulse.sln --verify-no-changes --no-restore` | Passed after normalizing EF generator line endings with the repository formatter. |
| `git diff --check` | Passed. |
| `node -p "require('./frontend/node_modules/typescript/package.json').version"` | Passed; confirmed TypeScript 5.9.3. |
| `npm run lint` / `frontend` | Passed. |
| `npm run test:ci` / `frontend` | Passed. |
| `npm run build` / `frontend` | Passed; production build completed. |

## Results

The initial migration created the intended SQL Server schema, identities, foreign keys, nullable fields, decimal precision, string enum representation, checks, and four query indexes. SQL Server-backed tests confirmed that duplicate normalized codes and invalid foreign keys fail, while valid relationships persist. The opt-in seed was confirmed idempotent and non-overwriting; its five assets, five alarms, and eleven events persisted through a container restart. VS Code now selects the committed Angular project's TypeScript 5.9.3 SDK from the repository root.

## Known limitations

Task 002 intentionally has no API resource contracts or write/read business operations; those begin in Task 003 and later. The local demo remains unauthenticated and SQL Server credentials must stay in ignored `.env` files, user secrets, or environment variables. .NET 8's November 2026 support deadline remains a deployment decision documented in the architecture.

## Git state / commit

Branch: `task/002-sql-server-ef-core-model`, based on merged main commit `5298af2`. This handoff is included with the Task 002 implementation commit after the final diff review and push; no unrelated changes were included.

## Exact recommended next task

**Task 003 — Core ASP.NET read API.** Implement only documented GET contracts for asset lists/details/history and alarm lists on the existing DbContext. First read this handoff, verify the disposable SQL Server test connection, then add DTO projections, bounded/ordered filters and paging, and the reusable API integration fixture. Do not add writes, Angular routes, or alarm lifecycle work yet.
