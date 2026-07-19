"use strict";

(function (global) {
    function parseRoute(search) {
        const parameters = new URLSearchParams(search || "");
        const codeClass = parameters.get("codeClass") || "";
        const codeValue = parameters.get("codeValue") || "";
        const id = Number(parameters.get("id"));
        return {
            codeClass: codeClass,
            codeValue: codeValue,
            id: Number.isSafeInteger(id) && id > 0 ? id : null
        };
    }

    function listUrl(codeClass) {
        return "index.aspx?codeClass=" + encodeURIComponent(codeClass || "");
    }

    function detailUrl(codeClass, codeValue, id) {
        return listUrl(codeClass) + "&codeValue=" + encodeURIComponent(codeValue || "") + "&id=" + encodeURIComponent(id);
    }

    function classTitle(classInfo) {
        return classInfo && classInfo.codeClassDesc ? classInfo.codeClassDesc + " - Code Admin" : "Code Admin";
    }

    function detailTitle(classInfo, codeValue) {
        return "Edit " + (codeValue || "") + " - " + (classInfo && classInfo.codeClassDesc ? classInfo.codeClassDesc : "Code Admin") + " - Code Admin";
    }

    global.CodeAdminNavigation = {
        parseRoute: parseRoute,
        listUrl: listUrl,
        detailUrl: detailUrl,
        classTitle: classTitle,
        detailTitle: detailTitle
    };

    if (typeof module !== "undefined" && module.exports) {
        module.exports = global.CodeAdminNavigation;
    }
}(typeof window !== "undefined" ? window : global));