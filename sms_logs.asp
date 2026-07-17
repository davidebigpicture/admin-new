<% pageTitle = "SMS Logs (Pilot)"%>
<%Response.Buffer = True%>
<!--#include file="topshell.asp"-->
<!--#include virtual="/classes/cCodeValueO.inc"-->
<!--#include virtual="/classes/cUtilities.inc"-->
<!--#include virtual="/admin/admin/includes/tools.inc"-->
<!--#include virtual="/classes/aspJSON1.17.asp"-->
<%
Response.CodePage = 65001
Response.CharSet = "utf-8"
dim lineClass, switch
dim recipient, message, status, xmlNodes, xmlNode
dim connG, connectionstringG, wsURL, xml, strJSON, oJSON, provider, dataSource, userID, password
dim rsSMS

if Application("CLIENT_ID")&"" = "" then
    Application("CLIENT_ID") = getCodeValueDesc("APPLICATION_DB","CLIENT_ID")
end if

connectionStringG = Application("ConnectionStringG")&""
if connectionStringG = "" then
	wsURL = "https://ws.ebigpicture.com/GlobalDBConnectionInformation/"

	Set xml = Server.CreateObject("MSXML2.ServerXMLHTTP")
	xml.Open "GET", wsURL, False
	xml.setRequestHeader "Content-Type", "application/json"
	xml.Send()
	strJSON = xml.responseText
	set xml = nothing

	Set oJSON = New aspJSON
	oJSON.loadJSON(strJSON)
	provider = oJSON.data("Provider")
	dataSource = oJSON.data("Data Source")
	userID = oJSON.data("User ID")
	password = oJSON.data("Password")
	set oJSON = nothing

	connectionStringG = "Provider=" & provider & ";Data Source=" & dataSource & ";User ID=" & userID & ";Password=" & password & ";"
	Application("ConnectionStringG") = connectionStringG
end if

dbConnect connG,connectionStringG

if request("action") = "viewDate" then
    viewDate
elseif request("action") = "viewEntry" then
    viewEntry
else
    menu
end if

dbDisconnect connG
%>
<!--#include file="bottomshell.asp"-->
<%
sub viewEntry

set cSql = new cRunSql
cSql.Conn = connG
cSql.SqlStr = "SELECT * FROM SMS WHERE SMS_ID = ? AND CLIENT_ID = ?"
cSql.AddParam request("id")
cSql.AddParam Application("CLIENT_ID")
set rsSMS = cSql.execute
if not rsSMS.eof then
%>
<h1>SMS Log - View Message</h1>
<div align="right"><input type="button" value="List" name="list" class="adminButton" onClick="parent.location = '<%=request.servervariables("SCRIPT_NAME")%>?action=viewDate&date=<%=FormatDateTime(rsSMS("CREATE_DT"), vbShortDate)%>'"></div>
<table border="0" cellpadding="0" cellspacing="0" align="center" class="controlsDetail">
    <tbody>
        <tr>
            <td align="right">Create Date/Time:</td>
            <td>&nbsp;</td>
            <td><%=timeZoneAdjust(rsSMS("CREATE_DT"))%></td>
        </tr>
        <tr>
            <td align="right">Start Date/Time:</td>
            <td>&nbsp;</td>
            <td><%=timeZoneAdjust(rsSMS("START_DT"))%></td>
        </tr>
        <tr>
            <td align="right">Complete Date/Time:</td>
            <td>&nbsp;</td>
            <td><%=timeZoneAdjust(rsSMS("COMPLETE_DT"))%></td>
        </tr>
        <tr>
            <td align="right">Recipient:</td>
            <td>&nbsp;</td>
            <td><%=rsSMS("PHONE_NO")%></td>
        </tr>
        <tr>
            <td align="right">Message:</td>
            <td>&nbsp;</td>
            <td><%=rsSMS("MESSAGE")%></td>
        </tr>
        <tr>
            <td align="right">Status:</td>
            <td>&nbsp;</td>
            <td><%=rsSMS("STATUS")%></td>
        </tr>
        <%if rsSMS("STATUS") = "ERROR" then%>
        <tr>
            <td align="right">Error:</td>
            <td>&nbsp;</td>
            <td><textarea><%=rsSMS("ERROR")%></textarea></td>
        </tr>
        <%end if%>
    </tbody>
