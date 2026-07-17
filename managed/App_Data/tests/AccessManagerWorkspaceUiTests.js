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
const indexHtml = fs.readFileSync(path.join(managedRoot, "access-manager", "index.html"), "utf8");
const appJs = fs.readFileSync(path.join(managedRoot, "access-manager", "js", "app.js"), "utf8");
const shellJs = fs.readFileSync(path.join(managedRoot, "shared", "shell.js"), "utf8");
const sectionsJs = fs.readFileSync(
    path.join(managedRoot, "access-manager", "js", "sections-view.js"),
    "utf8"
);

assertTrue(!indexHtml.includes("viewScripts"), "dedicated Scripts panel is removed");
assertTrue(!indexHtml.includes("viewAccess"), "dedicated Access panel is removed");
assertTrue(!appJs.includes("AccessManagerScriptsView"), "workspace no longer bootstraps Scripts view");
assertTrue(!appJs.includes("AccessManagerAccessView"), "workspace no longer bootstraps Access view");
assertTrue(indexHtml.includes("id=\"adminMenu\""), "workspace includes the legacy-style section menu");
assertTrue(
    appJs.includes("renderSectionMenu") && appJs.includes("session.menuSections"),
    "workspace renders access-filtered menu sections from the session"
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
