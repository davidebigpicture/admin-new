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
const managedMaster = fs.readFileSync(path.join(managedRoot, "shared", "ManagedShell.master"), "utf8");
const managedShellMaster = fs.readFileSync(path.join(managedRoot, "..", "App_Code", "AdminShell", "ManagedShellMaster.vb"), "utf8");
const managedToolPage = fs.readFileSync(path.join(managedRoot, "..", "App_Code", "AdminShell", "ManagedToolPage.vb"), "utf8");
const adminShellApiGuard = fs.readFileSync(path.join(managedRoot, "..", "App_Code", "AdminShell", "AdminShellApiGuard.vb"), "utf8");
const adminShellData = fs.readFileSync(path.join(managedRoot, "..", "App_Code", "AdminShell", "AdminShellData.vb"), "utf8");
const adminShellExceptions = fs.readFileSync(path.join(managedRoot, "..", "App_Code", "AdminShell", "AdminShellExceptions.vb"), "utf8");
const accessManagerSecurity = fs.readFileSync(path.join(managedRoot, "..", "App_Code", "AdminShell", "AccessManager", "AccessManagerSecurity.vb"), "utf8");
const accessManagerPage = fs.readFileSync(path.join(managedRoot, "..", "App_Code", "AdminShell", "AccessManager", "AccessManagerPage.vb"), "utf8");
const codeAdminSecurity = fs.readFileSync(path.join(managedRoot, "..", "App_Code", "AdminShell", "CodeAdmin", "CodeAdminSecurity.vb"), "utf8");
const accessManagerIndex = fs.readFileSync(path.join(managedRoot, "access-manager", "index.aspx"), "utf8");
const accessManagerApp = fs.readFileSync(path.join(managedRoot, "access-manager", "js", "app.js"), "utf8");
const codeAdminIndex = fs.readFileSync(path.join(managedRoot, "code-admin", "index.aspx"), "utf8");
const sessionHandler = fs.readFileSync(path.join(managedRoot, "api", "session.ashx"), "utf8");
const pilotJsonApi = fs.readFileSync(path.join(managedRoot, "..", "App_Code", "AdminShell", "PilotJsonApi.vb"), "utf8");
const webConfig = fs.readFileSync(path.join(managedRoot, "web.config"), "utf8");

