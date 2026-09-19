# AssetPulse roadmap

Tasks 000-005 are complete. Tasks 006-011 remain in the baseline; Task 012 is optional. **Next: Task 006 — Alarm management.**

Status values: `Planned`, `In progress`, `Blocked`, `Complete`, `Optional`. Every task updates this file and creates its own completion handoff under `docs/handoffs/`. Read [AGENTS.md](AGENTS.md), [architecture](docs/ARCHITECTURE.md), and the latest completed handoff before starting. Run applicable completion commands from AGENTS.md and record exact results. A fresh session should implement only one authorized task.

| Task | Title | Status | Dependencies |
| --- | --- | --- | --- |
| 000 | Project planning and architecture | Complete | None |
| 001 | Application scaffold | Complete | 000; user approval |
| 002 | SQL Server and EF Core data model | Complete | 001 |
| 003 | Core ASP.NET read API | Complete | 002 |
| 004 | Angular application shell | Complete | 003 |
| 005 | Asset management | Complete | 003, 004 |
| 006 | Alarm management | Planned | 005 |
| 007 | Dashboard and visualization | Planned | 006 |
| 008 | Reliability, validation, and error handling | Planned | 007 |
| 009 | Automated testing and CI | Planned | 008 |
| 010 | UI/UX and accessibility polish | Planned | 009 |
| 011 | Documentation and portfolio presentation | Planned | 010 |
| 012 | Optional production deployment | Optional | 011; explicit deployment authorization |

## Task 000 — Project planning and architecture

- **Objective:** Establish a bounded design and repeatable repository workflow without implementing the app.
- **Work:** Inspect repository/Git state; write operating instructions, roadmap, architecture, minimal README, and per-task handoff template; review stack coverage and lifecycle risks.
- **Acceptance:** All requested planning documents exist and agree on scope; every task has objectives, work, acceptance, checks, and dependencies; Task 001 remains unstarted.
- **Checks:** Read every new document, verify local Markdown links and roadmap dependencies, inspect Git state/new-file content, and run whitespace/diff checks. Application tests are not applicable.
- **Dependencies:** None.
- **Result:** Planning documents created and reviewed; no scaffolding, installations, application code, or migrations. [Handoff](docs/handoffs/task-000-project-planning.md).

## Task 001 — Application scaffold

- **Objective:** Create a minimal buildable Angular/.NET foundation and repeatable quality commands.
- **Work:** Create planned frontend, API, solution, and backend test project; pin an appropriate .NET 8 SDK and supported Angular 18+ release with compatible Node/TypeScript; add lockfiles, strict typing/nullability, `.editorconfig`, `.gitignore`, lint/format configuration, and the AGENTS.md command scripts. Add minimal meaningful API smoke and Angular shell tests, a basic health endpoint, and a relative `/api` development proxy. Add local setup documentation with prerequisites and run commands.
- **Acceptance:** Both applications start; frontend can reach API health through the proxy; development ports/configuration are documented; baseline tests/builds/lint/format pass; no business features or database scaffold yet.
- **Checks:** Execute backend and frontend completion commands from a clean dependency state; request API health directly and through the frontend proxy; verify ignored build output and secret files.
- **Dependencies:** 000 and explicit approval to begin implementation.
- **Result:** Angular 21/.NET 8 scaffold, quality command contract, health smoke test, and `/api` development proxy added without domain or database implementation. [Handoff](docs/handoffs/task-001-application-scaffold.md).

## Task 002 — SQL Server and EF Core data model

- **Objective:** Persist the small domain in a reproducible SQL Server 2022 environment.
- **Work:** Add Compose SQL Server service with loopback binding, named volume and readiness check; placeholder environment configuration; EF Core 8 SQL Server provider/DbContext/entities/configurations; local EF tool manifest; initial migration; opt-in repeatable development seeding. Implement lengths, precision, relationships, check constraints, and indexes from the architecture. Document isolated integration database setup and guarded cleanup, plus migration/seed commands.
- **Acceptance:** Migration applies to a fresh database; valid relationships persist and invalid foreign keys/duplicate normalized codes fail; seeded alarms produce the intended Healthy/Warning/Critical cases; a second seed does not duplicate or overwrite data; data survives container restart. No committed passwords and no automatic production seeding.
- **Checks:** Compose configuration/readiness; migrate a disposable database from empty; verify constraints and representative EF reads against SQL Server; seed twice and inspect counts; restart persistence check; backend build/test/format checks. Record actual schema/index inspection.
- **Dependencies:** 001.
- **Result:** SQL Server 2022 Compose service, EF Core 8 model/configurations, initial migration, opt-in repeatable development seeding, and isolated SQL Server persistence coverage added and verified. [Handoff](docs/handoffs/task-002-sql-server-ef-core-model.md).

## Task 003 — Core ASP.NET read API

