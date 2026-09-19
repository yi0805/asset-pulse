# Task 005 — Asset management

- Date: 2026-09-18
- Status: Complete
- Roadmap entry: [Task 005](../../ROADMAP.md#task-005--asset-management)

## Objective

Deliver the tested asset list, detail/history, create, and edit workflow without beginning alarm management.

## What changed

- Added explicit `AssetUpsertRequest` DTOs and `POST /api/assets` / `PUT /api/assets/{id}`. Text is trimmed, asset codes are invariant-uppercased, required/max/range/decimal-scale rules are validated, and unexpected database failures retain the existing safe 500 path.
- SQL Server unique violations are translated to a `409 application/problem+json` only when they identify `IX_Assets_AssetCode`; the response contains `errors.assetCode`.
- Creation uses one server UTC timestamp for `LastUpdated` and an atomic initial `null → Healthy` event. Updates change `LastUpdated` only when normalized editable fields differ, never create metadata-only history, and leave derived status untouched.
- Replaced the four asset placeholders with list, detail/history, create, edit, and a shared reactive form. List filters/page are query parameters; values, retry/error/empty states, units, local dates, status text, and history pagination are rendered directly from the API.

## Important files

- [write contracts and validation](../../backend/src/AssetPulse.Api/Contracts/AssetUpsertRequest.cs), [AssetWriteService](../../backend/src/AssetPulse.Api/Services/AssetWriteService.cs), and [AssetsController](../../backend/src/AssetPulse.Api/Controllers/AssetsController.cs)
- [real SQL write coverage](../../backend/tests/AssetPulse.Api.Tests/AssetWriteIntegrationTests.cs) and [isolated fixture](../../backend/tests/AssetPulse.Api.Tests/Infrastructure/SqlServerApiFixture.cs)
- [asset UI](../../frontend/src/app/assets) and [asset routes](../../frontend/src/app/app.routes.ts)

## Commands and tests executed

| Command / working directory | Result |
| --- | --- |
| `dotnet build backend/AssetPulse.sln --no-restore` | Passed, 0 warnings/errors. |
| `dotnet test backend/AssetPulse.sln --no-build` with `ASSET_PULSE_TEST_CONNECTION` | Passed: **29/29**, **0 skipped**, against temporary SQL Server 2022 CU22. |
| `dotnet format backend/AssetPulse.sln --verify-no-changes --no-restore` | Passed. |
| `npm ci` / `frontend` | Passed. |
| `npm run lint` / `frontend` | Passed. |
| `npm run format:check` / `frontend` | Passed. |
| `npm run test:ci` / `frontend` | Passed: **32/32**, including the delayed async-response regression. |
| `npm run build` / `frontend` | Passed. |

## Results

Manual browser end-to-end verification passed through `localhost:4200`, the `/api` proxy, the API, and a disposable real SQL Server database. It covered create/detail redirect, uppercase normalized codes, Healthy derived status, the `Initial → Healthy` event, unavailable null readings, search/reopen, edit/persisted refresh, no metadata-only history, duplicate-code feedback, narrow-screen usability, keyboard/focus, and real asynchronous list/detail/edit rendering.

Manual E2E initially exposed a frontend reactivity defect: `GET /api/assets/{id}` and `GET /api/assets/{id}/events` returned 200, but detail remained at `Loading asset…`. The Task 005 asset pages used plain mutable async template state in the current Angular configuration. Component-local signals now hold that state, and a delayed-response regression test verifies the DOM leaves loading after asset/history responses without a manual test-only change-detection call.

No schema migration was needed: Task 002's constraints, decimal columns, unique index, and event relation support this behavior. Temporary SQL Server instances used random in-memory passwords, no volumes, and guarded databases only. The stale development SQL container and `asset-pulse_assetpulse-sqlserver-data` volume were not modified.

## Known limitations

Task 006 alarm management has not started. Dashboard and alarms remain intentional placeholders.

## Git state / commit

Branch: `task/005-asset-management`. PR #5 exists against `main` and remains unmerged. Initial implementation commit: `bf8435f02c3509401dba7e25a7ada26eb8717dfb`; async-state fix: `23570b8769e236bc34c2ef4c5cd51778c8927127`. The final cleanup/completion commit follows this handoff update.

## Exact recommended next task

**Task 006 — Alarm management.** First inspect this handoff and Task 005's status/history behavior, then implement only the authorized alarm API transitions and UI. **Task 006 has NOT started.**
