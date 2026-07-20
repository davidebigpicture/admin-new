<%@ Page Language="VB" AutoEventWireup="false" MasterPageFile="../shared/ManagedShell.master" Inherits="AccessManagerPage" %>
<asp:Content ID="ToolHeadContent" ContentPlaceHolderID="ToolHead" runat="server">
    <link rel="stylesheet" href="access-manager.css?v=071726d">
</asp:Content>
<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">
    <div id="appMessage" class="message error" role="alert" hidden></div>
    <div id="viewSections" data-view-panel="sections"></div>
</asp:Content>
<asp:Content ID="ToolScriptsContent" ContentPlaceHolderID="ToolScripts" runat="server">
    <script src="js/state.js?v=071726c"></script>
    <script src="js/reorder.js?v=071726b"></script>
    <script src="js/sections-view.js?v=071726f"></script>
    <script src="js/app.js?v=0719s"></script>
</asp:Content>