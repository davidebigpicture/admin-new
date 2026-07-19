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

    assertTrue(InlineEdit.props.editorId && InlineEdit.template.includes(':id="editorId || null"') && InlineEdit.template.includes(':aria-label="label"') && InlineEdit.template.includes('admin-inline-edit__display-label') && InlineEdit.template.includes('admin-inline-edit__display-chevron') && InlineEdit.template.includes('aria-hidden="true"'), "editor IDs apply to edit controls while display buttons retain their accessible labels and a decorative chevron");
    const selectSize = InlineEdit.computed.selectSize;
    assertTrue(selectSize.call({ options: [] }) === 2 && selectSize.call({ options: [{ value: "one" }] }) === 2 && selectSize.call({ options: [{}, {}, {}] }) === 3 && selectSize.call({ options: [{}, {}, {}, {}, {}, {}, {}, {}, {}, {}] }) === 8, "select editor size is bounded between two and eight rows");
    assertTrue(InlineEdit.template.includes('class="admin-inline-edit__input admin-inline-edit__select"') && InlineEdit.template.includes(':size="selectSize"'), "select editor has a distinct inline listbox class and bounded size binding");
    assertTrue(InlineEdit.props.searchable && InlineEdit.props.commitOnChange && InlineEdit.template.includes('ref="filter"') && InlineEdit.template.includes('filteredOptions') && InlineEdit.template.includes('No matching options.'), "searchable selects provide a focused filter, bounded matching listbox, and empty state");
    assertTrue(InlineEdit.methods.onSelectChange && InlineEdit.methods.onCompositeFocusout && InlineEdit.template.includes('@change="onSelectChange"') && InlineEdit.template.includes('@focusout="onCompositeFocusout"'), "searchable selects support immediate selection commits without internal blur commits");
    assertTrue(/<select v-else-if="editorType === 'select'"[\s\S]*?@change="onSelectChange"[\s\S]*?@keydown="onKeydown"[\s\S]*?@blur="onBlur">/.test(InlineEdit.template), "non-searchable selects wire selection changes through the shared immediate-commit handler");
    const filteredOptions = InlineEdit.computed.filteredOptions;
    const options = [{ value: "ORG_SUB_TY_CD", label: "Organization Subtype" }, { value: "GROUP_TY_CD", label: "Group Types" }];
    assertTrue(filteredOptions.call({ filterText: "sub", options: options }).length === 1 && filteredOptions.call({ filterText: "group_ty", options: options }).length === 1 && filteredOptions.call({ filterText: "missing", options: options }).length === 0, "searchable select filters options case-insensitively by label and value");
    let immediateCommits = 0;
    InlineEdit.methods.onSelectChange.call({ commitOnChange: true, commit: function () { immediateCommits += 1; } });
    InlineEdit.methods.onSelectChange.call({ commitOnChange: false, commit: function () { immediateCommits += 1; } });
    assertTrue(immediateCommits === 1, "commit-on-change selects save exactly once per selection");
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

    let textareaCommits = 0;
    const textareaEvent = { key: "Enter", preventDefault: function () { textareaCommits += 10; } };
    InlineEdit.methods.onKeydown.call({ controller: { editing: true }, editorType: "textarea", commit: function () { textareaCommits += 1; }, cancelEdit: function () {} }, textareaEvent);
    assertTrue(textareaCommits === 0, "textarea Enter remains available for a newline without committing or interception");
    let textareaCancelled = 0;
    const textareaEscapeEvent = { key: "Escape", preventDefault: function () { textareaCancelled += 1; } };
    InlineEdit.methods.onKeydown.call({ controller: { editing: true }, editorType: "textarea", commit: function () {}, cancelEdit: function () { textareaCancelled += 1; } }, textareaEscapeEvent);
    assertTrue(textareaCancelled === 2, "textarea Escape retains the shared cancellation behavior");

    const priorVue = global.Vue;
    let reactiveCalls = 0;
    global.Vue = {
        reactive: function (controller) {
            reactiveCalls += 1;
            return controller;
        }
    };
    const beginSequence = [];
    const componentInstance = {
        value: "one",
        onSave: function () { return Promise.resolve(); },
        onError: function () {},
        $emit: function () {},
        $refs: { editor: { focus: function () { beginSequence.push("focus"); } } },
        $forceUpdate: function () { beginSequence.push("forceUpdate"); },
        $nextTick: function (callback) {
            beginSequence.push("nextTick");
            callback.call(this);
        }
    };
    componentInstance.reportError = InlineEdit.methods.reportError.bind(componentInstance);
    componentInstance.startEscapeCapture = InlineEdit.methods.startEscapeCapture.bind(componentInstance);
    componentInstance.stopEscapeCapture = InlineEdit.methods.stopEscapeCapture.bind(componentInstance);
    componentInstance.cancelEdit = InlineEdit.methods.cancelEdit.bind(componentInstance);
    InlineEdit.mounted.call(componentInstance);
    InlineEdit.methods.begin.call(componentInstance);
    assertTrue(reactiveCalls === 1 && componentInstance.controller.editing && beginSequence.join(",") === "forceUpdate,nextTick,focus", "successful begin forces a render before next-tick editor focus");

    componentInstance.disabled = true;
    componentInstance.controller.editing = false;
    beginSequence.length = 0;
    InlineEdit.methods.begin.call(componentInstance);
    assertTrue(beginSequence.length === 0 && !componentInstance.controller.editing, "disabled begin does not force a render or focus the editor");

    const listeners = [];
    const fakeDocument = {
        addEventListener: function (type, listener, capture) {
            listeners.push({ type: type, listener: listener, capture: capture });
        },
        removeEventListener: function (type, listener, capture) {
            const index = listeners.findIndex(function (entry) {
                return entry.type === type && entry.listener === listener && entry.capture === capture;
            });
            if (index >= 0) {
                listeners.splice(index, 1);
            }
        }
    };
    const priorDocument = global.document;
    global.document = fakeDocument;
    const escapeSequence = [];
    const escapeComponent = {
        value: "one",
        onSave: function () { return Promise.resolve(); },
        onError: function () {},
        $emit: function () {},
        $refs: { editor: { focus: function () {} } },
        $forceUpdate: function () { escapeSequence.push("forceUpdate"); },
        $nextTick: function (callback) { callback.call(this); }
    };
    escapeComponent.reportError = InlineEdit.methods.reportError.bind(escapeComponent);
    escapeComponent.startEscapeCapture = InlineEdit.methods.startEscapeCapture.bind(escapeComponent);
    escapeComponent.stopEscapeCapture = InlineEdit.methods.stopEscapeCapture.bind(escapeComponent);
    escapeComponent.cancelEdit = InlineEdit.methods.cancelEdit.bind(escapeComponent);
    InlineEdit.mounted.call(escapeComponent);
    InlineEdit.methods.begin.call(escapeComponent);
    assertTrue(listeners.length === 1 && listeners[0].type === "keydown" && listeners[0].capture === true, "begin registers one document capture Escape listener");

    const escapeEvent = {
        key: "Escape",
        preventDefault: function () { escapeSequence.push("preventDefault"); },
        stopPropagation: function () { escapeSequence.push("stopPropagation"); }
    };
    listeners[0].listener(escapeEvent);
    assertTrue(!escapeComponent.controller.editing && listeners.length === 0 && escapeSequence.includes("preventDefault") && escapeSequence.includes("stopPropagation"), "first captured Escape cancels editing and removes its listener");

    const inactiveEvent = {
        key: "Escape",
        preventDefault: function () { escapeSequence.push("inactivePreventDefault"); },
        stopPropagation: function () { escapeSequence.push("inactiveStopPropagation"); }
    };
    InlineEdit.methods.onKeydown.call(escapeComponent, inactiveEvent);
    assertTrue(!escapeSequence.includes("inactivePreventDefault") && !escapeSequence.includes("inactiveStopPropagation"), "inactive inline edit does not intercept Escape");

    InlineEdit.methods.begin.call(escapeComponent);
    const secondEscapeComponent = {
        value: "one",
        onSave: function () { return Promise.resolve(); },
        onError: function () {},
        $emit: function () {},
        $refs: { editor: { focus: function () {} } },
        $forceUpdate: function () {},
        $nextTick: function (callback) { callback.call(this); }
    };
    secondEscapeComponent.reportError = InlineEdit.methods.reportError.bind(secondEscapeComponent);
    secondEscapeComponent.startEscapeCapture = InlineEdit.methods.startEscapeCapture.bind(secondEscapeComponent);
    secondEscapeComponent.stopEscapeCapture = InlineEdit.methods.stopEscapeCapture.bind(secondEscapeComponent);
    secondEscapeComponent.cancelEdit = InlineEdit.methods.cancelEdit.bind(secondEscapeComponent);
    InlineEdit.mounted.call(secondEscapeComponent);
    InlineEdit.methods.begin.call(secondEscapeComponent);
    assertTrue(!escapeComponent.controller.editing && secondEscapeComponent.controller.editing && listeners.length === 1, "a second inline edit replaces the first active capture listener");
    InlineEdit.methods.cancelEdit.call(secondEscapeComponent);

    InlineEdit.methods.begin.call(escapeComponent);
    escapeComponent.controller.value = "two";
    await InlineEdit.methods.commit.call(escapeComponent);
    assertTrue(!escapeComponent.controller.editing && listeners.length === 0, "commit removes the active document capture listener");

    InlineEdit.methods.begin.call(escapeComponent);
    InlineEdit.beforeUnmount.call(escapeComponent);
    assertTrue(listeners.length === 0, "unmount removes the active document capture listener");
    global.document = priorDocument;
    global.Vue = priorVue;

    if (failures > 0) {
        process.exitCode = 1;
        return;
    }
    console.log("All CodeAdminInlineEdit tests passed.");
}

run();