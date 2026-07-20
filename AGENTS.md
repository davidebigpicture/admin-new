# Agent guide — admin-new

**Product:** managed admin shell (client-neutral). **Repo:** `admin-new` on GitHub.

Read **[`docs/admin-shell-platform.md`](docs/admin-shell-platform.md)** first — legacy global admin vs the pilot, auth bridge, relocatable config. WVBPS (or any mapped client path) is only where we happen to be testing today.

| Doc | Use when |
|-----|----------|
| [`docs/agent-handoff.md`](docs/agent-handoff.md) | Boundaries, verification, troubleshooting, **Code Admin** status |
| [`docs/vue-managed-screens.md`](docs/vue-managed-screens.md) | Building or changing zero-build Vue managed screens; Code Admin reference |
| [`docs/source-first-deployment-workflow.md`](docs/source-first-deployment-workflow.md) | Edit, validate, deploy, hash-check, browser-test, roll back, commit, and push workflow |
| [`docs/aspnet-web-site-vb48-workflow.md`](docs/aspnet-web-site-vb48-workflow.md) | Repo-agnostic staff guide for VB.NET 4.8 Web Sites, App_Code, code-behind, local Git, and workstation compilation |
| [`.cursor/rules/browser-e2e.mdc`](.cursor/rules/browser-e2e.mdc) | Browser MCP protocol for pilot UI verification |
| [`docs/github-repo.md`](docs/github-repo.md) | Git clone, sync, secrets |
| [`docs/legacy-credential-encoder.md`](docs/legacy-credential-encoder.md) | Legacy cookie bridge |
| [`.cursor/skills/commit-mapped-drive/SKILL.md`](.cursor/skills/commit-mapped-drive/SKILL.md) | Commits from mapped-drive workspaces |

**Hard rules:** `GLOBAL_6-next/admin` is read-only. Commit from `E:\web\repos\admin-new`. Never commit `managed/web.config.local`.
