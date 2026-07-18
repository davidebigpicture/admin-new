# Admin Shell Pilot: Agent Handoff

Last updated: July 18, 2026

This file is the operational handoff for the next agent. Read
[`admin-shell-platform.md`](admin-shell-platform.md) first, then this file with
[`managed-admin-shell-plan.md`](managed-admin-shell-plan.md),
[`shell-unification-plan.md`](shell-unification-plan.md), and
[`github-repo.md`](github-repo.md) before changing the pilot.

## Non-negotiable boundaries

- Treat `A:\GLOBAL_6-next\admin` as read-only source material. Never edit it.
- Make client-local Classic ASP changes only under
  `A:\wvbps\www\html\dev\adminshell`.
- Put shared pilot VB.NET source flat under
  `A:\wvbps\www\html\App_Code\AdminShell` using `Pilot*` /
  `AccessManager*` / `CodeAdmin*` filename prefixes. Do not nest App_Code
  subfolders under `AdminShell` (ASP.NET compiles them as separate assemblies and
  breaks cross-references; that shows up as remote 500s on `login.ashx`).
- This development machine is a workstation with mapped drives to the web
  server. It is not the IIS host. Do not treat local commands, local process
  state, or local port checks as evidence about the deployed application.
- IIS/runtime verification must be performed through the remote development
  URL or on the actual IIS server by the user.
- Do not add application-level ASP.NET sections to
  `adminshell\managed\web.config`. That folder is not an IIS application root.
- Keep global admin assets and endpoints on `/admin/admin/...`, including
  stylesheets, JavaScript, includes, Ajax handlers, and canonical ACL paths.
- Prefer semantic HTML5 and plain JavaScript backed by narrow VB.NET JSON APIs.
  Do not reintroduce Web Forms, server controls, postbacks, view state, or
  master pages.
- Preserve copied tool business logic unless the user separately requests a
  tool change. Migration changes should be limited to shell includes, title,
  required global include paths, and route/ACL registration.

## Code Admin (.NET rewrite)

**Status (July 18, 2026):** MVP deployed on WVBPS dev. Logged-in browser
verification passed: class dropdown (Oracle), value grid, Add enabled when a
class is selected, `workspace.ashx` and `values.ashx` return 200. Not a full
CRUD/regression pass against every Perl edge case.

**Perl source of truth (read-only):** `A:\GLOBAL_6-next\admin\cgi-bin\codeadminO.pl`

- Entry script; `require 'lib/tools.pl'`; data access in `cgi-bin/lib/dbo.pl`
  (`getCodeClassesO`, `getCodeClassO`, `getCodeValuesO`, etc.) via **`$dbhO`**
  (Oracle). When debugging SQL or behavior, open those two files — do not grep
  the whole legacy tree.
- Canonical ACL: `cgi-bin/codeadminO.pl` → pilot route
  `managed/code-admin/index.aspx` in `managed/web.config` `PilotRoutes`.

**Pilot entry:**

`https://dev.services.wvbps.wv.gov/dev/adminshell/managed/code-admin/index.aspx`

(Server-side auth on `index.aspx` via `CodeAdminPage`; unauthenticated users
redirect to `managed/login.html`.)

**VB.NET (repo → deploy):**

