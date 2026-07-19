"use strict";

const InlineEditController = require("../../shared/inline-edit.js").InlineEditController;
const InlineEdit = require("../../shared/inline-edit.js").InlineEdit;

let failures = 0;

function assertTrue(condition, message) {
    if (condition) {
        console.log("PASS: " + message);
        return;
    }
    failures += 1;
    console.error("FAIL: " + message);
}

async function run() {
    let saves = 0;
    const blurController = new InlineEditController({
        value: "one",
        onSave: function () { saves += 1; }
    });
    blurController.begin();
    blurController.value = "two";
    await blurController.handleBlur();
    assertTrue(saves === 1 && blurController.originalValue === "two" && !blurController.editing, "blur commits the changed value");

    const enterController = new InlineEditController({
        value: "one",
        onSave: function () { saves += 1; }
    });
    enterController.begin();
    enterController.value = "two";
    await Promise.all([enterController.commit(), enterController.handleBlur()]);
    assertTrue(saves === 2, "Enter commit and following blur do not duplicate saves");

    const escapeController = new InlineEditController({
        value: "one",
        onSave: function () { saves += 1; }
    });
    escapeController.begin();
    escapeController.value = "two";
    escapeController.cancel();
    await escapeController.handleBlur();
    assertTrue(saves === 2 && escapeController.originalValue === "one", "Escape restores and suppresses the following blur commit");

    const unchangedController = new InlineEditController({
        value: "one",
        onSave: function () { saves += 1; }
    });
    unchangedController.begin();
    await unchangedController.commit();
    assertTrue(saves === 2 && !unchangedController.editing, "unchanged values exit without saving");

    let reportedError = null;
    const failingController = new InlineEditController({
        value: "one",
        onSave: function () { return Promise.reject(new Error("save failed")); },
        onError: function (error) { reportedError = error; }
    });
    failingController.begin();
    failingController.value = "two";
    try {
        await failingController.commit();
    } catch (error) {
        // The component reports rejected saves; the controller intentionally retains the error for callers.
    }
    assertTrue(failingController.value === "one" && failingController.originalValue === "one" && reportedError && reportedError.message === "save failed", "async rejection restores the original value and reports the error");

    assertTrue(InlineEdit.props.editorId && typeof InlineEdit.setup === "function" && !InlineEdit.data && !InlineEdit.computed && !InlineEdit.methods && !InlineEdit.mounted && !InlineEdit.beforeUnmount, "InlineEdit uses Composition API state and lifecycle cleanup instead of Options API sections");
    assertTrue(InlineEdit.template.includes(':id="editorId || null"') && InlineEdit.template.includes(':aria-label="label"') && InlineEdit.template.includes('admin-inline-edit__display-label') && InlineEdit.template.includes('admin-inline-edit__display-chevron') && InlineEdit.template.includes('aria-hidden="true"'), "editor IDs apply to edit controls while display buttons retain their accessible labels and a decorative chevron");
    assertTrue(InlineEdit.template.includes('class="admin-inline-edit__input admin-inline-edit__select"') && InlineEdit.template.includes(':size="selectSize"') && InlineEdit.template.includes('ref="filter"') && InlineEdit.template.includes('filteredOptions') && InlineEdit.template.includes('No matching options.'), "select editors retain bounded searchable listbox behavior");
    assertTrue(/<select v-else-if="editorType === 'select'"[\s\S]*?@change="onSelectChange"[\s\S]*?@keydown="onKeydown"[\s\S]*?@blur="onBlur">/.test(InlineEdit.template), "non-searchable selects retain shared immediate commit and blur handling");
    let selectSaves = 0;
    const selectController = new InlineEditController({
        value: "N",
        onSave: function () { selectSaves += 1; }
    });
    selectController.begin();
    selectController.value = "Y";
    await selectController.commit();
    await selectController.handleBlur();
    assertTrue(selectSaves === 1 && selectController.originalValue === "Y" && !selectController.editing, "a non-searchable selection commit suppresses its following blur duplicate");
    assertTrue(InlineEdit.template.includes("editorType === 'textarea'") && InlineEdit.template.includes("<textarea") && InlineEdit.template.includes("admin-inline-edit__input--textarea") && InlineEdit.template.includes('ref="editor"') && InlineEdit.template.includes('@keydown="onKeydown"') && InlineEdit.template.includes('@blur="onBlur"'), "textarea editors retain the shared input binding, focus reference, and blur/key handling");

    assertTrue(InlineEdit.template.includes("editorType === 'textarea'") && InlineEdit.template.includes("<textarea") && InlineEdit.template.includes("admin-inline-edit__input--textarea") && InlineEdit.template.includes('ref="editor"') && InlineEdit.template.includes('@keydown="onKeydown"') && InlineEdit.template.includes('@blur="onBlur"'), "textarea editors retain the shared input binding, focus reference, and blur/key handling");

    if (failures > 0) {
        process.exitCode = 1;
        return;
    }
    console.log("All CodeAdminInlineEdit tests passed.");
}

run();