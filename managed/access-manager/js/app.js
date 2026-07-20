"use strict";

(function () {
    const stateApi = window.AccessManagerState;
    const sectionsView = window.AccessManagerSectionsView;

    const messageEl = document.getElementById("appMessage");
    const sectionsPanel = document.getElementById("viewSections");

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

    async function bootstrap() {
        sectionsView.init({ container: sectionsPanel, onError: handleError });

        try {
            const session = await window.ManagedShell.initialize({ sessionUrl: "api/session.ashx", apiBase: "" });
            const workspace = await window.PilotApiClient.get("api/workspace.ashx");
            stateApi.set({
                session: session,
                workspace: workspace,
                selectedScriptType: workspace.defaultScriptType || ""
            });

            sectionsView.render(stateApi.get());
            showMessage("");
        } catch (error) {
            handleError(error);
        }
    }

    stateApi.subscribe(function () {
        sectionsView.render(stateApi.get());
    });

    bootstrap();
}());
