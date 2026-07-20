"use strict";

(function (global) {
    let csrfToken = "";
    let loginUrl = "../login.html";
    let apiBase = "";

    function setApiBase(base) {
        apiBase = base || "";
    }

    function resolveUrl(url) {
        if (!url || url.indexOf("://") >= 0 || url.charAt(0) === "/") {
            return url;
        }
        return apiBase + url;
    }

    function setLoginUrl(url) {
        if (url) {
            loginUrl = url;
        }
    }

    function setCsrfToken(token) {
        csrfToken = token || "";
    }

    function getCsrfToken() {
        return csrfToken;
    }

    function buildReturnUrl() {
        return encodeURIComponent(global.location.pathname + global.location.search);
    }

    function redirectToLogin() {
        const separator = loginUrl.indexOf("?") >= 0 ? "&" : "?";
        global.location.assign(loginUrl + separator + "returnUrl=" + buildReturnUrl());
    }

    function refreshSessionTimer() {
        if (global.ManagedShell && typeof global.ManagedShell.refreshSessionTimer === "function") {
            global.ManagedShell.refreshSessionTimer();
        }
    }

    async function readJson(response) {
        const contentType = response.headers.get("content-type") || "";
        if (!contentType.toLowerCase().includes("application/json")) {
            throw new Error("The service returned an unexpected response.");
        }
        return response.json();
    }

    async function request(url, options) {
        const opts = Object.assign(
            {
                credentials: "same-origin",
                cache: "no-store",
                headers: {
                    Accept: "application/json"
                }
            },
            options || {}
        );

        opts.headers = Object.assign({}, opts.headers || {});
        const method = (opts.method || "GET").toUpperCase();
        if (method !== "GET" && method !== "HEAD") {
            if (!opts.headers["Content-Type"]) {
                opts.headers["Content-Type"] = "application/json";
            }
            if (csrfToken) {
                opts.headers["X-CSRF-Token"] = csrfToken;
            }
        }

        const response = await fetch(resolveUrl(url), opts);
        const payload = await readJson(response);

        if (response.status === 401) {
            const nextLogin = payload && payload.data && payload.data.loginUrl;
            if (nextLogin) {
                setLoginUrl(nextLogin);
            }
            redirectToLogin();
            throw new Error("Authentication is required.");
        }

        if (payload && payload.csrfToken) {
            setCsrfToken(payload.csrfToken);
        }

        refreshSessionTimer();

        if (!response.ok || !payload || payload.ok !== true) {
            const message = (payload && payload.error) || "The request could not be completed.";
            const error = new Error(message);
            error.status = response.status;
            throw error;
        }

        return payload.data;
    }

    function get(url) {
        return request(url, { method: "GET" });
    }

    function post(url, body) {
        return request(url, {
            method: "POST",
            body: JSON.stringify(body || {})
        });
    }

    global.PilotApiClient = {
        get: get,
        post: post,
        request: request,
        setApiBase: setApiBase,
        setCsrfToken: setCsrfToken,
        getCsrfToken: getCsrfToken,
        setLoginUrl: setLoginUrl,
        redirectToLogin: redirectToLogin
    };
}(window));
