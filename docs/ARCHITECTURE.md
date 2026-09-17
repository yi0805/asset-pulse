# AssetPulse architecture

Status: proposed in Task 000, 2026-09-18. This document defines implementation boundaries; it does not describe an application that already exists.

## System shape

```text
Angular frontend
        |
        | HTTP / JSON REST API
        v
ASP.NET Core .NET 8 API
        |
        | Entity Framework Core
        v
Microsoft SQL Server 2022
```

Use a single repository, one frontend application, one API project, one backend test project, and one relational database. Organize code into folders rather than adding architectural assemblies. Use ASP.NET Core controllers and built-in dependency injection. Small services own business operations; EF Core DbContext already provides the required persistence abstraction.

## Frontend responsibilities

Use an actively supported stable Angular release meeting the 18+ requirement. Task 001 must check [Angular release support](https://angular.dev/reference/releases) and [Node.js/TypeScript compatibility](https://angular.dev/reference/versions), record selected versions, and commit a lockfile. Do not select Angular 18 merely because it is the minimum.

Use standalone components, Angular Router, HttpClient, typed feature services, and reactive forms. Organize features under dashboard, assets, and alarms with a small shared UI folder. Keep server data authoritative; use component/service state without a global state library. Use ordinary HTML/CSS for the responsive shell and accessible status visualizations.

Planned routes: `/dashboard`, `/assets`, `/assets/new`, `/assets/:id`, `/assets/:id/edit`, and `/alarms`, plus an unknown-route page. Lists support search, filters, and pagination. Forms show field errors and preserve input on failed saves. Data views have loading, empty, and error states with retry. Acknowledge/resolve actions prevent duplicate submission and refresh affected data after success.

## Domain and persistence responsibilities

Use EF Core 8 with the SQL Server provider, migrations, required foreign keys, and explicit string lengths and decimal precision. IDs are database-generated integers. Timestamps use UTC `DateTimeOffset`, stored as `datetimeoffset` and serialized as ISO 8601; the browser labels displayed local times. Do not store time as formatted text.

| Entity | Proposed fields and rules |
| --- | --- |
| Asset | Id, Name (100), AssetCode (32), Type (50), Location (100), nullable Temperature, nullable Pressure, LastUpdated. Required strings are trimmed; codes are uppercase and unique. Measurements use `decimal(10,2)` with Celsius and kPa units. Temperature must be at least -273.15 C; pressure is absolute and nonnegative. Null means unavailable, never zero. |
| Alarm | Id, required AssetId, Severity (`Warning` or `Critical`), Message (500), Status (`Active`, `Acknowledged`, `Resolved`), CreatedAt, nullable AcknowledgedAt and ResolvedAt. All alarms have a nonblank message. |
| AssetEvent | Id, required AssetId, nullable PreviousStatus (null for initial creation), NewStatus (`Healthy`, `Warning`, `Critical`), Timestamp, Description (500). Append-only status history, ordered by Timestamp then Id. |

Asset has many Alarms and AssetEvents. Do not expose deletion in the initial scope; configure restrictive deletes to preserve history. Index unique AssetCode, Alarm(AssetId, Status), Alarm(Status, Severity), and AssetEvent(AssetId, Timestamp). Start with these query-driven indexes and inspect generated SQL before adding more. Store enums as bounded strings with database check constraints for valid values.

### Operational status and alarm rules

- Asset status is derived from unresolved alarms: any Critical alarm means Critical; otherwise any Warning alarm means Warning; otherwise Healthy. `Acknowledged` remains unresolved. Expose Status in DTOs, without an independently editable status column.
- Active-alarm dashboard count means `Active` plus `Acknowledged`; resolved alarms are excluded. UI labels must make this explicit.
- Normal transitions are Active -> Acknowledged -> Resolved. Direct Active -> Resolved is allowed. A repeated acknowledge or resolve returns the current resource without changing original timestamps. Acknowledge after resolution returns 409. Reopening and deleting alarms are outside scope.
- Creating an asset writes its initial Healthy event. Raising or resolving an alarm writes an event only when derived asset status changes. Acknowledgement changes alarm metadata but produces no status-change event.
- Each write updates Asset.LastUpdated when asset fields or related alarm state actually changes. Alarm mutation, parent timestamp, and any status event must commit atomically. Task 006 must protect same-asset concurrent transitions with a short database transaction and verify that history matches final status; choose the simplest SQL Server-safe approach and document it.
- Temperature and pressure are manually entered demo readings; they do not generate alarms or determine health. This app has no telemetry ingestion, threshold engine, real-time transport, or physical control functions. Make the synthetic-data nature visible in the UI.

### Demo data

Task 002 adds deterministic, development-only seed data spanning locations, asset types, all derived statuses, each alarm state, null measurements, and recent/history events. Seeding is repeatable without duplicates and never overwrites edits. Use an explicit opt-in command or Development-only switch, documented in local setup. Do not automatically migrate or seed a production database at application startup.

## API responsibilities and contracts

Controllers handle routing, binding, response codes, and cancellation. Injectable services enforce alarm transitions and transactional writes. DbContext manages data access. Read queries use projection, no tracking where appropriate, and async operations. Avoid loading full tables to compute summaries or status.

| Endpoint | Purpose / planned task |
| --- | --- |
| `GET /api/assets` | Search name/code, filter type/location/derived status, paginate; Task 003 |
| `GET /api/assets/{id}` | Asset details and measurements; Task 003 |
| `GET /api/assets/{id}/events` | Paginated status history, newest first; Task 003 |
| `POST /api/assets` | Create asset; Task 005 |
| `PUT /api/assets/{id}` | Edit asset fields and readings; Task 005 |
| `GET /api/alarms` | Filter asset/severity/status, paginate; Task 003 |
| `POST /api/alarms` | Manually raise a demo alarm through the API; Task 006 |
| `POST /api/alarms/{id}/acknowledge` | Acknowledge alarm; Task 006 |
| `POST /api/alarms/{id}/resolve` | Resolve alarm; Task 006 |
| `GET /api/dashboard` | Asset status counts, unresolved alarm count, limited recent events; Task 007 |

Alarm creation is available through the documented API for repeatable demonstrations; an alarm creation UI is outside the baseline. No auth, export, deletion, live polling, or API versioning framework is needed for the local demo.

List responses have `items`, `page`, `pageSize`, and `totalCount`. Default page size is 20; maximum 100. Validate positive page numbers and reject unsupported enum/filter values with 400. Use deterministic ordering (assets by AssetCode/Id; alarms and events newest first with Id as a tie-breaker). Unknown detail IDs and unknown parent IDs for history return 404; a valid query with no matches returns an empty page. Document query names and examples in Task 003.

Use explicit request/response DTOs, manual mapping/projection, and string enum values. Clients cannot set IDs, calculated status, event records, or server-owned timestamps. Creation returns 201 with an appropriate resource Location; asset creation points to its detail endpoint, and alarm creation must supply a retrievable resource URI (add `GET /api/alarms/{id}` in Task 006). Updates and transitions return 200 with the current DTO. Duplicate asset codes and invalid lifecycle transitions return 409. Do not add endpoints just to mirror tables.

## Validation and errors

Angular validates required values, lengths, units/ranges, and numeric inputs for immediate feedback. Server DTO validation repeats these checks authoritatively; services validate referenced assets and transitions. Database uniqueness, foreign keys, and check constraints provide the final safeguard. Translate expected uniqueness/concurrency conflicts to stable client errors, not raw SQL exceptions.

Establish ASP.NET Core Problem Details and a global exception handler in Task 003. Return `application/problem+json` with status, title, safe detail, instance, and a trace identifier; validation errors include a field-keyed errors map. Use 400 for bad input, 404 for missing resources, 409 for known conflicts, and 500 for unexpected failures. Log diagnostic exceptions server-side without passwords or personal data. Task 008 checks consistency across model binding, missing routes, validation, and unexpected failures.

Angular translates these responses into field errors or a retryable page/action message. Preserve entered data and distinguish unavailable data from a legitimate zero/empty result. Use request cancellation or switch-to-latest behavior for changing filters so older responses do not replace newer results.

## Local development and configuration

Run Angular and the API on the host, with SQL Server 2022 in Docker Compose and a named data volume. Bind SQL Server to `127.0.0.1`; use a readiness health check. Frontend requests go to relative `/api` URLs through the development proxy. If separate origins are used, allow only configured origins. Full application containerization is optional in Task 012.

Task 001 pins a .NET 8 SDK in `global.json`, compatible frontend tools, and package versions. Task 002 pins EF tooling in a local tool manifest and a tested SQL Server 2022 image tag/digest. Document hardware/container prerequisites, host ports, database readiness, and trusted local development certificates when applicable.

Commit safe defaults and `.env.example` placeholders. Ignore real `.env` files; place API passwords in .NET user secrets or environment variables such as `ConnectionStrings__AssetPulse`. Compose obtains its SQL password from local environment configuration. Browser-delivered configuration is public and must never contain credentials. Use GitHub Secrets for CI credentials and ephemeral databases for tests. Restrict any development-only certificate trust exceptions to local SQL connections.

The initial app is a local, single-user demo without authentication. Public write access is not acceptable by default. Task 012 must explicitly choose private access/authentication or a read-only public demo before deployment and handle TLS, secrets, migrations, and data lifecycle.

## Testing and CI

- Backend: use xUnit for focused business-rule tests, and ASP.NET Core's test host with real SQL Server for API/persistence tests. Cover query filters, pagination, DTO validation, uniqueness, HTTP errors, transactions, and transition timestamps/history.
- Give integration tests an explicitly named disposable database. Do not reuse developer data; isolate/reset fixtures reproducibly and guard destructive cleanup to test-only databases. Task 002 documents this contract; Task 003 supplies the reusable API fixture.
- Frontend: use the selected Angular release's supported test runner, HttpClient testing utilities, and component/form tests. Cover loading, empty, error, retry, field validation, routing, filtering, and alarm actions. Configure a non-watching headless script in Task 001.
- Task 009 adds GitHub Actions on pull requests and main pushes: restore/install, formatting, lint, production builds, backend unit/integration tests against SQL Server, frontend tests, and useful failure output. Install only the browser/runtime prerequisites required by the selected runner. Add one small browser smoke test for create asset -> raise demo alarm via API -> acknowledge/resolve in UI -> verify dashboard/history.
- Task 010 combines automated accessibility checks where useful with manual keyboard, focus, contrast, and responsive checks. Avoid arbitrary coverage percentages and tests that only duplicate implementation.

## Planning review and evidence

| Required evidence | Where it is built and demonstrated |
| --- | --- |
| Angular 18+, TypeScript | Version pinning in 001; routes, typed services and state in 004; reactive forms and interactions in 005/006 |
| HTML/CSS | Semantic responsive layouts in 004/007; keyboard, contrast and narrow-screen evidence in 010 |
| C#, .NET 8, ASP.NET Core | Compiled API in 001; controllers, DI, async operations, DTOs in 003/005/006 |
| REST APIs | Documented resources, status codes and query contracts in 003; tested writes/transitions in 005/006 |
| EF Core, Microsoft SQL Server | Real SQL Server container, migrations, relationships, indexes and seed in 002; integration tests from 003 |
| Git/GitHub | Focused branches and reviewable diffs throughout; published repository/PR history and actual CI evidence in 009/011 when authorized |
| Automated testing | Test runners in 001, behavior tests alongside each feature, integrated pipeline and smoke flow in 009 |
| Troubleshooting/error handling | Baseline Problem Details in 003, failure states in 004-007, reproducible failure/recovery cases and troubleshooting guide in 008 |
| CI | GitHub Actions definition and successful remote run evidence in 009; locally passing tests alone do not establish hosted CI success |

The roadmap covers all requested skills without requiring a telemetry platform. Tests, validation, and documentation start with the features; later tasks broaden and review them.

## Risks and boundaries

- .NET 8 remains a requested constraint. Its support ends November 10, 2026, according to [Microsoft's support policy](https://dotnet.microsoft.com/en-us/platform/support/policy). Record that limitation and seek an explicit stack decision before production deployment after support ends; do not silently change the requested runtime.
- Docker/SQL Server resource or platform limitations may prevent local integration tests. Record the blocker and provide a compatible SQL Server environment; do not substitute another provider and claim SQL Server verification.
- Keep dashboard visualization to status summaries, an accessible proportional bar, and recent events. No charting framework, time-series warehouse, or measurement-history model is necessary.
- Avoid auth/roles in the local baseline, external monitoring integrations, automatic thresholds, background ingestion, microservices, messaging, Kubernetes, and any AI functionality. Synthetic readings and manual alarm creation make the demo reproducible.