| Layer | Repo | Deploy |
|-------|------|--------|
| APIs + page auth | `App_Code/AdminShell/CodeAdmin*.vb` | `A:\wvbps\www\html\App_Code\AdminShell\` |
| UI | `managed/code-admin/` | `A:\wvbps\www\html\dev\adminshell\managed\code-admin\` |

Backend files: `CodeAdminModels.vb`, `CodeAdminValidation.vb`,
`CodeAdminRepositoryInterface.vb`, `CodeAdminRepository.vb`, `CodeAdminService.vb`,
`CodeAdminAccess.vb`, `CodeAdminApiGuard.vb`, `CodeAdminApiHandlers.vb` (includes
`CodeAdminPage`).

UI: `index.aspx`, `code-admin.css`, `js/state.js`, `js/view-model.js`, `js/app.js`,
`api/session.ashx`, `api/workspace.ashx`, `api/values.ashx`.

**Database:** **`ConnectionString`** (Oracle / OraOLEDB — same as Perl `$dbhO` and
`views.asp`). Implemented with **`System.Data.OleDb`** in `CodeAdminRepository.vb`.
Do **not** use **`ConnectionStringB`** (MySQL `dsn=wvbps`) — that caused wrong
tables, missing columns (`edit`, `code_value_id`), and bogus CMS class lists.
MySQL is for pilot login / Access Manager only.

**Config (WVBPS `managed/web.config`):**

- `CodeAdminMajorCode` = `7400` (WVBPS `ORG_ID`; avoids DB lookup on create/activate)
- `PilotRoutes` includes `managed/code-admin/index.aspx=cgi-bin/codeadminO.pl|Code Admin`

**Security:** Every `.ashx` calls `CodeAdminApiGuard` (session, host, ACL to
canonical route, CSRF on mutations). Parameterized SQL; dynamic column name in
delete “in use” check validated via `CodeAdminValidation.ValidateSqlIdentifier`.

**Fixes applied this session:**

- Wrong DB: `ConnectionStringB` → `ConnectionString` (Oracle OleDb).
- `code_class.edit` probe retained for mixed schemas; WVBPS Oracle has `edit`.
- VB unit tests: missing `AssertFalse` / `Pass` / `Fail` helpers fixed.
- `RequireMajorCode` typo (`End Sub` vs `End Function`) had broken all App_Code — fixed earlier in session.

**Tests (`managed/App_Data/tests/`):**

| Test | Run |
|------|-----|
| `CodeAdminViewModelTests.js` | `node CodeAdminViewModelTests.js` |
| `CodeAdminWorkspaceUiTests.js` | `node CodeAdminWorkspaceUiTests.js` |
| `CodeAdminValidationTests.vb` | Compile with `CodeAdminValidation.vb` + models (see below) |
| `CodeAdminServiceTests.vb` | Compile with all `AdminShell\CodeAdmin*.vb` + `AccessManagerModels.vb` |
| `Test-CodeAdminBrowser.ps1` | Unauthenticated smoke (401 session, page redirect) |

VB compile example (from repo `App_Code/AdminShell`):

```text
vbc /target:exe /reference:System.Configuration.dll,System.Data.dll
    CodeAdminValidationTests.vb CodeAdminValidation.vb CodeAdminModels.vb AccessManagerModels.vb
```

Service tests need full `AdminShell\*.vb` plus `RedisService.vb` / `RedisSession.vb`
and `A:\wvbps\www\html\bin\StackExchange.Redis.dll` only if pulling in
`PilotSecurity.vb` — prefer compiling just Code Admin modules + fakes.

**Browser verification:** Use Cursor **`cursor-ide-browser`** MCP when available.
Protocol: [`.cursor/rules/browser-e2e.mdc`](../.cursor/rules/browser-e2e.mdc).
On empty UI or errors, `browser_cdp` + `fetch('api/workspace.ashx')` and report
HTTP status + JSON body (do not guess from screenshots alone).

**UI styling (known debt — not Bootstrap):**

The pilot shell uses **`managed/shared/shell.css`** (see Access Manager), not
Bootstrap. Code Admin `app.js` still emits **Bootstrap 3 class names**
(`btn btn-primary btn-sm`, `btn-default`, etc.) **without loading Bootstrap CSS**,
so toolbar inputs and buttons look inconsistent.

**Do not add Bootstrap 5** for Code Admin alone. **Recommended fix:** align markup
with shell conventions (`button.primary`, `button.danger`, `.field`, `table.data`,
`.inline-form`) as in `managed/access-manager/js/sections-view.js`. Legacy global
admin uses `/admin/admin/bpstyles.css` (older stack); the pilot intentionally
does not import it.

**Not done / follow-up:**

- UI shell.css alignment (above).
- Full browser E2E: create, edit, patch, activate/deactivate, delete, protected
  values, `GROUP_TY_CD` / `APPLICATION_DB` delete rules.
- Org-specific Perl branches (e.g. WVBPS `LicenseObjType` extra columns) — skipped in MVP.
- Commit/push in `admin-new` if user requests.

**Deploy note:** After `App_Code` changes, recycle the WVBPS app pool or touch
`App_Code\AdminShell` so ASP.NET recompiles. Sync repo → deploy paths above.

## Current deployed shape

Entry point:

`https://dev.services.wvbps.wv.gov/dev/adminshell/managed/login.html`

