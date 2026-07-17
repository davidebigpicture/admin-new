"use strict";

function assertEqual(actual, expected, message) {
    if (actual !== expected) {
        throw new Error("FAIL: " + message + " (expected " + expected + ", got " + actual + ")");
    }
    console.log("PASS: " + message);
}

function run() {
    const state = {
        computeReorderPosition: function (currentIndex, direction, itemCount) {
            if (itemCount <= 0) {
                return 1;
            }
            const nextIndex = currentIndex + direction;
            if (nextIndex < 0 || nextIndex >= itemCount) {
                return currentIndex + 1;
            }
            return nextIndex + 1;
        },
        computeDropPosition: function (sourceIndex, targetIndex, dropAfter) {
            const sourcePrecedesTarget = sourceIndex < targetIndex;
            const insertionIndex =
                targetIndex + (dropAfter ? 1 : 0) - (sourcePrecedesTarget ? 1 : 0);
            return insertionIndex + 1;
        },
        clampSearchLimit: function (limit, maxLimit) {
            const parsed = parseInt(limit, 10);
            if (!parsed || parsed <= 0) {
                return maxLimit;
            }
            return Math.min(parsed, maxLimit);
        },
        principalTypeLabel: function (principalType) {
            if (principalType === "USER") {
                return "User";
            }
            if (principalType === "GROU") {
                return "Group";
            }
            return principalType || "";
        },
        matchesScriptQuery: function (script, query) {
            const needle = String(query || "").trim().toLowerCase();
            if (!needle) {
                return true;
            }
            return String((script && script.Title) || "").toLowerCase().indexOf(needle) >= 0 ||
                String((script && script.ScriptName) || "").toLowerCase().indexOf(needle) >= 0;
        }
    };

    assertEqual(state.computeReorderPosition(0, -1, 3), 1, "reorder stays at first position");
    assertEqual(state.computeReorderPosition(0, 1, 3), 2, "reorder moves down one");
    assertEqual(state.computeReorderPosition(2, 1, 3), 3, "reorder stays at last position");
    assertEqual(state.computeDropPosition(0, 2, true), 3, "drag first item after last");
    assertEqual(state.computeDropPosition(0, 2, false), 2, "drag first item before last");
    assertEqual(state.computeDropPosition(2, 0, false), 1, "drag last item before first");
    assertEqual(state.computeDropPosition(2, 0, true), 2, "drag last item after first");
    assertEqual(state.clampSearchLimit(0, 50), 50, "empty search limit uses default");
    assertEqual(state.clampSearchLimit(200, 50), 50, "search limit is capped");
    assertEqual(state.principalTypeLabel("GROU"), "Group", "group code has a readable label");
    assertEqual(state.principalTypeLabel("USER"), "User", "user code has a readable label");
    assertEqual(
        state.matchesScriptQuery({ Title: "Form Fields", ScriptName: "/admin/fields.asp" }, "form"),
        true,
        "script search matches title"
    );
    assertEqual(
        state.matchesScriptQuery({ Title: "Form Fields", ScriptName: "/admin/fields.asp" }, "FIELDS.ASP"),
        true,
        "script search matches path without case sensitivity"
    );
    assertEqual(
        state.matchesScriptQuery({ Title: "Form Fields", ScriptName: "/admin/fields.asp" }, "missing"),
        false,
        "script search rejects non-matches"
    );
    console.log("All AccessManagerState tests passed.");
}

try {
    run();
} catch (error) {
    console.error(error.message || error);
    process.exit(1);
}
