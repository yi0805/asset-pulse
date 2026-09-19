# Task 005 — Asset management

- Date: 2026-09-18
- Status: In progress
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
| `npm run lint` / `frontend` | Passed. |
| `npm run format:check` / `frontend` | Passed. |
| `npm run test:ci` / `frontend` | Passed: **31/31**. |
| `npm run build` / `frontend` | Passed. |

## Results

No schema migration was needed: Task 002's constraints, decimal columns, unique index, and event relation support this behavior. The temporary SQL Server had a random in-memory password, no volume, localhost-only port, and generated only guarded `AssetPulse_ApiTests_<guid>` databases. The stale development SQL container and `asset-pulse_assetpulse-sqlserver-data` volume were not modified.

## Known limitations

Automated verification is complete, but required Task 005 manual browser end-to-end verification is pending. Task 006 alarm management has not started. Dashboard and alarms remain intentional placeholders.

## Git state / commit

Branch: `task/005-asset-management`. Final implementation commit: `bf8435f02c3509401dba7e25a7ada26eb8717dfb`; a follow-up `3487cb0259870842429196e484aeee9ec93d0179` preserves pre-existing workspace settings unchanged. The branch is pushed; PR creation is blocked because GitHub CLI authentication is unavailable.

## Exact recommended next task

**Task 006 — Alarm management.** First inspect this handoff and Task 005's status/history behavior, then implement only the authorized alarm API transitions and UI. **Task 006 has NOT started.**
