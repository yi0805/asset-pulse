# Task 000 — Project planning and architecture

- Date: 2026-09-18
- Status: Complete
- Roadmap entry: [Task 000](../../ROADMAP.md#task-000--project-planning-and-architecture)

## Objective

Inspect the repository and create the project plan and operating documents for AssetPulse. This task explicitly excludes scaffolding, dependency installation, application implementation, and migrations.

## What changed

Created concise repository instructions, a numbered roadmap with acceptance checks/dependencies, a proposed architecture with a stack-evidence review, a minimal initial README, and a reusable per-task handoff template. Task 000 is marked complete; Task 001 remains planned and requires approval.

## Important files

- [AGENTS.md](../../AGENTS.md): scope, engineering expectations, workflow and future completion commands.
- [ROADMAP.md](../../ROADMAP.md): Tasks 000-012 and their acceptance criteria.
- [ARCHITECTURE.md](../ARCHITECTURE.md): system boundaries, entity/API rules, configuration, tests, evidence and risks.
- [README.md](../../README.md): initial project summary and honest development status.
- [TEMPLATE.md](TEMPLATE.md): structure for one handoff per completed task.
- This handoff: repository state and next-session starting point.

## Architecture and design decisions

- One Angular frontend, one ASP.NET Core .NET 8 API, and one SQL Server 2022 database through EF Core 8; no unnecessary architectural projects or generic repositories.
- Choose/pin a supported stable Angular release meeting 18+ during Task 001; retain the specifically requested .NET 8 stack and record its approaching support deadline.
- Derive asset health from the highest unresolved alarm severity. Acknowledgement remains unresolved; resolution may change status. Status history is append-only, with transactional updates.
- Readings and alarms are synthetic. Measurements are manually edited; a small API-only alarm creation path supports repeatable demonstrations. No telemetry ingestion or automatic threshold engine.
- Establish tests and safe errors alongside features; Task 009 adds hosted CI and a small browser smoke test. Real SQL Server verifies persistence.
- Local unauthenticated demo first; deployment and its access/runtime decisions remain optional and separately authorized.

## Commands and tests executed

Commands were run from `E:\asset-pulse` unless otherwise stated.

| Command / check | Result |
| --- | --- |
| `Get-Content -LiteralPath <user attachment path>` | Read the full Task 000 request. |
| `Get-Location`; `rg --files -g AGENTS.md -g package.json -g README* -g '!node_modules' -g '!vendor'`; `Get-ChildItem -Force`; `git ls-files` | Confirmed an empty working tree containing only `.git`; no existing application or tracked files. |
| `Get-ChildItem -LiteralPath 'E:\' -Filter AGENTS.md -Force` | No parent instruction file found; followed the global instructions supplied in the conversation. |
| `git status --short --branch`; `git log -1 --format='%h %s'` | Initial branch is `main`, with no commits; log reports that the branch has no commits. Upstream displays `origin/main [gone]`. No network fetch or push performed. |
| Official Angular support/compatibility and Microsoft .NET support documentation review | Linked primary sources in architecture; .NET 8 support ends November 10, 2026. No versions installed or changed. |
| PowerShell documentation audit (local links/anchors, task sections, dependency ordering, required fields, trailing whitespace, file scope) | Passed for all six documents and Tasks 000-012. |
| `git diff --check`; `git diff --stat`; `git diff`; direct review of all new documents | Passed review. Git diff has no tracked changes because all six new documents remain untracked; the documentation audit and new-file whitespace checks cover them. |
| `git -c core.autocrlf=false diff --no-index --check -- NUL <each new Markdown file>` | Checked new-file whitespace independently of the index; passed with no diagnostics. The initial audit incorrectly treated no-index's difference exit code 1 as failure; corrected the audit to accept 0/1 only with no diagnostics, then reran successfully. The per-command line-ending setting does not change repository configuration. |
| Application builds/tests, Docker and migrations | Not run: no application exists, and Task 000 prohibits scaffolding/installations. |

## Results

All requested planning artifacts are present. Every roadmap task includes an objective, work, acceptance criteria, checks and dependencies. The evidence matrix covers Angular, TypeScript, HTML/CSS, C#, .NET 8, ASP.NET Core, REST, EF Core, SQL Server, GitHub, tests, troubleshooting/error handling, and CI. The roadmap keeps Task 003 focused on read APIs and delivers writes within their feature tasks.

## Known limitations

No runtime behavior or toolchain availability has been verified. Proposed commands will become executable only as their owning tasks create the projects/configuration. SQL Server requires a compatible Docker/runtime environment. Remote repository state and hosted CI have not been verified. .NET 8's support deadline needs an explicit decision before later production deployment; this task does not authorize a stack change.

## Git state / commit

Branch: `main`, unborn (no commits). No commit was created. Initial status was clean. Final new/untracked content consists of `AGENTS.md`, `README.md`, `ROADMAP.md`, `docs/ARCHITECTURE.md`, `docs/handoffs/TEMPLATE.md`, and this handoff; Git's default short status may collapse the last three under `docs/`. The configured upstream is reported locally as `origin/main [gone]`; remote availability is unknown.

## Exact recommended next task

**Task 001 — Application scaffold**, after explicit user approval. First read AGENTS.md, Task 001 in ROADMAP.md, ARCHITECTURE.md, and this handoff; inspect Git status and installed tool versions. Then select compatible supported Angular/Node/TypeScript versions, pin .NET 8, create the minimal projects/test harnesses, establish the planned lint/format/build/test command contract, and document local startup. Do not begin Task 002 or business features as part of Task 001.
