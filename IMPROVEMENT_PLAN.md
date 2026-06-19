# File-Manager — Project Review & Improvement Plan

## Context

This repo is a personal homelab tool (LAN-only, `network_mode: host`, internal `10.0.0.x` qBittorrent) for managing a torrent file library. The user asked for a full description of the project plus a prioritized plan of fixes/improvements. Because exposure is LAN-only, classic security findings (open CORS, no auth, unauthenticated delete) are **deprioritized** in favor of correctness, dead code, and maintainability. Scope: **FileManager app**, **SimpleTable lib**, **Infra/build**. SimpleTableDemo only touched where it overlaps infra. Deliverable: this document only — no code changes yet.

---

## 1. What the project is (description)

Monorepo, one solution `FileManager.sln`, four .NET projects + two Angular apps:

| Project | TFM | Role |
|---------|-----|------|
| **FileManager** | net9.0 | Main app: ASP.NET Core API + Angular SPA (`ClientApp`). Scans `/torrent/TV` & `/torrent/Film`, detects hardlinks (libc `stat`), cross-references qBittorrent torrents/files, finds duplicates / empty / small folders, deletes files & folders. Caches scan results to JSON in `/qbit_data`. |
| **SimpleTable** | net10.0 | Reusable lib: server-side table (search/sort/page) over `IQueryable` (EF, expression-tree pushdown) or `IEnumerable` (in-memory). `[NoSearch]` attribute to exclude props. |
| **SimpleTableDemo** | net10.0 | SQLite "Cars" demo of SimpleTable. |
| **simpletable-frontend** | Angular lib (ng-packagr) | Packaged Angular `simple-table` component. |
| **FileManager/ClientApp** | Angular 20 | The real UI (Bootstrap + Material dialog + ngx-toastr + angular-datatables). |

### Backend flow (FileManager)
- `FileController` → `FileSystemService`: `getFiles(Post)`, `getFolders(Post)`, `getEmptyFolders`, `getSmallFolders`, `delete`, `deleteMultiple`, `deleteFolders`.
- `QBitController` → `QBittorrentService` → `QbittorrentClient` (HTTP to qBittorrent WebUI v2 API).
- Caches: `/qbit_data/file_cache.json`, `qbittorrent_cache.json`, `qbittorrent_files.json`.
- `Extensions.ToTableResponse` (reflection-based) does search/sort/page **in FileManager itself** — a parallel, duplicate implementation of the SimpleTable lib (FileManager does not reference SimpleTable).

### Frontend (ClientApp)
- Routes select a hardcoded scan path by `window.location.pathname`. `FileBrowserComponent` drives a custom `SimpleTableComponent` (DOM-manipulating client mode + server mode posting `TableRequest`). Filters persisted to `localStorage`.

---

## 2. Findings (grouped, with severity)

### A. Correctness bugs (HIGH — these make features silently not work)
1. **Duplicate detection is dead.** `ScanFilesInPath` is only ever called with `hashCheck=false`, so `PartialHash` is always `""` and `HashDuplicate` is always false. The `hashDuplicate` filter/UI does nothing. — `FileSystemService.GetFilesInDirectory` / `ScanFilesInPath`.
2. **`Extensions.IsFile()` is broken.** Hardcoded `File.GetAttributes(@"c:\Temp")`, ignores its `path` arg, Windows path on Linux. Returns garbage. — `FileManager/Extensions.cs`.
3. **Frontend `needConfirm` reads wrong key.** Constructor sets `needConfirm` from `localStorage.getItem("folderInQbit")`, not `"needConfirm"`. — `fileBrowser.component.ts` ctor.
4. **Delete-confirm logic inverted.** `deleteFile()` shows the confirm dialog under `if (!this.needConfirm)` and deletes *without* confirm in the `else`. Backwards. — `fileBrowser.component.ts`.
5. **Single global cache file ignores scan params.** `file_cache.json` is one blob keyed by nothing; `GetFilesInDirectory` returns it filtered by path, but folder vs file scans, and different `ScanFolders`, all share/overwrite the same file. Stale/incorrect results across views. — `FileSystemService`.
6. **`QbittorrentClient.AuthenticateAsync()` starts with `return;`** — all code below is unreachable; `_useBasicAuth` is never applied as a header. Auth works only because qBittorrent host-whitelists the LAN. Confusing dead code. — `QBittorrentClient.cs`.

