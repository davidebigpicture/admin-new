<%@ Page Language="VB" AutoEventWireup="false" Inherits="CodeAdminPage" %>
<!DOCTYPE html>
<html lang="en-US">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Code Admin</title>
    <link rel="stylesheet" href="https://maxcdn.bootstrapcdn.com/font-awesome/4.7.0/css/font-awesome.min.css">
    <link rel="stylesheet" href="../shared/shell.css?v=071726m">
    <link rel="stylesheet" href="code-admin.css?v=071726a">
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
            <div id="pilotToolNav"></div>
        </div>
    </header>

    <div class="admin-layout" id="adminLayout">
        <aside class="admin-menu" id="adminMenu" aria-label="Primary navigation"></aside>
        <main class="shell-main" id="appMain">
            <div id="appMessage" class="message error" role="alert" hidden></div>
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
    <script src="../shared/shell.js?v=071726m"></script>
    <script src="js/state.js?v=071726a"></script>
    <script src="js/view-model.js?v=071726c"></script>
    <script src="js/app.js?v=071726c"></script>
</body>
</html>
