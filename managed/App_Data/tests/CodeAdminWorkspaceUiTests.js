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
const indexAspx = fs.readFileSync(path.join(managedRoot, "code-admin", "index.aspx"), "utf8");
const appJs = fs.readFileSync(path.join(managedRoot, "code-admin", "js", "app.js"), "utf8");
const viewModelJs = fs.readFileSync(path.join(managedRoot, "code-admin", "js", "view-model.js"), "utf8");

assertTrue(indexAspx.includes('Inherits="CodeAdminPage"'), "code admin uses server-side page auth");
assertTrue(indexAspx.includes("view-model.js"), "code admin loads shared view model");
assertTrue(appJs.includes("CodeAdminViewModel"), "app delegates class selection rules to view model");
assertTrue(appJs.includes("disabled"), "add button is disabled without a selected class");
assertTrue(appJs.includes("hasSelectedClass"), "app checks for selected class before mutations");
assertTrue(viewModelJs.includes("canOpenAddEditor"), "view model exposes add guard");
assertTrue(appJs.includes('data-action="add"'), "workspace still exposes add action");

if (failures > 0) {
    process.exit(1);
}

console.log("All Code Admin workspace UI tests passed.");
