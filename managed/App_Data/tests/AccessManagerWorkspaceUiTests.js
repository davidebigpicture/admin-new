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

const managedRoot = path.resolve(__dirname, "..", "..");
const indexAspx = fs.readFileSync(path.join(managedRoot, "access-manager", "index.aspx"), "utf8");
const appJs = fs.readFileSync(path.join(managedRoot, "access-manager", "js", "app.js"), "utf8");
const accessCss = fs.readFileSync(path.join(managedRoot, "access-manager", "access-manager.css"), "utf8");
const shellCss = fs.readFileSync(path.join(managedRoot, "shared", "shell.css"), "utf8");
const shellJs = fs.readFileSync(path.join(managedRoot, "shared", "shell.js"), "utf8");
const dialogsJs = fs.readFileSync(path.join(managedRoot, "shared", "dialogs.js"), "utf8");
const reorderJs = fs.readFileSync(path.join(managedRoot, "access-manager", "js", "reorder.js"), "utf8");
const sectionsJs = fs.readFileSync(
    path.join(managedRoot, "access-manager", "js", "sections-view.js"),
    "utf8"
);
const accessManagerHandlers = fs.readFileSync(
    path.join(managedRoot, "..", "App_Code", "AdminShell", "AccessManager", "AccessManagerApiHandlers.vb"),
    "utf8"
);
const accessManagerService = fs.readFileSync(
    path.join(managedRoot, "..", "App_Code", "AdminShell", "AccessManager", "AccessManagerService.vb"),
    "utf8"
);

