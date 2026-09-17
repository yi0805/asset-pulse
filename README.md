# AssetPulse

AssetPulse is a local, synthetic industrial asset monitoring portfolio application. Task 001 established an Angular frontend, an ASP.NET Core API, and their baseline tests. Asset, alarm, dashboard, database, and persistence features are intentionally deferred to their roadmap tasks.

## Selected stack

- Angular 21.2.23, Angular CLI/build 21.2.24, TypeScript 5.9.3, and Vitest 4.1.11
- .NET SDK 8.0.425 and ASP.NET Core targeting .NET 8
- Node.js 24.14.x and npm 11.9.x

Angular 21 is an actively supported release and supports Node 24. The committed lockfile provides the reproducible frontend dependency graph; `global.json` selects the backend SDK.

## Prerequisites

- .NET SDK 8.0.425 (or a compatible latest 8.0 patch, per `global.json`)
- Node.js 24.14.x and npm 11.9.x (see `frontend/.nvmrc`)

No database, Docker service, or secret configuration is needed for Task 001.

## Run locally

Restore and start the API from the repository root:

```powershell
dotnet restore backend/AssetPulse.sln
dotnet run --project backend/src/AssetPulse.Api --launch-profile http
```

The API listens at `http://localhost:5200`; its health endpoint is `http://localhost:5200/api/health`.

In a second terminal, install and start the frontend:

```powershell
Set-Location frontend
npm ci
npm start
```

The Angular development server listens at `http://localhost:4200`. `frontend/proxy.conf.json` forwards relative `/api` requests to the API, so `http://localhost:4200/api/health` reaches the same health endpoint without enabling CORS.

## Quality commands

From the repository root:

```powershell
dotnet restore backend/AssetPulse.sln
dotnet build backend/AssetPulse.sln --no-restore
dotnet test backend/AssetPulse.sln --no-build
dotnet format backend/AssetPulse.sln --verify-no-changes --no-restore
```

From `frontend/`:

```powershell
npm ci
npm run lint
npm run format:check
npm run test:ci
npm run build
```

## Configuration and secrets

`.env` files and local appsettings files are ignored. `.env.example` contains no credentials because Task 001 needs none. Task 002 will introduce and document local SQL Server configuration; do not commit connection strings, passwords, or user secrets.

See the [roadmap](ROADMAP.md), [architecture](docs/ARCHITECTURE.md), [repository instructions](AGENTS.md), and [Task 000 handoff](docs/handoffs/task-000-project-planning.md).
