# Task 001 — Application scaffold

- Date: 2026-09-18
- Status: Complete
- Roadmap entry: [Task 001](../../ROADMAP.md#task-001--application-scaffold)

## Objective

Create a minimal, buildable Angular/.NET foundation with a repeatable quality command contract, a health smoke test, and a local `/api` development proxy. Both applications must start without adding business features or database scaffolding.

## What changed

- Added `frontend/`: strict Angular 21 standalone application, Vitest shell tests, ESLint, Prettier scripts, Node/npm constraints, lockfile, and an intentionally neutral application-shell message.
- Added `backend/AssetPulse.sln`: nullable .NET 8 API and xUnit test project. The API exposes only `GET /api/health`; its test verifies a `200 Healthy` response.
- Added a root `global.json`, `.editorconfig`, `.gitignore`, and credential-free `.env.example`.
- Configured the API HTTP port as `5200`, the Angular dev server as `4200`, and `frontend/proxy.conf.json` to forward relative `/api` requests to the API.
- Replaced the placeholder README with local prerequisites, startup, quality-command, port, and secret-handling documentation.

## Important files

- [global.json](../../global.json): pins SDK 8.0.425 with patch roll-forward.
- [README.md](../../README.md): setup, ports, exact quality commands, and configuration conventions.
- [AssetPulse.Api.csproj](../../backend/src/AssetPulse.Api/AssetPulse.Api.csproj): net8.0 API with nullable and warnings-as-errors enabled.
- [Program.cs](../../backend/src/AssetPulse.Api/Program.cs): health-only API pipeline.
- [HealthEndpointTests](../../backend/tests/AssetPulse.Api.Tests/HealthEndpointTests.cs): API smoke test via `WebApplicationFactory`.
- [package.json](../../frontend/package.json): pinned Angular 21.2.23, TypeScript 5.9.3, scripts, and frontend tooling.
- [proxy.conf.json](../../frontend/proxy.conf.json): local relative `/api` proxy.
- [package-lock.json](../../frontend/package-lock.json): exact frontend dependency graph.

## Architecture and design decisions

- Chose Angular 21.2.23 because it was actively supported and compatible with installed Node 24.14; Angular 22 required Node 24.15 at task time. The frontend uses Angular CLI 21.2.24 and TypeScript 5.9.3.
- The health route is under `/api` so the frontend uses the same relative URL convention as future API calls. HTTP remains available for the local proxy; the HTTPS launch profile is also retained for direct local use.
- There is no CORS policy because development traffic uses the proxy. There are no EF Core, SQL Server, Docker, domain, asset, alarm, dashboard, or persistence additions in this task.
- Root ignore rules exclude generated output, dependencies, `.env` files, and local appsettings while allowing `.env.example`.

## Commands and tests executed

Commands were run from `E:\asset-pulse` unless a working directory is stated.

| Command / working directory | Result |
| --- | --- |
| `winget install --id Microsoft.DotNet.SDK.8 --exact --accept-source-agreements --accept-package-agreements --silent` | Installed .NET SDK 8.0.425 after the initial environment check found no `dotnet` executable. |
| `dotnet restore backend/AssetPulse.sln` | Passed; API and test project restored. |
| `dotnet build backend/AssetPulse.sln --no-restore` | Passed; 0 warnings, 0 errors. |
| `dotnet test backend/AssetPulse.sln --no-build` | Passed; 1 of 1 health-endpoint test passed. |
| `dotnet format backend/AssetPulse.sln --verify-no-changes --no-restore` | Passed. |
| `npm ci` / `frontend` | Passed from the committed lockfile; npm reported 0 vulnerabilities. |
| `npm run lint` / `frontend` | Passed; Angular ESLint reported all files pass. |
| `npm run format:check` / `frontend` | Passed; Prettier reported all matched files formatted. |
| `npm run test:ci` / `frontend` | Passed; Vitest reported 2 of 2 shell tests passed. |
| `npm run build` / `frontend` | Passed; production bundle written to ignored `frontend/dist/frontend`. |
| `GET http://localhost:5200/api/health` | Passed; `200 Healthy` from the temporary API process. |
| `GET http://127.0.0.1:4200/api/health` | Passed; `200 Healthy` through the temporary Angular proxy. |

## Results

Both applications build and start. The API health endpoint is tested in-process and confirmed both directly and through the frontend proxy. Development ports and configuration conventions are documented. The command contract in [AGENTS.md](../../AGENTS.md) is implemented and passing. No business or persistence functionality was added.

## Known limitations

Task 001 intentionally contains no SQL Server/EF Core configuration, migrations, domain entities, API resource contracts, routing shell, or feature UI. The local API has no authentication because the baseline remains a local-only demo. .NET 8's November 2026 support deadline remains a documented later deployment decision.

## Git state / commit

Branch: `task/001-application-scaffold`, based on Task 000 baseline commit `e209b0aa126bf51266ecd2b941dc0f07d27c0539`. This handoff was authored before the final Task 001 commit; the final review, commit, push, and PR creation remain the closing steps for this task.

## Exact recommended next task

**Task 002 — SQL Server and EF Core data model.** Its objective is to add reproducible SQL Server 2022, EF Core 8 entities/configurations, migrations, and opt-in development seed data. Prerequisite: merge the Task 001 PR into `main`; then begin from the merged clean main branch. First read this handoff, the Task 002 roadmap section, and the architecture data-model rules. Do not begin API read contracts before Task 003.