- **Objective:** Establish tested REST contracts for assets, alarms, and status history.
- **Work:** Implement GET asset list/detail/history and GET alarm list; DTO projections, normalized filters, bounded pagination, deterministic ordering, and derived asset status. Add small DI services where business rules warrant them, async/cancellation, Problem Details exception handling, and OpenAPI documentation with example requests. Build the reusable test-host/isolated SQL Server fixture.
- **Acceptance:** All read endpoints return the documented DTOs; queries filter/page correctly; unknown resources return 404; invalid filters/pages return 400; empty results are successful empty pages; validation and unexpected failures use safe structured errors. Entities never leak through API contracts.
- **Checks:** SQL Server-backed endpoint tests for representative reads, combined filters, ordering/page boundaries, empty results, missing IDs and invalid input; controlled exception test for safe 500; backend completion checks; inspect representative generated queries for bounded execution.
- **Dependencies:** 002.

## Task 004 — Angular application shell

- **Objective:** Create responsive navigation and the shared frontend conventions needed by feature work.
- **Work:** Add layout, router configuration, active navigation, unknown-route page, and minimal feature route placeholders. Configure HttpClient and typed models/services for existing read contracts; add reusable loading/error/empty presentation only where needed; establish basic colors, spacing, status labels, accessible focus, and API error mapping.
- **Acceptance:** Routes, direct navigation, and back/forward navigation work; shell is usable at narrow and desktop widths; data services use the proxy and match API contracts; errors are visible and retryable where appropriate. Placeholders are clearly unfinished and do not imply completed features.
- **Checks:** Router and HTTP service tests for success/failure; state component tests; frontend completion commands; manual keyboard/narrow-screen shell and proxy checks.
- **Dependencies:** 003.

## Task 005 — Asset management

- **Status:** Complete.
- **Objective:** Deliver the end-to-end asset list, detail, creation, and editing workflow.
- **Work:** Add POST/PUT asset endpoints, normalized unique codes, request validation, creation events and server-owned timestamps. Build paginated searchable/filterable asset list, details with readings/status/history, and reusable reactive create/edit form. Match client/server validation and label units/null readings clearly.
- **Acceptance:** User can create an asset, find it, inspect it, and edit it; changes survive refresh; duplicate codes return 409 and show an actionable form error; invalid inputs are rejected on both sides; new assets start Healthy with an initial event; computed status cannot be edited. Every data page handles loading/error/empty states.
- **Checks:** API integration tests for 201/Location, update, missing IDs, validation, duplicate codes (including normalized input), and persisted history; frontend form/service/component tests; manual create -> search -> detail -> edit flow; both completion command sets.
- **Dependencies:** 003 and 004.

## Task 006 — Alarm management

- **Objective:** Deliver active/resolved alarm monitoring with reliable acknowledgement and resolution.
- **Work:** Add API-only manual alarm creation and detail retrieval, acknowledge and resolve endpoints; transactional status-history updates and same-asset transition protection. Build paginated alarm UI with asset/severity/status filters, action feedback, timestamps, and links to assets. Document API requests for raising demo alarms; do not add an alarm creation UI.
- **Acceptance:** Users filter and acknowledge/resolve alarms; acknowledgement remains unresolved; repeat actions preserve timestamps; acknowledge-after-resolution returns 409; direct resolution works. Derived asset health reflects the highest unresolved severity, and history records only real status changes. Alarm, parent timestamp, and history writes are atomic.
- **Checks:** Unit tests for transition/status rules; SQL Server API tests for all transitions, repeat requests, missing asset/alarm, multiple alarm severities, event creation/suppression, rollback, and competing same-asset transitions; frontend action/filter/error tests; manual raise through API -> acknowledge -> resolve -> inspect asset history; both completion command sets.
- **Dependencies:** 005.

## Task 007 — Dashboard and visualization

- **Objective:** Make current operational state and recent changes understandable at a glance.
- **Work:** Add aggregate dashboard endpoint with total/Healthy/Warning/Critical assets, unresolved alarm count, and a bounded recent-event list. Build summary cards, a simple CSS proportional status bar with text equivalents, recent events and links to filtered assets/alarms. Provide an explicit refresh action and label synthetic data and displayed update time.
- **Acceptance:** Healthy + Warning + Critical equals total assets; Active and Acknowledged alarms contribute to unresolved count; resolved alarms do not; recent events are ordered and limited. UI handles zero assets, all-one-status data, loading/error/retry, and narrow screens; current data appears after refresh.
- **Checks:** SQL Server aggregate tests for empty/mixed/all-resolved datasets; frontend rendering and refresh tests; verify dashboard counts after lifecycle changes; inspect aggregate queries and avoid per-asset database round trips; both completion command sets.
- **Dependencies:** 006.

## Task 008 — Reliability, validation, and error handling