assertTrue(pilotShell.includes("admin-layout"), "classic chrome uses the unified admin layout");
assertTrue(pilotShell.includes("adminMenu"), "classic chrome reserves the section menu container");
assertTrue(!pilotShell.includes("pilotToolNav"), "classic chrome omits the duplicate top tool navigation");
assertTrue(pilotShell.includes("managed/shared/shell.css"), "classic chrome loads the shared shell stylesheet");
assertTrue(pilotShell.includes("PilotSession.load()"), "classic chrome bootstraps the shared session API");
assertTrue(
    pilotShell.includes("window.ManagedShell.renderSectionMenu") &&
        !pilotShell.includes("window.PilotShell"),
    "classic chrome hydrates the section menu through the current shared shell API"
);
assertTrue(!pilotShell.includes("id=\"col-left\""), "classic chrome removes the legacy stub left column");
assertTrue(
    /class=""?shell-logo""?/.test(pilotShell) &&
        managedMaster.includes('class="shell-logo"') &&
        managedMaster.includes('src="https://www.ebigpicture.com/img/logo.png"') &&
        managedMaster.includes('alt="Big Picture Software"'),
    "classic chrome and the managed master show the linked Big Picture logo"
);
assertTrue(
    managedMaster.indexOf('class="shell-brand__client"><%= EncodedClientTitle %>') < managedMaster.indexOf('<h1><%= EncodedToolTitle %>') &&
        managedMaster.indexOf('<h1><%= EncodedToolTitle %>') < managedMaster.indexOf('class="shell-brand__subtitle"><%= EncodedToolSubtitle %>') &&
        managedMaster.includes('id="shellSessionTime"') &&
        managedMaster.includes('class="shell-logout" id="logoutButton"') &&
        managedMaster.includes('fa fa-sign-out') &&
        pilotShell.includes('"<h1>" & encodedTitle') &&
        pilotShell.includes('class=""shell-brand__client""') &&
        pilotShell.includes('class=""shell-session-time""') &&
        pilotShell.includes('class=""shell-logout""') &&
        !pilotShell.includes('shell-pilot-badge') &&
        !pilotShell.includes('Admin Shell Pilot'),
    "classic and managed headers show the tool, client, compact sign-out control, and legacy session time"
);
assertTrue(
    /id=""?adminMenuMobileToggle""?/.test(pilotShell) &&
        managedMaster.includes('id="adminMenuMobileToggle"') &&
        managedMaster.includes('aria-controls="adminMenu"') &&
        managedMaster.includes('class="fa fa-bars"'),
    "classic chrome and the managed master include the accessible mobile menu button"
);
assertTrue(
    [accessManagerIndex, codeAdminIndex].every(function (managedPage) {
        return managedPage.includes('MasterPageFile="../shared/ManagedShell.master"') &&
            !managedPage.includes("<!DOCTYPE") &&
            !managedPage.includes("<html") &&
            !managedPage.includes("<head") &&
            !managedPage.includes("<body") &&
            !managedPage.includes("shell.js") &&
            !managedPage.includes("api-client.js") &&
            !managedPage.includes('class="shell-header"') &&
            !managedPage.includes('id="adminLayout"') &&
            !managedPage.includes('id="adminMenu"') &&
            !managedPage.includes('class="shell-footer"') &&
            !managedPage.includes('id="shellUser"');
    }),
    "managed pages are thin content pages without duplicate document or shell markup"
);
assertTrue(
    managedMaster.includes("<!DOCTYPE html>") &&
        managedMaster.includes('<html lang="en-US">') &&
        managedMaster.includes('id="appMain"') &&
        managedMaster.includes('id="adminLayout"') &&
        managedMaster.includes('id="adminMenu"') &&
        managedMaster.includes('id="shellUser"') &&
        managedMaster.includes('id="shellUserName"') &&
        managedMaster.includes('id="logoutButton"') &&
        managedMaster.includes('class="shell-footer"') &&
        managedMaster.includes('>Admin Shell</div>') &&
        !managedMaster.includes('Admin Shell Pilot') &&
        managedMaster.indexOf('ContentPlaceHolder ID="ToolHead"') < managedMaster.indexOf('EncodedShellCssUrl') &&
        managedMaster.includes("EncodedApiClientUrl") &&
        managedMaster.includes("EncodedSessionUrl") &&
        managedMaster.includes("EncodedDialogsUrl") &&
        managedMaster.includes("EncodedShellScriptUrl") &&
        managedShellMaster.includes('PilotConfig.CombinePilot("managed/shared/shell.css") & "?v=" & ShellCssVersion') &&
        managedShellMaster.includes('PilotConfig.CombinePilot("managed/shared/api-client.js")') &&
        managedShellMaster.includes('PilotConfig.CombinePilot("managed/shared/session.js")') &&
        managedShellMaster.includes('PilotConfig.CombinePilot("managed/shared/dialogs.js")') &&
        managedShellMaster.includes('PilotConfig.CombinePilot("managed/shared/shell.js") & "?v=" & ShellAssetVersion') &&
        !managedShellMaster.includes("~/managed/shared") &&
        !managedShellMaster.includes("ResolveUrl(") &&
        (managedMaster.match(/runat="server"/g) || []).length === 3 &&
        !/<asp:(?!ContentPlaceHolder)/.test(managedMaster) &&
        !managedMaster.includes("<form") &&
        !managedMaster.includes("UpdatePanel") &&
        !managedMaster.includes("GridView"),
    "managed master owns the HTML5 document, shared chrome, and pilot-relative shared assets with only content placeholders as server controls"
);
assertTrue(
    !shellJs.includes("mountManagedShell") &&
        !shellJs.includes("shell-header") &&
        !shellJs.includes("shell-footer"),
    "shared shell JavaScript hydrates behavior without owning document markup"
);
assertTrue(
    managedToolPage.includes("Inherits Page") &&
        managedToolPage.includes("PilotConfig.IsEnabledForHost") &&
        managedToolPage.includes("PilotAuth.TryGetCurrentUser") &&
        managedToolPage.includes("Response.Redirect") &&
        managedToolPage.includes("Response.StatusCode = 403") &&
        managedToolPage.includes("shell.ToolTitle = ToolTitle") &&
        accessManagerPage.includes("PilotJsonApi.CanUseAccessManager(user)"),
    "managed tool pages centralize host, authentication, denial, and master metadata while Access Manager uses its API predicate"
);
assertTrue(
    adminShellApiGuard.includes("accessPredicate As Func(Of PilotUser, Boolean)") &&
        adminShellApiGuard.includes("RequireAuthorizedMutation") &&
        adminShellData.includes("Public Shared Function StringValue") &&
        adminShellData.includes("Public Shared Function NullableInt") &&
        adminShellData.includes("Public Shared Function NullableDate") &&
        adminShellExceptions.includes("Public Class AdminShellServiceException") &&
        adminShellExceptions.includes("Public Class AdminShellForbiddenException") &&
        adminShellExceptions.includes("Public Class AdminShellValidationException") &&
        adminShellExceptions.includes("Public Class AdminShellConcurrencyException") &&
        accessManagerSecurity.includes("AdminShellApiGuard.RequireAuthorized") &&
        codeAdminSecurity.includes("AdminShellApiGuard.RequireAuthorized"),
    "nested tools share neutral API guard, database conversion, and service exception abstractions through thin adapters"
);
assertTrue(
    !fs.existsSync(path.join(managedRoot, "access-manager", "index.html")),
    "obsolete Access Manager index.html entry is removed"
);
assertTrue(
    shellJs.includes("async function initialize(options)") &&
        shellJs.includes("global.PilotSession.configure") &&
        shellJs.includes("global.PilotApiClient.setApiBase") &&
        shellJs.includes("bindLogout(document.getElementById(\"logoutButton\"))") &&
        shellJs.includes("const session = await global.PilotSession.load()") &&
        shellJs.includes("userName.textContent = session.userName") &&
        shellJs.includes('startSessionTimer(document.getElementById("shellSessionTime"))') &&
        shellJs.includes('element.textContent = "Session time: " +') &&
        shellJs.includes("renderSectionMenu("),
    "managed shell initializer configures, hydrates, renders shared session chrome, and starts the session timer"
);
assertTrue(
    accessManagerApp.includes("await window.ManagedShell.initialize") &&
        accessManagerApp.indexOf("ManagedShell.initialize") < accessManagerApp.indexOf("api/workspace.ashx") &&
        !accessManagerApp.includes("PilotSession.load") &&
        !accessManagerApp.includes("bindLogout") &&
        !accessManagerApp.includes("renderSectionMenu") &&
        !accessManagerApp.includes("shellUserName"),
    "Access Manager awaits shared hydration before its workspace and owns no duplicate user, menu, or logout handling"
);
assertTrue(
    !accessManagerIndex.includes("pilotToolNav") &&
        !codeAdminIndex.includes("pilotToolNav") &&
        !fs.readFileSync(path.join(managedRoot, "access-manager", "js", "app.js"), "utf8").includes("renderNav(") &&
        !fs.readFileSync(path.join(managedRoot, "code-admin", "js", "app.js"), "utf8").includes("renderNav("),
    "managed pages rely on the left section menu instead of duplicate top links"
);
assertTrue(sessionHandler.includes("PilotSessionHandler"), "pilot-wide session handler is registered");
assertTrue(apiClientJs.includes("setApiBase"), "api client supports a managed base path");
assertTrue(sessionJs.includes("configure"), "session loader supports endpoint configuration");
assertTrue(
    webConfig.includes('managed/code-admin/index.aspx=cgi-bin/codeadminO.pl|Code Admin') &&
        webConfig.includes('managed/code-admin/index.aspx=cgi-bin/codeadmin.pl|Code Admin'),
    "both legacy Codes script identities route to managed Code Admin"
);
assertTrue(
    pilotJsonApi.includes('{"path", PreferredNavigationPath(route.PilotPath)}') &&
        pilotJsonApi.includes('{"Path", PreferredNavigationPath(ResolveMenuItemPath(item.Path))}') &&
        pilotJsonApi.includes('Const defaultDocument As String = "index.aspx"') &&
        pilotJsonApi.includes('Dim suffix = If(suffixStart < 0, String.Empty, path.Substring(suffixStart))'),
    "generated route and menu links prefer directory URLs while retaining URL suffixes"
);
assertTrue(
    /@media \(max-width: 900px\)[\s\S]*?\.admin-menu\s*\{[^}]*max-height:\s*70vh;[^}]*opacity:\s*1;[^}]*transition:\s*max-height[^}]*opacity[^}]*\}[\s\S]*?\.menu-collapsed \.admin-menu\s*\{[^}]*height:\s*0 !important;[^}]*max-height:\s*0 !important;[^}]*overflow:\s*hidden;[^}]*opacity:\s*0;[^}]*\}[\s\S]*?\.menu-collapsed \.admin-menu-content\s*\{[^}]*display:\s*block;[^}]*\}[\s\S]*?\.admin-menu-toggle\s*\{[^}]*display:\s*none;[^}]*\}[\s\S]*?\.admin-menu-mobile-toggle\s*\{[^}]*display:\s*inline-flex;/.test(shellCss),
    "mobile shell animates the menu while retaining the hamburger and hidden desktop edge control"
);
assertTrue(
    /@media \(max-width: 900px\)[\s\S]*?body\.pilot-classic \.admin-menu\s*\{[^}]*position:\s*static;[^}]*height:\s*auto;[^}]*max-height:\s*70vh;[^}]*\}/.test(shellCss),
    "classic pages retain the expanded mobile menu layout"
);
const finalCompactShellStart = shellCss.lastIndexOf("@media (max-width: 760px)");
const finalCompactShellCss = shellCss.slice(finalCompactShellStart);
assertTrue(
    finalCompactShellStart > shellCss.indexOf("@media (max-width: 900px)") &&
        /\.shell-header__inner\s*\{[^}]*grid-template-columns:\s*2\.5rem minmax\(0, 1fr\) 2\.25rem;[^}]*gap:\s*0\.5rem 0\.75rem;[^}]*padding:\s*0\.75rem;/.test(finalCompactShellCss) &&
        /\.shell-logo\s*\{[^}]*grid-column:\s*1;/.test(finalCompactShellCss) &&
        /\.shell-brand\s*\{[^}]*grid-column:\s*2;[^}]*grid-row:\s*1;/.test(finalCompactShellCss) &&
        /\.shell-brand h1\s*\{[^}]*white-space:\s*nowrap;/.test(finalCompactShellCss) &&
        /\.shell-brand p\s*\{[^}]*display:\s*none;/.test(finalCompactShellCss) &&
        /\.shell-user\s*\{[^}]*grid-column:\s*1 \/ -1;[^}]*grid-row:\s*2;[^}]*max-width:\s*100%;[^}]*justify-self:\s*end;[^}]*text-align:\s*right;/.test(finalCompactShellCss) &&
        /\.admin-menu-mobile-toggle\s*\{[^}]*grid-column:\s*3;[^}]*grid-row:\s*1;[^}]*justify-self:\s*end;/.test(finalCompactShellCss),
    "final compact shell breakpoint keeps the logo, brand, and hamburger on row one and the signed-in user on row two"
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
    /\.shell-header__inner,\s*\.shell-main,\s*\.shell-footer__inner\s*\{[^}]*padding-left:\s*clamp\(\.75rem, 1\.5vw, 1rem\);[^}]*padding-right:\s*clamp\(\.75rem, 1\.5vw, 1rem\);/.test(shellCss),
    "shared shell header, main, and footer use the restrained aligned gutter"
);
assertTrue(
    /menuState\.isMobileViewport\(\) \? true : menuState\.readStoredCollapsed\(\)/.test(shellJs),
    "shell starts mobile collapsed and restores the stored desktop state"
);
assertTrue(
    shellJs.includes("layout._managedShellMenuState"),
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

console.log("All managed shell UI tests passed.");