Configured tools:

- `/dev/adminshell/views.asp`
- `/dev/adminshell/loginlog.asp`
- `/dev/adminshell/sql_logs.asp`
- `/dev/adminshell/sms_logs.asp`
- `/dev/adminshell/managed/access-manager/index.html` (Unified Access Manager SPA)
- `/dev/adminshell/managed/code-admin/index.aspx` (.NET Code Admin SPA)

Access Manager entry:

`https://dev.services.wvbps.wv.gov/dev/adminshell/managed/access-manager/index.html`

Code Admin entry:

`https://dev.services.wvbps.wv.gov/dev/adminshell/managed/code-admin/index.aspx`

Sign in first at `managed/login.html` if the pilot cookie is missing. The SPA
bootstraps from `api/session.ashx`, then calls document-relative APIs under
`api/`. The UI is a single section-centered workspace rather than separate
Sections, Scripts, and Access tabs. It supports inline section/script editing,
script lifecycle and deletion from section detail, searchable script/principal
pickers, and principal-centric direct-grant lookup. The session API also returns
the authenticated user's ACL-filtered section/script hierarchy; `shell.js`
renders it as a searchable, collapsible left menu and remembers collapse state
in local storage. Perl admin tools remain at `/admin/admin/cgi-bin/...` for
rollback.

The route map lives in `managed\web.config`:

```text
PilotRootPath=/dev/adminshell
GlobalAdminRootPath=/admin/admin
PilotDefaultRoute=views.asp
PilotRoutes=<relativePilot>=<relativeCanonical>|<label>;...
```

`PilotRoutes` entries are relative to those roots. Absolute URLs are composed
at runtime. `PilotRoutes` is the single source for:

- deciding whether a pilot route exists;
- resolving its canonical ACL identity under `GlobalAdminRootPath`;
- validating safe post-login return URLs; and
- building shell navigation.

Unknown or malformed routes fail closed. Navigation currently displays every
configured route; authorization is still checked when a route is requested.

## Request flow

1. `login.html` gets a session-bound CSRF token from `login.ashx`.
2. It posts username/password JSON to `login.ashx`.
3. A successful login writes the encrypted, signed, HTTP-only
   `bp_admin_next` cookie scoped to `PilotRootPath`, and (via
   `PilotLegacySession`) legacy `/admin` cookies + Redis `LoginName`.
4. A copied Classic ASP page includes local `topshell.asp`.
5. `topshell.asp` forwards cookies to `authorize.ashx` through
   `includes\ssi.inc` (`pilotBuildCookieHeader`).
6. If `authorize.ashx` returns `NOSESSION`, `topshell.asp` redirects the
   browser to `/admin/admin/pilot-bridge.asp` (legacy cookies are not sent
   to `{PilotRootPath}` — see `admin-shell-platform.md`).
7. The handler resolves the requested pilot path through `PilotRoutes` and
   checks the existing script/section ACL for the canonical path.
8. The Classic ASP page runs only after authorization succeeds.
9. `chrome.ashx` renders the configured shell header and footer.

The pilot cookie contains no password. Legacy topshell still requires the
encrypted `username` cookie on `/admin` paths.

## What has been observed remotely

- The HTML5 login page loads from the remote development site.
- The password reveal control renders and toggles the password field.
- Submitting the non-allowlisted `admin` username returns HTTP 401 and the
  generic invalid-credentials message. This is expected.
