# Agent guide — admin-new

**Product:** managed admin shell (client-neutral). **Repo:** `admin-new` on GitHub.

Read **[`docs/admin-shell-platform.md`](docs/admin-shell-platform.md)** first — legacy global admin vs the pilot, auth bridge, relocatable config. WVBPS (or any mapped client path) is only where we happen to be testing today.

| Doc | Use when |
|-----|----------|
| [`docs/agent-handoff.md`](docs/agent-handoff.md) | Boundaries, verification, troubleshooting, **Code Admin** status |
| [`.cursor/rules/browser-e2e.mdc`](.cursor/rules/browser-e2e.mdc) | Browser MCP protocol for pilot UI verification |
| [`docs/github-repo.md`](docs/github-repo.md) | Git clone, sync, secrets |
| [`docs/legacy-credential-encoder.md`](docs/legacy-credential-encoder.md) | Legacy cookie bridge |
| [`.cursor/skills/commit-mapped-drive/SKILL.md`](.cursor/skills/commit-mapped-drive/SKILL.md) | Commits from mapped-drive workspaces |

**Hard rules:** `GLOBAL_6-next/admin` is read-only. Commit from `E:\web\repos\admin-new`. Never commit `managed/web.config.local`.
