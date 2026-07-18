"use strict";

(function () {
    const stateApi = window.CodeAdminState;
    const viewModel = window.CodeAdminViewModel;
    const root = document.getElementById("codeAdminApp");
    const messageEl = document.getElementById("appMessage");
    const userEl = document.getElementById("shellUser");
    const userNameEl = document.getElementById("shellUserName");

    function showMessage(message) {
        if (!message) {
            messageEl.hidden = true;
            messageEl.textContent = "";
            return;
        }
        messageEl.textContent = message;
        messageEl.hidden = false;
    }

    function handleError(error) {
        showMessage((error && error.message) || "The request could not be completed.");
    }

    function escapeHtml(value) {
        return String(value || "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;");
    }

    function hasSelectedClass(state) {
        return viewModel.hasSelectedClass(state.selectedClass);
    }

    function buildClassOptions(workspace, selectedClass) {
        return viewModel.buildClassOptions(workspace, selectedClass);
    }

    function renderEditor(editor) {
        if (!editor) {
            return "";
        }

        const isEdit = editor.mode === "edit";
        const valueField = isEdit
            ? '<p><strong>Value:</strong> ' + escapeHtml(editor.codeValue) + "</p>"
            : '<label>Value<input name="codeValue" value="' + escapeHtml(editor.codeValue || "") + '" required></label>';

        return (
            '<section class="editor-panel">' +
            "<h3>" + (isEdit ? "Edit Code Value" : "Add Code Value") + "</h3>" +
            '<form id="codeValueEditor">' +
            '<div class="editor-grid">' +
            valueField +
            '<label>Description<input name="codeValueDesc" value="' + escapeHtml(editor.codeValueDesc || "") + '" required></label>' +
            '<label>Long Description<textarea name="codeValueLongDesc" rows="4">' + escapeHtml(editor.codeValueLongDesc || "") + "</textarea></label>" +
            '<label>Minor Code<input name="minorCode" value="' + escapeHtml(editor.minorCode || "") + '"></label>' +
            "</div>" +
            '<div class="editor-actions">' +
            '<button type="submit" class="btn btn-primary btn-sm">Save</button>' +
            '<button type="button" class="btn btn-default btn-sm" data-action="cancel-editor">Cancel</button>' +
            "</div>" +
            "</form>" +
            "</section>"
        );
    }

    function renderTable(page, selectedIds) {
        if (!page || !page.items || page.items.length === 0) {
            return '<p class="shell-muted">No code values found for this class.</p>';
        }

        const rows = page.items
            .map(function (item) {
                const checked = selectedIds[item.codeValueId] ? " checked" : "";
                const rowClass = item.inactive ? "inactive" : "";
                const status = item.inactive ? "Inactive" : "Active";
                const statusClass = item.inactive ? "inactive" : "active";
                const longDesc = item.codeValueLongDesc
                    ? '<div class="long-desc">' + escapeHtml(item.codeValueLongDesc.slice(0, 200)) + (item.codeValueLongDesc.length >= 200 ? "..." : "") + "</div>"
                    : "";

                return (
                    "<tr class=\"" + rowClass + "\">" +
                    "<td>" + (item.orderBy || "") + "</td>" +
                    "<td><input type=\"checkbox\" data-select-id=\"" + item.codeValueId + "\"" + checked + (item.isProtected ? " disabled" : "") + "></td>" +
                    "<td>" +
                    '<div class="code-value">' + escapeHtml(item.codeValue) + "</div>" +
                    '<div class="inline-edit" contenteditable="true" data-patch-field="code_value_desc" data-id="' + item.codeValueId + '">' + escapeHtml(item.codeValueDesc) + "</div>" +
                    longDesc +
                    "</td>" +
                    "<td><span class=\"status-pill " + statusClass + "\">" + status + "</span></td>" +
                    "<td>" +
                    (item.isProtected
                        ? ""
                        : '<button type="button" class="btn btn-default btn-xs" data-action="edit" data-id="' + item.codeValueId + '">Edit</button> ') +
                    (item.inactive
                        ? '<button type="button" class="btn btn-default btn-xs" data-action="activate" data-class="' + escapeHtml(item.codeClass) + '" data-value="' + escapeHtml(item.codeValue) + '">Activate</button>'
                        : '<button type="button" class="btn btn-default btn-xs" data-action="deactivate" data-class="' + escapeHtml(item.codeClass) + '" data-value="' + escapeHtml(item.codeValue) + '">Deactivate</button>') +
                    "</td>" +
                    "</tr>"
                );
            })
            .join("");

        return (
            "<table>" +
            "<thead><tr><th>Rank</th><th></th><th>Code Value</th><th>Status</th><th>Actions</th></tr></thead>" +
            "<tbody>" + rows + "</tbody>" +
            "</table>"
        );
    }

    function render(state) {
        const workspace = state.workspace || { classes: [] };
        const page = state.page || { items: [], totalCount: 0, start: 0, pageSize: state.pageSize, canDelete: false };
        const total = page.totalCount || 0;
        const end = Math.min(page.start + page.pageSize, total);
        const hasPrev = page.start > 0;
        const hasNext = end < total;

        const canModify = hasSelectedClass(state);
        const addDisabled = canModify ? "" : " disabled";

        root.innerHTML =
            '<section class="workspace-heading">' +
            "<div><h2>Code Admin" + (canModify ? ": <strong>" + escapeHtml(state.selectedClass) + "</strong>" : "") + "</h2>" +
            "<p>Search, edit, activate, and reorder code values.</p></div>" +
            "</section>" +
            '<div class="actions">' +
            '<button type="button" class="btn btn-primary btn-sm" data-action="add"' + addDisabled + '><i class="fa fa-plus"></i> Add</button>' +
            (page.canDelete && canModify ? '<button type="button" class="btn btn-danger btn-sm" data-action="delete"><i class="fa fa-remove"></i> Delete</button>' : "") +
            "</div>" +
            '<div class="toolbar">' +
            '<label>Class<select id="codeClassSelect">' + buildClassOptions(workspace, state.selectedClass) + "</select></label>" +
            '<label>Rows<input type="number" id="rowsInput" min="1" max="500" value="' + state.pageSize + '"></label>' +
            '<label>Search<input type="search" id="searchInput" value="' + escapeHtml(state.search) + '" placeholder="Search values"></label>' +
            '<button type="button" class="btn btn-default btn-sm" data-action="search"><i class="fa fa-search"></i> Search</button>' +
            "</div>" +
            (canModify ? renderTable(page, state.selectedIds) : '<p class="shell-muted">Select a class to view and manage code values.</p>') +
            '<div class="pagination">' +
            (hasPrev ? '<button type="button" class="btn btn-default btn-sm" data-action="prev">Previous</button>' : "") +
            "<span>" + (total === 0 ? "0" : page.start + 1) + "–" + end + " of " + total + "</span>" +
            (hasNext ? '<button type="button" class="btn btn-default btn-sm" data-action="next">Next</button>' : "") +
            "</div>" +
            renderEditor(state.editor);
    }

    async function loadValues() {
        const state = stateApi.get();
        if (!hasSelectedClass(state)) {
            stateApi.set({ page: viewModel.emptyPage(state.pageSize) });
            return;
        }
        const query =
            "api/values.ashx?codeClass=" + encodeURIComponent(state.selectedClass) +
            "&search=" + encodeURIComponent(state.search || "") +
            "&start=" + encodeURIComponent(state.start) +
            "&rows=" + encodeURIComponent(state.pageSize);
        const page = await window.PilotApiClient.get(query);
        stateApi.set({ page: page });
        showMessage("");
    }

    async function loadWorkspace() {
        const workspace = await window.PilotApiClient.get("api/workspace.ashx");
        const selectedClass = workspace.defaultCodeClass || "";
        stateApi.set({
            workspace: workspace,
            selectedClass: selectedClass,
            start: 0,
            selectedIds: {},
            editor: null
        });
        await loadValues();
    }

    async function saveEditor(form) {
        const state = stateApi.get();
        if (!hasSelectedClass(state)) {
            showMessage("Select a class before adding a code value.");
            return;
        }
        const editor = state.editor;
        const payload = {
            codeClass: state.selectedClass,
            codeValue: form.codeValue.value.trim(),
            codeValueDesc: form.codeValueDesc.value.trim(),
            codeValueLongDesc: form.codeValueLongDesc.value.trim(),
            minorCode: form.minorCode.value.trim()
        };

        if (editor.mode === "edit") {
            payload.codeValueId = editor.codeValueId;
            payload.codeValue = editor.codeValue;
            await window.PilotApiClient.post("api/values.ashx?action=update", payload);
        } else {
            await window.PilotApiClient.post("api/values.ashx?action=create", payload);
        }

        stateApi.set({ editor: null });
        await loadValues();
    }

    async function patchField(id, fieldName, fieldValue) {
        await window.PilotApiClient.post("api/values.ashx?action=patch", {
            codeValueId: Number(id),
            fieldName: fieldName,
            fieldValue: fieldValue
        });
        await loadValues();
    }

    async function deleteSelected() {
        const state = stateApi.get();
        const ids = Object.keys(state.selectedIds).map(Number);
        if (!ids.length) {
            showMessage("Select at least one code value to delete.");
            return;
        }
        if (!window.confirm("Delete the selected code values?")) {
            return;
        }
        const result = await window.PilotApiClient.post("api/values.ashx?action=delete", {
            codeValueIds: ids
        });
        const skipped = (result.results || []).filter(function (item) {
            return item.skippedInUse;
        });
        stateApi.clearSelected();
        await loadValues();
        if (skipped.length) {
            showMessage(skipped.map(function (item) { return item.message; }).join(" "));
        }
    }

    function bindEvents() {
        root.addEventListener("click", async function (event) {
            const target = event.target.closest("[data-action]");
            if (!target) {
                return;
            }

            const action = target.getAttribute("data-action");
            const state = stateApi.get();

            try {
                if (action === "search") {
                    if (!hasSelectedClass(state)) {
                        showMessage("Select a class before searching code values.");
                        return;
                    }
                    stateApi.set({
                        search: document.getElementById("searchInput").value.trim(),
                        pageSize: Number(document.getElementById("rowsInput").value) || 200,
                        start: 0
                    });
                    await loadValues();
                } else if (action === "prev") {
                    stateApi.set({ start: Math.max(0, state.start - state.pageSize) });
                    await loadValues();
                } else if (action === "next") {
                    stateApi.set({ start: state.start + state.pageSize });
                    await loadValues();
                } else if (action === "add") {
                    if (!viewModel.canOpenAddEditor(state.selectedClass)) {
                        showMessage("Select a class before adding a code value.");
                        return;
                    }
                    stateApi.set({
                        editor: {
                            mode: "create",
                            codeValue: "",
                            codeValueDesc: "",
                            codeValueLongDesc: "",
                            minorCode: ""
                        }
                    });
                } else if (action === "cancel-editor") {
                    stateApi.set({ editor: null });
                } else if (action === "delete") {
                    await deleteSelected();
                } else if (action === "edit") {
                    const item = await window.PilotApiClient.get("api/values.ashx?id=" + encodeURIComponent(target.getAttribute("data-id")));
                    stateApi.set({
                        editor: {
                            mode: "edit",
                            codeValueId: item.codeValueId,
                            codeValue: item.codeValue,
                            codeValueDesc: item.codeValueDesc,
                            codeValueLongDesc: item.codeValueLongDesc,
                            minorCode: item.minorCode
                        }
                    });
                } else if (action === "activate" || action === "deactivate") {
                    await window.PilotApiClient.post("api/values.ashx?action=" + action, {
                        codeClass: target.getAttribute("data-class"),
                        codeValue: target.getAttribute("data-value")
                    });
                    await loadValues();
                }
            } catch (error) {
                handleError(error);
            }
        });

        root.addEventListener("change", function (event) {
            if (event.target.id === "codeClassSelect") {
                const selectedClass = event.target.value;
                stateApi.set({
                    selectedClass: selectedClass,
                    start: 0,
                    selectedIds: {},
                    editor: null
                });
                if (selectedClass) {
                    loadValues().catch(handleError);
                } else {
                    stateApi.set({ page: viewModel.emptyPage(stateApi.get().pageSize) });
                    showMessage("");
                }
            } else if (event.target.matches("[data-select-id]")) {
                stateApi.toggleSelected(Number(event.target.getAttribute("data-select-id")), event.target.checked);
            }
        });

        root.addEventListener("submit", function (event) {
            if (event.target.id !== "codeValueEditor") {
                return;
            }
            event.preventDefault();
            saveEditor(event.target).catch(handleError);
        });

        root.addEventListener(
            "blur",
            function (event) {
                const target = event.target;
                if (!target.matches("[data-patch-field]")) {
                    return;
                }
                const id = target.getAttribute("data-id");
                const fieldName = target.getAttribute("data-patch-field");
                const fieldValue = target.textContent.trim();
                patchField(id, fieldName, fieldValue).catch(handleError);
            },
            true
        );
    }

    async function bootstrap() {
        window.PilotSession.configure({ sessionUrl: "api/session.ashx" });
        window.PilotApiClient.setApiBase("");
        window.PilotShell.bindLogout(document.getElementById("logoutButton"));
        bindEvents();
        stateApi.subscribe(render);

        try {
            const session = await window.PilotSession.load();
            userNameEl.textContent = session.userName;
            userEl.hidden = false;
            window.PilotShell.renderNav(
                document.getElementById("pilotToolNav"),
                (session.paths && session.paths.routes) || [],
                window.location.pathname
            );
            window.PilotShell.renderSectionMenu(
                document.getElementById("adminMenu"),
                session.menuSections || [],
                window.location.pathname
            );
            stateApi.set({ session: session });
            await loadWorkspace();
        } catch (error) {
            handleError(error);
        }
    }

    bootstrap();
}());
