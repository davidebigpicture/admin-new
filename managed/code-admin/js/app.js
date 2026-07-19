"use strict";

(function (global) {
    const viewModel = global.CodeAdminViewModel;
    const pageSize = 200;
    let searchTimer = null;
    let metadataTimer = null;
    let loadSequence = 0;
    let editorRequestSequence = 0;
    let classChangeSequence = 0;
    let routeSequence = 0;
    let applyingPopstate = false;

    const app = global.Vue.createApp({
        components: global.CodeAdminComponents,
        data: function () {
            return { workspace: { classes: [] }, page: viewModel.emptyPage(), selectedClass: "", search: "", start: 0, loading: false, selectedIds: {}, editor: null, rankUpdating: false, statusUpdatingId: null, message: "" };
        },
        computed: { hasSelectedClass: function () { return viewModel.hasSelectedClass(this.selectedClass); } },
        methods: {
            handleError: function (error) { this.message = (error && error.message) || "The request could not be completed."; },
            invalidateEditorRequests: function () { editorRequestSequence += 1; return editorRequestSequence; },
            clearSelected: function () { this.selectedIds = {}; },
            getClassInfo: function (codeClass) { return viewModel.getSelectedClass(this.workspace, codeClass); },
            writeListRoute: function (mode) {
                const navigation = global.CodeAdminNavigation;
                global.document.title = navigation.classTitle(this.getClassInfo(this.selectedClass));
                if (mode) { global.history[mode + "State"]({ codeClass: this.selectedClass }, "", navigation.listUrl(this.selectedClass)); }
            },
            writeDetailRoute: function (item, mode) {
                const navigation = global.CodeAdminNavigation;
                global.document.title = navigation.detailTitle(this.getClassInfo(this.selectedClass), item.codeValue);
                if (mode) { global.history[mode + "State"]({ codeClass: this.selectedClass, codeValue: item.codeValue, id: item.codeValueId }, "", navigation.detailUrl(this.selectedClass, item.codeValue, item.codeValueId)); }
            },
            toggleSelected: function (id, checked) { if (checked) { this.selectedIds[id] = true; } else { delete this.selectedIds[id]; } },
            loadValues: async function () {
                const requestSequence = ++loadSequence;
                if (!this.hasSelectedClass) { this.page = viewModel.emptyPage(); this.loading = false; return; }
                this.loading = true;
                try {
                    const page = await global.PilotApiClient.get("api/values.ashx?codeClass=" + encodeURIComponent(this.selectedClass) + "&search=" + encodeURIComponent(this.search || "") + "&start=" + encodeURIComponent(this.start));
                    if (requestSequence === loadSequence) { this.page = page; this.loading = false; this.message = ""; }
                } catch (error) { if (requestSequence === loadSequence) { this.loading = false; throw error; } }
            },
            loadWorkspace: async function () {
                this.workspace = await global.PilotApiClient.get("api/workspace.ashx");
                const route = global.CodeAdminNavigation.parseRoute(global.location.search);
                const defaultClass = this.workspace.defaultCodeClass || "";
                this.selectedClass = this.getClassInfo(route.codeClass) ? route.codeClass : defaultClass;
                this.start = 0; this.clearSelected(); this.editor = null;
                await this.loadValues();
                if (route.id) { await this.openDetailEditor(route.id, route, "replace"); } else { this.writeListRoute("replace"); }
            },
            loadDetailMetadata: function (codeClass, codeValue) {
                return global.PilotApiClient.get("api/values.ashx?metadata=true&codeClass=" + encodeURIComponent(codeClass) + "&codeValue=" + encodeURIComponent(codeValue || ""));
            },
            prepareEditor: function (editor) {
                const fields = viewModel.getDetailFields({ fieldMetadata: { [this.selectedClass]: editor.fieldMetadata } }, this.selectedClass);
                fields.forEach(function (field) {
                    if (field.controlType === "multiselect" && !Array.isArray(editor[field.key])) {
                        editor[field.key] = String(editor[field.key] || "").split(",").map(function (value) { return value.trim(); }).filter(Boolean);
                    }
                });
                return editor;
            },
            selectClass: async function (newCodeClass, originalCodeClass) {
                if (newCodeClass === this.selectedClass) { return; }
                const classChange = ++classChangeSequence;
                const priorState = { selectedClass: originalCodeClass == null ? this.selectedClass : originalCodeClass, start: this.start, selectedIds: this.selectedIds, editor: this.editor, page: this.page, message: this.message };
                this.invalidateEditorRequests(); this.selectedClass = newCodeClass; this.start = 0; this.clearSelected(); this.editor = null;
                if (!newCodeClass) { this.page = viewModel.emptyPage(); this.message = ""; return; }
                try {
                    await this.loadValues();
                    if (classChange === classChangeSequence) { this.writeListRoute(applyingPopstate ? null : "push"); }
                } catch (error) {
                    if (classChange === classChangeSequence) {
                        this.selectedClass = priorState.selectedClass; this.start = priorState.start; this.selectedIds = priorState.selectedIds; this.editor = priorState.editor; this.page = priorState.page; this.message = priorState.message;
                    }
                    throw error;
                }
            },
            onSearchInput: function (search) {
                const component = this;
                this.search = search;
                global.clearTimeout(searchTimer);
                searchTimer = global.setTimeout(function () {
                    component.invalidateEditorRequests(); component.start = 0; component.clearSelected(); component.editor = null;
                    component.loadValues().catch(component.handleError);
                }, 300);
            },
            changePage: async function (direction) {
                this.invalidateEditorRequests(); this.start = Math.max(0, this.start + direction * pageSize); this.clearSelected(); this.editor = null;
                await this.loadValues();
            },
            openCreateEditor: async function () {
                if (!viewModel.canOpenAddEditor(this.selectedClass)) { this.message = "Select a class before adding a code value."; return; }
                const requestSequence = this.invalidateEditorRequests(); const selectedClass = this.selectedClass;
                try {
                    const metadata = await this.loadDetailMetadata(selectedClass, "");
                    if (requestSequence === editorRequestSequence && selectedClass === this.selectedClass) {
                        this.editor = this.prepareEditor({ mode: "create", codeValue: "", codeValueError: "", codeValueDesc: "", codeValueLongDesc: "", fieldMetadata: metadata });
                    }
                } catch (error) { if (requestSequence === editorRequestSequence) { throw error; } }
            },
            openDetailEditor: async function (codeValueId, route, historyMode) {
                const requestSequence = this.invalidateEditorRequests(); const selectedClass = this.selectedClass;
                try {
                    const item = await global.PilotApiClient.get("api/values.ashx?id=" + encodeURIComponent(codeValueId));
                    if (requestSequence !== editorRequestSequence || selectedClass !== this.selectedClass) { return; }
                    if (item.codeClass !== selectedClass || (route && route.codeValue && item.codeValue !== route.codeValue)) {
                        this.editor = null; this.writeListRoute(historyMode === "replace" ? "replace" : null); return;
                    }
                    this.editor = this.prepareEditor(Object.assign({ mode: "edit", originalOrderBy: item.orderBy }, item));
                    this.writeDetailRoute(item, applyingPopstate ? null : historyMode);
                } catch (error) { if (requestSequence === editorRequestSequence) { throw error; } }
            },
            openEditEditor: function (codeValueId) { return this.openDetailEditor(codeValueId, null, "push"); },
            cancelEditor: function () { this.invalidateEditorRequests(); this.editor = null; this.writeListRoute("replace"); },
            applyRoute: async function (route) {
                const application = ++routeSequence;
                const nextClass = this.getClassInfo(route.codeClass) ? route.codeClass : (this.workspace.defaultCodeClass || "");
                applyingPopstate = true;
                try {
                    if (nextClass !== this.selectedClass) { await this.selectClass(nextClass, this.selectedClass); }
                    if (application !== routeSequence) { return; }
                    if (route.id) { await this.openDetailEditor(route.id, route, null); } else { this.invalidateEditorRequests(); this.editor = null; this.writeListRoute(null); }
                } finally { applyingPopstate = false; }
            },
            refreshCreateMetadata: async function () {
                if (!this.editor || this.editor.mode !== "create" || this.selectedClass !== "ORG_SUB_TY_CD") { return; }
                const requestSequence = this.invalidateEditorRequests(); const selectedClass = this.selectedClass; const editor = this.editor; const codeValue = editor.codeValue.trim();
                try {
                    const metadata = await this.loadDetailMetadata(selectedClass, codeValue);
                    if (requestSequence === editorRequestSequence && this.editor === editor && this.editor.mode === "create" && this.editor.codeValue.trim() === codeValue && this.selectedClass === selectedClass) {
                        this.editor = this.prepareEditor(Object.assign({}, editor, { fieldMetadata: metadata }));
                    }
                } catch (error) { if (requestSequence === editorRequestSequence) { throw error; } }
            },
            onCodeValueInput: function () {
                const component = this;
                if (this.editor && this.editor.mode === "create") { this.editor.codeValueError = ""; this.message = ""; }
                this.invalidateEditorRequests(); global.clearTimeout(metadataTimer);
                metadataTimer = global.setTimeout(function () { component.refreshCreateMetadata().catch(component.handleError); }, 300);
            },
            saveEditor: async function () {
                if (!this.hasSelectedClass || !this.editor) { this.message = "Select a class before adding a code value."; return; }
                const editor = this.editor; const requestSequence = this.invalidateEditorRequests();
                const requestedRank = Number(editor.orderBy);
                const rankChanged = editor.mode === "edit" && requestedRank !== Number(editor.originalOrderBy);
                if (rankChanged && (!Number.isInteger(requestedRank) || requestedRank < 1)) { throw new Error("Rank must be a positive whole number."); }
                const payload = { codeClass: this.selectedClass, codeValue: editor.mode === "edit" ? editor.codeValue : editor.codeValue.trim(), codeValueDesc: "", codeValueLongDesc: "" };
                viewModel.getPayloadFieldKeys(this.workspace, this.selectedClass).forEach(function (key) { payload[key] = Array.isArray(editor[key]) ? editor[key].join(", ") : String(editor[key] || "").trim(); });
                try {
                    if (editor.mode === "edit") {
                        payload.codeValueId = editor.codeValueId;
                        await global.PilotApiClient.post("api/values.ashx?action=update", payload);
                        if (rankChanged) { await global.PilotApiClient.post("api/values.ashx?action=position", { codeClass: editor.codeClass, codeValue: editor.codeValue, newPosition: requestedRank }); }
                    } else { await global.PilotApiClient.post("api/values.ashx?action=create", payload); }
                    if (requestSequence === editorRequestSequence) { this.editor = null; await this.loadValues(); this.writeListRoute("replace"); }
                } catch (error) {
                    if (requestSequence === editorRequestSequence) {
                        if (editor.mode === "create") { editor.codeValueError = (error && error.message) || "The value could not be created."; }
                        throw error;
                    }
                }
            },
            saveListText: async function (item, fieldName, newValue) {
                if (fieldName !== "codeValueDesc" && fieldName !== "codeValueLongDesc") { throw new Error("Unsupported list text field."); }
                const classAtStart = this.selectedClass;
                const textValue = String(newValue == null ? "" : newValue).trim();
                const record = await global.PilotApiClient.get("api/values.ashx?id=" + encodeURIComponent(item.codeValueId));
                if (classAtStart !== this.selectedClass || record.codeClass !== classAtStart || record.codeValue !== item.codeValue) { throw new Error("The code value changed before its text could be saved."); }
                const payload = { codeValueId: record.codeValueId, codeClass: record.codeClass, codeValue: record.codeValue, codeValueDesc: record.codeValueDesc || "", codeValueLongDesc: record.codeValueLongDesc || "" };
                payload[fieldName] = textValue;
                viewModel.getPayloadFieldKeys(this.workspace, classAtStart).forEach(function (key) { if (key !== "codeValueDesc" && key !== "codeValueLongDesc") { payload[key] = Array.isArray(record[key]) ? record[key].join(", ") : String(record[key] || "").trim(); } });
                await global.PilotApiClient.post("api/values.ashx?action=update", payload);
                if (classAtStart === this.selectedClass) { await this.loadValues(); }
            },
            saveDescription: function (item, newDescription) { return this.saveListText(item, "codeValueDesc", newDescription); },
            saveLongDescription: function (item, newDescription) { return this.saveListText(item, "codeValueLongDesc", newDescription); },
            deleteSelected: async function () {
                const ids = Object.keys(this.selectedIds).map(Number);
                if (!ids.length) { this.message = "Select at least one code value to delete."; return; }
                const confirmed = await global.PilotDialogs.confirm({ title: "Delete code values", message: "Delete " + ids.length + " selected code value" + (ids.length === 1 ? "" : "s") + "? Values currently in use will be skipped.", confirmLabel: "Delete", cancelLabel: "Cancel", danger: true });
                if (!confirmed) { return; }
                const result = await global.PilotApiClient.post("api/values.ashx?action=delete", { codeValueIds: ids });
                this.clearSelected(); await this.loadValues();
                const skipped = (result.results || []).filter(function (item) { return item.skippedInUse; });
                if (skipped.length) { this.message = skipped.map(function (item) { return item.message; }).join(" "); }
            },
            setStatus: async function (item, status) {
                const currentStatus = item.status === "N" || item.status === "Y" || item.status === "A" ? item.status : (item.inactive ? "Y" : "N");
                if (item.isProtected || status === currentStatus || this.statusUpdatingId !== null) { return; }
                this.statusUpdatingId = item.codeValueId;
                try {
                    await global.PilotApiClient.post("api/values.ashx?action=status", { codeClass: item.codeClass, codeValue: item.codeValue, status: status });
                    await this.loadValues();
                } catch (error) {
                    try { await this.loadValues(); } catch (reloadError) {}
                    throw error;
                } finally { this.statusUpdatingId = null; }
            },
            saveRank: async function (item, value) {
                const newPosition = Number(value);
                if (!Number.isInteger(newPosition) || newPosition < 1) { throw new Error("Rank must be a positive whole number."); }
                if (this.rankUpdating) { return; }
                this.rankUpdating = true;
                try {
                    await global.PilotApiClient.post("api/values.ashx?action=position", { codeClass: item.codeClass, codeValue: item.codeValue, newPosition: newPosition });
                    await this.loadValues();
                } finally { this.rankUpdating = false; }
            }
        },
        template: `
            <div>
                <div v-if="message" class="alert alert-danger code-admin-message" role="alert">{{ message }}</div>
                <Editor v-if="editor" :editor="editor" :selected-class="selectedClass" :workspace="workspace" @cancel="cancelEditor" @save="function () { saveEditor().catch(handleError); }" @code-value-input="onCodeValueInput" @refresh-metadata="function () { refreshCreateMetadata().catch(handleError); }"></Editor>
                <Workspace v-else :workspace="workspace" :page="page" :selected-class="selectedClass" :search="search" :selected-ids="selectedIds" :loading="loading" :rank-updating="rankUpdating" :status-updating-id="statusUpdatingId" :on-class-change="selectClass" :on-save-rank="saveRank" :on-set-status="setStatus" @search-change="onSearchInput" @selection-change="toggleSelected" @add="function () { openCreateEditor().catch(handleError); }" @edit="function (id) { openEditEditor(id).catch(handleError); }" @description-save="function (item, value) { return saveDescription(item, value); }" @long-description-save="function (item, value) { return saveLongDescription(item, value); }" @delete="function () { deleteSelected().catch(handleError); }" @page-change="function (direction) { changePage(direction).catch(handleError); }" @error="handleError"></Workspace>
            </div>`
    });

    app.component("InlineEdit", global.InlineEdit);
    const component = app.mount("#codeAdminApp");

    global.addEventListener("popstate", function () {
        component.applyRoute(global.CodeAdminNavigation.parseRoute(global.location.search)).catch(component.handleError);
    });

    async function bootstrap() {
        global.PilotSession.configure({ sessionUrl: "api/session.ashx" });
        global.PilotApiClient.setApiBase("");
        global.PilotShell.bindLogout(document.getElementById("logoutButton"));
        try {
            const session = await global.PilotSession.load();
            document.getElementById("shellUserName").textContent = session.userName;
            document.getElementById("shellUser").hidden = false;
            global.PilotShell.renderSectionMenu(document.getElementById("adminMenu"), session.menuSections || [], global.location.pathname);
            await component.loadWorkspace();
        } catch (error) { component.handleError(error); }
    }

    bootstrap();
}(window));