- The allowlisted `dhoffman` login reached the copied Views tool.
- Copied `views.asp` was observed at about 0.5 seconds versus about 2.5 seconds
  through the legacy shell. Treat those numbers as an informal browser
  observation, not a controlled benchmark.
- The `/admin/admin/bpstyles.css` path is required for global shell styling.

The three Wave 2 log tools have been copied and wired but still require remote
browser and ACL validation.

## Adding another copied Classic ASP tool

1. Select a low-risk page from `A:\GLOBAL_6-next\admin`; prefer read-only tools
   with few dependencies.
2. Copy it into `A:\wvbps\www\html\dev\adminshell`.
3. Add `pageTitle` before the shell include.
4. Change its shell includes to local `file="topshell.asp"` and
   `file="bottomshell.asp"`.
5. Point global includes/assets to `/admin/admin/...`. Keep `/classes/...`
   virtual includes where the original already uses them.
6. Do not otherwise refactor or repair the copied tool.
7. Add `<pilot path>=<canonical path>|<label>` to `PilotRoutes`.
8. Add route parsing/resolution coverage if the route format changes.
9. Check IDE diagnostics on every modified file.
10. Ask the user to verify login, direct navigation, denial without the
    canonical ACL, and the tool's normal read/write behavior through the
    remote URL.

Do not add a route without its canonical ACL mapping: the mapping is what
prevents a copied URL from bypassing the existing authorization model.

## Remote verification still needed

- Sign in as `dhoffman` and open all pilot navigation tabs (Views, logs, Access
  Manager, **Code Admin**).
- Confirm each tool succeeds only when `dhoffman` has its corresponding
  canonical ACL:
  - `/admin/admin/views.asp`
  - `/admin/admin/loginlog.asp`
  - `/admin/admin/sql_logs.asp`
  - `/admin/admin/sms_logs.asp`
  - `/admin/admin/cgi-bin/accessadmin.pl` (Access Manager SPA entry)
  - `/admin/admin/cgi-bin/codeadminO.pl` (Code Admin entry)
- In Access Manager, verify the one-screen section workspace, inline section and
  script editing, create/select script modal, principal grant modal, access-by-
  principal lookup, keyboard/drag reorder, lifecycle actions, and hard-delete
  preview for a low-risk test section/script.
- In **Code Admin**, verify class dropdown (Oracle classes), value grid, Add
  gating without a class, create/edit/activate/deactivate/delete on a test class,
  and 401/403 without ACL. Compare behavior to Perl `codeadminO.pl` when unsure.
- Remove or test without one ACL and confirm that route returns the pilot
  403 response before tool logic runs.
- Confirm an unknown pilot route returns 404.
- Test logout, expired cookie, session timeout, and safe return URLs.
- Exercise Views inline edits and verify they still call the global
  `/admin/admin/...` Ajax endpoint. After pilot login, `ajax.asp?action=session`
  should return 200 (not 401) once the legacy session bridge is configured.
- **Legacy session bridge:** see [`legacy-credential-encoder.md`](legacy-credential-encoder.md)
  and [`admin-shell-platform.md`](admin-shell-platform.md). Set
  `PilotMembershipEncryptionKey` in gitignored `managed/web.config.local`
  (copy from `web.config.local.example`).
- Check Login Log filtering/details, SQL Log list/file/detail, and SMS Log
  list/date/detail.
- Confirm the legacy Perl login and global `/admin/admin/...` tools remain
  unchanged.
- Run `PilotPolicyTests.vb` and `PilotLoginApiPolicyTests.vb` on an appropriate
  configured machine. Do not claim they ran merely from IDE diagnostics.
- Run `AccessManagerValidationTests.vb`, `AccessManagerServiceTests.vb`,
  `node AccessManagerStateTests.js`, and
  `node AccessManagerWorkspaceUiTests.js` on a configured machine.
- Run Code Admin tests: `node CodeAdminViewModelTests.js`,
  `node CodeAdminWorkspaceUiTests.js`, `CodeAdminValidationTests.vb`,
  `CodeAdminServiceTests.vb`, and `Test-CodeAdminBrowser.ps1` (see Code Admin
  section above).
