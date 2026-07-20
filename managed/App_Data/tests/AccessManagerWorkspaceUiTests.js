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
const shellJs = fs.readFileSync(path.join(managedRoot, "shared", "shell.js"), "utf8");
const sectionsJs = fs.readFileSync(
    path.join(managedRoot, "access-manager", "js", "sections-view.js"),
    "utf8"
);

assertTrue(indexAspx.includes('Inherits="AccessManagerPage"') && indexAspx.includes('MasterPageFile="../shared/ManagedShell.master"'), "Access Manager retains server-side authorization through the managed master host");
assertTrue(!indexAspx.includes("viewScripts"), "dedicated Scripts panel is removed");
assertTrue(!indexAspx.includes("viewAccess"), "dedicated Access panel is removed");
assertTrue(indexAspx.indexOf("js/state.js") < indexAspx.indexOf("js/reorder.js") && indexAspx.indexOf("js/reorder.js") < indexAspx.indexOf("js/sections-view.js") && indexAspx.indexOf("js/sections-view.js") < indexAspx.indexOf("js/app.js") && indexAspx.includes('js/app.js?v=0719s'), "Access Manager runtime dependencies load before the refreshed app bootstrap");
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
assertTrue(
    sectionsJs.includes("form.addEventListener(\"keydown\"") &&
        sectionsJs.includes("event.key === \"Escape\""),
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

if (failures > 0) {
    process.exit(1);
}

console.log("All Access Manager workspace UI tests passed.");
