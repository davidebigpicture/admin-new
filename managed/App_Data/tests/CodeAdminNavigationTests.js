"use strict";

const navigation = require("../../code-admin/js/navigation.js");
let failures = 0;

function assertTrue(condition, message) {
    if (condition) {
        console.log("PASS: " + message);
        return;
    }
    failures += 1;
    console.error("FAIL: " + message);
}

const parsedDetail = navigation.parseRoute("?codeClass=ORG_SUB_TY_CD&codeValue=CORP&id=42");
assertTrue(parsedDetail.codeClass === "ORG_SUB_TY_CD" && parsedDetail.codeValue === "CORP" && parsedDetail.id === 42, "parses approved detail deep links");
assertTrue(navigation.parseRoute("?codeClass=ABC&id=not-a-number").id === null, "rejects invalid detail identifiers");
assertTrue(navigation.listUrl("A B") === "./?codeClass=A%20B", "builds encoded directory list URLs");
assertTrue(navigation.detailUrl("ORG_SUB_TY_CD", "CORP", 42) === "./?codeClass=ORG_SUB_TY_CD&codeValue=CORP&id=42", "builds approved directory detail URLs");
assertTrue(navigation.classTitle({ codeClassDesc: "Organization Subtype" }) === "Organization Subtype - Code Admin" && navigation.classTitle(null) === "Code Admin", "builds list titles with fallback");
assertTrue(navigation.detailTitle({ codeClassDesc: "Organization Subtype" }, "CORP") === "Edit CORP - Organization Subtype - Code Admin", "builds approved detail titles");

if (failures > 0) {
    process.exit(1);
}
console.log("All Code Admin navigation tests passed.");