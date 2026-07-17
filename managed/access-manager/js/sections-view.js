"use strict";

(function (global) {
    const api = global.PilotApiClient;
    const dialogs = global.PilotDialogs;
    const reorder = global.AccessManagerReorder;
    const stateApi = global.AccessManagerState;

    let container = null;
    let onError = null;

    function init(options) {
        container = options.container;
        onError = options.onError;
        reorder.bind(container, {
            onReorder: function (index, position, row) {
                if (row && row.closest("#sectionItemsTable")) {
                    handleItemReorder(index, position);
                    return;
                }
                handleSectionReorder(index, position);
            }
        });
    }

    function render(appState) {
        if (!container) {
            return;
        }

        const caps = (appState.session && appState.session.capabilities) || {};
        if (!caps.canManageSections && !caps.canManageMemberships && !caps.canManageGrants) {
            container.innerHTML = "<p class=\"empty-state\">You do not have permission to manage sections.</p>";
            return;
        }

        const sections = (appState.workspace && appState.workspace.sections) || [];
        const detail = (appState.workspace && appState.workspace.sectionDetail) || {};
        const selectedId = appState.selectedSectionId;
        const selected = sections.find(function (section) {
            return section.SectionId === selectedId;
        });

        container.innerHTML = "";

        const workspaceHeader = document.createElement("div");
        workspaceHeader.className = "workspace-heading";
        workspaceHeader.innerHTML =
            "<div><h2>Sections and access</h2><p>Manage each section, its scripts, and its grants in one workspace.</p></div>";
        if (caps.canManageGrants) {
            const accessButton = document.createElement("button");
            accessButton.type = "button";
            accessButton.innerHTML = "<i class=\"fa fa-search\" aria-hidden=\"true\"></i> Find access";
            accessButton.addEventListener("click", openAccessExplorer);
            workspaceHeader.appendChild(accessButton);
        }
        container.appendChild(workspaceHeader);

        const layout = document.createElement("div");
        layout.className = "split-layout";

        const listPanel = document.createElement("section");
        listPanel.className = "panel";
        listPanel.innerHTML = "<h2>Sections</h2>";
        listPanel.appendChild(renderSectionToolbar(appState, caps));
        listPanel.appendChild(renderSectionList(sections, selectedId, appState.includeInactiveSections));

        const detailPanel = document.createElement("section");
        detailPanel.className = "panel section-detail";
        detailPanel.innerHTML = "<h2>Section detail</h2>";
        if (!selected) {
            detailPanel.innerHTML += "<p class=\"empty-state\">Select a section to manage items and grants.</p>";
        } else {
            detailPanel.appendChild(renderSectionHeader(selected, caps));
            if (caps.canManageMemberships) {
                detailPanel.appendChild(renderItemsPanel(selected, detail.items || [], caps));
            }
            if (caps.canManageGrants) {
                detailPanel.appendChild(renderGrantsPanel(selected, detail.grants || [], caps));
            }
        }

        layout.appendChild(listPanel);
        layout.appendChild(detailPanel);
        container.appendChild(layout);
    }

    function renderSectionToolbar(appState, caps) {
        const toolbar = document.createElement("div");
        toolbar.className = "toolbar";

        const inactiveLabel = document.createElement("label");
        const inactiveToggle = document.createElement("input");
        inactiveToggle.type = "checkbox";
        inactiveToggle.checked = !!appState.includeInactiveSections;
        inactiveToggle.addEventListener("change", function () {
            stateApi.set({ includeInactiveSections: inactiveToggle.checked });
            refreshSections();
        });
        inactiveLabel.appendChild(inactiveToggle);
        inactiveLabel.appendChild(document.createTextNode(" Show inactive"));
        toolbar.appendChild(inactiveLabel);

        if (caps.canManageSections) {
            const createButton = document.createElement("button");
            createButton.type = "button";
            createButton.className = "primary";
            createButton.textContent = "New section";
            createButton.addEventListener("click", createSection);
            toolbar.appendChild(createButton);
        }

        return toolbar;
    }

    function renderSectionList(sections, selectedId, includeInactive) {
        const list = document.createElement("ul");
        list.className = "list-nav";
        list.setAttribute("data-reorder-count", String(sections.filter(function (s) { return !s.Inactive; }).length));

        const visible = sections.filter(function (section) {
            return includeInactive || !section.Inactive;
        });

        visible.forEach(function (section, index) {
            const item = document.createElement("li");
            item.setAttribute("data-reorder-index", String(index));

            const button = document.createElement("button");
            button.type = "button";
            button.textContent = section.SectionName + (section.Inactive ? " (inactive)" : "");
            if (section.SectionId === selectedId) {
                button.setAttribute("aria-current", "true");
            }
            button.addEventListener("click", function () {
                selectSection(section.SectionId);
            });
            item.appendChild(button);

            const caps = (stateApi.get().session && stateApi.get().session.capabilities) || {};
            if (caps.canManageSections && !section.Inactive) {
                item.appendChild(reorder.renderControls(index, visible.filter(function (s) { return !s.Inactive; }).length));
            }

            list.appendChild(item);
        });

        if (!visible.length) {
            const empty = document.createElement("p");
            empty.className = "empty-state";
            empty.textContent = "No sections found.";
            return empty;
        }

        return list;
    }

    function renderSectionHeader(section, caps) {
        const wrapper = document.createElement("div");
        wrapper.className = "section-heading";

        const identity = document.createElement("div");
        identity.className = "section-identity";
        const titleRow = document.createElement("div");
        titleRow.className = "editable-heading";
        const title = document.createElement("h3");
        title.textContent = section.SectionName;
        titleRow.appendChild(title);
        if (caps.canManageSections) {
            const editNameButton = document.createElement("button");
            editNameButton.type = "button";
            editNameButton.className = "icon-button";
            editNameButton.innerHTML = "<i class=\"fa fa-pencil\" aria-hidden=\"true\"></i>";
            editNameButton.title = "Edit section name";
            editNameButton.setAttribute("aria-label", "Edit section name");
            editNameButton.addEventListener("click", function () {
                beginSectionNameEdit(section, identity);
            });
            titleRow.appendChild(editNameButton);
        }
        identity.appendChild(titleRow);

        const meta = document.createElement("p");
        meta.className = "empty-state section-meta";
        meta.innerHTML = "Update #" + section.UpdateNo + " &middot; " +
            (section.Inactive
                ? "<span class=\"status-pill inactive\">Inactive</span>"
                : "<span class=\"status-pill active\">Active</span>");
        identity.appendChild(meta);
        wrapper.appendChild(identity);

        if (!caps.canManageSections) {
            return wrapper;
        }

        const toolbar = document.createElement("div");
        toolbar.className = "toolbar";

        const lifecycleButton = document.createElement("button");
        lifecycleButton.type = "button";
        lifecycleButton.innerHTML = section.Inactive
            ? "<i class=\"fa fa-undo\" aria-hidden=\"true\"></i> Activate"
            : "<i class=\"fa fa-ban\" aria-hidden=\"true\"></i> Deactivate";
        lifecycleButton.addEventListener("click", function () {
            toggleSection(section);
        });

        const deleteButton = document.createElement("button");
        deleteButton.type = "button";
        deleteButton.className = "danger";
        deleteButton.innerHTML = "<i class=\"fa fa-trash\" aria-hidden=\"true\"></i> Delete";
        deleteButton.addEventListener("click", function () {
            deleteSection(section);
        });

        toolbar.appendChild(lifecycleButton);
        toolbar.appendChild(deleteButton);
        wrapper.appendChild(toolbar);
        return wrapper;
    }

    function renderItemsPanel(section, items, caps) {
        const panel = document.createElement("div");
        panel.className = "section-subpanel";
        const heading = document.createElement("div");
        heading.className = "subpanel-heading";
        heading.innerHTML = "<h3>Section scripts</h3>";

        if (caps.canManageMemberships) {
            const addButton = document.createElement("button");
            addButton.type = "button";
            addButton.className = "primary";
            addButton.innerHTML = "<i class=\"fa fa-plus\" aria-hidden=\"true\"></i> Add script";
            addButton.addEventListener("click", function () {
                openScriptPicker(section, items);
            });
            heading.appendChild(addButton);
        }
        panel.appendChild(heading);

        const tableWrap = document.createElement("div");
        tableWrap.className = "table-wrap";
        tableWrap.id = "sectionItemsTable";
        tableWrap.setAttribute("data-reorder-count", String(items.length));

        const table = document.createElement("table");
        table.className = "data";
        table.innerHTML = "<thead><tr><th>Title</th><th>Script</th><th>Status</th><th><span class=\"sr-only\">Actions</span></th></tr></thead>";
        const body = document.createElement("tbody");

        items.forEach(function (item, index) {
            const row = document.createElement("tr");
            if (item.ScriptInactive) {
                row.className = "inactive";
            }
            row.setAttribute("data-reorder-index", String(index));
            row.innerHTML =
                "<td></td>" +
                "<td class=\"path-cell\">" + escapeHtml(item.ScriptName) + "</td>" +
                "<td>" + (item.ScriptInactive
                    ? "<span class=\"status-pill inactive\">Inactive</span>"
                    : "<span class=\"status-pill active\">Active</span>") + "</td>" +
                "<td class=\"action-cell\"></td>";
            const titleCell = row.firstElementChild;
            const actions = row.lastElementChild;

            const titleButton = document.createElement("button");
            titleButton.type = "button";
            titleButton.className = "script-title-button";
            titleButton.textContent = item.Title || item.ScriptName;
            titleButton.title = caps.canManageScripts ? "Edit script details" : "Script details";
            titleButton.addEventListener("click", function () {
                beginScriptEdit(section, item, row, caps.canManageScripts);
            });
            titleCell.appendChild(titleButton);

            if (caps.canManageMemberships) {
                actions.appendChild(reorder.renderControls(index, items.length));
            }

            if (caps.canManageScripts) {
                const editButton = document.createElement("button");
                editButton.type = "button";
                editButton.className = "icon-button";
                editButton.innerHTML = "<i class=\"fa fa-pencil\" aria-hidden=\"true\"></i>";
                editButton.title = "Edit script";
                editButton.setAttribute("aria-label", "Edit " + item.Title);
                editButton.addEventListener("click", function () {
                    beginScriptEdit(section, item, row, true);
                });
                actions.appendChild(editButton);

                const lifecycleButton = document.createElement("button");
                lifecycleButton.type = "button";
                lifecycleButton.className = "icon-button";
                lifecycleButton.innerHTML = item.ScriptInactive
                    ? "<i class=\"fa fa-undo\" aria-hidden=\"true\"></i>"
                    : "<i class=\"fa fa-ban\" aria-hidden=\"true\"></i>";
                lifecycleButton.title = item.ScriptInactive ? "Activate script" : "Deactivate script";
                lifecycleButton.setAttribute("aria-label", lifecycleButton.title + " " + item.Title);
                lifecycleButton.addEventListener("click", function () {
                    toggleScript(section, item);
                });
                actions.appendChild(lifecycleButton);

                const deleteButton = document.createElement("button");
                deleteButton.type = "button";
                deleteButton.className = "icon-button danger-icon";
                deleteButton.innerHTML = "<i class=\"fa fa-trash\" aria-hidden=\"true\"></i>";
                deleteButton.title = "Delete script everywhere";
                deleteButton.setAttribute("aria-label", "Delete " + item.Title + " everywhere");
                deleteButton.addEventListener("click", function () {
                    deleteScript(section, item);
                });
                actions.appendChild(deleteButton);
            }

            if (caps.canManageMemberships) {
                const removeButton = document.createElement("button");
                removeButton.type = "button";
                removeButton.className = "icon-button";
                removeButton.innerHTML = "<i class=\"fa fa-chain-broken\" aria-hidden=\"true\"></i>";
                removeButton.title = "Remove from section";
                removeButton.setAttribute("aria-label", "Remove " + item.Title + " from section");
                removeButton.addEventListener("click", function () {
                    removeItem(section, item);
                });
                actions.appendChild(removeButton);
            }

            body.appendChild(row);
        });

        table.appendChild(body);
        tableWrap.appendChild(table);
        if (!items.length) {
            tableWrap.innerHTML = "<p class=\"empty-state\">No scripts are assigned to this section.</p>";
        }
        panel.appendChild(tableWrap);
        return panel;
    }

    function renderGrantsPanel(section, grants, caps) {
        const panel = document.createElement("div");
        panel.className = "section-subpanel";
        const heading = document.createElement("div");
        heading.className = "subpanel-heading";
        heading.innerHTML = "<h3>Section grants</h3>";

        if (caps.canManageGrants) {
            const addButton = document.createElement("button");
            addButton.type = "button";
            addButton.className = "primary";
            addButton.innerHTML = "<i class=\"fa fa-plus\" aria-hidden=\"true\"></i> Add grant";
            addButton.addEventListener("click", function () {
                openGrantPicker(section, grants);
            });
            heading.appendChild(addButton);
        }
        panel.appendChild(heading);

        const tableWrap = document.createElement("div");
        tableWrap.className = "table-wrap";
        const table = document.createElement("table");
        table.className = "data";
        table.innerHTML = "<thead><tr><th>Principal</th><th>Type</th><th>Status</th><th>Actions</th></tr></thead>";
        const body = document.createElement("tbody");

        grants.forEach(function (grant) {
            const row = document.createElement("tr");
            if (grant.Inactive) {
                row.className = "inactive";
            }
            row.innerHTML =
                "<td>" + escapeHtml(grant.PrincipalLabel || ("#" + grant.UserId)) + "</td>" +
                "<td>" + escapeHtml(stateApi.principalTypeLabel(grant.UserTy)) + "</td>" +
                "<td>" + (grant.Inactive
                    ? "<span class=\"status-pill inactive\">Inactive</span>"
                    : "<span class=\"status-pill active\">Active</span>") + "</td>" +
                "<td class=\"action-cell\"></td>";
            const actions = row.lastElementChild;
            if (caps.canManageGrants) {
                const toggle = document.createElement("button");
                toggle.type = "button";
                toggle.className = "icon-button";
                toggle.innerHTML = grant.Inactive
                    ? "<i class=\"fa fa-undo\" aria-hidden=\"true\"></i>"
                    : "<i class=\"fa fa-ban\" aria-hidden=\"true\"></i>";
                toggle.title = grant.Inactive ? "Activate grant" : "Deactivate grant";
                toggle.setAttribute("aria-label", toggle.title + " for " + (grant.PrincipalLabel || grant.UserId));
                toggle.addEventListener("click", function () {
                    toggleGrant(grant, grant.Inactive ? "activate" : "deactivate");
                });
                actions.appendChild(toggle);
            }
            body.appendChild(row);
        });

        table.appendChild(body);
        tableWrap.appendChild(table);
        if (!grants.length) {
            tableWrap.innerHTML = "<p class=\"empty-state\">No direct grants for this section.</p>";
        }
        panel.appendChild(tableWrap);
        return panel;
    }

    function beginScriptEdit(section, item, row, canEdit) {
        const detailCell = document.createElement("td");
        detailCell.colSpan = 4;
        row.classList.add("script-editing");
        row.replaceChildren(detailCell);

        if (!canEdit) {
            detailCell.innerHTML =
                "<div class=\"script-details\"><div><strong>" + escapeHtml(item.Title) + "</strong>" +
                "<p class=\"path-cell\">" + escapeHtml(item.ScriptName) + "</p>" +
                "<p class=\"empty-state\">Type: " + escapeHtml(item.ScriptTy) + "</p></div>" +
                "<button type=\"button\" class=\"icon-button close-details\" aria-label=\"Close\"><i class=\"fa fa-times\" aria-hidden=\"true\"></i></button></div>";
            detailCell.querySelector(".close-details").addEventListener("click", function () {
                render(stateApi.get());
            });
            return;
        }

        const form = document.createElement("form");
        form.className = "script-inline-editor";
        form.innerHTML =
            "<div class=\"field\"><label>Title</label><input name=\"title\" required></div>" +
            "<div class=\"field script-path-field\"><label>Script path</label><input name=\"scriptName\" required></div>" +
            "<div class=\"field\"><label>Type</label><select name=\"scriptTy\"></select></div>" +
            "<div class=\"inline-edit-actions\"><button type=\"submit\" class=\"primary\"><i class=\"fa fa-check\" aria-hidden=\"true\"></i> Save</button>" +
            "<button type=\"button\" class=\"cancel-edit\"><i class=\"fa fa-times\" aria-hidden=\"true\"></i> Cancel</button></div>";
        const titleInput = form.querySelector("[name=\"title\"]");
        const pathInput = form.querySelector("[name=\"scriptName\"]");
        const typeSelect = form.querySelector("[name=\"scriptTy\"]");
        titleInput.value = item.Title;
        pathInput.value = item.ScriptName;
        (((stateApi.get().workspace || {}).scriptTypes) || []).forEach(function (type) {
            const option = document.createElement("option");
            option.value = type.CodeValue;
            option.textContent = type.CodeValueDesc || type.CodeValue;
            option.selected = type.CodeValue === item.ScriptTy;
            typeSelect.appendChild(option);
        });

        function cancelEdit() {
            render(stateApi.get());
        }

        form.querySelector(".cancel-edit").addEventListener("click", cancelEdit);
        form.addEventListener("keydown", function (event) {
            if (event.key === "Escape") {
                event.preventDefault();
                cancelEdit();
            }
        });
        form.addEventListener("submit", async function (event) {
            event.preventDefault();
            try {
                await api.post("api/scripts.ashx?action=update", {
                    ScriptId: item.ScriptId,
                    ScriptTy: typeSelect.value,
                    ScriptName: pathInput.value.trim(),
                    Title: titleInput.value.trim(),
                    ExpectedUpdateNo: item.UpdateNo
                });
                await selectSection(section.SectionId);
            } catch (error) {
                reportError(error);
            }
        });
        detailCell.appendChild(form);
        titleInput.focus();
        titleInput.select();
    }

    async function toggleScript(section, item) {
        const action = item.ScriptInactive ? "activate" : "deactivate";
        const confirmed = await dialogs.confirm({
            title: item.ScriptInactive ? "Activate script" : "Deactivate script",
            message: "This changes the script everywhere it is used. Continue with \"" + item.Title + "\"?"
        });
        if (!confirmed) {
            return;
        }
        try {
            await api.post("api/scripts.ashx?action=" + action, {
                ScriptId: item.ScriptId,
                ExpectedUpdateNo: item.UpdateNo
            });
            await selectSection(section.SectionId);
        } catch (error) {
            reportError(error);
        }
    }

    async function deleteScript(section, item) {
        try {
            const impact = await api.post("api/scripts.ashx?action=deletePreview", {
                ScriptId: item.ScriptId,
                ExpectedUpdateNo: item.UpdateNo
            });
            const confirmed = await dialogs.confirm({
                title: "Delete script everywhere",
                message: "Permanently delete \"" + item.Title + "\"?\n\nAccess rows: " +
                    impact.AccessRowCount + "\nSection assignments: " + impact.SectionScriptRowCount,
                danger: true,
                confirmLabel: "Delete permanently"
            });
            if (!confirmed) {
                return;
            }
            await api.post("api/scripts.ashx?action=delete", {
                ScriptId: item.ScriptId,
                ExpectedUpdateNo: item.UpdateNo,
                Confirm: true
            });
            await selectSection(section.SectionId);
        } catch (error) {
            reportError(error);
        }
    }

    function openScriptPicker(section, assignedItems) {
        const picker = openPickerDialog("Add script to " + section.SectionName);
        const appState = stateApi.get();
        const scriptTypes = (appState.workspace && appState.workspace.scriptTypes) || [];
        const assignedIds = {};
        assignedItems.forEach(function (item) {
            assignedIds[item.ScriptId] = true;
        });

        const filters = document.createElement("div");
        filters.className = "picker-filters";
        filters.innerHTML =
            "<div class=\"field\"><label for=\"pickerScriptType\">Script type</label><select id=\"pickerScriptType\"></select></div>" +
            "<div class=\"field picker-search\"><label for=\"pickerScriptSearch\">Find a script</label><input id=\"pickerScriptSearch\" type=\"search\" placeholder=\"Search title or path\"></div>";
        const typeSelect = filters.querySelector("select");
        const searchInput = filters.querySelector("input");
        const results = document.createElement("div");
        results.className = "picker-results";
        const createArea = document.createElement("div");
        createArea.className = "picker-create";
        const createToggle = document.createElement("button");
        createToggle.type = "button";
        createToggle.innerHTML = "<i class=\"fa fa-plus\" aria-hidden=\"true\"></i> Add a script not listed";
        const createForm = document.createElement("form");
        createForm.className = "script-create-form";
        createForm.hidden = true;
        createForm.innerHTML =
            "<div class=\"field\"><label for=\"newScriptTitle\">Title</label><input id=\"newScriptTitle\" name=\"title\" required></div>" +
            "<div class=\"field\"><label for=\"newScriptPath\">Script path</label><input id=\"newScriptPath\" name=\"scriptName\" required placeholder=\"/admin/admin/example.asp\"></div>" +
            "<div class=\"inline-edit-actions\"><button type=\"submit\" class=\"primary\">Add to section</button></div>";
        let scripts = [];

        createToggle.addEventListener("click", function () {
            createForm.hidden = !createForm.hidden;
            createToggle.setAttribute("aria-expanded", createForm.hidden ? "false" : "true");
            if (!createForm.hidden) {
                createForm.querySelector("[name=\"title\"]").focus();
            }
        });
        createForm.addEventListener("submit", async function (event) {
            event.preventDefault();
            const submitButton = createForm.querySelector("[type=\"submit\"]");
            try {
                submitButton.disabled = true;
                const script = await api.post("api/scripts.ashx?action=create", {
                    ScriptTy: typeSelect.value,
                    ScriptName: createForm.querySelector("[name=\"scriptName\"]").value.trim(),
                    Title: createForm.querySelector("[name=\"title\"]").value.trim()
                });
                await api.post("api/sections.ashx?action=addItem", {
                    SectionId: section.SectionId,
                    ScriptId: script.ScriptId
                });
                picker.close();
                await selectSection(section.SectionId);
            } catch (error) {
                submitButton.disabled = false;
                reportError(error);
            }
        });
        createArea.appendChild(createToggle);
        createArea.appendChild(createForm);

        scriptTypes.forEach(function (type) {
            const option = document.createElement("option");
            option.value = type.CodeValue;
            option.textContent = type.CodeValueDesc || type.CodeValue;
            option.selected = type.CodeValue === (appState.workspace.defaultScriptType || "");
            typeSelect.appendChild(option);
        });

        function renderResults() {
            const visible = scripts.filter(function (script) {
                return stateApi.matchesScriptQuery(script, searchInput.value);
            });
            results.innerHTML = "";
            if (!visible.length) {
                results.innerHTML = "<p class=\"empty-state picker-empty\">No matching scripts found.</p>";
                return;
            }

            const table = document.createElement("table");
            table.className = "data";
            table.innerHTML = "<thead><tr><th>Title</th><th>Script</th><th><span class=\"sr-only\">Action</span></th></tr></thead>";
            const body = document.createElement("tbody");
            visible.forEach(function (script) {
                const row = document.createElement("tr");
                row.innerHTML =
                    "<td>" + escapeHtml(script.Title) + "</td>" +
                    "<td class=\"path-cell\">" + escapeHtml(script.ScriptName) + "</td>" +
                    "<td class=\"action-cell\"></td>";
                const addButton = document.createElement("button");
                addButton.type = "button";
                addButton.className = "primary";
                addButton.textContent = assignedIds[script.ScriptId] ? "Added" : "Add";
                addButton.disabled = !!assignedIds[script.ScriptId];
                addButton.addEventListener("click", async function () {
                    try {
                        addButton.disabled = true;
                        await api.post("api/sections.ashx?action=addItem", {
                            SectionId: section.SectionId,
                            ScriptId: script.ScriptId
                        });
                        picker.close();
                        await selectSection(section.SectionId);
                    } catch (error) {
                        addButton.disabled = false;
                        reportError(error);
                    }
                });
                row.lastElementChild.appendChild(addButton);
                body.appendChild(row);
            });
            table.appendChild(body);
            results.appendChild(table);
        }

        async function loadScripts() {
            results.innerHTML = "<p class=\"empty-state picker-empty\">Loading scripts…</p>";
            try {
                scripts = await api.get(
                    "api/scripts.ashx?scriptTy=" + encodeURIComponent(typeSelect.value) +
                    "&includeInactive=false"
                );
                renderResults();
            } catch (error) {
                results.innerHTML = "<p class=\"empty-state picker-empty\">Scripts could not be loaded.</p>";
                reportError(error);
            }
        }

        typeSelect.addEventListener("change", loadScripts);
        searchInput.addEventListener("input", renderResults);
        picker.content.appendChild(createArea);
        picker.content.appendChild(filters);
        picker.content.appendChild(results);
        loadScripts();
        searchInput.focus();
    }

    function openGrantPicker(section, existingGrants) {
        const picker = openPickerDialog("Add grant to " + section.SectionName);
        const form = document.createElement("form");
        form.className = "picker-filters";
        form.innerHTML =
            "<div class=\"field\"><label for=\"pickerPrincipalType\">Principal type</label><select id=\"pickerPrincipalType\" name=\"principalTy\"><option value=\"GROU\">Group</option><option value=\"USER\">User</option></select></div>" +
            "<div class=\"field picker-search\"><label for=\"pickerPrincipalSearch\">Find a user or group</label><input id=\"pickerPrincipalSearch\" name=\"query\" type=\"search\" required placeholder=\"Enter a name or username\"></div>" +
            "<div class=\"field\"><button type=\"submit\" class=\"primary\"><i class=\"fa fa-search\" aria-hidden=\"true\"></i> Search</button></div>";
        const results = document.createElement("div");
        results.className = "picker-results";
        results.innerHTML = "<p class=\"empty-state picker-empty\">Search by name or username. IDs are handled automatically.</p>";
        const principalTypeSelect = form.querySelector("[name=\"principalTy\"]");
        const principalSearchInput = form.querySelector("[name=\"query\"]");

        form.addEventListener("submit", async function (event) {
            event.preventDefault();
            results.innerHTML = "<p class=\"empty-state picker-empty\">Searching…</p>";
            try {
                const principals = await api.get(
                    "api/principals.ashx?q=" + encodeURIComponent(principalSearchInput.value) +
                    "&ty=" + encodeURIComponent(principalTypeSelect.value) +
                    "&limit=50"
                );
                renderPrincipalResults(principals);
            } catch (error) {
                results.innerHTML = "<p class=\"empty-state picker-empty\">The search could not be completed.</p>";
                reportError(error);
            }
        });

        function renderPrincipalResults(principals) {
            results.innerHTML = "";
            if (!principals.length) {
                results.innerHTML = "<p class=\"empty-state picker-empty\">No matching principals found.</p>";
                return;
            }
            const table = document.createElement("table");
            table.className = "data";
            table.innerHTML = "<thead><tr><th>Name</th><th>Username</th><th>Type</th><th><span class=\"sr-only\">Action</span></th></tr></thead>";
            const body = document.createElement("tbody");
            principals.forEach(function (principal) {
                const row = document.createElement("tr");
                const alreadyGranted = existingGrants.some(function (grant) {
                    return !grant.Inactive &&
                        grant.UserTy === principal.PrincipalTy &&
                        grant.UserId === principal.PrincipalId;
                });
                row.innerHTML =
                    "<td>" + escapeHtml(principal.DisplayName) + "</td>" +
                    "<td>" + escapeHtml(principal.UserName) + "</td>" +
                    "<td>" + escapeHtml(stateApi.principalTypeLabel(principal.PrincipalTy)) + "</td>" +
                    "<td class=\"action-cell\"></td>";
                const grantButton = document.createElement("button");
                grantButton.type = "button";
                grantButton.className = "primary";
                grantButton.textContent = alreadyGranted ? "Granted" : "Grant";
                grantButton.disabled = alreadyGranted;
                grantButton.addEventListener("click", async function () {
                    try {
                        grantButton.disabled = true;
                        await api.post("api/grants.ashx?action=create", {
                            SecureTy: "SECT",
                            SecureId: section.SectionId,
                            PrincipalTy: principal.PrincipalTy,
                            PrincipalId: principal.PrincipalId
                        });
                        picker.close();
                        await selectSection(section.SectionId);
                    } catch (error) {
                        grantButton.disabled = false;
                        reportError(error);
                    }
                });
                row.lastElementChild.appendChild(grantButton);
                body.appendChild(row);
            });
            table.appendChild(body);
            results.appendChild(table);
        }

        picker.content.appendChild(form);
        picker.content.appendChild(results);
        principalSearchInput.focus();
    }

    function openAccessExplorer() {
        const picker = openPickerDialog("Find access by person or group");
        const form = document.createElement("form");
        form.className = "picker-filters";
        form.innerHTML =
            "<div class=\"field\"><label for=\"accessPrincipalType\">Principal type</label><select id=\"accessPrincipalType\"><option value=\"GROU\">Group</option><option value=\"USER\">User</option></select></div>" +
            "<div class=\"field picker-search\"><label for=\"accessPrincipalSearch\">Name or username</label><input id=\"accessPrincipalSearch\" type=\"search\" required></div>" +
            "<div class=\"field\"><button type=\"submit\" class=\"primary\"><i class=\"fa fa-search\" aria-hidden=\"true\"></i> Search</button></div>";
        const typeSelect = form.querySelector("select");
        const searchInput = form.querySelector("input");
        const results = document.createElement("div");
        results.className = "picker-results";
        results.innerHTML = "<p class=\"empty-state picker-empty\">Search for a user or group to review all direct grants.</p>";

        form.addEventListener("submit", async function (event) {
            event.preventDefault();
            results.innerHTML = "<p class=\"empty-state picker-empty\">Searching…</p>";
            try {
                const principals = await api.get(
                    "api/principals.ashx?q=" + encodeURIComponent(searchInput.value) +
                    "&ty=" + encodeURIComponent(typeSelect.value) +
                    "&limit=50"
                );
                renderPrincipalChoices(principals);
            } catch (error) {
                results.innerHTML = "<p class=\"empty-state picker-empty\">The search could not be completed.</p>";
                reportError(error);
            }
        });

        function renderPrincipalChoices(principals) {
            results.innerHTML = "";
            if (!principals.length) {
                results.innerHTML = "<p class=\"empty-state picker-empty\">No matching users or groups found.</p>";
                return;
            }
            const table = document.createElement("table");
            table.className = "data";
            table.innerHTML = "<thead><tr><th>Name</th><th>Username</th><th>Type</th><th><span class=\"sr-only\">Action</span></th></tr></thead>";
            const body = document.createElement("tbody");
            principals.forEach(function (principal) {
                const row = document.createElement("tr");
                row.innerHTML =
                    "<td>" + escapeHtml(principal.DisplayName) + "</td>" +
                    "<td>" + escapeHtml(principal.UserName) + "</td>" +
                    "<td>" + escapeHtml(stateApi.principalTypeLabel(principal.PrincipalTy)) + "</td>" +
                    "<td class=\"action-cell\"></td>";
                const viewButton = document.createElement("button");
                viewButton.type = "button";
                viewButton.className = "primary";
                viewButton.textContent = "View access";
                viewButton.addEventListener("click", function () {
                    loadPrincipalAccess(principal);
                });
                row.lastElementChild.appendChild(viewButton);
                body.appendChild(row);
            });
            table.appendChild(body);
            results.appendChild(table);
        }

        async function loadPrincipalAccess(principal) {
            results.innerHTML = "<p class=\"empty-state picker-empty\">Loading access…</p>";
            try {
                const grants = await api.get(
                    "api/grants.ashx?principal=true" +
                    "&principalTy=" + encodeURIComponent(principal.PrincipalTy) +
                    "&principalId=" + encodeURIComponent(principal.PrincipalId) +
                    "&includeInactive=true"
                );
                renderPrincipalAccess(principal, grants);
            } catch (error) {
                results.innerHTML = "<p class=\"empty-state picker-empty\">Access could not be loaded.</p>";
                reportError(error);
            }
        }

        function renderPrincipalAccess(principal, grants) {
            results.innerHTML = "";
            const summary = document.createElement("div");
            summary.className = "access-summary";
            summary.innerHTML =
                "<div><strong>" + escapeHtml(principal.DisplayName || principal.UserName) + "</strong>" +
                "<p>Direct section and script grants</p></div>";
            const backButton = document.createElement("button");
            backButton.type = "button";
            backButton.innerHTML = "<i class=\"fa fa-arrow-left\" aria-hidden=\"true\"></i> Results";
            backButton.addEventListener("click", function () {
                form.dispatchEvent(new Event("submit", { cancelable: true }));
            });
            summary.appendChild(backButton);
            results.appendChild(summary);

            if (!grants.length) {
                const empty = document.createElement("p");
                empty.className = "empty-state picker-empty";
                empty.textContent = "No direct grants found.";
                results.appendChild(empty);
                return;
            }

            const table = document.createElement("table");
            table.className = "data";
            table.innerHTML = "<thead><tr><th>Target</th><th>Type</th><th>Status</th><th><span class=\"sr-only\">Action</span></th></tr></thead>";
            const body = document.createElement("tbody");
            grants.forEach(function (grant) {
                const row = document.createElement("tr");
                row.innerHTML =
                    "<td>" + escapeHtml(grant.SecureLabel || ("#" + grant.SecureId)) + "</td>" +
                    "<td>" + (grant.SecureTy === "SECT" ? "Section" : "Script") + "</td>" +
                    "<td>" + (grant.Inactive
                        ? "<span class=\"status-pill inactive\">Inactive</span>"
                        : "<span class=\"status-pill active\">Active</span>") + "</td>" +
                    "<td class=\"action-cell\"></td>";
                const toggle = document.createElement("button");
                toggle.type = "button";
                toggle.className = "icon-button";
                toggle.innerHTML = grant.Inactive
                    ? "<i class=\"fa fa-undo\" aria-hidden=\"true\"></i>"
                    : "<i class=\"fa fa-ban\" aria-hidden=\"true\"></i>";
                toggle.title = grant.Inactive ? "Activate grant" : "Deactivate grant";
                toggle.setAttribute("aria-label", toggle.title + " for " + grant.SecureLabel);
                toggle.addEventListener("click", async function () {
                    try {
                        await api.post(
                            "api/grants.ashx?action=" + (grant.Inactive ? "activate" : "deactivate"),
                            { AccessId: grant.AccessId, ExpectedUpdateNo: grant.UpdateNo }
                        );
                        await loadPrincipalAccess(principal);
                    } catch (error) {
                        reportError(error);
                    }
                });
                row.lastElementChild.appendChild(toggle);
                body.appendChild(row);
            });
            table.appendChild(body);
            results.appendChild(table);
        }

        picker.content.appendChild(form);
        picker.content.appendChild(results);
        searchInput.focus();
    }

    function openPickerDialog(titleText) {
        const existing = document.querySelector(".picker-dialog-backdrop");
        if (existing) {
            existing.remove();
        }

        const backdrop = document.createElement("div");
        backdrop.className = "dialog-backdrop picker-dialog-backdrop";
        const dialog = document.createElement("section");
        dialog.className = "dialog picker-dialog";
        dialog.setAttribute("role", "dialog");
        dialog.setAttribute("aria-modal", "true");
        dialog.setAttribute("aria-labelledby", "pickerDialogTitle");

        const header = document.createElement("header");
        header.className = "picker-dialog-header";
        const title = document.createElement("h2");
        title.id = "pickerDialogTitle";
        title.textContent = titleText;
        const closeButton = document.createElement("button");
        closeButton.type = "button";
        closeButton.className = "icon-button";
        closeButton.innerHTML = "<i class=\"fa fa-times\" aria-hidden=\"true\"></i>";
        closeButton.setAttribute("aria-label", "Close");
        const content = document.createElement("div");
        content.className = "picker-dialog-content";

        function close() {
            backdrop.remove();
        }

        closeButton.addEventListener("click", close);
        backdrop.addEventListener("click", function (event) {
            if (event.target === backdrop) {
                close();
            }
        });
        dialog.addEventListener("keydown", function (event) {
            if (event.key === "Escape") {
                close();
            }
        });

        header.appendChild(title);
        header.appendChild(closeButton);
        dialog.appendChild(header);
        dialog.appendChild(content);
        backdrop.appendChild(dialog);
        document.body.appendChild(backdrop);
        return { content: content, close: close };
    }

    async function refreshSections() {
        const appState = stateApi.get();
        const includeInactive = appState.includeInactiveSections;
        const sections = await api.get("api/sections.ashx?includeInactive=" + (includeInactive ? "true" : "false"));
        stateApi.set({
            workspace: Object.assign({}, appState.workspace || {}, { sections: sections })
        });
        if (appState.selectedSectionId) {
            await selectSection(appState.selectedSectionId);
        }
    }

    async function selectSection(sectionId) {
        const workspace = await api.get("api/workspace.ashx?sectionId=" + sectionId);
        const appState = stateApi.get();
        stateApi.set({
            selectedSectionId: sectionId,
            workspace: Object.assign({}, appState.workspace || {}, workspace)
        });
    }

    async function createSection() {
        const name = await dialogs.prompt({
            title: "New section",
            message: "Enter a unique section name.",
            promptLabel: "Section name"
        });
        if (!name) {
            return;
        }
        try {
            const section = await api.post("api/sections.ashx?action=create", {
                SectionName: name,
                ParentId: 0
            });
            await refreshSections();
            await selectSection(section.SectionId);
        } catch (error) {
            reportError(error);
        }
    }

    function beginSectionNameEdit(section, identity) {
        const form = document.createElement("form");
        form.className = "inline-edit inline-edit-section";
        form.innerHTML =
            "<label class=\"sr-only\" for=\"inlineSectionName\">Section name</label>" +
            "<input id=\"inlineSectionName\" name=\"sectionName\" required>" +
            "<button type=\"submit\" class=\"icon-button primary-icon\" title=\"Save\" aria-label=\"Save section name\"><i class=\"fa fa-check\" aria-hidden=\"true\"></i></button>" +
            "<button type=\"button\" class=\"icon-button cancel-edit\" title=\"Cancel\" aria-label=\"Cancel\"><i class=\"fa fa-times\" aria-hidden=\"true\"></i></button>";
        const input = form.querySelector("input");
        input.value = section.SectionName;
        identity.replaceChildren(form);
        input.focus();
        input.select();

        form.querySelector(".cancel-edit").addEventListener("click", function () {
            render(stateApi.get());
        });
        form.addEventListener("submit", async function (event) {
            event.preventDefault();
            const name = input.value.trim();
            if (!name || name === section.SectionName) {
                render(stateApi.get());
                return;
            }
            try {
                await api.post("api/sections.ashx?action=update", {
                    SectionId: section.SectionId,
                    SectionName: name,
                    ExpectedUpdateNo: section.UpdateNo
                });
                await refreshSections();
                await selectSection(section.SectionId);
            } catch (error) {
                reportError(error);
                render(stateApi.get());
            }
        });
    }

    async function toggleSection(section) {
        const action = section.Inactive ? "activate" : "deactivate";
        const confirmed = await dialogs.confirm({
            title: section.Inactive ? "Activate section" : "Deactivate section",
            message: "Change the active state for \"" + section.SectionName + "\"?"
        });
        if (!confirmed) {
            return;
        }
        try {
            await api.post("api/sections.ashx?action=" + action, {
                SectionId: section.SectionId,
                ExpectedUpdateNo: section.UpdateNo
            });
            await refreshSections();
            await selectSection(section.SectionId);
        } catch (error) {
            reportError(error);
        }
    }

    async function deleteSection(section) {
        try {
            const impact = await api.post("api/sections.ashx?action=deletePreview", {
                SectionId: section.SectionId,
                ExpectedUpdateNo: section.UpdateNo
            });
            const message = "Delete \"" + section.SectionName + "\" permanently?\n\n" +
                "Access rows: " + impact.AccessRowCount + "\n" +
                "Section-script rows: " + impact.SectionScriptRowCount + "\n" +
                "Child sections: " + impact.ChildSectionCount;
            const confirmed = await dialogs.confirm({
                title: "Delete section",
                message: message,
                danger: true,
                confirmLabel: "Delete permanently"
            });
            if (!confirmed) {
                return;
            }
            await api.post("api/sections.ashx?action=delete", {
                SectionId: section.SectionId,
                ExpectedUpdateNo: section.UpdateNo,
                Confirm: true
            });
            stateApi.set({ selectedSectionId: null });
            await refreshSections();
        } catch (error) {
            reportError(error);
        }
    }

    async function removeItem(section, item) {
        const confirmed = await dialogs.confirm({
            title: "Remove script",
            message: "Remove \"" + item.ScriptName + "\" from this section?"
        });
        if (!confirmed) {
            return;
        }
        try {
            await api.post("api/sections.ashx?action=removeItem", {
                SectionId: section.SectionId,
                ScriptId: item.ScriptId
            });
            await selectSection(section.SectionId);
        } catch (error) {
            reportError(error);
        }
    }

    async function toggleGrant(grant, action) {
        try {
            await api.post("api/grants.ashx?action=" + action, {
                AccessId: grant.AccessId,
                ExpectedUpdateNo: grant.UpdateNo
            });
            await selectSection(stateApi.get().selectedSectionId);
        } catch (error) {
            reportError(error);
        }
    }

    async function handleSectionReorder(index, newPosition) {
        const appState = stateApi.get();
        const sections = (appState.workspace && appState.workspace.sections) || [];
        const activeSections = sections.filter(function (section) {
            return !section.Inactive;
        });
        const section = activeSections[index];
        if (!section) {
            return;
        }
        try {
            await api.post("api/sections.ashx?action=reorder", {
                SectionId: section.SectionId,
                NewPosition: newPosition,
                ExpectedUpdateNo: section.UpdateNo
            });
            await refreshSections();
        } catch (error) {
            reportError(error);
        }
    }

    async function handleItemReorder(index, newPosition) {
        const appState = stateApi.get();
        const items = (((appState.workspace || {}).sectionDetail || {}).items) || [];
        const item = items[index];
        if (!item) {
            return;
        }
        try {
            await api.post("api/sections.ashx?action=reorderItem", {
                SectionId: item.SectionId,
                ScriptId: item.ScriptId,
                NewPosition: newPosition
            });
            await selectSection(item.SectionId);
        } catch (error) {
            reportError(error);
        }
    }

    function reportError(error) {
        if (typeof onError === "function") {
            onError(error);
        }
    }

    function escapeHtml(value) {
        return String(value || "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;");
    }

    global.AccessManagerSectionsView = {
        init: init,
        render: render,
        refresh: refreshSections,
        selectSection: selectSection
    };
}(window));
