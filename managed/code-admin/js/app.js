"use strict";

(function () {
    const stateApi = window.CodeAdminState;
    const viewModel = window.CodeAdminViewModel;
    const root = document.getElementById("codeAdminApp");
    const messageEl = document.getElementById("appMessage");
    const userEl = document.getElementById("shellUser");
    const userNameEl = document.getElementById("shellUserName");
    const pageSize = 200;
    let searchTimer = null;
    let metadataRefreshTimer = null;
    let loadSequence = 0;
    let editorRequestSequence = 0;
    let rankUpdateInFlight = false;

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

    function invalidateEditorRequests() {
        return ++editorRequestSequence;
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

    function getSelectedClass(workspace, selectedClass) {
        return viewModel.getSelectedClass(workspace, selectedClass);
    }

    function renderFormGroup(id, label, control, className) {
        return (
            '<div class="form-group ' + (className || "") + '">' +
            '<label class="control-label" for="' + id + '">' + escapeHtml(label) + "</label> " +
            control +
            "</div>"
        );
    }

    function renderMetadataField(field, editor) {
        const value = viewModel.normalizeEditorValue(editor[field.key], field.controlType);
        const required = field.required ? " required" : "";
        const options = field.options || [];
        const id = "detail-" + field.key;
        let control;

        if (field.controlType === "textarea") {
            control = '<textarea class="form-control" id="' + id + '" name="' + field.key + '" rows="4"' + required + ">" + escapeHtml(value) + "</textarea>";
        } else if (field.controlType === "radio") {
            control = '<fieldset class="code-admin-radio-options" id="' + id + '" aria-required="' + (field.required ? "true" : "false") + '"><legend class="sr-only">' + escapeHtml(field.label) + '</legend>' + options.map(function (option, index) {
                const checked = option.value === value ? " checked" : "";
                const optionRequired = field.required && index === 0 ? " required" : "";
                const optionId = id + "-" + index;
                return '<label class="radio-inline" for="' + optionId + '"><input type="radio" id="' + optionId + '" name="' + field.key + '" value="' + escapeHtml(option.value) + '"' + checked + optionRequired + "> " + escapeHtml(option.label) + "</label>";
            }).join("") + "</fieldset>";
        } else if (field.controlType === "select" || field.controlType === "multiselect") {
            const selectedValues = field.controlType === "multiselect" ? value.split(",").map(function (item) { return item.trim(); }).filter(Boolean) : [value];
            const multiple = field.controlType === "multiselect" ? " multiple" : "";
            const placeholder = field.controlType === "multiselect" ? "" : '<option value=""></option>';
            control = '<select class="form-control" id="' + id + '" name="' + field.key + '"' + multiple + required + ">" + placeholder + options.map(function (option) {
                const selected = selectedValues.indexOf(option.value) >= 0 ? " selected" : "";
                return '<option value="' + escapeHtml(option.value) + '"' + selected + ">" + escapeHtml(option.label) + "</option>";
            }).join("") + "</select>";
        } else {
            control = '<input class="form-control" id="' + id + '" name="' + field.key + '" value="' + escapeHtml(value) + '"' + required + ">";
        }

        return renderFormGroup(id, field.label, control, field.controlType === "textarea" ? "editor-field-wide" : "");
    }

    function renderEditor(editor) {
        if (!editor) {
            return "";
        }

        const isEdit = editor.mode === "edit";
        const selectedClass = getSelectedClass(stateApi.get().workspace, stateApi.get().selectedClass);
        const classContext = selectedClass
            ? escapeHtml(selectedClass.codeClassDesc + " (" + selectedClass.codeClass + ")")
            : escapeHtml(stateApi.get().selectedClass);
        const editorTitle = isEdit ? "Edit " + escapeHtml(editor.codeValue) : "Add code value";
        const valueField = isEdit
            ? '<div class="form-group"><label class="control-label">Value</label><p class="form-control-static code-value-static">' + escapeHtml(editor.codeValue) + "</p></div>"
            : renderFormGroup(
                "codeValue",
                "Value",
                '<input class="form-control" id="codeValue" name="codeValue" value="' + escapeHtml(editor.codeValue || "") + '" required data-code-value>',
                ""
            );

        const workspace = editor.fieldMetadata
            ? { fieldMetadata: { [stateApi.get().selectedClass]: editor.fieldMetadata } }
            : stateApi.get().workspace;
        const detailFields = viewModel.getDetailFields(workspace, stateApi.get().selectedClass);
        const metadataFields = detailFields.map(function (field) {
            return renderMetadataField(field, editor);
        }).join("");

        return (
            '<section class="editor-panel code-admin-workspace panel panel-default" aria-labelledby="codeValueEditorTitle">' +
            '<div class="panel-heading editor-panel-heading"><div><h3 class="panel-title" id="codeValueEditorTitle">' + editorTitle + "</h3>" +
            '<p class="editor-class-context">' + classContext + "</p></div>" +
            '<button type="button" class="btn btn-default btn-sm" data-action="cancel-editor"><i class="fa fa-arrow-left" aria-hidden="true"></i> Back to list</button></div>' +
            '<div class="panel-body">' +
            '<form id="codeValueEditor">' +
            '<div class="editor-grid">' +
            valueField +
            metadataFields +
            "</div>" +
            '<div class="editor-actions">' +
            '<button type="submit" class="btn btn-primary btn-sm"><i class="fa fa-check" aria-hidden="true"></i> Save</button>' +
            '<button type="button" class="btn btn-default btn-sm" data-action="cancel-editor">Cancel</button>' +
            "</div>" +
            "</form></div>" +
            "</section>"
        );
    }

    function renderTable(page, selectedIds, rankUpdating) {
        if (!page || !page.items || page.items.length === 0) {
            return '<div class="alert alert-info code-admin-empty">No code values found for this class.</div>';
        }

        const rows = page.items
            .map(function (item) {
                const checked = selectedIds[item.codeValueId] ? " checked" : "";
                const rowClass = item.inactive ? "inactive code-value-row--inactive" : "";
                const status = item.inactive ? "Inactive" : "Active";
                const statusClass = item.inactive ? "status-indicator status-indicator--inactive" : "status-indicator status-indicator--active";
                const encodedCodeValue = escapeHtml(item.codeValue);
                const encodedCodeClass = escapeHtml(item.codeClass);
                const rank = item.orderBy || "";
                const rankControl = item.inactive || item.isProtected
                    ? '<span class="rank-unavailable" aria-label="' + (item.isProtected ? "Protected" : "Inactive") + ' values are not ranked">&mdash;</span>'
                    : '<input class="rank-control" type="number" min="1" step="1" value="' + rank + '" placeholder="Set" aria-label="Rank for ' + encodedCodeValue + '" data-rank-position data-class="' + encodedCodeClass + '" data-value="' + encodedCodeValue + '"' + (rankUpdating ? " disabled" : "") + ">";
                const description = '<div class="code-value-description">' + escapeHtml(item.codeValueDesc) + "</div>";
                const longDesc = item.codeValueLongDesc
                    ? '<div class="long-desc">' + escapeHtml(item.codeValueLongDesc.slice(0, 200)) + (item.codeValueLongDesc.length >= 200 ? "..." : "") + "</div>"
                    : "";

                return (
                    "<tr class=\"" + rowClass + "\">" +
                    '<td class="select-cell"><input type="checkbox" aria-label="Select ' + encodedCodeValue + '" data-select-id="' + item.codeValueId + '"' + checked + (item.isProtected ? " disabled" : "") + "></td>" +
                    '<td class="rank-cell">' + rankControl + "</td>" +
                    '<td class="value-cell"><button type="button" class="code-value" data-action="edit" data-id="' + item.codeValueId + '">' + encodedCodeValue + "</button></td>" +
                    '<td class="description-cell">' +
                    description +
                    longDesc +
                    "</td>" +
                    '<td class="status-cell"><span class="' + statusClass + '">' + status + "</span></td>" +
                    '<td class="action-cell"><div class="row-actions" role="group" aria-label="Actions for ' + encodedCodeValue + '">' +
                    (item.isProtected
                        ? '<span class="protected-value">Protected</span>'
                        : (item.inactive
                                ? '<button type="button" class="row-action" data-action="activate" data-class="' + encodedCodeClass + '" data-value="' + encodedCodeValue + '"><i class="fa fa-play" aria-hidden="true"></i> Activate</button>'
                                : '<button type="button" class="row-action row-action--danger" data-action="deactivate" data-class="' + encodedCodeClass + '" data-value="' + encodedCodeValue + '"><i class="fa fa-pause" aria-hidden="true"></i> Deactivate</button>')) +
                    "</div></td>" +
                    "</tr>"
                );
            })
            .join("");

        return (
            '<div class="table-responsive code-values-table-wrap">' +
            '<table class="table table-striped table-hover table-condensed">' +
            '<thead><tr><th class="select-cell"><span class="sr-only">Select</span></th><th class="rank-cell">Rank</th><th class="value-cell">Code value</th><th class="description-cell">Description</th><th class="status-cell">Status</th><th class="action-cell">Actions</th></tr></thead>' +
            "<tbody>" + rows + "</tbody>" +
            "</table></div>"
        );
    }

    function render(state) {
        if (state.editor) {
            root.innerHTML = renderEditor(state.editor);
            return;
        }

        const workspace = state.workspace || { classes: [] };
        const page = state.page || viewModel.emptyPage();
        const total = page.totalCount || 0;
        const end = Math.min(page.start + page.pageSize, total);
        const hasPrev = page.start > 0;
        const hasNext = end < total;
        const pageCount = total === 0 ? 0 : Math.ceil(total / page.pageSize);
        const pageNumber = total === 0 ? 0 : Math.floor(page.start / page.pageSize) + 1;
        const selectedCount = Object.keys(state.selectedIds || {}).length;

        const canModify = hasSelectedClass(state);
        const addDisabled = canModify ? "" : " disabled";
        const deleteDisabled = selectedCount > 0 ? "" : " disabled";

        root.innerHTML =
            '<section class="panel panel-default code-admin-workspace" aria-label="Code Admin workspace"' + (state.loading ? ' aria-busy="true"' : "") + ">" +
            '<div class="panel-heading code-admin-panel-heading">' +
            renderFormGroup(
                "codeClassSelect",
                "Class",
                '<select class="form-control" id="codeClassSelect">' + buildClassOptions(workspace, state.selectedClass) + "</select>",
                "class-filter"
            ) +
            '<div class="code-admin-actions">' +
            '<button type="button" class="btn btn-primary btn-sm" data-action="add"' + addDisabled + '><i class="fa fa-plus" aria-hidden="true"></i> Add value</button>' +
            (page.canDelete && canModify ? '<button type="button" class="btn btn-danger btn-sm" data-action="delete"' + deleteDisabled + '><i class="fa fa-trash" aria-hidden="true"></i> Delete selected (' + selectedCount + ")</button>" : "") +
            "</div></div>" +
            '<div class="panel-body">' +
            '<div class="form-inline code-admin-filters" role="search" aria-label="Filter code values">' +
            renderFormGroup(
                "searchInput",
                "Search",
                '<input class="form-control" type="search" id="searchInput" value="' + escapeHtml(state.search) + '" placeholder="Code or description" autocomplete="off">',
                "search-filter"
            ) +
            '<span class="sr-only" aria-live="polite">' + (state.loading ? "Loading code values" : "") + "</span>" +
            "</div>" +
            (canModify ? renderTable(page, state.selectedIds, state.rankUpdating) : '<div class="alert alert-info code-admin-empty">Select a class to view and manage code values.</div>') +
            '<nav class="code-admin-pager" aria-label="Code value pages">' +
            '<div class="btn-group btn-group-sm" role="group" aria-label="Page controls">' +
            '<button type="button" class="btn btn-default" data-action="prev"' + (hasPrev ? "" : " disabled") + '><i class="fa fa-chevron-left" aria-hidden="true"></i> Previous</button>' +
            '<button type="button" class="btn btn-default" data-action="next"' + (hasNext ? "" : " disabled") + '>Next <i class="fa fa-chevron-right" aria-hidden="true"></i></button>' +
            "</div>" +
            '<p class="code-admin-page-status"><strong>' + (total === 0 ? "0" : page.start + 1) + "–" + end + "</strong> of " + total + " items <span>Page " + pageNumber + " of " + pageCount + "</span></p>" +
            "</nav>" +
            "</div></section>";
    }

    async function loadValues() {
        const state = stateApi.get();
        const requestSequence = ++loadSequence;
        if (!hasSelectedClass(state)) {
            stateApi.set({ page: viewModel.emptyPage(), loading: false });
            return;
        }
        stateApi.set({ loading: true });
        const query =
            "api/values.ashx?codeClass=" + encodeURIComponent(state.selectedClass) +
            "&search=" + encodeURIComponent(state.search || "") +
            "&start=" + encodeURIComponent(state.start);
        try {
            const page = await window.PilotApiClient.get(query);
            if (requestSequence === loadSequence) {
                stateApi.set({ page: page, loading: false });
                showMessage("");
            }
        } catch (error) {
            if (requestSequence !== loadSequence) {
                return;
            }
            stateApi.set({ loading: false });
            throw error;
        }
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

    async function loadDetailMetadata(codeClass, codeValue) {
        return window.PilotApiClient.get(
            "api/values.ashx?metadata=true&codeClass=" + encodeURIComponent(codeClass) +
            "&codeValue=" + encodeURIComponent(codeValue || "")
        );
    }

    function captureEditorValues(form, editor) {
        const values = Object.assign({}, editor);
        Array.prototype.forEach.call(form.elements, function (control) {
            if (!control.name) {
                return;
            }
            if (control.tagName === "SELECT" && control.multiple) {
                values[control.name] = Array.prototype.slice.call(control.selectedOptions).map(function (option) {
                    return option.value;
                }).join(", ");
            } else {
                values[control.name] = control.value;
            }
        });
        return values;
    }

    async function refreshCreateMetadata(form) {
        const state = stateApi.get();
        const editor = state.editor;
        if (!editor || editor.mode !== "create" || state.selectedClass !== "ORG_SUB_TY_CD") {
            return;
        }
        const requestSequence = invalidateEditorRequests();
        const selectedClass = state.selectedClass;
        const codeValue = form.codeValue.value.trim();
        try {
            const metadata = await loadDetailMetadata(selectedClass, codeValue);
            const currentState = stateApi.get();
            const currentEditor = currentState.editor;
            const currentForm = document.getElementById("codeValueEditor");
            if (
                requestSequence !== editorRequestSequence ||
                !currentEditor ||
                currentEditor !== editor ||
                currentEditor.mode !== "create" ||
                currentState.selectedClass !== selectedClass ||
                !currentForm ||
                !currentForm.isConnected ||
                !currentForm.codeValue ||
                currentForm.codeValue.value.trim() !== codeValue
            ) {
                return;
            }
            stateApi.set({ editor: Object.assign(captureEditorValues(currentForm, currentEditor), { fieldMetadata: metadata }) });
        } catch (error) {
            if (requestSequence === editorRequestSequence) {
                throw error;
            }
        }
    }

    async function saveEditor(form) {
        const state = stateApi.get();
        if (!hasSelectedClass(state)) {
            showMessage("Select a class before adding a code value.");
            return;
        }
        const editor = state.editor;
        const requestSequence = invalidateEditorRequests();
        const payload = {
            codeClass: state.selectedClass,
            codeValue: editor.mode === "edit" ? editor.codeValue : form.codeValue.value.trim(),
            codeValueDesc: "",
            codeValueLongDesc: ""
        };
        viewModel.getPayloadFieldKeys(state.workspace, state.selectedClass).forEach(function (fieldKey) {
            const control = form.elements[fieldKey];
            if (control && control.tagName === "SELECT" && control.multiple) {
                payload[fieldKey] = Array.prototype.slice.call(control.selectedOptions).map(function (option) {
                    return option.value;
                }).join(", ");
            } else if (control) {
                payload[fieldKey] = String(control.value || "").trim();
            } else {
                payload[fieldKey] = String(editor[fieldKey] || "").trim();
            }
        });

        try {
            if (editor.mode === "edit") {
                payload.codeValueId = editor.codeValueId;
                payload.codeValue = editor.codeValue;
                await window.PilotApiClient.post("api/values.ashx?action=update", payload);
            } else {
                await window.PilotApiClient.post("api/values.ashx?action=create", payload);
            }
            if (requestSequence !== editorRequestSequence) {
                return;
            }
            invalidateEditorRequests();
            stateApi.set({ editor: null });
            await loadValues();
        } catch (error) {
            if (requestSequence === editorRequestSequence) {
                throw error;
            }
        }
    }

    async function deleteSelected() {
        const state = stateApi.get();
        const ids = Object.keys(state.selectedIds).map(Number);
        if (!ids.length) {
            showMessage("Select at least one code value to delete.");
            return;
        }
        const confirmed = await window.PilotDialogs.confirm({
            title: "Delete code values",
            message: "Delete " + ids.length + " selected code value" + (ids.length === 1 ? "" : "s") + "? Values currently in use will be skipped.",
            confirmLabel: "Delete",
            cancelLabel: "Cancel",
            danger: true
        });
        if (!confirmed) {
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
                if (action === "prev") {
                    invalidateEditorRequests();
                    stateApi.set({
                        start: Math.max(0, state.start - pageSize),
                        selectedIds: {},
                        editor: null
                    });
                    await loadValues();
                } else if (action === "next") {
                    invalidateEditorRequests();
                    stateApi.set({
                        start: state.start + pageSize,
                        selectedIds: {},
                        editor: null
                    });
                    await loadValues();
                } else if (action === "add") {
                    if (!viewModel.canOpenAddEditor(state.selectedClass)) {
                        showMessage("Select a class before adding a code value.");
                        return;
                    }
                    const requestSequence = invalidateEditorRequests();
                    const selectedClass = state.selectedClass;
                    try {
                        const metadata = await loadDetailMetadata(selectedClass, "");
                        if (requestSequence !== editorRequestSequence || stateApi.get().selectedClass !== selectedClass) {
                            return;
                        }
                        stateApi.set({
                            editor: {
                                mode: "create",
                                codeValue: "",
                                codeValueDesc: "",
                                codeValueLongDesc: "",
                                fieldMetadata: metadata
                            }
                        });
                        const firstField = document.getElementById("codeValue");
                        if (firstField) {
                            firstField.focus();
                        }
                    } catch (error) {
                        if (requestSequence === editorRequestSequence) {
                            throw error;
                        }
                    }
                } else if (action === "cancel-editor") {
                    invalidateEditorRequests();
                    stateApi.set({ editor: null });
                } else if (action === "delete") {
                    await deleteSelected();
                } else if (action === "edit") {
                    const requestSequence = invalidateEditorRequests();
                    const selectedClass = state.selectedClass;
                    const codeValueId = target.getAttribute("data-id");
                    try {
                        const item = await window.PilotApiClient.get("api/values.ashx?id=" + encodeURIComponent(codeValueId));
                        if (requestSequence !== editorRequestSequence || stateApi.get().selectedClass !== selectedClass) {
                            return;
                        }
                        stateApi.set({
                            editor: Object.assign({ mode: "edit" }, item)
                        });
                        const descriptionField = document.getElementById("detail-codeValueDesc") || document.querySelector("#codeValueEditor input, #codeValueEditor textarea, #codeValueEditor select");
                        if (descriptionField) {
                            descriptionField.focus();
                        }
                    } catch (error) {
                        if (requestSequence === editorRequestSequence) {
                            throw error;
                        }
                    }
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

        root.addEventListener("change", async function (event) {
            if (event.target.id === "codeClassSelect") {
                const selectedClass = event.target.value;
                invalidateEditorRequests();
                stateApi.set({
                    selectedClass: selectedClass,
                    start: 0,
                    selectedIds: {},
                    editor: null
                });
                if (selectedClass) {
                    loadValues().catch(handleError);
                } else {
                    stateApi.set({ page: viewModel.emptyPage() });
                    showMessage("");
                }
            } else if (event.target.matches("[data-select-id]")) {
                stateApi.toggleSelected(Number(event.target.getAttribute("data-select-id")), event.target.checked);
            } else if (event.target.matches("[data-rank-position]")) {
                const rankControl = event.target;
                const newPosition = Number(rankControl.value);
                if (!Number.isInteger(newPosition) || newPosition < 1) {
                    showMessage("Rank must be a positive whole number.");
                    rankControl.focus();
                    return;
                }
                if (rankUpdateInFlight) {
                    return;
                }
                rankUpdateInFlight = true;
                stateApi.set({ rankUpdating: true });
                try {
                    await window.PilotApiClient.post("api/values.ashx?action=position", {
                        codeClass: rankControl.getAttribute("data-class"),
                        codeValue: rankControl.getAttribute("data-value"),
                        newPosition: newPosition
                    });
                    await loadValues();
                } catch (error) {
                    handleError(error);
                } finally {
                    rankUpdateInFlight = false;
                    stateApi.set({ rankUpdating: false });
                }
            }
        });

        root.addEventListener("input", function (event) {
            if (event.target.matches("#codeValueEditor [data-code-value]")) {
                invalidateEditorRequests();
                if (metadataRefreshTimer) {
                    window.clearTimeout(metadataRefreshTimer);
                }
                metadataRefreshTimer = window.setTimeout(function () {
                    refreshCreateMetadata(event.target.form).catch(handleError);
                }, 300);
                return;
            }
            if (event.target.id !== "searchInput") {
                return;
            }
            const search = event.target.value.trim();
            if (searchTimer) {
                window.clearTimeout(searchTimer);
            }
            searchTimer = window.setTimeout(function () {
                invalidateEditorRequests();
                stateApi.set({
                    search: search,
                    start: 0,
                    selectedIds: {},
                    editor: null
                });
                loadValues().catch(handleError);
            }, 300);
        });

        root.addEventListener("keydown", function (event) {
            if (event.target.id !== "searchInput" || event.key !== "Enter") {
                return;
            }
            event.preventDefault();
            if (searchTimer) {
                window.clearTimeout(searchTimer);
                searchTimer = null;
            }
            invalidateEditorRequests();
            stateApi.set({
                search: event.target.value.trim(),
                start: 0,
                selectedIds: {},
                editor: null
            });
            loadValues().catch(handleError);
        });

        root.addEventListener("blur", function (event) {
            if (event.target.matches("#codeValueEditor [data-code-value]")) {
                if (metadataRefreshTimer) {
                    window.clearTimeout(metadataRefreshTimer);
                    metadataRefreshTimer = null;
                }
                refreshCreateMetadata(event.target.form).catch(handleError);
            }
        }, true);

        root.addEventListener("submit", function (event) {
            if (event.target.id !== "codeValueEditor") {
                return;
            }
            event.preventDefault();
            saveEditor(event.target).catch(handleError);
        });

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
