"use strict";

const fs = require("fs");
const path = require("path");

let failures = 0;

function assertTrue(condition, message) {
    if (condition) {
        console.log("PASS: " + message);
        return;
    }
    failures += 1;
    console.error("FAIL: " + message);
}

function read(filePath) {
    return fs.readFileSync(filePath, "utf8");
}

const managedRoot = path.resolve(__dirname, "..", "..");
const shellCss = read(path.join(managedRoot, "shared", "shell.css"));
const dialogsJs = read(path.join(managedRoot, "shared", "dialogs.js"));
const accessSectionsJs = read(path.join(managedRoot, "access-manager", "js", "sections-view.js"));
const codeWorkspaceJs = read(path.join(managedRoot, "code-admin", "js", "components", "workspace.js"));
const codeEditorJs = read(path.join(managedRoot, "code-admin", "js", "components", "editor.js"));
const codeAppJs = read(path.join(managedRoot, "code-admin", "js", "app.js"));
const accessIndex = read(path.join(managedRoot, "access-manager", "index.aspx"));
const codeIndex = read(path.join(managedRoot, "code-admin", "index.aspx"));
const managedShellMaster = read(path.join(managedRoot, "..", "App_Code", "AdminShell", "ManagedShellMaster.vb"));
const legacyShell = read(path.join(managedRoot, "..", "App_Code", "AdminShell", "PilotShell.vb"));

assertTrue(
    [".admin-actions", ".admin-actions--end", ".admin-action", ".admin-action--sm", ".admin-action--primary", ".admin-action--secondary", ".admin-action--danger", ".admin-action--activate", ".admin-action--deactivate", ".admin-action--quiet", ".admin-action--icon"].every(function (selector) { return shellCss.includes(selector); }) &&
        shellCss.includes('.admin-action[aria-busy="true"]') &&
        shellCss.includes(".admin-status--active") &&
        shellCss.includes(".admin-status--inactive") &&
        shellCss.includes(".admin-status--archived"),
    "shared semantic action and status variants are defined"
);
assertTrue(
    shellCss.includes("--admin-status-active-text") &&
        shellCss.includes("--admin-status-inactive-text") &&
        shellCss.includes("--admin-status-archived-text") &&
        shellCss.includes(".admin-inline-edit--status-active") &&
        shellCss.includes("var(--admin-status-active-text)") &&
        shellCss.includes("var(--admin-status-inactive-text)") &&
        shellCss.includes("var(--admin-status-archived-text)"),
    "badges and editable statuses share one palette"
);
assertTrue(
    dialogsJs.includes("global.AdminShellDialogs") &&
        dialogsJs.includes("admin-shell-dialog-title") &&
        dialogsJs.includes("admin-shell-dialog-input") &&
        dialogsJs.includes("dialog-actions admin-actions admin-actions--end") &&
        !dialogsJs.includes("PilotDialogs") &&
        !dialogsJs.includes("pilot-dialog-"),
    "shared dialogs use the neutral API, IDs, and semantic actions"
);
assertTrue(
    !/PilotDialogs|pilot-dialog-/.test(read(path.join(managedRoot, "shared", "dialogs.js")) + accessSectionsJs + codeAppJs),
    "managed dialog consumers contain no retired dialog API or ID"
);
assertTrue(
    !/status-pill|icon-button|className = "(primary|danger)"|class=\\"(primary|danger)/.test(accessSectionsJs) &&
        codeWorkspaceJs.includes("admin-action") &&
        codeEditorJs.includes("admin-action") &&
        !/btn btn-(primary|danger|default)/.test(codeWorkspaceJs + codeEditorJs),
    "active Access and Code commands use semantic action and status classes"
);
assertTrue(
    accessIndex.includes("access-manager.css?v=0719aj") &&
        accessIndex.includes("reorder.js?v=0719ak") &&
        accessIndex.includes("sections-view.js?v=0719route1") &&
        codeIndex.includes("code-admin.css?v=0719ads1") &&
        codeIndex.includes("components/editor.js?v=0719ads1") &&
        codeIndex.includes("components/workspace.js?v=0719ads1") &&
        codeIndex.includes("js/app.js?v=0719ads1") &&
        managedShellMaster.includes('ShellCssVersion As String = "0719ads1"') &&
        managedShellMaster.includes('DialogsAssetVersion As String = "0719ads1"') &&
        managedShellMaster.includes('"managed/shared/dialogs.js") & "?v=" & DialogsAssetVersion') &&
        legacyShell.includes('ShellAssetVersion As String = "0719ads1"'),
    "changed action assets use the approved cache version"
);

if (failures > 0) {
    process.exit(1);
}

console.log("All shared design system tests passed.");