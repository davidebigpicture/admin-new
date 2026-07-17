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
const pilotShell = fs.readFileSync(
    path.join(managedRoot, "..", "App_Code", "AdminShell", "PilotShell.vb"),
    "utf8"
);
const sessionJs = fs.readFileSync(path.join(managedRoot, "shared", "session.js"), "utf8");
const apiClientJs = fs.readFileSync(path.join(managedRoot, "shared", "api-client.js"), "utf8");
const sessionHandler = fs.readFileSync(path.join(managedRoot, "api", "session.ashx"), "utf8");

assertTrue(pilotShell.includes("admin-layout"), "classic chrome uses the unified admin layout");
assertTrue(pilotShell.includes("adminMenu"), "classic chrome reserves the section menu container");
assertTrue(pilotShell.includes("pilotToolNav"), "classic chrome reserves pilot tool navigation");
assertTrue(pilotShell.includes("managed/shared/shell.css"), "classic chrome loads the shared shell stylesheet");
assertTrue(pilotShell.includes("PilotSession.load()"), "classic chrome bootstraps the shared session API");
assertTrue(pilotShell.includes("renderSectionMenu"), "classic chrome hydrates the section menu");
assertTrue(!pilotShell.includes("id=\"col-left\""), "classic chrome removes the legacy stub left column");
assertTrue(sessionHandler.includes("PilotSessionHandler"), "pilot-wide session handler is registered");
assertTrue(apiClientJs.includes("setApiBase"), "api client supports a managed base path");
assertTrue(sessionJs.includes("configure"), "session loader supports endpoint configuration");

if (failures > 0) {
    process.exit(1);
}

console.log("All PilotShell UI tests passed.");
