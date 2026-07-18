---
name: commit-mapped-drive
description: >-
  Commit and inspect git state when the workspace is on a mapped drive (e.g. A:)
  but the real git repo lives on a local clone (e.g. E:\web\repos\...). Use when
  the user asks to commit, push, or create a PR while editing synced/mapped paths,
  or when git commands fail with "not recognized", PowerShell && errors, or bash
  path-not-found on Windows.
---

# Git commits with mapped-drive workspaces

> **Repo:** `admin-new` — git root is `E:\web\repos\admin-new`, not a mapped IIS path.
> Platform doc: [`docs/admin-shell-platform.md`](../../../docs/admin-shell-platform.md).

## Mental model

| Layer | Typical path | Role |
|-------|--------------|------|
| **Git repo (commit here)** | `E:\web\repos\admin-new` | Source of truth for `git add`, `commit`, `push` |
| **Mapped IIS sync** | Client-specific (example: `A:\...\dev\adminshell`) | Deploy/test only — not the product name |
| **Global reference** | `A:\GLOBAL_6-next\admin` | Read-only legacy source |

```powershell
Test-Path "E:\web\repos\admin-new\.git"   # must be True before committing
```

If edits were made only on `A:\...`, copy/sync into the E: clone first, then commit from E:.

---

## Git executable on Windows (PowerShell)

`git` is often **not** on the PowerShell `PATH`.

```powershell
$git = "C:\Program Files\Git\bin\git.exe"
if (-not (Test-Path $git)) { $git = "C:\Program Files\Git\cmd\git.exe" }
$repo = "E:\web\repos\admin-new"
```

Use **`& $git -C $repo <subcommand>`** for every git call.

---

## Pre-commit inspection (run in parallel)

```powershell
& $git -C $repo status
& $git -C $repo diff
& $git -C $repo log -5 --oneline
```

```powershell
& $git -C $repo diff --staged
```

### Secrets — never commit

| Never commit | Use instead |
|--------------|-------------|
| `PilotMembershipEncryptionKey` | `managed/web.config.local` (gitignored) |
| `.env`, tokens, `*.pem` | `web.config.local` / server secrets |

`managed/web.config` uses `<appSettings file="web.config.local">`. Commit only `web.config.local.example` with empty placeholders.

If a secret was committed on an **unpushed** HEAD you created: move to `.local`, amend to scrub history, then push.

---

## Stage and commit

```powershell
& $git -C $repo add <paths>
& $git -C $repo status
```

### Commit message — use a file (`-F`)

```powershell
& $git -C $repo commit -F "$repo\tmp\commit-msg.txt"
& $git -C $repo status
& $git -C $repo log -1 --oneline
```

### What fails on Windows PowerShell

| Approach | Result |
|----------|--------|
| `git commit -m "$(cat <<'EOF' ... EOF)"` | **Fails** — heredoc not valid in PowerShell |
| `bash -lc 'cd /e/web/repos/... && git commit'` | **Often fails** — `/e/...` path missing |
| Chaining with `&&` | **Fails** on PS 5.x — use `;` |
| `git commit` without author | **Fails** — set env vars below |

---

## Author identity (never `git config`)

```powershell
& $git -C $repo log -1 --format="%an <%ae>"

$env:GIT_AUTHOR_NAME = "dhoffman"
$env:GIT_AUTHOR_EMAIL = "dhoffman@users.noreply.github.com"
$env:GIT_COMMITTER_NAME = $env:GIT_AUTHOR_NAME
$env:GIT_COMMITTER_EMAIL = $env:GIT_AUTHOR_EMAIL
```

---

## Push and PR

Only when the user explicitly asks:

```powershell
& $git -C $repo push -u origin HEAD
```

---

## Example sync targets (one client — adjust per deployment)

| Repo path | Example mapped path (WVBPS dev) |
|-----------|----------------------------------|
| repo root | `A:\wvbps\www\html\dev\adminshell\` |
| `App_Code/AdminShell/`, `Redis*.vb` | `A:\wvbps\www\html\App_Code\` |
| `global-bridge/pilot-bridge.asp` | `A:\GLOBAL_6-next\admin\pilot-bridge.asp` |

Recycle the app pool after `App_Code` changes.

---

## Checklist

```
- [ ] Changes synced A:\ → E:\web\repos\admin-new
- [ ] $git = C:\Program Files\Git\bin\git.exe
- [ ] status / diff / log from E: clone
- [ ] No secrets in staged files
- [ ] commit -F tmp/commit-msg.txt
- [ ] Author env vars if needed
- [ ] Push only if user asked
```

---

## Do not

- Run `git config` for user.name / user.email
- Commit from `A:\` unless `.git` is verified there
- Push unless requested
- Use bash heredocs or `&&` in PowerShell for commits
