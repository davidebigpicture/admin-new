"use strict";

(function (global) {
    let activeDialog = null;

    function closeDialog() {
        if (activeDialog && activeDialog.parentNode) {
            activeDialog.parentNode.removeChild(activeDialog);
        }
        activeDialog = null;
    }

    function openDialog(options) {
        closeDialog();

        const backdrop = document.createElement("div");
        backdrop.className = "dialog-backdrop";
        backdrop.setAttribute("role", "presentation");

        const dialog = document.createElement("div");
        dialog.className = "dialog";
        dialog.setAttribute("role", "dialog");
        dialog.setAttribute("aria-modal", "true");
        dialog.setAttribute("aria-labelledby", "admin-shell-dialog-title");

        const title = document.createElement("h2");
        title.id = "admin-shell-dialog-title";
        title.textContent = options.title || "Confirm";

        const message = document.createElement("p");
        message.textContent = options.message || "";

        const actions = document.createElement("div");
        actions.className = "dialog-actions admin-actions admin-actions--end";

        const cancelButton = document.createElement("button");
        cancelButton.type = "button";
        cancelButton.className = "admin-action admin-action--secondary";
        cancelButton.textContent = options.cancelLabel || "Cancel";

        const confirmButton = document.createElement("button");
        confirmButton.type = "button";
        confirmButton.className = options.danger
            ? "admin-action admin-action--danger"
            : "admin-action admin-action--primary";
        confirmButton.textContent = options.confirmLabel || "OK";

        actions.appendChild(cancelButton);
        actions.appendChild(confirmButton);
        dialog.appendChild(title);
        dialog.appendChild(message);

        if (options.promptLabel) {
            const field = document.createElement("div");
            field.className = "field";
            const label = document.createElement("label");
            label.setAttribute("for", "admin-shell-dialog-input");
            label.textContent = options.promptLabel;
            const input = document.createElement("input");
            input.id = "admin-shell-dialog-input";
            input.type = "text";
            input.value = options.defaultValue || "";
            field.appendChild(label);
            field.appendChild(input);
            dialog.appendChild(field);
        }

        dialog.appendChild(actions);
        backdrop.appendChild(dialog);
        document.body.appendChild(backdrop);
        activeDialog = backdrop;

        function resolve(value) {
            closeDialog();
            if (typeof options.onClose === "function") {
                options.onClose(value);
            }
        }

        cancelButton.addEventListener("click", function () {
            resolve(options.promptLabel ? null : false);
        });
        confirmButton.addEventListener("click", function () {
            if (options.promptLabel) {
                const input = dialog.querySelector("#admin-shell-dialog-input");
                resolve(input ? input.value : "");
                return;
            }
            resolve(true);
        });

        backdrop.addEventListener("click", function (event) {
            if (event.target === backdrop) {
                resolve(options.promptLabel ? null : false);
            }
        });

        dialog.addEventListener("keydown", function (event) {
            if (event.key === "Escape") {
                event.preventDefault();
                resolve(options.promptLabel ? null : false);
            }
        });

        const focusTarget = dialog.querySelector("#admin-shell-dialog-input") || confirmButton;
        focusTarget.focus();
        if (focusTarget.select) {
            focusTarget.select();
        }

        return {
            close: closeDialog
        };
    }

    function confirm(options) {
        return new Promise(function (resolve) {
            openDialog(Object.assign({}, options, {
                onClose: resolve
            }));
        });
    }

    function prompt(options) {
        return new Promise(function (resolve) {
            openDialog(Object.assign({}, options, {
                onClose: resolve
            }));
        });
    }

    global.AdminShellDialogs = {
        confirm: confirm,
        prompt: prompt,
        close: closeDialog
    };
}(window));
