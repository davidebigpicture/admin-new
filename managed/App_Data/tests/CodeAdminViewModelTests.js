"use strict";

const vm = require("../../code-admin/js/view-model.js").CodeAdminViewModel;

function assertTrue(condition, message) {
    if (!condition) {
        throw new Error("FAIL: " + message);
    }
    console.log("PASS: " + message);
}

function assertEqual(actual, expected, message) {
    if (actual !== expected) {
        throw new Error("FAIL: " + message + " (expected " + expected + ", got " + actual + ")");
    }
    console.log("PASS: " + message);
}

const workspace = {
    showClassCodes: false,
    classes: [
        { codeClass: "GROUP_TY_CD", codeClassDesc: "Group Types" },
        { codeClass: "WINDOW_SHADE_CD", codeClassDesc: "Window Shades" }
    ]
};

assertEqual(vm.hasSelectedClass(""), false, "blank class is not selected");
assertEqual(vm.hasSelectedClass("GROUP_TY_CD"), true, "non-blank class is selected");
assertEqual(vm.canOpenAddEditor(""), false, "add editor requires a class");
assertEqual(vm.canOpenAddEditor("GROUP_TY_CD"), true, "add editor opens with a class");
assertTrue(vm.buildClassOptions(workspace, "").indexOf("Select a class...") >= 0, "class dropdown includes placeholder");
assertTrue(
    vm.buildClassOptions(workspace, "GROUP_TY_CD").indexOf('value="GROUP_TY_CD" selected') >= 0,
    "class dropdown selects the current class"
);
assertEqual(vm.emptyPage(100).pageSize, 100, "empty page preserves page size");

console.log("All CodeAdminViewModel tests passed.");
