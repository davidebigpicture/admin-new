# WVBPS Admin Shell Pilot

This is an unlinked, client-local pilot. Files under `A:\GLOBAL_6-next\admin`
are read-only source material and must not be edited.

See [`docs/managed-admin-shell-plan.md`](docs/managed-admin-shell-plan.md) for
the current implementation status, IIS setup, verification, and later waves.
Before continuing work, also read
[`docs/agent-handoff.md`](docs/agent-handoff.md) for environment boundaries,
known constraints, the tool-migration checklist, and pending remote tests.

## IIS layout

- `/dev/adminshell` remains inside the existing WVBPS front-end application.
- The managed endpoints inherit the parent .NET Framework 4.8 configuration.
- Shared pilot VB classes live in the application-root
  `A:\wvbps\www\html\App_Code\AdminShell` folder when deployed to IIS.
  This repository also keeps a copy under `App_Code/AdminShell/` for version
  control; deploy those files to the application-root `App_Code\AdminShell`
  folder (flat — no nested subfolders).
- `managed\web.config` contains only pilot app settings; it must not contain
  application-level sections such as `authentication`, `compilation`, or
  `sessionState`.

Pilot entry point:

`https://dev.services.wvbps.wv.gov/dev/adminshell/managed/login.html`

Copied pilot tools:

| Pilot route | Canonical ACL identity |
|-------------|------------------------|
| `/dev/adminshell/views.asp` | `/admin/admin/views.asp` |
| `/dev/adminshell/loginlog.asp` | `/admin/admin/loginlog.asp` |
| `/dev/adminshell/sql_logs.asp` | `/admin/admin/sql_logs.asp` |
| `/dev/adminshell/sms_logs.asp` | `/admin/admin/sms_logs.asp` |
| `/dev/adminshell/managed/access-manager/index.html` | `/admin/admin/cgi-bin/accessadmin.pl` |

Access Manager SPA:

`https://dev.services.wvbps.wv.gov/dev/adminshell/managed/access-manager/index.html`

The SPA uses document-relative JSON APIs under `managed/access-manager/api/`
and shared shell assets under `managed/shared/`. It now uses one section-centered
workspace: section names and assigned scripts are editable inline, script CRUD
is available from the section, and modal lookups replace raw script/principal
IDs. Principal-centric access search is launched from the same workspace.
The shell also renders the user's access-filtered sections and scripts in a
searchable, collapsible left menu following the legacy navigation hierarchy.
Perl admin tools remain
available at their canonical `/admin/admin/cgi-bin/...` paths as rollback.

Route mappings, nav labels, and the default post-login route are configured in
`managed/web.config` (`PilotRoutes`, `PilotDefaultRoute`). Unknown routes are
denied. The HTML5 login still defaults to Views.

## Isolation

- The managed login uses the separate `bp_admin_next` cookie.
- The cookie contains no password and does not satisfy legacy Perl auth.
- The login UI is semantic HTML5 and JavaScript backed by a VB.NET JSON API;
  it does not use Web Forms, server controls, postbacks, or view state.
- Only the configured host and `PilotUsers` allowlist can sign in.
- Each copied route is authorized against its own canonical
  `/admin/admin/...` ACL identity from `PilotRoutes`.
- Pilot shell navigation is generated from the same route configuration.
- No existing menu or login route points to this pilot.

## Verification

The managed pages and application-root classes compile as part of the parent
.NET Framework 4.8 application.
`managed/App_Data/tests/PilotPolicyTests.vb` covers host/user gating, config-
driven route mapping (happy path, unknown route, malformed config, case-
insensitive matching), return URL validation, and hash comparison.
`managed/App_Data/tests/AccessManagerValidationTests.vb` and
`AccessManagerServiceTests.vb` cover Access Manager validation and service
rules with an in-memory repository fake.
`managed/App_Data/tests/AccessManagerStateTests.js` covers pure client reorder
helpers (`node AccessManagerStateTests.js` when Node is available).
`managed/App_Data/tests/Test-PilotData.ps1` verifies the pilot user and
canonical Views ACL when run on a machine with the WVBPS ODBC DSN.
`managed/App_Data/tests/Test-AccessManagerData.ps1` verifies the pilot user
has the Access Manager grant capability ACL. This mapped-
drive workstation is not the IIS host; deployed behavior must be verified
through the remote development URL or on the actual server.