- Run `Test-PilotData.ps1` only where the WVBPS ODBC DSN is installed. It
  currently checks the pilot user and Views ACL only.
- Run `Test-AccessManagerData.ps1` on the IIS server to confirm the grant
  capability ACL for Access Manager.

## Known constraints and inherited risks

- `loginlog.asp`, `sql_logs.asp`, and `sms_logs.asp` intentionally retain
  their original business logic. Any legacy SQL concatenation, file parameter
  handling, remote service dependency, or output encoding behavior remains
  unchanged.
- SQL Logs depends on the existing FSx log share and the IIS identity's share
  permissions.
- SMS Logs depends on the global database connection/configuration service and
  existing `/classes/...` includes.
- Shell authorization and chrome are separate server-side HTTP calls. This is
  working and fast enough for the pilot; combine them only after measuring and
  preserving behavior.
- `authorize.ashx` URL-encodes the username in its `OK|username` response.
  `topshell.asp` currently assigns that value directly because Classic ASP
  does not provide `Server.URLDecode`. This is harmless for the current ASCII
  allowlisted username, but the response contract must be revised before
  allowing usernames containing spaces or reserved URL characters.
- A configured navigation link may be visible before its per-route ACL is
  known. Clicking it still performs the ACL check and denies access.
- The password hash verification still depends on the existing external hash
  service.

## Troubleshooting landmarks

- `allowDefinition='MachineToApplication'`: an application-level section was
  placed in the child `managed\web.config`; remove it or configure it at the
  true application root.
- Pilot class not defined / opaque 500 on `login.ashx`: verify VB files are
  flat under application-root `App_Code\AdminShell` with `Pilot*` /
  `AccessManager*` / `CodeAdmin*` prefixes (no nested App_Code folders under
  AdminShell), not under `managed\App_Code`.
- Code Admin empty class list or `Unknown column 'edit'`: repository is on
  wrong connection — must be **`ConnectionString`** (Oracle), not
  `ConnectionStringB` (MySQL).
- Code Admin `workspace.ashx` / `values.ashx` 503: check JSON `data.detail`
  for `OdbcException` / OleDb errors; compare SQL to matching `dbo.pl` sub.
- Still seeing the old blue **BIG PICTURE** banner and **Configuration** tabs
  on Views after a shell deploy: the Classic ASP page is still getting the
  previous compiled `PilotShell` from the parent app pool. Recycle the WVBPS
  front-end application pool on the IIS server (or touch any file under
  `App_Code\AdminShell` and wait for ASP.NET to recompile). Confirm in View
  Source: you should see `<!-- pilot-shell-unified:... -->` and
  `<header class="shell-header">`, not `class="row banner"`.
- Do not keep a duplicate `dev\adminshell\App_Code` tree; only application-root
  `App_Code\AdminShell` is compiled.
- Parent “Authentication and Access Control” page: the parent layer intercepted
  the request before the pilot page.
- `NOSESSION`: pilot cookie missing, invalid, or expired.
- `DENY`: user authenticated but lacks the canonical route ACL.
- `UNKNOWN`: requested pilot path is absent from `PilotRoutes`.
- `UNAVAILABLE`: managed authentication, database, hash service, or ACL call
  threw an exception. The public response intentionally hides details.

## Rollback

The pilot is unlinked from the existing admin experience. To roll back a
single copied tool, remove its `PilotRoutes` entry and its client-local ASP
copy. To roll back only Access Manager, remove the Access Manager route from
`PilotRoutes` and delete `managed\access-manager` while keeping Perl tools at
`/admin/admin/cgi-bin/...`. To roll back only Code Admin, remove its
`PilotRoutes` entry and delete `managed\code-admin` plus `CodeAdmin*.vb` from
`App_Code\AdminShell`. To remove the entire pilot, remove `dev\adminshell`
and the `App_Code\AdminShell` tree. Do not change global admin files during
rollback.
