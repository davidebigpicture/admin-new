"use strict";

const fs = require("fs");
const path = require("path");

let failures = 0;

function assertTrue(condition, message) {
    if (condition) {
        console.log("PASS: " + message);
        return;
    }
    failures += 1;
    console.error("FAIL: " + message);
}

const managedRoot = path.resolve(__dirname, "..", "..");
const pilotShell = fs.readFileSync(
    path.join(managedRoot, "..", "App_Code", "AdminShell", "PilotShell.vb"),
    "utf8"
);
const sessionJs = fs.readFileSync(path.join(managedRoot, "shared", "session.js"), "utf8");
const apiClientJs = fs.readFileSync(path.join(managedRoot, "shared", "api-client.js"), "utf8");
const shellCss = fs.readFileSync(path.join(managedRoot, "shared", "shell.css"), "utf8");
const shellJs = fs.readFileSync(path.join(managedRoot, "shared", "shell.js"), "utf8");
const accessManagerIndex = fs.readFileSync(path.join(managedRoot, "access-manager", "index.html"), "utf8");
const codeAdminIndex = fs.readFileSync(path.join(managedRoot, "code-admin", "index.aspx"), "utf8");
const sessionHandler = fs.readFileSync(path.join(managedRoot, "api", "session.ashx"), "utf8");

