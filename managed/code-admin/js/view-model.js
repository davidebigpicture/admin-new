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

    function getSelectedClass(workspace, selectedClass) {
        const classes = (workspace && workspace.classes) || [];
        for (let index = 0; index < classes.length; index += 1) {
            if (classes[index].codeClass === selectedClass) {
                return classes[index];
            }
        }
        return null;
    }

    function emptyPage() {
        return {
            items: [],
            totalCount: 0,
            start: 0,
            pageSize: 200,
            canDelete: false
        };
    }

    function canOpenAddEditor(selectedClass) {
        return hasSelectedClass(selectedClass);
    }

    function getDetailFields(workspace, selectedClass) {
        const metadata = workspace && workspace.fieldMetadata && workspace.fieldMetadata[selectedClass];
        const fields = (metadata && metadata.fields) || [];
        return fields.slice().sort(function (left, right) {
            return (left.order || 0) - (right.order || 0);
        });
    }

    function getPayloadFieldKeys(workspace, selectedClass) {
        const keys = ["minorCode"];
        getDetailFields(workspace, selectedClass).forEach(function (field) {
            if (keys.indexOf(field.key) < 0) {
                keys.push(field.key);
            }
        });
        return keys;
    }

    function normalizeEditorValue(value, controlType) {
        if (controlType === "multiselect" && Array.isArray(value)) {
            return value.join(", ");
        }
        return value == null ? "" : String(value);
    }

    global.CodeAdminViewModel = {
        hasSelectedClass: hasSelectedClass,
        buildClassOptions: buildClassOptions,
        getSelectedClass: getSelectedClass,
        emptyPage: emptyPage,
        canOpenAddEditor: canOpenAddEditor,
        getDetailFields: getDetailFields,
        getPayloadFieldKeys: getPayloadFieldKeys,
        normalizeEditorValue: normalizeEditorValue
    };

    if (typeof module !== "undefined" && module.exports) {
        module.exports = { CodeAdminViewModel: global.CodeAdminViewModel };
    }
}(typeof window !== "undefined" ? window : global));
