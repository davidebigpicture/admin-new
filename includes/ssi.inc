<%
Function pilotFetch(url, params)
    Dim fullUrl, xml, pilotCookie
    fullUrl = url
    If params <> "" Then
        fullUrl = fullUrl & "?" & params
    End If

    Set xml = Server.CreateObject("MSXML2.ServerXMLHTTP.6.0")
    xml.setOption 2, 13056
    xml.setTimeouts 5000, 5000, 10000, 10000
    xml.Open "GET", fullUrl, False

    pilotCookie = Request.Cookies("bp_admin_next")
    If pilotCookie <> "" Then
        xml.setRequestHeader "Cookie", "bp_admin_next=" & pilotCookie
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
