"use strict";

(function (global) {
    function bindReorderButtons(container, options) {
        if (!container) {
            return;
        }

        container.addEventListener("keydown", function (event) {
            if (event.key !== "ArrowUp" && event.key !== "ArrowDown") {
                return;
            }
            const row = event.target.closest("[data-reorder-index]");
            if (!row) {
                return;
            }
            event.preventDefault();
            const index = parseInt(row.getAttribute("data-reorder-index"), 10);
            const direction = event.key === "ArrowUp" ? -1 : 1;
            const orderContainer = row.closest("[data-reorder-count]");
            const count = parseInt((orderContainer && orderContainer.getAttribute("data-reorder-count")) || "0", 10);
            const position = global.AccessManagerState.computeReorderPosition(index, direction, count);
            if (typeof options.onReorder === "function") {
                options.onReorder(index, position, row);
            }
        });

        bindDragAndDrop(container, options);
    }

    function bindDragAndDrop(container, options) {
        let sourceRow = null;
        let targetRow = null;
        let dropAfter = false;

        container.addEventListener("dragstart", function (event) {
            const handle = event.target.closest(".drag-handle");
            if (!handle) {
                return;
            }
            sourceRow = handle.closest("[data-reorder-index]");
            if (!sourceRow) {
                event.preventDefault();
                return;
            }
            sourceRow.classList.add("is-dragging");
            event.dataTransfer.effectAllowed = "move";
            event.dataTransfer.setData("text/plain", sourceRow.getAttribute("data-reorder-index"));
        });

        container.addEventListener("dragover", function (event) {
            if (!sourceRow) {
                return;
            }
            const candidate = event.target.closest("[data-reorder-index]");
            if (!candidate ||
                candidate === sourceRow ||
                candidate.closest("[data-reorder-count]") !== sourceRow.closest("[data-reorder-count]")) {
                return;
            }

            event.preventDefault();
            clearDropTarget(targetRow);
            targetRow = candidate;
            const bounds = candidate.getBoundingClientRect();
            dropAfter = event.clientY >= bounds.top + (bounds.height / 2);
            candidate.classList.add(dropAfter ? "drop-after" : "drop-before");
            event.dataTransfer.dropEffect = "move";
        });

        container.addEventListener("drop", function (event) {
            if (!sourceRow || !targetRow) {
                return;
            }
            event.preventDefault();

            const sourceIndex = parseInt(sourceRow.getAttribute("data-reorder-index"), 10);
            const targetIndex = parseInt(targetRow.getAttribute("data-reorder-index"), 10);
            const newPosition = global.AccessManagerState.computeDropPosition(
                sourceIndex,
                targetIndex,
                dropAfter
            );

            if (newPosition !== sourceIndex + 1 && typeof options.onReorder === "function") {
                options.onReorder(sourceIndex, newPosition, sourceRow);
            }
            resetDrag();
        });

        container.addEventListener("dragend", resetDrag);

        function resetDrag() {
            if (sourceRow) {
                sourceRow.classList.remove("is-dragging");
            }
            clearDropTarget(targetRow);
            sourceRow = null;
            targetRow = null;
            dropAfter = false;
        }

        function clearDropTarget(row) {
            if (row) {
                row.classList.remove("drop-before", "drop-after");
            }
        }
    }

    function renderReorderControls() {
        const wrapper = document.createElement("span");
        wrapper.className = "reorder-controls";
        wrapper.setAttribute("role", "group");
        wrapper.setAttribute("aria-label", "Reorder");

        const handle = document.createElement("button");
        handle.type = "button";
        handle.className = "drag-handle admin-action admin-action--quiet admin-action--icon";
        handle.draggable = true;
        handle.innerHTML = "<i class=\"fa fa-bars\" aria-hidden=\"true\"></i>";
        handle.title = "Drag to reorder";
        handle.setAttribute("aria-label", "Drag to reorder");

        wrapper.appendChild(handle);
        return wrapper;
    }

    global.AccessManagerReorder = {
        bind: bindReorderButtons,
        renderControls: renderReorderControls
    };
}(window));