assertTrue(pilotShell.includes("admin-layout"), "classic chrome uses the unified admin layout");
assertTrue(pilotShell.includes("adminMenu"), "classic chrome reserves the section menu container");
assertTrue(pilotShell.includes("pilotToolNav"), "classic chrome reserves pilot tool navigation");
assertTrue(pilotShell.includes("managed/shared/shell.css"), "classic chrome loads the shared shell stylesheet");
assertTrue(pilotShell.includes("PilotSession.load()"), "classic chrome bootstraps the shared session API");
assertTrue(pilotShell.includes("renderSectionMenu"), "classic chrome hydrates the section menu");
assertTrue(!pilotShell.includes("id=\"col-left\""), "classic chrome removes the legacy stub left column");
assertTrue(
    [pilotShell, accessManagerIndex, codeAdminIndex].every(function (headerOwner) {
        return /id=""?adminMenuMobileToggle""?/.test(headerOwner) &&
            /aria-controls=""?adminMenu""?/.test(headerOwner) &&
            headerOwner.includes('fa fa-bars');
    }),
    "all shell header owners include the accessible mobile menu button"
);
assertTrue(sessionHandler.includes("PilotSessionHandler"), "pilot-wide session handler is registered");
assertTrue(apiClientJs.includes("setApiBase"), "api client supports a managed base path");
assertTrue(sessionJs.includes("configure"), "session loader supports endpoint configuration");
assertTrue(
    /@media \(max-width: 900px\)[\s\S]*?\.admin-menu\s*\{[^}]*max-height:\s*70vh;[^}]*opacity:\s*1;[^}]*transition:\s*max-height[^}]*opacity[^}]*\}[^]*?\.menu-collapsed \.admin-menu\s*\{[^}]*max-height:\s*0;[^}]*overflow:\s*hidden;[^}]*opacity:\s*0;[^}]*\}[^]*?\.menu-collapsed \.admin-menu-content\s*\{[^}]*display:\s*block;[^}]*\}[\s\S]*?\.admin-menu-toggle\s*\{[^}]*display:\s*none;[^}]*\}[\s\S]*?\.admin-menu-mobile-toggle\s*\{[^}]*display:\s*inline-flex;/.test(shellCss),
    "mobile shell animates the menu while retaining the hamburger and hidden desktop edge control"
);
assertTrue(
    /\.admin-menu-mobile-toggle\[aria-expanded="true"\]\s*\{[^}]*border-color:[^}]*background:[^}]*color:/.test(shellCss),
    "expanded mobile menu toggle has an active visual treatment"
);
assertTrue(
    /@media \(prefers-reduced-motion: reduce\)\s*\{[\s\S]*?@media \(max-width: 900px\)\s*\{\s*\.admin-menu\s*\{[^}]*transition:\s*none;/.test(shellCss),
    "reduced-motion users receive immediate mobile menu state changes"
);
assertTrue(
    /\.admin-section__content\s*\{[^}]*display:\s*grid;[^}]*grid-template-rows:\s*0fr;[^}]*visibility:\s*hidden;[^}]*opacity:\s*0;[^}]*transition:\s*grid-template-rows[^}]*opacity[^}]*visibility[^}]*\}[^]*?\.admin-section\[open\]\s*>\s*\.admin-section__content\s*\{[^}]*grid-template-rows:\s*1fr;[^}]*visibility:\s*visible;[^}]*opacity:\s*1;/.test(shellCss),
    "section navigation content animates between closed and open states"
);
assertTrue(
    /@media \(prefers-reduced-motion: reduce\)\s*\{\s*\.admin-section__content\s*\{[^}]*transition:\s*none;/.test(shellCss),
    "reduced-motion users receive immediate section state changes"
);
assertTrue(
    shellJs.includes('global.matchMedia("(max-width: 900px)")'),
    "shell detects the mobile viewport with matchMedia"
);
assertTrue(
    /menuState\.isMobileViewport\(\) \? true : menuState\.readStoredCollapsed\(\)/.test(shellJs),
    "shell starts mobile collapsed and restores the stored desktop state"
);
assertTrue(
    shellJs.includes("layout._pilotShellMenuState"),
    "shell reuses the viewport listener across menu rerenders"
);
assertTrue(
    shellJs.includes("menuState.setCollapsed(!layout.classList.contains(\"menu-collapsed\"), true)"),
    "desktop menu toggles continue to persist the collapsed preference"
);
assertTrue(
    shellJs.includes("mobileToggle.onclick") &&
        shellJs.includes("menuState.setCollapsed(!layout.classList.contains(\"menu-collapsed\"), false)"),
    "mobile toggle replaces its handler and changes state without persistence"
);
assertTrue(
    shellJs.includes('const mobileLabel = value ? "Show admin menu" : "Hide admin menu"') &&
        shellJs.includes('menuState.mobileToggle.setAttribute("aria-expanded", value ? "false" : "true")') &&
        shellJs.includes('mobileIcon.classList.toggle("fa-bars", value)') &&
        shellJs.includes('mobileIcon.classList.toggle("fa-times", !value)'),
    "mobile toggle updates its ARIA state, label, and bars/times icon"
);
assertTrue(
    shellJs.includes('menuContent.inert = value') &&
        shellJs.includes('menuContent.setAttribute("inert", "")') &&
        shellJs.includes('menuContent.removeAttribute("inert")'),
    "collapsed menu content synchronizes inert state with an attribute fallback"
);
assertTrue(
    shellJs.includes('menuContent.contains(document.activeElement)') &&
        shellJs.includes('const controllingToggle = menuState.isMobileViewport()') &&
        shellJs.includes('controllingToggle.focus()'),
    "collapsing a focused menu transfers focus to the viewport control"
);
assertTrue(
    shellJs.includes('toggle.setAttribute("aria-controls", "adminMenu")'),
    "desktop edge control identifies the menu it controls"
);
assertTrue(
    shellJs.includes('sectionContent.className = "admin-section__content"') &&
        shellJs.includes('sectionContent.appendChild(list)') &&
        shellJs.includes('group.appendChild(sectionContent)'),
    "section navigation wraps list content for height animation"
);
assertTrue(
    shellJs.includes('group.classList.add("is-closing")') &&
        shellJs.includes('global.setTimeout(function ()') &&
        shellJs.includes('group.open = false') &&
        shellCss.includes('.admin-section.is-closing > .admin-section__content'),
    "section closing retains native details content long enough to animate"
);

if (failures > 0) {
    process.exit(1);
}

console.log("All PilotShell UI tests passed.");
