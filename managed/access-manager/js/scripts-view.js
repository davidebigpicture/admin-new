"use strict";

(function (global) {
    const api = global.PilotApiClient;
    const dialogs = global.PilotDialogs;
    const stateApi = global.AccessManagerState;

    let container = null;
    let onError = null;

    function init(options) {
        container = options.container;
        onError = options.onError;
    }

    function render(appState) {
        if (!container) {
            return;
        }

        const caps = (appState.session && appState.session.capabilities) || {};
        if (!caps.canManageScripts) {
            container.innerHTML = "<p class=\"empty-state\">You do not have permission to manage scripts.</p>";
            return;
        }

        const scriptTypes = (appState.workspace && appState.workspace.scriptTypes) || [];
        const defaultType = appState.selectedScriptType || (appState.workspace && appState.workspace.defaultScriptType) || "";
        const scripts = appState.scripts || [];

        container.innerHTML = "";

        const panel = document.createElement("section");
        panel.className = "panel";
        panel.innerHTML = "<h2>Scripts</h2>";
        panel.appendChild(renderFilters(defaultType, scriptTypes, appState.includeInactiveScripts));

        if (caps.canManageScripts) {
            panel.appendChild(renderCreateForm(defaultType, scriptTypes));
        }

        panel.appendChild(renderTable(scripts, caps));
        container.appendChild(panel);
    }

    function renderFilters(selectedType, scriptTypes, includeInactive) {
        const form = document.createElement("form");
        form.className = "filters";
        form.innerHTML =
            "<div class=\"field\"><label for=\"scriptTypeFilter\">Script type</label><select id=\"scriptTypeFilter\" name=\"scriptTy\"></select></div>" +
            "<div class=\"field\"><label><input type=\"checkbox\" name=\"includeInactive\" " + (includeInactive ? "checked" : "") + "> Show inactive</label></div>" +
            "<div class=\"field\"><button type=\"submit\" class=\"primary\">Refresh</button></div>";

        const select = form.querySelector("select");
        scriptTypes.forEach(function (type) {
            const option = document.createElement("option");
            option.value = type.CodeValue;
            option.textContent = type.CodeValueDesc || type.CodeValue;
            if (type.CodeValue === selectedType) {
                option.selected = true;
            }
            select.appendChild(option);
        });

        form.addEventListener("submit", async function (event) {
            event.preventDefault();
            stateApi.set({
                selectedScriptType: form.scriptTy.value,
                includeInactiveScripts: form.includeInactive.checked
            });
            await refreshScripts();
        });

        return form;
    }

    function renderCreateForm(defaultType, scriptTypes) {
        const form = document.createElement("form");
        form.className = "inline-form";
        form.innerHTML =
            "<div class=\"field\"><label for=\"createScriptTy\">Type</label><select id=\"createScriptTy\" name=\"scriptTy\"></select></div>" +
            "<div class=\"field\"><label for=\"createScriptName\">Path</label><input id=\"createScriptName\" name=\"scriptName\" required placeholder=\"/admin/admin/example.asp\"></div>" +
            "<div class=\"field\"><label for=\"createScriptTitle\">Title</label><input id=\"createScriptTitle\" name=\"title\" required></div>" +
            "<div class=\"field\"><button type=\"submit\" class=\"primary\">Create script</button></div>";

        const select = form.querySelector("select");
        scriptTypes.forEach(function (type) {
            const option = document.createElement("option");
            option.value = type.CodeValue;
            option.textContent = type.CodeValueDesc || type.CodeValue;
            if (type.CodeValue === defaultType) {
                option.selected = true;
            }
            select.appendChild(option);
        });

        form.addEventListener("submit", async function (event) {
            event.preventDefault();
            try {
                await api.post("api/scripts.ashx?action=create", {
                    ScriptTy: form.scriptTy.value,
                    ScriptName: form.scriptName.value,
                    Title: form.title.value
                });
                form.reset();
                await refreshScripts();
            } catch (error) {
                reportError(error);
            }
        });

        return form;
    }

    function renderTable(scripts, caps) {
        const wrap = document.createElement("div");
        wrap.className = "table-wrap";
        const table = document.createElement("table");
        table.className = "data";
        table.innerHTML = "<thead><tr><th>Path</th><th>Title</th><th>Type</th><th>Status</th><th>Actions</th></tr></thead>";
        const body = document.createElement("tbody");

        scripts.forEach(function (script) {
            const row = document.createElement("tr");
            if (script.Inactive) {
                row.className = "inactive";
            }
            row.innerHTML =
                "<td class=\"path-cell\">" + escapeHtml(script.ScriptName) + "</td>" +
                "<td>" + escapeHtml(script.Title) + "</td>" +
                "<td>" + escapeHtml(script.ScriptTy) + "</td>" +
                "<td>" + (script.Inactive
                    ? "<span class=\"status-pill inactive\">Inactive</span>"
                    : "<span class=\"status-pill active\">Active</span>") + "</td>" +
                "<td class=\"action-cell\"></td>";
            const actions = row.lastElementChild;

            const editButton = document.createElement("button");
            editButton.type = "button";
            editButton.textContent = "Edit";
            editButton.addEventListener("click", function () {
                editScript(script);
            });
            actions.appendChild(editButton);

            const lifecycleButton = document.createElement("button");
            lifecycleButton.type = "button";
            lifecycleButton.textContent = script.Inactive ? "Activate" : "Deactivate";
            lifecycleButton.addEventListener("click", function () {
                toggleScript(script);
            });
            actions.appendChild(lifecycleButton);

            const deleteButton = document.createElement("button");
            deleteButton.type = "button";
            deleteButton.className = "danger";
            deleteButton.textContent = "Delete";
            deleteButton.addEventListener("click", function () {
                deleteScript(script);
            });
            actions.appendChild(deleteButton);

            body.appendChild(row);
        });

        if (!scripts.length) {
            const empty = document.createElement("p");
            empty.className = "empty-state";
            empty.textContent = "No scripts found for the selected type.";
            return empty;
        }

        table.appendChild(body);
        wrap.appendChild(table);
        return wrap;
    }

    async function refreshScripts() {
        const appState = stateApi.get();
        const scriptTy = appState.selectedScriptType || appState.workspace.defaultScriptType;
        const includeInactive = appState.includeInactiveScripts;
        const scripts = await api.get(
            "api/scripts.ashx?scriptTy=" + encodeURIComponent(scriptTy) +
            "&includeInactive=" + (includeInactive ? "true" : "false")
        );
        stateApi.set({ scripts: scripts, selectedScriptType: scriptTy });
    }

    async function editScript(script) {
        const title = await dialogs.prompt({
            title: "Edit script",
            message: "Update the script title.",
            promptLabel: "Title",
            defaultValue: script.Title
        });
        if (!title) {
            return;
        }
        const path = await dialogs.prompt({
            title: "Edit script",
            message: "Update the script path.",
            promptLabel: "Path",
            defaultValue: script.ScriptName
        });
        if (!path) {
            return;
        }
        try {
            await api.post("api/scripts.ashx?action=update", {
                ScriptId: script.ScriptId,
                ScriptTy: script.ScriptTy,
                ScriptName: path,
                Title: title,
                ExpectedUpdateNo: script.UpdateNo
            });
            await refreshScripts();
        } catch (error) {
            reportError(error);
        }
    }

    async function toggleScript(script) {
        const action = script.Inactive ? "activate" : "deactivate";
        const confirmed = await dialogs.confirm({
            title: script.Inactive ? "Activate script" : "Deactivate script",
            message: "Change the active state for \"" + script.ScriptName + "\"?"
        });
        if (!confirmed) {
            return;
        }
        try {
            await api.post("api/scripts.ashx?action=" + action, {
                ScriptId: script.ScriptId,
                ExpectedUpdateNo: script.UpdateNo
            });
            await refreshScripts();
        } catch (error) {
            reportError(error);
        }
    }

    async function deleteScript(script) {
        try {
            const impact = await api.post("api/scripts.ashx?action=deletePreview", {
                ScriptId: script.ScriptId,
                ExpectedUpdateNo: script.UpdateNo
            });
            const confirmed = await dialogs.confirm({
                title: "Delete script",
                message: "Delete \"" + script.ScriptName + "\" permanently?\n\nAccess rows: " +
                    impact.AccessRowCount + "\nSection-script rows: " + impact.SectionScriptRowCount,
                danger: true,
                confirmLabel: "Delete permanently"
            });
            if (!confirmed) {
                return;
            }
            await api.post("api/scripts.ashx?action=delete", {
                ScriptId: script.ScriptId,
                ExpectedUpdateNo: script.UpdateNo,
                Confirm: true
            });
            await refreshScripts();
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

    global.AccessManagerScriptsView = {
        init: init,
        render: render,
        refresh: refreshScripts
    };
}(window));
