"use strict";

(function (global) {
    const api = global.PilotApiClient;
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
        if (!caps.canManageGrants) {
            container.innerHTML = "<p class=\"empty-state\">You do not have permission to manage grants.</p>";
            return;
        }

        container.innerHTML = "";

        const panel = document.createElement("section");
        panel.className = "panel";
        panel.innerHTML = "<h2>Effective access lookup</h2>";
        panel.appendChild(renderLookupForm(appState.effectiveAccess));
        if (appState.effectiveAccess) {
            panel.appendChild(renderEffectiveAccess(appState.effectiveAccess));
        }
        container.appendChild(panel);

        const grantsPanel = document.createElement("section");
        grantsPanel.className = "panel";
        grantsPanel.innerHTML = "<h2>Target grants</h2>";
        grantsPanel.appendChild(renderTargetForm(appState.grantTarget, appState.targetGrants || []));
        container.appendChild(grantsPanel);
    }

    function renderLookupForm(result) {
        const form = document.createElement("form");
        form.className = "lookup-grid";
        form.innerHTML =
            "<div class=\"field\"><label for=\"lookupPrincipalTy\">Principal type</label><select id=\"lookupPrincipalTy\" name=\"principalTy\"><option value=\"USER\">User</option><option value=\"GROU\">Group</option></select></div>" +
            "<div class=\"field\"><label for=\"lookupPrincipalId\">Principal ID</label><input id=\"lookupPrincipalId\" name=\"principalId\" type=\"number\" min=\"1\" required></div>" +
            "<div class=\"field\"><label for=\"lookupScriptId\">Script ID</label><input id=\"lookupScriptId\" name=\"scriptId\" type=\"number\" min=\"1\" required></div>" +
            "<div class=\"field\"><button type=\"submit\" class=\"primary\">Lookup</button></div>";

        if (result) {
            form.principalTy.value = result.PrincipalTy || "USER";
            form.principalId.value = result.PrincipalId || "";
            form.scriptId.value = result.ScriptId || "";
        }

        form.addEventListener("submit", async function (event) {
            event.preventDefault();
            try {
                const data = await api.get(
                    "api/grants.ashx?effective=true" +
                    "&principalTy=" + encodeURIComponent(form.principalTy.value) +
                    "&principalId=" + encodeURIComponent(form.principalId.value) +
                    "&scriptId=" + encodeURIComponent(form.scriptId.value)
                );
                stateApi.set({ effectiveAccess: data });
            } catch (error) {
                reportError(error);
            }
        });

        return form;
    }

    function renderEffectiveAccess(result) {
        const wrapper = document.createElement("div");
        const summary = document.createElement("p");
        summary.innerHTML = "<strong>" + escapeHtml(result.ScriptName || ("Script #" + result.ScriptId)) + "</strong> · " +
            (result.HasEffectiveAccess ? "<span class=\"status-pill active\">Effective access</span>" : "<span class=\"status-pill inactive\">No effective access</span>");
        wrapper.appendChild(summary);

        wrapper.appendChild(renderGrantGroup("Direct script grants", result.DirectScriptGrants));
        wrapper.appendChild(renderGrantGroup("Direct section grants", result.DirectSectionGrants));
        wrapper.appendChild(renderGrantGroup("Inherited section grants", result.InheritedSectionGrants));
        return wrapper;
    }

    function renderGrantGroup(title, grants) {
        const group = document.createElement("div");
        group.innerHTML = "<h3>" + escapeHtml(title) + "</h3>";
        if (!grants || !grants.length) {
            group.innerHTML += "<p class=\"empty-state\">None</p>";
            return group;
        }

        const list = document.createElement("ul");
        grants.forEach(function (grant) {
            const item = document.createElement("li");
            item.textContent = (grant.SectionName || grant.SecureTy) + " · " + (grant.PrincipalLabel || grant.PrincipalId);
            list.appendChild(item);
        });
        group.appendChild(list);
        return group;
    }

    function renderTargetForm(target, grants) {
        const form = document.createElement("form");
        form.className = "lookup-grid";
        form.innerHTML =
            "<div class=\"field\"><label for=\"targetSecureTy\">Secure type</label><select id=\"targetSecureTy\" name=\"secureTy\"><option value=\"SECT\">Section</option><option value=\"SCRI\">Script</option></select></div>" +
            "<div class=\"field\"><label for=\"targetSecureId\">Secure ID</label><input id=\"targetSecureId\" name=\"secureId\" type=\"number\" min=\"1\" required></div>" +
            "<div class=\"field\"><button type=\"submit\" class=\"primary\">Load grants</button></div>";

        if (target) {
            form.secureTy.value = target.secureTy || "SECT";
            form.secureId.value = target.secureId || "";
        }

        form.addEventListener("submit", async function (event) {
            event.preventDefault();
            try {
                const secureTy = form.secureTy.value;
                const secureId = parseInt(form.secureId.value, 10);
                const rows = await api.get(
                    "api/grants.ashx?secureTy=" + encodeURIComponent(secureTy) +
                    "&secureId=" + encodeURIComponent(secureId) +
                    "&includeInactive=true"
                );
                stateApi.set({
                    grantTarget: { secureTy: secureTy, secureId: secureId },
                    targetGrants: rows
                });
            } catch (error) {
                reportError(error);
            }
        });

        const table = renderGrantsTable(grants);
        const wrapper = document.createElement("div");
        wrapper.appendChild(form);
        wrapper.appendChild(table);
        return wrapper;
    }

    function renderGrantsTable(grants) {
        const wrap = document.createElement("div");
        wrap.className = "table-wrap";
        const table = document.createElement("table");
        table.className = "data";
        table.innerHTML = "<thead><tr><th>Principal</th><th>Type</th><th>Status</th><th>Actions</th></tr></thead>";
        const body = document.createElement("tbody");

        (grants || []).forEach(function (grant) {
            const row = document.createElement("tr");
            if (grant.Inactive) {
                row.className = "inactive";
            }
            row.innerHTML =
                "<td>" + escapeHtml(grant.PrincipalLabel || ("#" + grant.UserId)) + "</td>" +
                "<td>" + escapeHtml(grant.UserTy) + "</td>" +
                "<td>" + (grant.Inactive
                    ? "<span class=\"status-pill inactive\">Inactive</span>"
                    : "<span class=\"status-pill active\">Active</span>") + "</td>" +
                "<td class=\"action-cell\"></td>";
            const actions = row.lastElementChild;
            const toggle = document.createElement("button");
            toggle.type = "button";
            toggle.textContent = grant.Inactive ? "Activate" : "Deactivate";
            toggle.addEventListener("click", async function () {
                try {
                    const action = grant.Inactive ? "activate" : "deactivate";
                    await api.post("api/grants.ashx?action=" + action, {
                        AccessId: grant.AccessId,
                        ExpectedUpdateNo: grant.UpdateNo
                    });
                    const appState = stateApi.get();
                    if (appState.grantTarget) {
                        const rows = await api.get(
                            "api/grants.ashx?secureTy=" + encodeURIComponent(appState.grantTarget.secureTy) +
                            "&secureId=" + encodeURIComponent(appState.grantTarget.secureId) +
                            "&includeInactive=true"
                        );
                        stateApi.set({ targetGrants: rows });
                    }
                } catch (error) {
                    reportError(error);
                }
            });
            actions.appendChild(toggle);
            body.appendChild(row);
        });

        table.appendChild(body);
        wrap.appendChild(table);
        if (!grants || !grants.length) {
            wrap.innerHTML = "<p class=\"empty-state\">Load a target to review its grants.</p>";
        }
        return wrap;
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

    global.AccessManagerAccessView = {
        init: init,
        render: render
    };
}(window));
