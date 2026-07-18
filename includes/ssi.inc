<%
Function pilotBuildCookieHeader()
    Dim parts(4)
    Dim partCount
    partCount = -1

    If Request.Cookies("bp_admin_next") <> "" Then
        partCount = partCount + 1
        parts(partCount) = "bp_admin_next=" & Request.Cookies("bp_admin_next")
    End If

    If Request.Cookies("username") <> "" Then
        partCount = partCount + 1
        parts(partCount) = "username=" & Request.Cookies("username")
    End If

    If Request.Cookies("password") <> "" Then
        partCount = partCount + 1
        parts(partCount) = "password=" & Request.Cookies("password")
    End If

    If Request.Cookies("authenticated") <> "" Then
        partCount = partCount + 1
        parts(partCount) = "authenticated=" & Request.Cookies("authenticated")
    End If

    If Request.Cookies("sessionIDadmin") <> "" Then
        partCount = partCount + 1
        parts(partCount) = "sessionIDadmin=" & Request.Cookies("sessionIDadmin")
    End If

    If partCount < 0 Then
        pilotBuildCookieHeader = ""
    Else
        pilotBuildCookieHeader = Join(parts, "; ")
    End If
End Function

Function pilotFetch(url, params)
    Dim fullUrl, xml, cookieHeader
    fullUrl = url
    If params <> "" Then
        fullUrl = fullUrl & "?" & params
    End If

    Set xml = Server.CreateObject("MSXML2.ServerXMLHTTP.6.0")
    xml.setOption 2, 13056
    xml.setTimeouts 5000, 5000, 10000, 10000
    xml.Open "GET", fullUrl, False

    cookieHeader = pilotBuildCookieHeader()
    If cookieHeader <> "" Then
        xml.setRequestHeader "Cookie", cookieHeader
    End If

    On Error Resume Next
    xml.Send
    If Err.Number <> 0 Then
        pilotFetch = "UNAVAILABLE"
        Err.Clear
    Else
        pilotFetch = xml.responseText
    End If
    On Error GoTo 0

    Set xml = Nothing
End Function

Sub pilotWrite(url, params)
    Response.Write pilotFetch(url, params)
End Sub
%>
