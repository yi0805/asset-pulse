# AssetPulse

AssetPulse is a local, synthetic industrial asset monitoring portfolio application. Task 003 adds its read-only REST API over the Task 002 SQL Server model; write operations and user-interface features remain deferred to later roadmap tasks.

## Selected stack

- Angular 21.2.23, Angular CLI/build 21.2.24, TypeScript 5.9.3, and Vitest 4.1.11
- .NET SDK 8.0.425 and ASP.NET Core targeting .NET 8
- EF Core 8.0.31 with the SQL Server provider and `dotnet-ef` 8.0.31
- SQL Server 2022 CU22 (`mcr.microsoft.com/mssql/server:2022-CU22-ubuntu-22.04`)
- Node.js 24.14.x and npm 11.9.x

Angular 21 is an actively supported release and supports Node 24. The committed lockfile provides the reproducible frontend dependency graph; `global.json` selects the backend SDK.

## Prerequisites

- .NET SDK 8.0.425 (or a compatible latest 8.0 patch, per `global.json`)
- Node.js 24.14.x and npm 11.9.x (see `frontend/.nvmrc`)
- Docker Desktop with the Linux container engine running and sufficient memory for SQL Server

SQL Server is local-only: Compose binds it to `127.0.0.1:1433` and retains its data in the named `assetpulse-sqlserver-data` volume.

## Local SQL Server, migrations, and seed data

Copy `.env.example` to an ignored `.env`, replace the password placeholder with a unique local SQL Server password, then start the database:

```powershell
Copy-Item .env.example .env
# Edit .env and set MSSQL_SA_PASSWORD to a secure local value.
docker compose config --quiet
docker compose up -d --wait sqlserver
```

Store the matching API connection string outside the repository. The API project uses .NET user secrets in Development; replace `<local-password>` below with the `.env` value:

```powershell
dotnet user-secrets set --project backend/src/AssetPulse.Api "ConnectionStrings:AssetPulse" "Server=127.0.0.1,1433;Database=AssetPulse;User Id=sa;Password=<local-password>;TrustServerCertificate=True"
dotnet tool restore
dotnet ef database update --project backend/src/AssetPulse.Api --startup-project backend/src/AssetPulse.Api
```

The initial migration is `InitialDataModel`. It creates Assets, Alarms, and AssetEvents with required foreign keys, restrictive deletes, UTC `datetimeoffset` timestamps, unique normalized asset codes, query indexes, enum/check constraints, and `decimal(10,2)` measurements.

Seed data is deliberately opt-in and is available only in the Development environment. It applies pending migrations and inserts a fixed five-asset sample once. Running it again does not duplicate rows or overwrite edits:

```powershell
dotnet run --project backend/src/AssetPulse.Api -- --seed-development-data
```

Normal API startup neither migrates nor seeds a database. Do not use the development seed command against a production database.

To verify data persists through a container restart, run the seed command, restart SQL Server, wait for health, and inspect the resulting count:

```powershell
docker compose restart sqlserver
docker compose up -d --wait sqlserver
docker compose exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P '<local-password>' -C -d AssetPulse -Q "SELECT COUNT(*) AS AssetCount FROM Assets;"
```

## Run locally

Restore and start the API from the repository root:

```powershell
dotnet restore backend/AssetPulse.sln
dotnet run --project backend/src/AssetPulse.Api --launch-profile http
```

The API listens at `http://localhost:5200`; its health endpoint is `http://localhost:5200/api/health`.

## Read API and OpenAPI

In Development, Swagger UI is available at `http://localhost:5200/swagger` and its OpenAPI JSON document is at `http://localhost:5200/swagger/v1/swagger.json`. Enums are returned as strings. Expected input failures and missing resources use `application/problem+json`; validation responses include an `errors` map and every Problem Details response includes a `traceId`.

| Endpoint | Query parameters | Ordering |
| --- | --- | --- |
| `GET /api/assets` | `search`, `type`, `location`, `status` (`Healthy`, `Warning`, `Critical`), `page`, `pageSize` | `AssetCode`, then `Id` |
| `GET /api/assets/{id}` | None | N/A |
| `GET /api/assets/{id}/events` | `page`, `pageSize` | `Timestamp` DESC, then `Id` DESC |
| `GET /api/alarms` | `assetId`, `severity` (`Warning`, `Critical`), `status` (`Active`, `Acknowledged`, `Resolved`), `page`, `pageSize` | `CreatedAt` DESC, then `Id` DESC |

List responses are shaped as `{ "items": [], "page": 1, "pageSize": 20, "totalCount": 0 }`. `page` defaults to 1, `pageSize` defaults to 20 and is capped at 100; both must be positive. Text filters are trimmed and blank text is ignored. Unsupported enum values, non-positive page values, and page sizes above 100 return a 400 validation Problem Details response. A valid filter with no records returns an empty 200 page.

Asset `status` is calculated in SQL from unresolved alarms: an Active or Acknowledged Critical alarm takes precedence, then an Active or Acknowledged Warning alarm, otherwise the asset is Healthy. Resolved alarms do not affect status.

Example requests:

```powershell
Invoke-RestMethod "http://localhost:5200/api/assets?search=pump&status=Warning&page=1&pageSize=20"
Invoke-RestMethod "http://localhost:5200/api/assets/1/events?page=1&pageSize=20"
Invoke-RestMethod "http://localhost:5200/api/alarms?severity=Critical&status=Active"
```

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

The SQL Server persistence test is intentionally isolated from developer data. After Compose is healthy, set a connection string with authority to create a temporary database, then run the backend tests:

```powershell
$env:ASSET_PULSE_TEST_CONNECTION = "Server=127.0.0.1,1433;Database=master;User Id=sa;Password=<local-password>;TrustServerCertificate=True"
dotnet test backend/AssetPulse.sln --no-build
Remove-Item Env:ASSET_PULSE_TEST_CONNECTION
```

The test creates a randomly named `AssetPulse_Task002Tests_<guid>` database, applies migrations, verifies schema/indexes, foreign-key and normalized-code constraints, and seeds twice. Its cleanup is guarded to delete only databases with that exact prefix. If a test process is interrupted, inspect the name and remove only that disposable database; it never resets the `AssetPulse` development database.

From `frontend/`:

```powershell
npm ci
npm run lint
npm run format:check
npm run test:ci
npm run build
```

## Configuration and secrets

`.env` files, local appsettings files, user secrets, and connection-string passwords are not committed. `.env.example` has only a placeholder. Compose reads the local `.env`; the API uses `ConnectionStrings__AssetPulse` or Development user secrets. Browser-delivered configuration never contains credentials.

See the [roadmap](ROADMAP.md), [architecture](docs/ARCHITECTURE.md), [repository instructions](AGENTS.md), and [Task 000 handoff](docs/handoffs/task-000-project-planning.md).
