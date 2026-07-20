<%@ Page Language="VB" AutoEventWireup="false" MasterPageFile="../shared/ManagedShell.master" Inherits="CodeAdminPage" %>
<asp:Content ID="ToolHeadContent" ContentPlaceHolderID="ToolHead" runat="server">
    <link rel="stylesheet" href="https://maxcdn.bootstrapcdn.com/bootstrap/3.4.1/css/bootstrap.min.css">
    <link rel="stylesheet" href="../shared/inline-edit.css?v=0719af">
    <link rel="stylesheet" href="code-admin.css?v=0719ads1">
</asp:Content>
<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">
    <div id="codeAdminApp"></div>
</asp:Content>
<asp:Content ID="ToolScriptsContent" ContentPlaceHolderID="ToolScripts" runat="server">
    <script src="../shared/vendor/vue.global.prod.js?v=3.5.13"></script>
    <script src="../shared/inline-edit.js?v=0719ag"></script>
    <script src="js/view-model.js?v=071826f"></script>
    <script src="js/navigation.js?v=0719t"></script>
    <script src="js/components/editor.js?v=0719ads1"></script>
    <script src="js/components/workspace.js?v=0719ads1"></script>
    <script src="js/app.js?v=0719ads1"></script>
</asp:Content>
