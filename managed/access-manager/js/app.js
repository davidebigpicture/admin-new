"use strict";

(function () {
    const stateApi = window.AccessManagerState;
    const sectionsView = window.AccessManagerSectionsView;

    const messageEl = document.getElementById("appMessage");
    const userEl = document.getElementById("shellUser");
    const userNameEl = document.getElementById("shellUserName");
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

        window.PilotShell.bindLogout(document.getElementById("logoutButton"));

        try {
            const session = await window.PilotSession.load();
            const workspace = await window.PilotApiClient.get("api/workspace.ashx");
            stateApi.set({
                session: session,
                workspace: workspace,
                selectedScriptType: workspace.defaultScriptType || ""
            });

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
