"use strict";

(function () {
    const form = document.getElementById("loginForm");
    const userName = document.getElementById("userName");
    const password = document.getElementById("password");
    const passwordToggle = document.getElementById("passwordToggle");
    const passwordSlash = document.getElementById("passwordSlash");
    const loginButton = document.getElementById("loginButton");
    const errorMessage = document.getElementById("errorMessage");
    const requestedReturnUrl = new URLSearchParams(window.location.search).get("returnUrl") || "";
    let csrfToken = "";

    function showError(message) {
        errorMessage.textContent = message;
        errorMessage.hidden = false;
    }

    function clearError() {
        errorMessage.textContent = "";
        errorMessage.hidden = true;
    }

    async function readJson(response) {
        const contentType = response.headers.get("content-type") || "";
        if (!contentType.toLowerCase().includes("application/json")) {
            throw new Error("The sign-in service returned an unexpected response.");
        }
        return response.json();
    }

    async function initialize() {
        try {
            const response = await fetch("login.ashx", {
                method: "GET",
                credentials: "same-origin",
                cache: "no-store",
                headers: { "Accept": "application/json" }
            });
            const result = await readJson(response);

            if (!response.ok) {
                throw new Error(result.error || "The sign-in service is temporarily unavailable.");
            }
            if (result.redirectUrl) {
                window.location.replace(result.redirectUrl);
                return;
            }

            csrfToken = result.csrfToken || "";
            if (!csrfToken) {
                throw new Error("The sign-in service did not provide a security token.");
            }
            loginButton.disabled = false;
        } catch (error) {
            showError(error.message || "The sign-in service is temporarily unavailable.");
        }
    }

    passwordToggle.addEventListener("click", function () {
        const reveal = password.type === "password";
        password.type = reveal ? "text" : "password";
        passwordToggle.setAttribute("aria-pressed", reveal ? "true" : "false");
        passwordToggle.setAttribute("aria-label", reveal ? "Hide password" : "Show password");
        passwordSlash.hidden = reveal;
        password.focus();
    });

    form.addEventListener("submit", async function (event) {
        event.preventDefault();
        clearError();

        if (!form.reportValidity() || !csrfToken) {
            return;
        }

        loginButton.disabled = true;
        loginButton.textContent = "Signing in…";

        try {
            const response = await fetch("login.ashx", {
                method: "POST",
                credentials: "same-origin",
                cache: "no-store",
                headers: {
                    "Accept": "application/json",
                    "Content-Type": "application/json",
                    "X-CSRF-Token": csrfToken
                },
                body: JSON.stringify({
                    userName: userName.value,
                    password: password.value,
                    returnUrl: requestedReturnUrl
                })
            });
            const result = await readJson(response);

            if (!response.ok || !result.redirectUrl) {
                csrfToken = result.csrfToken || csrfToken;
                throw new Error(result.error || "The username or password is not valid for this pilot.");
            }

            window.location.assign(result.redirectUrl);
        } catch (error) {
            password.value = "";
            showError(error.message || "The sign-in service is temporarily unavailable.");
            password.focus();
            loginButton.disabled = false;
            loginButton.textContent = "Sign in";
        }
    });

    initialize();
}());
