<%
Response.ContentType = "text/html"
Response.AddHeader "Content-Type", "text/html;charset=UTF-8"
Response.CodePage = 65001
Response.CharSet = "UTF-8"
Response.Buffer = True
%>
<!--#include virtual="/classes/cRunSql.inc"-->
<!--#include virtual="/admin/admin/includes/utilities.inc"-->
<!--#include virtual="/www/includes/session.inc"-->
<!--#include virtual="/www/includes/dbtools.inc"-->
<!--#include virtual="/classes/cLog.inc"-->
<!--#include file="includes/ssi.inc"-->
<%
Dim pilotManagedBase, pilotScriptPath, pilotAuthResult, pilotAuthParts
Dim pilotReturnUrl, pilotHeaderParams, pilotPrintUrl, isDev
Dim pilotRoot, slashPos

isDev = (Application("DEV_DOMAIN") = Request.ServerVariables("SERVER_NAME"))
pilotScriptPath = Request.ServerVariables("SCRIPT_NAME")

' Derive the relocatable pilot root from the current script path so Classic ASP
' does not hardcode /dev/adminshell. Example: /dev/adminshell/views.asp -> /dev/adminshell
slashPos = InStrRev(pilotScriptPath, "/")
If slashPos > 1 Then
    pilotRoot = Left(pilotScriptPath, slashPos - 1)
Else
    pilotRoot = ""
End If
pilotManagedBase = Application("URL") & pilotRoot & "/managed"

startSession 60

pilotAuthResult = pilotFetch( _
    pilotManagedBase & "/authorize.ashx", _
    "script=" & Server.URLEncode(pilotScriptPath))

If Left(pilotAuthResult, 3) = "OK|" Then
    pilotAuthParts = Split(pilotAuthResult, "|", 2)
    Session("LoginName") = pilotAuthParts(1)
ElseIf pilotAuthResult = "NOSESSION" Then
    pilotReturnUrl = pilotScriptPath
    If Request.QueryString <> "" Then
        pilotReturnUrl = pilotReturnUrl & "?" & Request.QueryString
    End If
    ' Legacy auth cookies use Path=/admin and are not sent to /dev/adminshell.
    ' Bridge through /admin/admin/pilot-bridge.asp where those cookies are available.
    Response.Redirect Application("URL") & "/admin/admin/pilot-bridge.asp?returnUrl=" & Server.URLEncode(pilotReturnUrl)
ElseIf pilotAuthResult = "DENY" Then
    Response.Status = "403 Forbidden"
    Response.Write "<div align=""center"">You are not authorized to access this admin shell tool.</div>"
    Response.End
ElseIf pilotAuthResult = "UNKNOWN" Then
    Response.Status = "404 Not Found"
    Response.Write "Unknown admin shell pilot route."
    Response.End
Else
    Response.Status = "503 Service Unavailable"
    Response.Write "<div align=""center"">The admin shell pilot is temporarily unavailable.</div>"
    Response.End
End If

pilotHeaderParams = "part=header"
pilotHeaderParams = pilotHeaderParams & "&script=" & Server.URLEncode(pilotScriptPath)
pilotHeaderParams = pilotHeaderParams & "&title=" & Server.URLEncode(pageTitle)
pilotWrite pilotManagedBase & "/chrome.ashx", pilotHeaderParams

pilotPrintUrl = pilotScriptPath & "?print=1"
If Request.QueryString <> "" Then
    pilotPrintUrl = pilotPrintUrl & "&" & Request.QueryString
End If
%>
<script>
function doPrintASP() {
    var printWindow = window.open('<%=Replace(pilotPrintUrl, "'", "%27")%>', 'printwindow', 'scrollbars,resizable,menubar');
    if (printWindow) {
        printWindow.focus();
        printWindow.print();
    }
}
</script>
<div id="spinner" align="center" style="display:none"><i class="fa fa-spinner fa-pulse fa-2x"></i></div>