### B. Async / concurrency (MEDIUM)
7. **Sync-over-async everywhere.** `QBittorrentService` methods are `async Task<>` but bodies are fully synchronous inside `lock`, using `.GetAwaiter().GetResult()`. `FileSystemService` calls them with `.GetAwaiter().GetResult()` too. No real async; risk of deadlock/thread-pool starvation. — `QBittorrentService`, `FileSystemService`.
8. **Global `static` locks** (`FileSystemService._lockObj`, `QBittorrentService.Lock`) serialize every request through one mutex. Acceptable for a single-user tool but worth noting.
9. **`GetDirectoriesInDirectory` / `GetSmallFolders` re-walk overlapping trees** (`EnumerateDirectories(AllDirectories)` then per-dir `EnumerateFiles(AllDirectories)`) → roughly O(n²) disk walks on large libraries.

### C. Dead / scaffold code (LOW, cleanup)
10. Large commented-out block in `GetDirectoriesInDirectory`. — `FileSystemService`.
11. Commented-out `FeatureLoggerSeeder` block + identical if/else branches in `Program.cs` and `Startup.Configure`. — `Program.cs`, `Startup.cs`.
12. Empty `Models/PropertyMapping.cs`; leftover scaffolding `SomeService` / `data.ts` / `IndexController` "Hello world from backend".
13. **`UseDeveloperExceptionPage()` in BOTH branches** of `Startup.Configure` (incl. non-dev). Fine for homelab but pointless; the prod branch should use real error handling or at least be intentional.
14. **`ng serve` spawned via `Process.Start` on every dev startup** (random free port, never disposed). Works but leaks node processes across restarts. — `Startup.Configure`.

### D. Duplication / architecture (MEDIUM)
15. **Two parallel table implementations.** `FileManager/Extensions.ToTableResponse` (reflection, sorts whole list, re-reflects per row) duplicates `SimpleTable` lib. FileManager should reference and use the `SimpleTable` lib's `IEnumerable` overload and delete its own copy — single source of truth, faster, and the lib already handles `[NoSearch]`, nested objects, etc.
16. **Two `TableRequest`/`TableResult` definitions** (FileManager `Models/Dtos` vs SimpleTable `Models`) plus a third TS copy. Consolidate on the lib's.
17. **Frontend query-param building duplicated** between `file.service.ts` and `fileBrowser.component.ts.buildUrl()` (same hardlink/inQbit/folderInQbit/clearCache logic, twice). Centralize in the service.

### E. SimpleTable lib (MEDIUM)
18. **EF reference via hardcoded `HintPath`** to `..\..\..\.nuget\packages\microsoft.entityframeworkcore\10.0.0\...dll`. Fragile, machine-specific. Replace with `<PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.0" />`.
19. `TableResult.ReturnedRecords => Items.Count()` can enumerate a lazy `IEnumerable` twice; ensure `Items` is materialized (it is, via `.ToList()` — fine, but type is `IEnumerable<T>`; consider `IReadOnlyList<T>`).
20. No `FilteredCount` in the lib's `TableResult` (FileManager's has it) — paging UI can't show "x of y after filter" consistently. Align the shapes.

### F. Infra / build (MEDIUM)
21. **Mixed target frameworks** in one solution: FileManager net9.0, SimpleTable/Demo net10.0. Pick one (net9 or net10) across the board to avoid SDK/runtime drift and the EF version split.
22. **Tracked/dirty SQLite files**: `SimpleTableDemo/database.db`, `.db-shm`, `.db-wal`, `database_v2.db` show as untracked. Root `.gitignore` only has `.idea`. Add `*.db`, `*.db-shm`, `*.db-wal`, `bin/`, `obj/` to gitignore; stop tracking db artifacts.
23. **Duplicate `PackageReference` to `Microsoft.AspNetCore.SpaServices.Extensions`** in `SimpleTableDemo.csproj`.
24. `SimpleTableDemo.SeedDatabase()` called inside `ConfigureServices` (DI registration side-effect) and `ContextService` news up `CarsContext` directly instead of via DI. Demo-only, low priority.
25. Dockerfile: redundant `FROM build AS publish` + `COPY --from=build /app/publish .` stage that does nothing useful; `dotnet build` then `dotnet publish` both run (publish already builds). Minor image-time waste.

### G. Frontend modernization (LOW, optional)
26. `HttpClientModule` is deprecated → `provideHttpClient()`. `standalone: false` NgModule app could move to standalone, but that's a larger migration — optional.
27. `SimpleTableComponent` does **DOM-based** client-side sort/filter (`querySelectorAll`, `appendChild`, `style.display`) — fights Angular's renderer. Server-side mode is the one actually used by FileBrowser; consider dropping/clearly-separating the DOM client mode.
28. No tests anywhere (only default karma/spec scaffolds).

---

## 3. Recommended plan (prioritized)

> Ordered by value-for-effort for a single-user homelab tool. Each item is independent; do top-down.

