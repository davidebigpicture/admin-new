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
const repoRoot = path.resolve(managedRoot, "..");
const repositoryVb = fs.readFileSync(path.join(repoRoot, "App_Code", "AdminShell", "CodeAdminRepository.vb"), "utf8");
const serviceVb = fs.readFileSync(path.join(repoRoot, "App_Code", "AdminShell", "CodeAdminService.vb"), "utf8");

assertTrue(indexAspx.includes('Inherits="CodeAdminPage"'), "code admin uses server-side page auth");
assertTrue(indexAspx.includes("view-model.js"), "code admin loads shared view model");
const bootstrapIndex = indexAspx.indexOf("bootstrap/3.4.1/css/bootstrap.min.css");
const legacyStylesIndex = indexAspx.indexOf("PilotConfig.StylesheetUrl");
const shellStylesIndex = indexAspx.indexOf("../shared/shell.css");
const codeAdminStylesIndex = indexAspx.indexOf("code-admin.css");
assertTrue(bootstrapIndex >= 0, "code admin loads Bootstrap 3.4.1 CSS");
assertTrue(legacyStylesIndex > bootstrapIndex, "code admin loads configured legacy styles after Bootstrap");
assertTrue(shellStylesIndex > legacyStylesIndex, "code admin loads shared shell styles after legacy styles");
assertTrue(codeAdminStylesIndex > shellStylesIndex, "code admin loads scoped overrides last");
assertTrue(appJs.includes("CodeAdminViewModel"), "app delegates class selection rules to view model");
assertTrue(appJs.includes("disabled"), "add button is disabled without a selected class");
assertTrue(appJs.includes("hasSelectedClass"), "app checks for selected class before mutations");
assertTrue(viewModelJs.includes("canOpenAddEditor"), "view model exposes add guard");
assertTrue(appJs.includes('data-action="add"'), "workspace still exposes add action");
assertTrue(appJs.includes("panel panel-default"), "workspace uses a Bootstrap panel");
assertTrue(appJs.includes("form-inline"), "filters use a Bootstrap inline form");
assertTrue(appJs.includes("form-control"), "filters and editor use Bootstrap form controls");
assertTrue(appJs.includes("code-admin-panel-heading") && appJs.includes('id="codeClassSelect"'), "class selector is rendered in the panel heading");
assertTrue(!appJs.includes("rowsInput"), "workspace has no configurable rows control");
assertTrue(!appJs.includes('data-action="search"'), "workspace has no search button");
assertTrue(!appJs.includes("&rows="), "value loads do not send a dynamic page size");
assertTrue(appJs.includes("let searchTimer") && appJs.includes("}, 300)"), "search input is debounced by 300ms");
const searchInputHandlerStart = appJs.indexOf('if (event.target.id !== "searchInput")');
const searchInputHandlerEnd = appJs.indexOf('root.addEventListener("keydown"', searchInputHandlerStart);
const searchInputHandler = appJs.slice(searchInputHandlerStart, searchInputHandlerEnd);
const capturedSearchIndex = searchInputHandler.indexOf("const search = event.target.value.trim();");
const searchTimeoutIndex = searchInputHandler.indexOf("searchTimer = window.setTimeout");
assertTrue(
    capturedSearchIndex >= 0 && capturedSearchIndex < searchTimeoutIndex &&
    searchInputHandler.includes("search: search,") &&
    !searchInputHandler.includes('document.getElementById("searchInput")'),
    "search debounce captures the input value before scheduling and never reads the replaced search input"
);
assertTrue(appJs.includes("let loadSequence") && appJs.includes("requestSequence === loadSequence"), "stale search responses cannot overwrite newer results");
assertTrue(appJs.includes("let editorRequestSequence = 0") && appJs.includes("function invalidateEditorRequests()"), "editor requests use a dedicated generation token");
assertTrue(
    appJs.includes("requestSequence !== editorRequestSequence || stateApi.get().selectedClass !== selectedClass"),
    "stale Add and Edit metadata responses cannot open an editor after a context change"
);
assertTrue(
    appJs.includes("currentEditor !== editor") &&
    appJs.includes("!currentForm.isConnected") &&
    appJs.includes("currentForm.codeValue.value.trim() !== codeValue") &&
    appJs.includes("captureEditorValues(currentForm, currentEditor)"),
    "metadata refresh ignores detached or changed create editors and preserves live form values"
);
assertTrue(
    appJs.includes("if (requestSequence === editorRequestSequence)") &&
    appJs.includes('else if (action === "cancel-editor")') &&
    appJs.includes("invalidateEditorRequests();"),
    "stale editor request errors are suppressed and cancel invalidates pending editor work"
);
assertTrue(appJs.includes('data-action="edit" data-id="') && appJs.includes('class="code-value"'), "clicking the code value opens detail editing");
assertTrue(!appJs.includes("contenteditable"), "descriptions are plain display text without inline editing");
assertTrue(!appJs.includes("data-patch-field") && !appJs.includes("patchField("), "workspace has no inline patch handler");
assertTrue(appJs.includes("getDetailFields") && appJs.includes("renderMetadataField"), "detail editor renders fields from workspace metadata");
assertTrue(appJs.includes("controlType === \"radio\"") && appJs.includes("controlType === \"select\"") && appJs.includes("controlType === \"multiselect\""), "detail editor supports radio, select, and multiselect metadata controls");
assertTrue(appJs.includes('const optionId = id + "-" + index') && appJs.includes('for="\' + optionId + \'"'), "radio choices use unique IDs with associated labels");
assertTrue(appJs.includes('<fieldset class="code-admin-radio-options"') && appJs.includes('<legend class="sr-only">'), "radio groups have an accessible group label");
assertTrue(appJs.includes('document.getElementById("detail-codeValueDesc")'), "edit focus targets the rendered metadata description field");
assertTrue(appJs.includes("editor.fieldMetadata"), "detail editor uses row-specific hydrated metadata when returned");
assertTrue(!appJs.includes("lookupSource"), "client receives concrete options rather than lookup source parameters");
assertTrue(appJs.includes("field.required ? \" required\""), "metadata-required fields render with HTML required validation");
assertTrue(appJs.includes("getPayloadFieldKeys") && appJs.includes("form.elements[fieldKey]"), "detail save serializes metadata fields using their payload keys");
assertTrue(!appJs.includes("optionIndex <= 17"), "editor rendering is not hard-coded to an optional-value loop");
assertTrue(appJs.includes("table-responsive"), "value table has responsive containment");
assertTrue(
    appJs.indexOf("if (state.editor)") < appJs.indexOf('id="codeClassSelect"', appJs.indexOf("function render(state)")),
    "editor rendering branches before list class selection rendering"
);
assertTrue(
    appJs.indexOf("if (state.editor)") < appJs.indexOf("code-admin-filters", appJs.indexOf("function render(state)")) &&
    appJs.indexOf("if (state.editor)") < appJs.indexOf("renderTable(page", appJs.indexOf("function render(state)")),
    "editor rendering branches before list filters and table rendering"
);
assertTrue(appJs.includes("Edit " + '" + escapeHtml(editor.codeValue)') && appJs.includes("Add code value"), "editor heading identifies edit and add modes");
assertTrue(appJs.includes('fa-arrow-left') && appJs.includes('> Back to list</button>') && appJs.includes('data-action="cancel-editor"'), "editor heading exposes Back to list through the cancel action");
assertTrue(
    appJs.slice(appJs.indexOf('action === "cancel-editor"'), appJs.indexOf('action === "delete"')).includes("stateApi.set({ editor: null });"),
    "cancel returns to the existing list state without loading or mutating values"
);
assertTrue(
    appJs.includes("table table-striped table-hover table-condensed"),
    "value grid uses compact Bootstrap table styling"
);
assertTrue(appJs.includes("getSelectedClass"), "workspace resolves friendly selected-class metadata");
assertTrue(appJs.includes("metadata=true&codeClass="), "Add loads hydrated metadata only for the selected class");
assertTrue(appJs.includes("refreshCreateMetadata") && appJs.includes("data-code-value"), "ORG_SUB_TY_CD add refreshes contextual metadata after its value is entered");
assertTrue(appJs.includes("captureEditorValues"), "metadata refresh preserves current editor values");
assertTrue(serviceVb.includes("GetDetailMetadata") && !serviceVb.includes("HydrateMetadata(CodeAdminFieldMetadataRegistry.Build(majorCode, classes(classIndex).CodeClass), Nothing)"), "workspace metadata remains static while detail metadata hydrates on demand");
assertTrue(repositoryVb.includes("code_value.code_class = 'ORG_SUB_TY_CD'") && repositoryVb.includes("membership_column_detail.column_desc") && repositoryVb.includes("membership_column_detail.column_rpt_desc"), "ORG_SUB_TY_CD column lookup follows the legacy code-value join and projection");
assertTrue(repositoryVb.includes("String.Equals(organizationId, \"825\"") && repositoryVb.includes('"product"') && repositoryVb.includes('"batch_product"'), "product lookup table is selected server-side by organization");
assertTrue(appJs.includes('class="rank-control"'), "active values expose a numeric ranker control");
assertTrue(appJs.includes("data-rank-position"), "ranker controls post position changes");
assertTrue(appJs.includes('api/values.ashx?action=position'), "ranker uses the position endpoint");
assertTrue(appJs.includes("Rank must be a positive whole number."), "ranker rejects invalid positions before posting");
assertTrue(appJs.includes("let rankUpdateInFlight = false") && appJs.includes("if (rankUpdateInFlight)") && appJs.includes("rankUpdateInFlight = true"), "rank updates cannot overlap");
assertTrue(appJs.includes("rankUpdating: true") && appJs.includes("rankUpdating: false") && appJs.includes("rankUpdating ? \" disabled\""), "rank controls are disabled while updating and restored afterward");
assertTrue(appJs.includes("if (requestSequence !== loadSequence)") && appJs.includes("throw error;"), "only current load failures surface to the error handler");
assertTrue(/selectedIds:\s*\{\},\s*editor:\s*null/.test(appJs) && appJs.includes('data-action="prev"') && appJs.includes('data-action="next"'), "paging clears hidden selection and closes the editor");
assertTrue(
    appJs.includes("Code value</th>") && appJs.includes("Description</th>"),
    "value and description use separate columns"
);
assertTrue(appJs.includes("status-indicator"), "status uses a quiet text indicator");
assertTrue(
    !appJs.includes("label label-success") && !appJs.includes("label label-default"),
    "status no longer looks like a button"
);
assertTrue(appJs.includes("row-actions"), "row actions use an inline action layout");
assertTrue(appJs.includes("protected-value"), "protected values do not expose mutation actions");
assertTrue(appJs.includes("code-value-description"), "descriptions use a plain display element");
assertTrue(appJs.includes("PilotDialogs.confirm"), "bulk delete uses the shared confirmation dialog");
assertTrue(!appJs.includes("window.confirm"), "bulk delete no longer uses the browser confirmation dialog");
assertTrue(
    appJs.includes('codeValue: editor.mode === "edit" ? editor.codeValue'),
    "edit save uses the static code value instead of a missing form field"
);

if (failures > 0) {
    process.exit(1);
}

console.log("All Code Admin workspace UI tests passed.");
