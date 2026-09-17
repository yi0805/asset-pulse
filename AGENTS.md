# AssetPulse operating instructions

## Purpose and scope

Build a small industrial asset monitoring portfolio application demonstrating Angular 18+, TypeScript, HTML/CSS, C#, .NET 8, ASP.NET Core REST APIs, EF Core, SQL Server, automated tests, and GitHub Actions. Read [ROADMAP.md](ROADMAP.md) and [architecture](docs/ARCHITECTURE.md) before changing code.

No LLMs, agents, RAG, vector databases, chatbots, microservices, Kubernetes, real device integration, or speculative enterprise features. Do only the authorized numbered task. Task 000 is documentation only; Task 001 requires the user's approval.

## Repository layout

Only the documentation exists after Task 000. Planned paths:

- `frontend/`: Angular standalone components, routes, typed HTTP services, reactive forms, colocated tests.
- `backend/AssetPulse.sln`: .NET solution.
- `backend/src/AssetPulse.Api/`: controllers, DTOs, small business services, entities, EF configuration and migrations.
- `backend/tests/AssetPulse.Api.Tests/`: unit and API integration tests.
- `compose.yaml`: local SQL Server 2022 service.
- `.github/workflows/`: CI workflows.
- `docs/ARCHITECTURE.md`, `ROADMAP.md`, `docs/handoffs/`: decisions, task status, and individual handoffs.

## Architecture and quality

- Inspect relevant code and verify assumptions before editing. Diagnose the root cause of failures.
- Use one Angular app, one ASP.NET Core API, and one SQL Server database. Keep changes surgical.
- Keep HTTP concerns in controllers, business rules in small injectable services, and persistence in EF Core. Do not add generic repositories, CQRS, mediator libraries, or extra architectural projects without a demonstrated need.
- Keep EF entities separate from request/response DTOs. Use async database operations and cancellation tokens; project read queries and bound list sizes.
- Use strict TypeScript, nullable C#, descriptive names, consistent formatting, semantic HTML, responsive CSS, and accessible controls.
- Validate on both client and server. Return consistent Problem Details errors with useful HTTP status codes. Never expose raw exceptions.
- Follow the status, alarm lifecycle, UTC timestamp, and measurement conventions in the architecture document.

## Testing and security

- Add meaningful tests with each behavior change; do not defer all tests to Task 009. Cover validation, failures, and alarm transitions as well as successful requests.
- Exercise persistence and relational constraints against isolated SQL Server test data. EF's in-memory provider is not evidence of SQL Server correctness.
- Frontend tests should exercise forms, HTTP interactions, state rendering, and user actions. Perform keyboard and narrow-screen checks for UI changes.
- Never commit credentials, connection-string passwords, `.env`, user secrets, or production data. Commit placeholder examples only. Use local user secrets/environment variables and CI secrets.
- Bind development SQL Server to loopback, allow only explicit development origins if CORS is needed, validate inputs, and log without sensitive data.
- The initial demo has no authentication and is local only. Public deployment requires Task 012's explicit access/security decision; CORS is not access control.

## Git and task workflow

1. Read the task, architecture, relevant code, and the previous completed task's handoff. Inspect `git status --short --branch` and preserve unrelated user changes.
2. State success in terms of the task's acceptance criteria. Record `In progress` in ROADMAP.md when starting implementation.
3. Use a focused `codex/task-XXX-short-name` branch for implementation when practical. Never discard user work, rewrite shared history, or push without authorization. Commit only when requested; use a task-specific message if committing.
4. Implement only the task's scope. Record any necessary design change in the architecture and roadmap.
5. Run relevant checks below, inspect the full diff and new files, and record exact results and any blockers. Do not claim unrun tests passed.
6. Every task must update ROADMAP.md. Mark `Complete` only when acceptance criteria are verified; otherwise record remaining work and its reason.
7. Every completed task must create `docs/handoffs/task-XXX-short-name.md` using [the template](docs/handoffs/TEMPLATE.md). Include the exact next task and Git state. Do not create a growing `HANDOFFS.md`.
8. Report changes, validation, limitations, next task, and Git status. Stop at the authorized task boundary.

## Completion commands

For every task, run from the repository root: `git status --short --branch`, `git diff --check`, `git diff --stat`, and `git diff`. Git diff omits untracked files: inspect every new file too. Documentation-only tasks must also check local links, task dependencies, and consistency; application commands are not applicable to Task 000.

Task 001 must establish and verify the following command contract, including package scripts and a headless test runner. These are planned commands, not commands available in the empty repository:

| Scope | Commands and working directory |
| --- | --- |
| Backend | From root: `dotnet restore backend/AssetPulse.sln`; `dotnet build backend/AssetPulse.sln --no-restore`; `dotnet test backend/AssetPulse.sln --no-build`; `dotnet format backend/AssetPulse.sln --verify-no-changes --no-restore` |
| Frontend | From `frontend/`: `npm ci`; `npm run lint`; `npm run format:check`; `npm run test:ci`; `npm run build` |
| Database, from Task 002 | From root: `docker compose config --quiet`; `docker compose up -d --wait sqlserver`; `dotnet tool restore`; `dotnet ef database update --project backend/src/AssetPulse.Api --startup-project backend/src/AssetPulse.Api` |

Run build, test, lint, and formatting checks for changed application areas and both sides when API contracts change. Run restore/install when needed for a clean or changed dependency state. Database checks require local configuration and an explicitly selected disposable development/test database; never reset existing data casually. Task 002 must document isolated test database configuration, and Task 009 must run equivalent checks in CI. If a command changes, update this file and its callers in the same task.