</table>
<%
end if
rsSMS.close
set rsSMS = nothing
end sub

sub viewDate
%>
<h1>SMS Log - View Date</h1>
<div align="right"><input type="button" value="List" name="list" class="adminButton" onClick="parent.location = '<%=request.servervariables("SCRIPT_NAME")%>'"></div>
<table border="0" cellpadding="0" cellspacing="0" align="center" width="50%" class="list">
    <thead>
        <tr class="adminTitlebar">
            <th>Date/Time</th>
            <th>Recipient</th>
            <th>Message</th>
            <th>Status</th>
        </tr>
    </thead>
    <tbody>
<%
set cSql = new cRunSql
cSql.Conn = connG
cSql.SqlStr = "SELECT SMS_ID,TO_CHAR(CREATE_DT, 'MM/DD/YYYY HH24:MI:SS') AS CREATE_DT,PHONE_NO,MESSAGE,STATUS FROM SMS WHERE CLIENT_ID = ? AND TRUNC(CREATE_DT) = TO_DATE(?, 'MM/DD/YYYY') ORDER BY CREATE_DT"
cSql.AddParam Application("CLIENT_ID")
cSql.AddParam request.querystring("date")
set rsSMS = cSql.execute
set cSql = nothing
switch = 0
do while not rsSMS.eof
    switch = 1 - switch
    if switch > 0 then
        lineClass = "activeLineItem1"
    else
        lineClass = "activeLineItem2"
    end if
%>
    <tr class="<%=lineClass%>">
        <td><a href="sms_logs.asp?action=viewEntry&id=<%=rsSMS("SMS_ID")%>"><%=timeZoneAdjust(rsSMS("CREATE_DT"))%></a></td>
        <td><%=rsSMS("PHONE_NO")%></td>
        <td><%=rsSMS("MESSAGE")%></td> 
        <td><%=rsSMS("STATUS")%></td>
    </tr>
    <%
    response.flush
    rsSMS.movenext
loop
%>
    </tbody>
</table>
<%
rsSMS.close
set rsSMS = nothing
end sub 

Sub menu
dim fileCount
%>
<h1>SMS Logs - List</h1>
<table border="0" cellpadding="0" cellspacing="0" align="center" width="50%" class="list">
    <thead>
        <tr class="adminTitlebar">
            <th></th>
            <th>Date</th>
            <th>Last Modified</th>
        </tr>
    </thead>
    <tbody>
<%
fileCount = 0
switch = 0

set cSql = new cRunSql
cSql.Conn = connG
cSql.SqlStr = "SELECT TO_CHAR(TRUNC(CREATE_DT), 'MM/DD/YYYY') AS CREATE_DATE,MAX(COMPLETE_DT) AS MAX_COMPLETE_DT FROM SMS WHERE CLIENT_ID = ? GROUP BY TRUNC(CREATE_DT) ORDER BY TRUNC(CREATE_DT) DESC"
cSql.AddParam Application("CLIENT_ID")
set rsSMS = cSql.execute
set cSql = nothing
do while not rsSMS.eof
    switch = 1 - switch
    if switch > 0 then
        lineClass = "activeLineItem1"
    else
        lineClass = "activeLineItem2"
    end if
    fileCount = fileCount + 1
%>
	<tr class="<%=lineClass%>">
        <td><%=fileCount%>.</td>
        <td><a href="sms_logs.asp?action=viewDate&date=<%=rsSMS("CREATE_DATE")%>"><%=rsSMS("CREATE_DATE")%></a></td>
        <td><%=timeZoneAdjust(rsSMS("MAX_COMPLETE_DT"))%></td>
    </tr>
<%
    response.flush
    rsSMS.movenext
loop
rsSMS.close
set rsSMS = nothing
%>
    </tbody>
</table>
<%
end sub
%>