### Phase 1 — Fix what's silently broken (HIGH)
- **P1.1** Fix frontend delete safety: correct `needConfirm` localStorage key (#3) and un-invert the confirm-dialog branch in `deleteFile()` (#4). *Files: `fileBrowser.component.ts`.*
- **P1.2** Decide on duplicate detection (#1): either wire `hashCheck=true` through `ScanFilesInPath` (accepting the 8 MB-per-inode hashing cost, ideally gated behind the `hashDuplicate` query flag so it only runs when asked) **or** remove the dead `PartialHash`/`HashDuplicate` code + UI. Recommend: gate it behind the flag so the feature actually works on demand. *Files: `FileSystemService.cs`, `Extensions.FilterResults`.*
- **P1.3** Fix or delete `Extensions.IsFile()` (#2) — it's unused-looking; grep first, then remove or implement with the real `path` arg. *Files: `FileManager/Extensions.cs`.*
- **P1.4** Make the file-scan cache key off the actual scope (per scan-root / per mode), or store the full scan once and always filter from it consistently (#5). Define one clear cache contract. *Files: `FileSystemService.cs`.*

### Phase 2 — De-duplicate the table stack (MEDIUM)
- **P2.1** Have FileManager reference the **SimpleTable** project and replace `FileManager/Extensions.ToTableResponse` + `Models/Dtos/TableRequestDto`/`TableResult` with the lib's `IEnumerable` overload and models (#15, #16). Delete the duplicates. Align `TableResult` shape (add `FilteredCount` or standardize, #20).
- **P2.2** Centralize frontend query-param building in `file.service.ts`; `fileBrowser.component.ts` calls the service instead of re-building URLs (#17).

### Phase 3 — Async & build hygiene (MEDIUM)
- **P3.1** Make `QBittorrentService` genuinely async (real `await` on `HttpClient`/file IO; replace `lock` with `SemaphoreSlim` for the cache critical section) and remove `.GetAwaiter().GetResult()` call chains in `FileSystemService` (#7, #8). Bigger change — do after Phase 1/2.
- **P3.2** Replace SimpleTable EF `HintPath` with a `PackageReference` (#18).
- **P3.3** Unify target frameworks across the solution (net9 **or** net10) (#21).
- **P3.4** `.gitignore`: add `bin/`, `obj/`, `*.db*`; `git rm --cached` the tracked db files; remove duplicate SpaServices PackageReference in demo (#22, #23).

### Phase 4 — Cleanup (LOW)
- Remove dead/commented code (#10–#14): commented blocks, empty `PropertyMapping.cs`, leftover scaffold (`SomeService`, `data.ts`, demo "Hello world"), duplicate `Program.cs` branches, make the prod exception-page branch intentional, dispose/guard the `ng serve` process.
- Dockerfile: drop the no-op `publish` stage, run only `dotnet publish` (#25).

### Phase 5 — Frontend redesign (in-place modernization) — see §6
The frontend is an old "Template_Angular" scaffold carrying heavy, mostly-unused baggage. Modernize **in place** (no fresh app), **Bootstrap-only** UI, and **rewrite the custom `simple-table` Angular-idiomatically**. Detailed below. Do after Phase 2 (so the table data contract is already unified).

---

## 4. Verification

No build/test harness exists, so verify per phase manually:

- **Build:** `dotnet build FileManager.sln` (after #21 the whole solution builds on one TFM). Currently FileManager builds net9; confirm green before/after each phase.
- **Backend run:** `docker compose up --build` (or `dotnet run --project FileManager` with `ASPNETCORE_ENVIRONMENT=Development`). Hit:
  - `GET /api/qbit` and `/api/qbit/torrentfiles` → returns torrent/file lists.
  - `POST /api/file/getFilesPost?path=/torrent/TV` with a `TableRequest` body → paged result.
  - For **P1.2**: `...&hashDuplicate=true` → rows now actually flagged as duplicates.
- **Frontend:** load `/files/tv`, `/directories/empty/tv`, `/directories/small/tv`; toggle the "need confirm" setting and delete a file — **P1.1** verified when confirm dialog appears only when confirmation is requested, and the setting persists across reload.
- **Cache (P1.4):** switch between a file view and a folder view, then back; results must not bleed between scopes. Use the `clearCache=true` flag to force a rescan and confirm cache files in `/qbit_data` regenerate.
- **SimpleTable lib (P3.2):** `dotnet build SimpleTable/SimpleTable.csproj` on a clean machine/after `dotnet nuget locals all --clear` — must restore EF via PackageReference without the HintPath.

---

## 6. Frontend redesign (in-place modernization, Bootstrap-only)

### Decisions (per answers)
- **Approach:** modernize the existing app in place (keep `FileManager/ClientApp`, keep routes/API contracts) — not a fresh rewrite.
- **UI kit:** **Bootstrap 5 only.** Remove Angular Material, ng-bootstrap, jQuery, datatables.
- **Table:** keep a **custom `simple-table`**, but rewrite it Angular-idiomatic (data-bound `@for`, no `querySelector`/`appendChild`/`style.display`).

### Current state (why redesign)
- `package.json` is bloated with unused/legacy deps: `oidc-client`, `prismjs`, `marked`, `chart.js`, `jquery`, `datatables.net*`, `angular-datatables`, `aspnet-prerendering`, plus SSR (`@angular/ssr`, `platform-server`, `express`, `server.ts`) that isn't used by this LAN tool.
- Mixed UI kits loaded at once: Bootstrap **+** Angular Material (`MatDialog`) **+** ng-bootstrap (`ngbTooltip`) **+** ngx-toastr.
- NgModule-based, `standalone: false`, `HttpClientModule` (deprecated).
- `app.component.html` is `<html><body><index></body></html>` — **no app shell / nav menu**; navigation between the 10 routes is URL-only.
- Routing decides scan path via `window.location.pathname` string matching in the component, not route `data`.
- `SimpleTableComponent` manipulates the DOM directly and mixes a server mode (used) with a DOM-based client mode (unused by FileBrowser).

### Redesign tasks
- **P5.1 — Dependency diet.** Remove `oidc-client`, `prismjs`, `marked`, `chart.js`, `jquery`, `datatables.net`, `datatables.net-bs5`, `datatables.net-dt`, `angular-datatables`, `aspnet-prerendering`, `@angular/material`, `@angular/cdk`, `@ng-bootstrap/ng-bootstrap`. Decide on SSR: drop `@angular/ssr`/`platform-server`/`express`/`server.ts`/`app.server.module.ts` unless wanted (recommend drop for this tool). Keep: Angular core/common/forms/router, `bootstrap`, `ngx-toastr`, `@popperjs/core` (for Bootstrap JS), `rxjs`. Rename the package from `template_angular`.
- **P5.2 — Standalone + signals.** Convert components to `standalone: true`; replace `AppModule`/`AppRoutingModule` with `bootstrapApplication` + `provideRouter` + `provideHttpClient()` + `provideAnimations` (or Bootstrap CSS transitions) + ngx-toastr provider. Use signals for component state (`fileList`, filters, `loading`).
- **P5.3 — App shell + nav.** Real `app.component` layout: Bootstrap navbar/sidebar linking the routes (qbit dashboard, qbit files, files TV/Film, directories, empty/small). Render `<router-outlet>` instead of `<index>` directly.
- **P5.4 — Route-driven config.** Replace `window.location.pathname` `if`-chains with route `data` (e.g. `{ scanPath:'/torrent/TV', mode:'files' }`) read via `ActivatedRoute`. Collapses the big `ngOnInit`/`buildUrl` branching.
- **P5.5 — Rewrite `simple-table`.** Data-bound rendering: `@for` over `items()` signal, header click sets sort signal, search/pagination via signals + a single `(request)` output / input `url`; backend POST stays the same `TableRequest`/`TableResult`. Delete DOM client mode (or keep a pure in-memory `computed` filter — no DOM). Column defs passed as inputs instead of projected `<thead>` + `data-column-key` strings.
- **P5.6 — Replace Material/ng-bootstrap UI.** `MatDialog` confirm → Bootstrap modal component (`DialogService` returns a promise/observable as today). `ngbTooltip` → Bootstrap tooltip (`data-bs-toggle="tooltip"` + popper) or a tiny title-based directive.
- **P5.7 — De-dupe + tidy.** Centralize query-param building in `file.service.ts` (ties into **P2.2**); remove leftover scaffold (`SomeService`, `data.ts`, `index` "Hello world"); fix the `needConfirm`/confirm-dialog bugs here if not already done in **P1.1**.

### Redesign verification
- `yarn build` succeeds with the trimmed `package.json`; bundle is noticeably smaller (no jQuery/datatables/Material).
- App shell renders with working nav between all routes; deep-linking still works.
- File & folder grids load via server mode, sort by clicking headers, paginate, search, and select — all without any `querySelector`/DOM mutation in the component.
- Delete confirm modal (Bootstrap) appears per the `needConfirm` setting; tooltip on torrent-path info icon works.

## 7. Explicitly out of scope (per answers)
- Auth / CORS lockdown / delete-endpoint hardening — LAN-only homelab, deprioritized. (Noted for the record: open CORS, no auth, path-only-length-10 delete guard.)
- SimpleTableDemo beyond the infra overlaps (gitignore, duplicate package ref).
- Test suite buildout (none today) — flagged but not planned.
