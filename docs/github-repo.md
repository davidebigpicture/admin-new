# GitHub Repository

Last updated: July 17, 2026

## Remote

- **URL:** https://github.com/davidebigpicture/admin-new
- **Default branch:** `main`
- **Account:** `davidebigpicture`

## Local working copy (git)

Use a **local disk** clone — not the mapped drive:

```text
E:\web\repos\admin-new
```

Clone or pull here, commit, and push. Sync changed files to the IIS mapped drive
for deployment.

```bash
git clone https://github.com/davidebigpicture/admin-new.git E:/web/repos/admin-new
```

## What is versioned

The repository contains:

1. **Pilot tree** — Classic ASP tools, managed endpoints, Access Manager SPA,
   shared shell assets, tests, and docs (mirrors
   `A:\wvbps\www\html\dev\adminshell` on the server).
2. **`App_Code/AdminShell/`** — shared VB.NET classes. On IIS these deploy to
   the application-root `App_Code\AdminShell` folder (flat; no nested
   subfolders).
3. **`App_Code/RedisService.vb` and `App_Code/RedisSession.vb`** — shared
   StackExchange.Redis helpers (DAPE-derived, CacheManager-compatible). On IIS
   these deploy to the application-root `App_Code\` folder beside any existing
   site services such as `FormSessionService.vb`.

## Live vs repo layout

| Environment | Pilot files | VB.NET classes |
|-------------|-------------|----------------|
| **IIS (live)** | `www/html/dev/adminshell/` | `www/html/App_Code/AdminShell/` plus `www/html/App_Code/RedisService.vb` and `RedisSession.vb` |
| **Git repo** | repo root | `App_Code/AdminShell/` plus `App_Code/RedisService.vb` and `RedisSession.vb` |

**Workflow:** edit in `E:\web\repos\admin-new` → commit/push → copy/sync to
mapped drive paths for remote IIS testing.

Do **not** keep the git working tree on `A:\` (network mapped drive). Git is
slow and requires `safe.directory` workarounds there.

## Secrets

Do not commit:

- Production credentials or tokens
- Local `web.config` overrides with secrets
- `.env` files

`managed/web.config` contains pilot **appSettings** only (hosts, route map,
allowlist). Review before pushing if values are environment-specific.

## Agent reminder

**Put plans in `docs/`** before or as part of implementation work. Link new plan
documents from `agent-handoff.md` and update status checklists when steps
complete.
