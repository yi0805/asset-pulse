# Task 003 — Core ASP.NET read API

- Date: 2026-09-18
- Status: Complete
- Roadmap entry: [Task 003](../../ROADMAP.md#task-003--core-aspnet-read-api)

## Objective

Provide tested, read-only ASP.NET Core REST contracts for assets, asset details, asset status history, and alarms over the Task 002 SQL Server model. Acceptance requires explicit DTOs, bounded database-side filtering and pagination, derived status, safe Problem Details, OpenAPI, and reusable real-SQL Server API tests.

## What changed

- Added `GET /api/assets`, `GET /api/assets/{id}`, `GET /api/assets/{id}/events`, and `GET /api/alarms`; no write or Task 004 endpoint was added.
- Added explicit DTOs and manual EF projections. Lists return `items`, `page`, `pageSize`, and `totalCount`; page defaults to 1, page size defaults to 20, and page size is capped at 100.
- Asset lists trim `search`, `type`, and `location`; search checks name/code. Status, severity, and alarm-status query values are validated, including unsupported numeric enum values. Assets sort by AssetCode/Id; alarms and events sort newest-first with Id tie-breakers.
- Status is derived inside EF-translated SQL: unresolved (Active or Acknowledged) Critical alarms win, then unresolved Warning alarms, otherwise Healthy. Resolved alarms have no effect.
- Established safe `application/problem+json` responses with `traceId`, validation errors maps, expected 404 details, and logged generic 500 handling. A test-host-only service replacement verifies that exception text is not exposed.
- Added Development-only Swagger UI at `/swagger`, OpenAPI JSON at `/swagger/v1/swagger.json`, response/status metadata, and README endpoint examples.
- Added a reusable `WebApplicationFactory<Program>` fixture. It creates a guarded random `AssetPulse_Task003Tests_<guid>` database from `ASSET_PULSE_TEST_CONNECTION`, migrates it, resets only that database between tests, and refuses unsafe database cleanup.

## Important files

- [Controllers](../../backend/src/AssetPulse.Api/Controllers): read endpoints and Problem Details helpers.
- [Contracts](../../backend/src/AssetPulse.Api/Contracts): API DTOs and query models.
- [AssetReadService.cs](../../backend/src/AssetPulse.Api/Services/AssetReadService.cs): projected asset reads and derived-status query expression.
- [SqlServerApiFixture.cs](../../backend/tests/AssetPulse.Api.Tests/Infrastructure/SqlServerApiFixture.cs): reusable isolated SQL API fixture.
- [ReadApiIntegrationTests.cs](../../backend/tests/AssetPulse.Api.Tests/ReadApiIntegrationTests.cs): real SQL Server endpoint coverage.
- [GeneratedSqlInspectionTests.cs](../../backend/tests/AssetPulse.Api.Tests/GeneratedSqlInspectionTests.cs): representative generated SQL inspection.
- [README.md](../../README.md): API/OpenAPI setup, contracts, and examples.

## Architecture and design decisions

- EF entities and navigation collections are never serialized. Asset detail/list DTOs expose only the needed asset fields and derived status; alarm DTOs include lightweight asset code/name via the same SQL projection.
- `AsNoTracking`, async operations, cancellation tokens, database-side filters/projections, and bounded pagination are used throughout. Very large valid pages safely return an empty page without `int` overflow.
- `Swashbuckle.AspNetCore` 6.6.2 adds the intentionally lightweight Swagger/OpenAPI surface. `Xunit.SkippableFact` 1.4.13 is test-only infrastructure so a missing SQL contract is reported as skipped rather than a misleading pass; with a valid contract all SQL tests execute.
- Generated SQL inspection showed filtered asset status uses correlated `EXISTS` subqueries, alarm/event filters and deterministic `ORDER BY` execute in SQL, and `OFFSET ... FETCH` applies pagination. There is no entity graph loading, `SELECT *`, or N+1 status query.

## Commands and tests executed

| Command / working directory | Result |
| --- | --- |
| `git fetch origin --prune`; switch/pull `main` | Passed; starting main was `695fa281efbeb99f524b9825ae95703cb1d60f44`, equal to `origin/main`. |
| `dotnet restore backend/AssetPulse.sln` | Passed. |
| `dotnet build backend/AssetPulse.sln --no-restore` | Passed with 0 warnings and 0 errors. |
| `dotnet test backend/AssetPulse.sln --no-build` with an isolated temporary SQL Server | Passed: **18/18**, 0 skipped. The Task 003 SQL endpoint suite executed against a generated guarded database. |
| `dotnet test ... --filter FullyQualifiedName~GeneratedSqlInspectionTests --logger "console;verbosity=detailed"` | Passed; inspected asset list, filtered alarm, and event SQL. |
| `dotnet format backend/AssetPulse.sln --verify-no-changes --no-restore` | Passed. |
| `npm ci`; `npm run lint`; `npm run format:check`; `npm run test:ci`; `npm run build` / `frontend` | Passed. |
| Development API Swagger and malformed query checks | Passed: Swagger returned 200 and exposed assets/alarms; invalid asset/alarm queries returned 400 `application/problem+json`. |
| `git diff --check` | Passed. |

SQL verification used a temporary `assetpulse-task003-sqltest` SQL Server 2022 CU22 container on `127.0.0.1:1434` with an in-memory random password and no volume mount. It was removed after tests. The stale stopped development container and `asset-pulse_assetpulse-sqlserver-data` volume were not modified.

## Results

All Task 003 acceptance criteria are implemented and verified, including the preserved `/api/health` endpoint, DTO-only output, status lifecycle semantics, OpenAPI, safe expected/unexpected Problem Details, SQL-backed integration behavior, and representative generated SQL translation.

## Known limitations

The local demo remains unauthenticated by design. Its existing development SQL container has a stale container credential and is intentionally left stopped; Task 003 verification used only the disposable SQL container. .NET 8's November 2026 support deadline remains the documented deployment decision.

## Git state / commit

Branch: `task/003-core-aspnet-read-api`, based on `695fa281efbeb99f524b9825ae95703cb1d60f44`. The final commit and pull request are recorded after final diff review; no unrelated changes are included.

## Exact recommended next task

**Task 004 — Angular application shell.** It has **not** started. First review this handoff and the completed Task 003 API contracts, then create only the standalone Angular shell, routes, and API-read integration required by Task 004. Do not begin asset/alarm writes.
