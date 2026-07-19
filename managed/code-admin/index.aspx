<%@ Page Language="VB" AutoEventWireup="false" Inherits="CodeAdminPage" %>
<!DOCTYPE html>
<html lang="en-US">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Code Admin</title>
    <link rel="stylesheet" href="https://maxcdn.bootstrapcdn.com/bootstrap/3.4.1/css/bootstrap.min.css">
    <link rel="stylesheet" href="https://maxcdn.bootstrapcdn.com/font-awesome/4.7.0/css/font-awesome.min.css">
    <link rel="stylesheet" href="<%= System.Web.HttpUtility.HtmlAttributeEncode(PilotConfig.StylesheetUrl) %>">
        <link rel="stylesheet" href="../shared/shell.css?v=0719n">
    <link rel="stylesheet" href="../shared/inline-edit.css?v=0719af">
                <link rel="stylesheet" href="code-admin.css?v=0719ab">
</head>
<body>
    <header class="shell-header">
        <div class="shell-header__inner">
            <div class="shell-brand">
                <h1>Code Admin</h1>
                <p>Manage code classes and values</p>
            </div>
            <p class="shell-user" id="shellUser" hidden>
                Signed in as <strong id="shellUserName"></strong>
                <button type="button" id="logoutButton">Sign out</button>
            </p>
            <button type="button" class="admin-menu-mobile-toggle" id="adminMenuMobileToggle" aria-controls="adminMenu" aria-expanded="false" aria-label="Show admin menu" title="Show admin menu"><i class="fa fa-bars" aria-hidden="true"></i></button>
        </div>
    </header>

    <div class="admin-layout" id="adminLayout">
        <aside class="admin-menu" id="adminMenu" aria-label="Primary navigation"></aside>
        <main class="shell-main" id="appMain">
            <div id="codeAdminApp"></div>
        </main>
    </div>

    <footer class="shell-footer">
        <div class="shell-footer__inner">
            <span>Admin Shell Pilot</span>
        </div>
    </footer>

    <script src="../shared/api-client.js"></script>
    <script src="../shared/session.js"></script>
    <script src="../shared/dialogs.js"></script>
    <script src="../shared/shell.js?v=071827b"></script>
    <script src="../shared/vendor/vue.global.prod.js?v=3.5.13"></script>
    <script src="../shared/inline-edit.js?v=0719ad"></script>
    <script src="js/view-model.js?v=071826f"></script>
    <script src="js/navigation.js?v=0719a"></script>
    <script src="js/components/editor.js?v=0719n"></script>
        <script src="js/components/workspace.js?v=0719ac"></script>
    <script src="js/app.js?v=0719ac"></script>
</body>
</html>
