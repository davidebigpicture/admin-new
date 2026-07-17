"use strict";

(function (global) {
    const state = {
        session: null,
        workspace: null,
        activeView: "sections",
        selectedSectionId: null,
        selectedScriptType: "",
        includeInactiveSections: false,
        includeInactiveScripts: false
    };

    const listeners = [];

    function clone(value) {
        return JSON.parse(JSON.stringify(value));
    }

    function getState() {
        return clone(state);
    }

    function setState(patch) {
        Object.assign(state, patch || {});
        listeners.forEach(function (listener) {
            listener(getState());
        });
    }

    function subscribe(listener) {
        listeners.push(listener);
        return function unsubscribe() {
            const index = listeners.indexOf(listener);
            if (index >= 0) {
                listeners.splice(index, 1);
            }
        };
    }

    function computeReorderPosition(currentIndex, direction, itemCount) {
        if (itemCount <= 0) {
            return 1;
        }
        const nextIndex = currentIndex + direction;
        if (nextIndex < 0 || nextIndex >= itemCount) {
            return currentIndex + 1;
        }
        return nextIndex + 1;
    }

    function computeDropPosition(sourceIndex, targetIndex, dropAfter) {
        const sourcePrecedesTarget = sourceIndex < targetIndex;
        const insertionIndex =
            targetIndex + (dropAfter ? 1 : 0) - (sourcePrecedesTarget ? 1 : 0);
        return insertionIndex + 1;
    }

    function clampSearchLimit(limit, maxLimit) {
        const parsed = parseInt(limit, 10);
        if (!parsed || parsed <= 0) {
            return maxLimit;
        }
        return Math.min(parsed, maxLimit);
    }

    function principalTypeLabel(principalType) {
        if (principalType === "USER") {
            return "User";
        }
        if (principalType === "GROU") {
            return "Group";
        }
        return principalType || "";
    }

    function matchesScriptQuery(script, query) {
        const needle = String(query || "").trim().toLowerCase();
        if (!needle) {
            return true;
        }
        return String((script && script.Title) || "").toLowerCase().indexOf(needle) >= 0 ||
            String((script && script.ScriptName) || "").toLowerCase().indexOf(needle) >= 0;
    }

    global.AccessManagerState = {
        get: getState,
        set: setState,
        subscribe: subscribe,
        computeReorderPosition: computeReorderPosition,
        computeDropPosition: computeDropPosition,
        clampSearchLimit: clampSearchLimit,
        principalTypeLabel: principalTypeLabel,
        matchesScriptQuery: matchesScriptQuery
    };
}(window));