assertTrue(indexAspx.includes('Inherits="AccessManagerPage"') && indexAspx.includes('MasterPageFile="../shared/ManagedShell.master"'), "Access Manager retains server-side authorization through the managed master host");
assertTrue(!indexAspx.includes("viewScripts"), "dedicated Scripts panel is removed");
assertTrue(!indexAspx.includes("viewAccess"), "dedicated Access panel is removed");
assertTrue(indexAspx.indexOf("js/state.js") < indexAspx.indexOf("js/reorder.js") && indexAspx.indexOf("js/reorder.js") < indexAspx.indexOf("js/sections-view.js") && indexAspx.indexOf("js/sections-view.js") < indexAspx.indexOf("js/app.js") && indexAspx.includes('access-manager.css?v=0719route3') && indexAspx.includes('reorder.js?v=0719ak') && indexAspx.includes('sections-view.js?v=0719route6'), "Access Manager runtime dependencies load before the refreshed app bootstrap and use the current asset versions");
assertTrue(!appJs.includes("AccessManagerScriptsView"), "workspace no longer bootstraps Scripts view");
assertTrue(!appJs.includes("AccessManagerAccessView"), "workspace no longer bootstraps Access view");
assertTrue(indexAspx.includes("id=\"appMessage\"") && indexAspx.includes("id=\"viewSections\""), "workspace retains its content regions");
assertTrue(
    appJs.includes("await window.ManagedShell.initialize") &&
        appJs.indexOf("ManagedShell.initialize") < appJs.indexOf("api/workspace.ashx"),
    "workspace awaits access-filtered shell hydration before loading data"
);
assertTrue(
    !indexAspx.includes("<!DOCTYPE") && !indexAspx.includes("<html") && !indexAspx.includes("<head") && !indexAspx.includes("<body") &&
        !indexAspx.includes("shell.js") && !indexAspx.includes("api-client.js") && !indexAspx.includes("adminMenu") && !indexAspx.includes("shell-header"),
    "Access Manager is a thin master-content page without duplicate document or shell chrome"
);
assertTrue(shellJs.includes("bpAdminMenuCollapsed"), "section menu remembers its collapsed state");
assertTrue(shellJs.includes("adminMenuFilter"), "section menu provides section and tool filtering");
assertTrue(!shellJs.includes("Admin sections"), "section menu uses an unlabeled edge affordance");
assertTrue(shellJs.includes("closeOtherSections"), "section menu behaves as a single-open accordion");
assertTrue(sectionsJs.includes("beginSectionNameEdit"), "section names support inline editing");
assertTrue(!sectionsJs.includes("Update #") && sectionsJs.includes('admin-status admin-status--active') && sectionsJs.includes('admin-status admin-status--inactive'), "section detail metadata shows only the meaningful lifecycle status");
assertTrue(
    /function beginSectionNameEdit[\s\S]*?form\.addEventListener\("keydown", function \(event\) \{[\s\S]*?event\.key === "Escape"[\s\S]*?cancelEdit\(\);/.test(sectionsJs),
    "Escape cancels inline section-name editing"
);
assertTrue(sectionsJs.includes("beginScriptEdit"), "section scripts support inline editing");
assertTrue(sectionsJs.includes("openAccessExplorer"), "principal access lookup is available in workspace");
assertTrue(
    sectionsJs.includes("api/grants.ashx?principal=true"),
    "access explorer loads grants by selected principal"
);
assertTrue(
    !sectionsJs.includes("grantPrincipalId"),
    "grant creation does not expose a principal ID field"
);
assertTrue(
    !sectionsJs.includes("addScriptId"),
    "section script selection does not expose a script ID field"
);
assertTrue(
    sectionsJs.includes('action=checkRoute&scriptName=') &&
        sectionsJs.includes('pathInput.addEventListener("blur", checkRoute)') &&
        sectionsJs.includes('submitButton.disabled = !titleInput.value.trim() || !pathInput.value.trim() || !typeSelect.value || !!pathError;') &&
        sectionsJs.includes('Route is reachable.') &&
        !sectionsJs.includes('Check the script path before adding it.') &&
        sectionsJs.includes('createArea.appendChild(routeAlert)') &&
        accessCss.includes('.script-route-alert {'),
    "new scripts require complete fields, a reachable path, and show route feedback above the control row"
);
assertTrue(
    accessManagerHandlers.includes('action = "checkroute"') &&
        accessManagerHandlers.includes('AccessManagerValidation.ValidateScriptName(scriptName)') &&
        accessManagerHandlers.includes('request.Method = "HEAD"') &&
        accessManagerHandlers.includes('New Dictionary(Of String, Object) From {{"reachable", IsRouteReachable(context, scriptName)}}'),
    "script path checks validate only absolute script paths and probe the route server-side"
);
assertTrue(
    sectionsJs.includes('"&includeInactive=" + (includeInactive ? "true" : "false")') &&
        accessManagerHandlers.includes('Dim workspace = service.GetWorkspace(includeInactive)') &&
        accessManagerService.includes('Public Function GetWorkspace(Optional includeInactive As Boolean = False)') &&
        accessManagerService.includes('.Sections = _repository.ListSections(0, includeInactive)'),
    "section detail reloads retain the inactive-section filter"
);
assertTrue(
    sectionsJs.includes('button.classList.toggle("is-inactive", !!section.Inactive);') &&
        accessCss.includes('.list-nav li > button:first-child.is-inactive,') &&
        accessCss.includes('.list-nav li > button:first-child.is-inactive[aria-current="true"]') &&
        accessCss.includes('font-weight: 400;'),
    "inactive sections use muted regular-weight text, including when selected"
);
assertTrue(
    shellCss.includes(".workspace-heading {") &&
        shellCss.includes(".icon-button,") &&
        shellCss.includes(".reorder-controls {") &&
        shellCss.includes(".table-wrap {") &&
        !accessCss.includes(".workspace-heading {") &&
        !accessCss.includes(".icon-button,") &&
        !accessCss.includes(".reorder-controls {") &&
        !accessCss.includes(".table-wrap {"),
    "generic workspace headings, icon actions, reorder controls, and framed tables are owned by the shared shell"
);
assertTrue(
    accessCss.includes(".picker-dialog {") &&
        accessCss.includes("width: min(1280px, 100%);") &&
        accessCss.includes("#viewSections > .split-layout > .panel:first-child") &&
        accessCss.includes(".script-create-form {") &&
        !accessCss.includes("#viewScripts") &&
        !accessCss.includes("#viewAccess"),
    "Access-specific picker and section layout rules remain local while retired view rules are removed"
);
assertTrue(
    accessCss.includes(".dialog.picker-dialog {") &&
        sectionsJs.includes("admin-action admin-action--primary admin-action--sm") &&
        sectionsJs.includes("fa fa-plus") &&
        sectionsJs.includes("</i> Add") &&
        sectionsJs.includes("</i> Grant"),
    "picker dialogs override shared dialog sizing and row creation actions use compact primary icon controls"
);
assertTrue(
    accessCss.includes("#viewSections table.data td.action-cell.admin-actions {") &&
        accessCss.includes("display: table-cell;") &&
        accessCss.includes("width: 12rem;") &&
        accessCss.includes("min-width: 12rem;") &&
        accessCss.includes("white-space: nowrap;"),
    "section table action cells preserve a horizontal table layout for all controls"
);
assertTrue(
    accessCss.includes("grid-template-columns: minmax(340px, 400px) minmax(0, 1fr);") &&
        /@media \(max-width: 860px\)[\s\S]*?#viewSections > \.split-layout\s*\{[\s\S]*?grid-template-columns: 1fr;/.test(accessCss),
    "Sections navigation receives a wider desktop column and remains single-column on narrow screens"
);
assertTrue(
    reorderJs.includes("drag-handle") &&
        reorderJs.includes('wrapper.className = "reorder-controls";') &&
        reorderJs.includes("ArrowUp") &&
        reorderJs.includes("ArrowDown") &&
        !reorderJs.includes("fa-chevron-up") &&
        !reorderJs.includes("fa-chevron-down") &&
        !reorderJs.includes("data-reorder-direction"),
    "reorder controls use drag handles without redundant visible move chevrons"
);
const modernSplitLayoutMatch = /\.split-layout\s*\{[^}]*grid-template-columns:\s*minmax\(260px, 320px\)\s+minmax\(0, 1fr\);/.exec(shellCss);
const modernSplitLayoutStart = modernSplitLayoutMatch ? modernSplitLayoutMatch.index : -1;
const finalResponsiveSplitLayoutStart = shellCss.lastIndexOf("@media (max-width: 860px)");
const finalResponsiveSplitLayoutCss = shellCss.slice(finalResponsiveSplitLayoutStart);
assertTrue(
    modernSplitLayoutStart >= 0 &&
        finalResponsiveSplitLayoutStart > modernSplitLayoutStart &&
        /\.split-layout\s*\{[^}]*grid-template-columns:\s*1fr;/.test(finalResponsiveSplitLayoutCss),
    "the final responsive split layout overrides the modern two-column workspace grid"
);
assertTrue(
    !fs.existsSync(path.join(managedRoot, "access-manager", "js", "scripts-view.js")) &&
        !fs.existsSync(path.join(managedRoot, "access-manager", "js", "access-view.js")) &&
        !indexAspx.includes("scripts-view.js") &&
        !indexAspx.includes("access-view.js"),
    "retired Scripts and Access view modules are absent from the workspace files and runtime page"
);
assertTrue(
    sectionsJs.includes("global.AdminShellDialogs") &&
        dialogsJs.includes("global.AdminShellDialogs") &&
        dialogsJs.includes("admin-shell-dialog-title") &&
        dialogsJs.includes("admin-action admin-action--secondary") &&
        !dialogsJs.includes("PilotDialogs") &&
        !dialogsJs.includes("pilot-dialog-"),
    "Access Manager uses the neutral shared dialog API and semantic dialog controls"
);
assertTrue(
    sectionsJs.includes("admin-action admin-action--primary") &&
        sectionsJs.includes("admin-action admin-action--secondary") &&
        sectionsJs.includes("admin-action admin-action--danger") &&
        sectionsJs.includes("admin-action admin-action--activate admin-action--icon") &&
        sectionsJs.includes("admin-action admin-action--deactivate admin-action--icon") &&
        sectionsJs.includes("admin-action admin-action--quiet admin-action--icon") &&
        sectionsJs.includes("admin-status admin-status--active") &&
        sectionsJs.includes("admin-status admin-status--inactive") &&
        sectionsJs.includes("setPending") &&
        !/status-pill|icon-button|className = "(primary|danger)"|class=\\"(primary|danger)/.test(sectionsJs),
    "Access actions, statuses, and pending add or grant requests use shared semantic controls"
);

if (failures > 0) {
    process.exit(1);
}

console.log("All Access Manager workspace UI tests passed.");
