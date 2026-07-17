<%
pilotWrite pilotManagedBase & "/chrome.ashx", _
    "part=footer&script=" & Server.URLEncode(pilotScriptPath)
closeAll
%>
