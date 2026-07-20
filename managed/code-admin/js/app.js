"use strict";

(function (global) {
    const viewModel = global.CodeAdminViewModel;
    const pageSize = 200;

    const app = global.Vue.createApp({
        components: global.CodeAdminComponents,
        setup: function () {
            const state = global.Vue.reactive({ workspace: { classes: [] }, page: viewModel.emptyPage(), selectedClass: "", search: "", start: 0, loading: false, selectedIds: {}, editor: null, rankUpdating: false, statusUpdatingId: null, message: "" });
            const hasSelectedClass = global.Vue.computed(function () { return viewModel.hasSelectedClass(state.selectedClass); });
            let searchTimer = null;
            let metadataTimer = null;
            let loadSequence = 0;
            let editorRequestSequence = 0;
            let classChangeSequence = 0;
            let routeSequence = 0;
            let applyingPopstate = false;

            function handleError(error) { state.message = (error && error.message) || "The request could not be completed."; }
            function invalidateEditorRequests() { editorRequestSequence += 1; return editorRequestSequence; }
            function clearSelected() { state.selectedIds = {}; }
            function getClassInfo(codeClass) { return viewModel.getSelectedClass(state.workspace, codeClass); }
            function writeListRoute(mode) {
                const navigation = global.CodeAdminNavigation;
                global.document.title = navigation.classTitle(getClassInfo(state.selectedClass));
                if (mode) { global.history[mode + "State"]({ codeClass: state.selectedClass }, "", navigation.listUrl(state.selectedClass)); }
            }
            function writeDetailRoute(item, mode) {
                const navigation = global.CodeAdminNavigation;
                global.document.title = navigation.detailTitle(getClassInfo(state.selectedClass), item.codeValue);
                if (mode) { global.history[mode + "State"]({ codeClass: state.selectedClass, codeValue: item.codeValue, id: item.codeValueId }, "", navigation.detailUrl(state.selectedClass, item.codeValue, item.codeValueId)); }
            }
            function toggleSelected(id, checked) { if (checked) { state.selectedIds[id] = true; } else { delete state.selectedIds[id]; } }
            async function loadValues() {
                const requestSequence = ++loadSequence;
                if (!hasSelectedClass.value) { state.page = viewModel.emptyPage(); state.loading = false; return; }
                state.loading = true;
                try {
                    const page = await global.PilotApiClient.get("api/values.ashx?codeClass=" + encodeURIComponent(state.selectedClass) + "&search=" + encodeURIComponent(state.search || "") + "&start=" + encodeURIComponent(state.start));
                    if (requestSequence === loadSequence) { state.page = page; state.loading = false; state.message = ""; }
                } catch (error) { if (requestSequence === loadSequence) { state.loading = false; throw error; } }
            }
            async function loadWorkspace() {
                state.workspace = await global.PilotApiClient.get("api/workspace.ashx");
                const route = global.CodeAdminNavigation.parseRoute(global.location.search);
                const defaultClass = state.workspace.defaultCodeClass || "";
                state.selectedClass = getClassInfo(route.codeClass) ? route.codeClass : defaultClass;
                state.start = 0; clearSelected(); state.editor = null;
                await loadValues();
                if (route.id) { await openDetailEditor(route.id, route, "replace"); } else { writeListRoute("replace"); }
            }
            function loadDetailMetadata(codeClass, codeValue) {
                return global.PilotApiClient.get("api/values.ashx?metadata=true&codeClass=" + encodeURIComponent(codeClass) + "&codeValue=" + encodeURIComponent(codeValue || ""));
            }
            function prepareEditor(editor) {
                const fields = viewModel.getDetailFields({ fieldMetadata: { [state.selectedClass]: editor.fieldMetadata } }, state.selectedClass);
                fields.forEach(function (field) {
                    if (field.controlType === "multiselect" && !Array.isArray(editor[field.key])) {
                        editor[field.key] = String(editor[field.key] || "").split(",").map(function (value) { return value.trim(); }).filter(Boolean);
                    }
                });
                return editor;
            }
            async function selectClass(newCodeClass, originalCodeClass) {
                if (newCodeClass === state.selectedClass) { return; }
                const classChange = ++classChangeSequence;
                const priorState = { selectedClass: originalCodeClass == null ? state.selectedClass : originalCodeClass, start: state.start, selectedIds: state.selectedIds, editor: state.editor, page: state.page, message: state.message };
                invalidateEditorRequests(); state.selectedClass = newCodeClass; state.start = 0; clearSelected(); state.editor = null;
                if (!newCodeClass) { state.page = viewModel.emptyPage(); state.message = ""; return; }
                try {
                    await loadValues();
                    if (classChange === classChangeSequence) { writeListRoute(applyingPopstate ? null : "push"); }
                } catch (error) {
                    if (classChange === classChangeSequence) {
                        state.selectedClass = priorState.selectedClass; state.start = priorState.start; state.selectedIds = priorState.selectedIds; state.editor = priorState.editor; state.page = priorState.page; state.message = priorState.message;
                    }
                    throw error;
                }
            }
            function onSearchInput(search) {
                state.search = search;
                global.clearTimeout(searchTimer);
                searchTimer = global.setTimeout(function () {
                    invalidateEditorRequests(); state.start = 0; clearSelected(); state.editor = null;
                    loadValues().catch(handleError);
                }, 300);
            }
            async function changePage(direction) {
                invalidateEditorRequests(); state.start = Math.max(0, state.start + direction * pageSize); clearSelected(); state.editor = null;
                await loadValues();
            }
            async function openCreateEditor() {
                if (!viewModel.canOpenAddEditor(state.selectedClass)) { state.message = "Select a class before adding a code value."; return; }
                const requestSequence = invalidateEditorRequests(); const selectedClass = state.selectedClass;
                try {
                    const metadata = await loadDetailMetadata(selectedClass, "");
                    if (requestSequence === editorRequestSequence && selectedClass === state.selectedClass) {
                        state.editor = prepareEditor({ mode: "create", codeValue: "", codeValueError: "", codeValueDesc: "", codeValueLongDesc: "", fieldMetadata: metadata });
                    }
                } catch (error) { if (requestSequence === editorRequestSequence) { throw error; } }
            }
            async function openDetailEditor(codeValueId, route, historyMode) {
                const requestSequence = invalidateEditorRequests(); const selectedClass = state.selectedClass;
                try {
                    const item = await global.PilotApiClient.get("api/values.ashx?id=" + encodeURIComponent(codeValueId));
                    if (requestSequence !== editorRequestSequence || selectedClass !== state.selectedClass) { return; }
                    if (item.codeClass !== selectedClass || (route && route.codeValue && item.codeValue !== route.codeValue)) {
                        state.editor = null; writeListRoute(historyMode === "replace" ? "replace" : null); return;
                    }
                    state.editor = prepareEditor(Object.assign({ mode: "edit", originalOrderBy: item.orderBy }, item));
                    writeDetailRoute(item, applyingPopstate ? null : historyMode);
                } catch (error) { if (requestSequence === editorRequestSequence) { throw error; } }
            }
            function openEditEditor(codeValueId) { return openDetailEditor(codeValueId, null, "push"); }
            function cancelEditor() { invalidateEditorRequests(); state.editor = null; writeListRoute("replace"); }
            async function applyRoute(route) {
                const application = ++routeSequence;
                const nextClass = getClassInfo(route.codeClass) ? route.codeClass : (state.workspace.defaultCodeClass || "");
                applyingPopstate = true;
                try {
                    if (nextClass !== state.selectedClass) { await selectClass(nextClass, state.selectedClass); }
                    if (application !== routeSequence) { return; }
                    if (route.id) { await openDetailEditor(route.id, route, null); } else { invalidateEditorRequests(); state.editor = null; writeListRoute(null); }
                } finally { applyingPopstate = false; }
            }
            async function refreshCreateMetadata() {
                if (!state.editor || state.editor.mode !== "create" || state.selectedClass !== "ORG_SUB_TY_CD") { return; }
                const requestSequence = invalidateEditorRequests(); const selectedClass = state.selectedClass; const editor = state.editor; const codeValue = editor.codeValue.trim();
                try {
                    const metadata = await loadDetailMetadata(selectedClass, codeValue);
                    if (requestSequence === editorRequestSequence && state.editor === editor && state.editor.mode === "create" && state.editor.codeValue.trim() === codeValue && state.selectedClass === selectedClass) {
                        state.editor = prepareEditor(Object.assign({}, editor, { fieldMetadata: metadata }));
                    }
                } catch (error) { if (requestSequence === editorRequestSequence) { throw error; } }
            }
            function updateEditor(editor) { state.editor = editor; }
            function onCodeValueInput(editor) {
                if (editor) { state.editor = editor; }
                if (state.editor && state.editor.mode === "create") { state.editor.codeValueError = ""; state.message = ""; }
                invalidateEditorRequests(); global.clearTimeout(metadataTimer);
                metadataTimer = global.setTimeout(function () { refreshCreateMetadata().catch(handleError); }, 300);
            }
            async function saveEditor() {
                if (!hasSelectedClass.value || !state.editor) { state.message = "Select a class before adding a code value."; return; }
                const editor = state.editor; const requestSequence = invalidateEditorRequests();
                const requestedRank = Number(editor.orderBy);
                const rankChanged = editor.mode === "edit" && requestedRank !== Number(editor.originalOrderBy);
                if (rankChanged && (!Number.isInteger(requestedRank) || requestedRank < 1)) { throw new Error("Rank must be a positive whole number."); }
                const payload = { codeClass: state.selectedClass, codeValue: editor.mode === "edit" ? editor.codeValue : editor.codeValue.trim(), codeValueDesc: "", codeValueLongDesc: "" };
                viewModel.getPayloadFieldKeys(state.workspace, state.selectedClass).forEach(function (key) { payload[key] = Array.isArray(editor[key]) ? editor[key].join(", ") : String(editor[key] || "").trim(); });
                try {
                    if (editor.mode === "edit") {
                        payload.codeValueId = editor.codeValueId;
                        await global.PilotApiClient.post("api/values.ashx?action=update", payload);
                        if (rankChanged) { await global.PilotApiClient.post("api/values.ashx?action=position", { codeClass: editor.codeClass, codeValue: editor.codeValue, newPosition: requestedRank }); }
                    } else { await global.PilotApiClient.post("api/values.ashx?action=create", payload); }
                    if (requestSequence === editorRequestSequence) { state.editor = null; await loadValues(); writeListRoute("replace"); }
                } catch (error) {
                    if (requestSequence === editorRequestSequence) {
                        if (editor.mode === "create") { editor.codeValueError = (error && error.message) || "The value could not be created."; }
                        throw error;
                    }
                }
            }
            async function saveListText(item, fieldName, newValue) {
                if (fieldName !== "codeValueDesc" && fieldName !== "codeValueLongDesc") { throw new Error("Unsupported list text field."); }
                const classAtStart = state.selectedClass;
                const textValue = String(newValue == null ? "" : newValue).trim();
                const record = await global.PilotApiClient.get("api/values.ashx?id=" + encodeURIComponent(item.codeValueId));
                if (classAtStart !== this.selectedClass || record.codeClass !== classAtStart || record.codeValue !== item.codeValue) { throw new Error("The code value changed before its text could be saved."); }
                const payload = { codeValueId: record.codeValueId, codeClass: record.codeClass, codeValue: record.codeValue, codeValueDesc: record.codeValueDesc || "", codeValueLongDesc: record.codeValueLongDesc || "" };
                payload[fieldName] = textValue;
                viewModel.getPayloadFieldKeys(state.workspace, classAtStart).forEach(function (key) { if (key !== "codeValueDesc" && key !== "codeValueLongDesc") { payload[key] = Array.isArray(record[key]) ? record[key].join(", ") : String(record[key] || "").trim(); } });
                await global.PilotApiClient.post("api/values.ashx?action=update", payload);
                if (classAtStart === state.selectedClass) { await loadValues(); }
            }
            function saveDescription(item, newDescription) { return saveListText(item, "codeValueDesc", newDescription); }
            function saveLongDescription(item, newDescription) { return saveListText(item, "codeValueLongDesc", newDescription); }
            async function deleteSelected() {
                const ids = Object.keys(state.selectedIds).map(Number);
                if (!ids.length) { state.message = "Select at least one code value to delete."; return; }
                const confirmed = await global.AdminShellDialogs.confirm({ title: "Delete code values", message: "Delete " + ids.length + " selected code value" + (ids.length === 1 ? "" : "s") + "? Values currently in use will be skipped.", confirmLabel: "Delete", cancelLabel: "Cancel", danger: true });
                if (!confirmed) { return; }
                const result = await global.PilotApiClient.post("api/values.ashx?action=delete", { codeValueIds: ids });
                clearSelected(); await loadValues();
                const skipped = (result.results || []).filter(function (item) { return item.skippedInUse; });
                if (skipped.length) { state.message = skipped.map(function (item) { return item.message; }).join(" "); }
            }
            async function setStatus(item, status) {
                const currentStatus = item.status === "N" || item.status === "Y" || item.status === "A" ? item.status : (item.inactive ? "Y" : "N");
                if (item.isProtected || status === currentStatus || state.statusUpdatingId !== null) { return; }
                state.statusUpdatingId = item.codeValueId;
                try {
                    await global.PilotApiClient.post("api/values.ashx?action=status", { codeClass: item.codeClass, codeValue: item.codeValue, status: status });
                    await loadValues();
                } catch (error) {
                    try { await loadValues(); } catch (reloadError) {}
                    throw error;
                } finally { state.statusUpdatingId = null; }
            }
            async function saveRank(item, value) {
                const newPosition = Number(value);
                if (!Number.isInteger(newPosition) || newPosition < 1) { throw new Error("Rank must be a positive whole number."); }
                if (state.rankUpdating) { return; }
                state.rankUpdating = true;
                try {
                    await global.PilotApiClient.post("api/values.ashx?action=position", { codeClass: item.codeClass, codeValue: item.codeValue, newPosition: newPosition });
                    await loadValues();
                } finally { state.rankUpdating = false; }
            }
            function onPopstate() {
                applyRoute(global.CodeAdminNavigation.parseRoute(global.location.search)).catch(handleError);
            }
            async function bootstrap() {
                try {
                    await global.ManagedShell.initialize({ sessionUrl: "api/session.ashx", apiBase: "" });
                    await loadWorkspace();
                } catch (error) { handleError(error); }
            }
            global.Vue.onMounted(function () {
                global.addEventListener("popstate", onPopstate);
                bootstrap();
            });
            global.Vue.onBeforeUnmount(function () {
                global.clearTimeout(searchTimer);
                global.clearTimeout(metadataTimer);
                global.removeEventListener("popstate", onPopstate);
            });

            return Object.assign(global.Vue.toRefs(state), { hasSelectedClass: hasSelectedClass, handleError: handleError, toggleSelected: toggleSelected, loadWorkspace: loadWorkspace, selectClass: selectClass, onSearchInput: onSearchInput, changePage: changePage, openCreateEditor: openCreateEditor, openEditEditor: openEditEditor, cancelEditor: cancelEditor, applyRoute: applyRoute, refreshCreateMetadata: refreshCreateMetadata, updateEditor: updateEditor, onCodeValueInput: onCodeValueInput, saveEditor: saveEditor, saveDescription: saveDescription, saveLongDescription: saveLongDescription, deleteSelected: deleteSelected, saveRank: saveRank, setStatus: setStatus });
        },
        template: `
            <div>
                <div v-if="message" class="alert alert-danger code-admin-message" role="alert">{{ message }}</div>
                <Editor v-if="editor" :editor="editor" :selected-class="selectedClass" :workspace="workspace" @cancel="cancelEditor" @save="function () { saveEditor().catch(handleError); }" @update:editor="updateEditor" @code-value-input="onCodeValueInput" @refresh-metadata="function () { refreshCreateMetadata().catch(handleError); }"></Editor>
                <Workspace v-else :workspace="workspace" :page="page" :selected-class="selectedClass" :search="search" :selected-ids="selectedIds" :loading="loading" :rank-updating="rankUpdating" :status-updating-id="statusUpdatingId" :on-class-change="selectClass" :on-save-rank="saveRank" :on-set-status="setStatus" @search-change="onSearchInput" @selection-change="toggleSelected" @add="function () { openCreateEditor().catch(handleError); }" @edit="function (id) { openEditEditor(id).catch(handleError); }" @description-save="function (item, value) { return saveDescription(item, value); }" @long-description-save="function (item, value) { return saveLongDescription(item, value); }" @delete="function () { deleteSelected().catch(handleError); }" @page-change="function (direction) { changePage(direction).catch(handleError); }" @error="handleError"></Workspace>
            </div>`
    });

    app.component("InlineEdit", global.InlineEdit);
    app.mount("#codeAdminApp");
}(window));
