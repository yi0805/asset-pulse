# Task 004 — Angular application shell

- Date: 2026-09-18
- Status: Complete
- Roadmap entry: [Task 004](../../ROADMAP.md#task-004--angular-application-shell)

## Objective

Build the standalone Angular shell and frontend conventions needed by later feature tasks: routing, responsive navigation, typed read-only API access, reusable request feedback, and focused frontend tests. Asset, alarm, and dashboard features remain out of scope.

## What changed

- Replaced the scaffold screen with a semantic AssetPulse shell: brand/context, primary navigation, active route styling, and a routed main area.
- Configured standalone Angular Router and HttpClient providers. `/` redirects to `/dashboard`; all future feature routes are deliberately unfinished placeholders, and the wildcard route is an accessible Not Found page.
- Added explicit API string-enum unions, response models, list-query models, and small typed read-only asset/alarm services. All URLs remain relative `/api` URLs.
- Added a small Problem Details mapper and standalone loading, empty, and retry-capable error presentation components.
- Added routing/shell, HTTP client, Problem Details, and shared state tests. No feature page requests data or includes fake monitoring data.

## Important files

- [app.routes.ts](../../frontend/src/app/app.routes.ts): route map and placeholder route data.
- [app.html](../../frontend/src/app/app.html) and [app.css](../../frontend/src/app/app.css): application shell and responsive navigation.
- [api.models.ts](../../frontend/src/app/models/api.models.ts): API contracts and read query types.
- [asset-api.service.ts](../../frontend/src/app/assets/asset-api.service.ts) and [alarm-api.service.ts](../../frontend/src/app/alarms/alarm-api.service.ts): typed Task 003 read endpoints.
- [api-error.ts](../../frontend/src/app/shared/api-error.ts): safe Problem Details mapping.
- [states](../../frontend/src/app/shared/states): reusable loading, empty, and error presentation components.

## Architecture and design decisions

The shell uses ordinary semantic HTML and CSS variables rather than a UI library. Navigation wraps on narrow screens, content remains within a 72rem shell, and global `:focus-visible` outlines make keyboard focus clear. Status color variables are established only as future styling primitives; no status is represented in Task 004 UI.

The API types mirror Task 003's camel-case JSON and string enums. Query builders include a parameter only when its value is not `undefined`, preserving valid zero values. Low-level services return `HttpClient` observables without swallowing errors; parent feature pages will decide when to map and render errors.

## Commands and tests executed

| Command / working directory | Result |
| --- | --- |
| `npm ci` / `frontend` | Passed after a stale partial local `node_modules` installation completed cleanup; no package changes. |
| `npm run lint` / `frontend` | Passed. |
| `npm run format:check` / `frontend` | Passed. |
| `npm run test:ci` / `frontend` | Passed: **18/18 tests**, 5 files. |
| `npm run build` / `frontend` | Passed. |
| `dotnet restore backend/AssetPulse.sln` / root | Passed; all projects up to date. |
| `dotnet build backend/AssetPulse.sln --no-restore` / root | Passed: 0 warnings, 0 errors. |
| Angular development-server route checks | Passed: `/`, `/dashboard`, `/assets`, `/assets/new`, `/assets/1`, `/assets/1/edit`, `/alarms`, and an unknown route all returned the shell. |
| Rendered browser checks | Passed at 360px, 768px, and 1280px: expected placeholder/Not Found text rendered and `scrollWidth` did not exceed viewport width. Client navigation updated the active link; browser back/forward returned `/assets` then `/alarms`; Tab focused the brand link. |
| `http://localhost:4200/api/health` | Passed: proxy returned `200 Healthy` from `http://localhost:5200/api/health`. |

## Results

Task 004's route map, active responsive shell, standalone HttpClient/Router setup, typed read contracts, error mapping, and reusable presentation states are implemented and verified. The test suite covers root redirection, shell navigation/active state, required feature placeholders, Not Found navigation, HTTP query serialization/omission, response contracts, Problem Details mapping, and state feedback/retry emission.

## Known limitations

The dashboard, asset list/detail/create/edit, and alarm management pages are placeholders by design. The stale long-lived development SQL Server container and its volume were not reset or modified; only the existing API health endpoint was used through the frontend proxy. No `.env` file was created.

## Git state / commit

Branch: `task/004-angular-application-shell`, created from `8dc18b2321ecd31ac30b81a3fe23766f8c98f740` (`main` and `origin/main` after merged PR #3). This handoff is included in the Task 004 commit after final diff review; the final commit and PR are reported with the task completion.

## Exact recommended next task

**Task 005 — Asset management.** It may begin only after Task 004 review/merge as appropriate. First read this handoff and inspect the typed asset service/routes, then implement actual asset list, filtering, detail/history, and create/edit behavior using the existing Task 003 reads and Task 005-authorized write contracts. **Task 005 has not started.**
