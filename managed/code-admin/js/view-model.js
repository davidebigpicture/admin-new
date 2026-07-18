"use strict";

(function (global) {
    function hasSelectedClass(selectedClass) {
        return !!(selectedClass && String(selectedClass).trim());
    }

    function escapeHtml(value) {
        return String(value || "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;");
    }

    function buildClassOptions(workspace, selectedClass) {
        const options = (workspace.classes || [])
            .map(function (item) {
                const label = workspace.showClassCodes
                    ? item.codeClassDesc + " - " + item.codeClass
                    : item.codeClassDesc;
                const selected = item.codeClass === selectedClass ? " selected" : "";
                return '<option value="' + escapeHtml(item.codeClass) + '"' + selected + ">" + escapeHtml(label) + "</option>";
            })
            .join("");

        const placeholderSelected = hasSelectedClass(selectedClass) ? "" : " selected";
        return '<option value="" disabled' + placeholderSelected + ">Select a class...</option>" + options;
    }

    function emptyPage(pageSize) {
        return {
            items: [],
            totalCount: 0,
            start: 0,
            pageSize: pageSize || 200,
            canDelete: false
        };
    }

    function canOpenAddEditor(selectedClass) {
        return hasSelectedClass(selectedClass);
    }

    global.CodeAdminViewModel = {
        hasSelectedClass: hasSelectedClass,
        buildClassOptions: buildClassOptions,
        emptyPage: emptyPage,
        canOpenAddEditor: canOpenAddEditor
    };

    if (typeof module !== "undefined" && module.exports) {
        module.exports = { CodeAdminViewModel: global.CodeAdminViewModel };
    }
}(typeof window !== "undefined" ? window : global));
