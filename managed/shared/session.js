"use strict";

(function (global) {
    let session = null;
    let sessionUrl = "api/session.ashx";

    function configure(options) {
        if (options && options.sessionUrl) {
            sessionUrl = options.sessionUrl;
        }
    }

    async function loadSession(url) {
        const target = url || sessionUrl;
        const data = await global.PilotApiClient.get(target);
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
        configure: configure,
        load: loadSession,
        get: getSession,
        capabilities: getCapabilities,
        paths: getPaths,
        can: can
    };
}(window));