- **Objective:** Close cross-feature failure gaps and document how to diagnose them.
- **Work:** Review validation and Problem Details across all routes; exercise unavailable API/database, timeouts/cancellation, duplicate submissions, stale filter responses, missing resources, and known database conflicts. Fix demonstrated gaps, verify transaction/concurrency behavior from Task 006, add useful structured logs/trace IDs, and write `docs/TROUBLESHOOTING.md` with reproducible diagnosis/recovery cases.
- **Acceptance:** Forms retain data after failed saves; UI distinguishes errors from empty data; stale responses cannot overwrite current filters; repeated actions do not corrupt lifecycle/history. Expected failures return consistent safe errors, and logs support diagnosis without secrets. Troubleshooting includes actual observed failure/recovery evidence.
- **Checks:** Targeted regression tests for discovered failures; controlled local API/database outage and recovery; validation/error contract tests including unknown API routes; inspect logs; both completion command sets. Do not replace earlier tests with blanket defensive code.
- **Dependencies:** 007.

## Task 009 — Automated testing and CI

- **Objective:** Make the existing quality checks repeatable in GitHub Actions and close meaningful test gaps.
- **Work:** Add workflows for PRs/main pushes with pinned runtime setup, lockfile installation, builds, formatting/lint, backend unit and SQL Server integration tests, frontend tests, and useful failure reports. Add one browser smoke flow from the architecture; document test database isolation and local CI reproduction. Review gaps around key user journeys rather than chasing arbitrary coverage.
- **Acceptance:** All checks run from a clean checkout without developer credentials/data; tests are deterministic and fail the job on failure; SQL Server readiness precedes integration tests. Record a successful GitHub Actions run when remote access and publishing authorization exist; otherwise mark remote verification blocked and leave this acceptance criterion pending.
- **Checks:** Run equivalent pipeline commands locally against disposable SQL Server; validate workflow syntax; run browser smoke; inspect actual hosted run logs/artifacts when authorized. Never describe a locally passing workflow as a verified remote CI run.
- **Dependencies:** 008; GitHub repository access and authorization are prerequisites for hosted evidence, not for preparing workflow files.

## Task 010 — UI/UX and accessibility polish

- **Objective:** Make completed workflows clear and usable across screen sizes and input methods.
- **Work:** Refine spacing, hierarchy, status badges with text/icons, tables, forms, focus management, labels, live feedback, contrast, and touch targets. Make empty/error/loading states consistent; handle long names/messages, missing readings, and long histories. Use the existing CSS approach and avoid a design-system rewrite.
- **Acceptance:** Dashboard, asset create/edit/detail/list, and alarm workflows work using keyboard only and at representative 360px, 768px, and desktop widths; status never depends on color alone; focus is visible; fields have associated errors; no unintended page overflow.
- **Checks:** Manual keyboard/focus and responsive checks; automated accessibility checks on key routes; targeted regression tests for interactive changes; frontend completion checks and the existing smoke flow. Record limitations of automated accessibility evidence.
- **Dependencies:** 009.

## Task 011 — Documentation and portfolio presentation

- **Objective:** Present a truthful, reproducible demonstration of the finished project and target skills.
- **Work:** Replace minimal README with purpose, screenshots from the real app, architecture, features, stack versions, setup/configuration/migration/seed/test commands, demo walkthrough, CI link, and known limitations. Reconcile architecture and roadmap with actual code. Explain representative DTO/EF/validation/testing choices and troubleshooting evidence. Verify GitHub presentation, meaningful commit/PR history and linked workflow results when publishing is authorized.
- **Acceptance:** A fresh reader can run the synthetic-data demo and repeat the key workflows; screenshots/links reflect actual behavior; every required skill has concrete implementation or test evidence; unsupported claims and stale setup steps are removed. No final portfolio claim depends on optional deployment.
- **Checks:** Follow documented setup on a fresh/disposable environment; run the demo walkthrough and documented checks; validate links and absence of credentials; inspect screenshots and evidence matrix; confirm hosted links when available. Record any unverified environment or remote setup explicitly.
- **Dependencies:** 010.

## Task 012 — Optional production deployment

- **Objective:** Publish a small safe demonstration only if separately requested.
- **Work:** Choose a hosting target/budget and access model (private/authenticated or read-only public); review supported runtime decision; prepare deployment configuration, TLS, secrets, migration execution, demo-data reset/retention policy, health checks, and rollback instructions. Add only infrastructure needed for the chosen target.
- **Acceptance:** User approves concrete deployment and any cost/exposure decisions; deployment uses an explicitly accepted supported stack plan and protected writes; secrets remain server-side; smoke checks, migrations, health and rollback work. Local baseline remains reproducible. Keep this task optional when no deployment is requested.
- **Checks:** Predeployment configuration/security review tied to actual hosting; deployment smoke tests, authorization/read-only checks, HTTPS and secret handling checks, migration/rollback rehearsal. Record service URLs and results only after successful authorized deployment.
- **Dependencies:** 011; explicit deployment authorization, hosting access, access-model decision, and .NET lifecycle review.

## Review outcome

The [architecture evidence matrix](docs/ARCHITECTURE.md#planning-review-and-evidence) maps every requested skill to a concrete task. Testing starts in 001, SQL Server verification in 002/003, and error handling in 003; 008/009 consolidate this work. Task 003 is limited to read contracts so asset/alarm write rules stay within their feature tasks. Optional deployment, authentication, and external systems do not expand the baseline.
