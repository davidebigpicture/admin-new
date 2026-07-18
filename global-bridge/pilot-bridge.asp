<%@ Language="VBScript" CodePage="65001" %>
<%
Response.Buffer = True
Response.Charset = "UTF-8"
%>
<!--#include virtual="/dev/adminshell/includes/ssi.inc"-->
<%
Dim returnUrl, establishUrl, loginUrl, cookieHeader, http, headers, lines, i, line, cookieValue

returnUrl = Request.QueryString("returnUrl")
If returnUrl = "" Then
    returnUrl = "/dev/adminshell/views.asp"
End If

loginUrl = Application("URL") & "/dev/adminshell/managed/login.html?returnUrl=" & Server.URLEncode(returnUrl)
establishUrl = Application("URL") & "/dev/adminshell/managed/pilot-establish.ashx?returnUrl=" & Server.URLEncode(returnUrl)
cookieHeader = pilotBuildCookieHeader()

Set http = Server.CreateObject("WinHttp.WinHttpRequest.5.1")
http.Open "GET", establishUrl, False
If cookieHeader <> "" Then
    http.SetRequestHeader "Cookie", cookieHeader
End If
http.Send

If http.Status = 200 And http.ResponseText = "OK" Then
    headers = http.GetAllResponseHeaders
    If headers <> "" Then
        lines = Split(headers, vbLf)
        For i = 0 To UBound(lines)
            line = Trim(Replace(lines(i), vbCr, ""))
            If LCase(Left(line, 11)) = "set-cookie:" Then
                cookieValue = Trim(Mid(line, 12))
                If InStr(1, cookieValue, "bp_admin_next=", vbTextCompare) > 0 Then
                    Response.AddHeader "Set-Cookie", cookieValue
                End If
            End If
        Next
    End If
    Response.Redirect returnUrl
Else
    Response.Redirect loginUrl
End If
%>
