"use strict";

(function (global) {
    let session = null;

    async function loadSession() {
        const data = await global.PilotApiClient.get("api/session.ashx");
        global.PilotApiClient.setCsrfToken(data.csrfToken);
        if (data.paths && data.paths.loginUrl) {
            global.PilotApiClient.setLoginUrl(data.paths.loginUrl);
        }
        session = data;
        return session;
    }

    function getSession() {
        return session;
    }

    function getCapabilities() {
        return (session && session.capabilities) || {};
    }

    function getPaths() {
        return (session && session.paths) || {};
    }

    function can(capabilityKey) {
        const caps = getCapabilities();
        return !!caps[capabilityKey];
    }

    global.PilotSession = {
        load: loadSession,
        get: getSession,
        capabilities: getCapabilities,
        paths: getPaths,
        can: can
    };
}(window));
