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
assertEqual(
    vm.getSelectedClass(workspace, "WINDOW_SHADE_CD").codeClassDesc,
    "Window Shades",
    "selected class metadata exposes the friendly name"
);
assertEqual(vm.getSelectedClass(workspace, "UNKNOWN"), null, "unknown selected class has no metadata");
assertEqual(vm.emptyPage(100).pageSize, 200, "empty page always uses the fixed page size");

const metadataWorkspace = {
    fieldMetadata: {
        WINDOW_SHADE_CD: {
            fields: [
                { key: "minorCode", label: "Default State", controlType: "radio", order: 25 },
                { key: "codeValueDesc", label: "Description", controlType: "text", required: true, order: 10 },
                { key: "formDisplay", label: "Form Display", controlType: "text", order: 30 }
            ]
        }
    }
};

assertEqual(vm.getDetailFields(metadataWorkspace, "WINDOW_SHADE_CD")[0].key, "codeValueDesc", "detail metadata is ordered for rendering");
assertTrue(vm.getPayloadFieldKeys(metadataWorkspace, "WINDOW_SHADE_CD").includes("minorCode"), "payload metadata preserves minor code when hidden");
assertEqual(vm.normalizeEditorValue(["ONE", "TWO"], "multiselect"), "ONE, TWO", "multiselect values normalize for persistence");
assertEqual(vm.normalizeEditorValue("ONE, TWO", "multiselect"), "ONE, TWO", "stored multiselect values round-trip unchanged");

console.log("All CodeAdminViewModel tests passed.");
