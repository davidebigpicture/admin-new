<% pageTitle = "Login Log (Pilot)"%>
<!--#include file="topshell.asp"-->

<%'Response.Buffer = True%>
<%server.ScriptTimeout = 15400%>
<%'Session.Timeout = 240%>


<%
Set conn = Server.CreateObject("ADODB.Connection")
conn.Open Application("ConnectionString")

dim rs, loginLogID, startRec

loginLogID = request.querystring("login_log_id")
startRec = request.querystring("start")

if loginLogID = "" then
	mainMenu
else
	showDetails loginLogID,startRec
end if


sub mainMenu

dim recsPerPage, recStart, recEnd, numPages, pageNo, arrLoginLog, arrNumPages
dim arrLogin, rsLogin, login, search

login = ""
search = ""

if request("login") <> "" then
    login = request("login")
end if

if request("search") <> "" then
    search = request("search")
end if

sqlStr = "select login,first_name,last_name from login order by login"
set rsLogin = conn.execute(sqlStr)
if rsLogin.eof then
else
    arrLogin = rsLogin.GetRows
end if
rsLogin.close
set rsLogin = nothing

recsPerPage = 100

if search = "" then
    sqlStr = "select * from login_log "
    if login <> "" then
        sqlStr = sqlStr & " where login = '" & login & "' "
    end if
    sqlStr = sqlStr & "order by create_dt desc"
else
    sqlStr = "select * from login_log where 1=1 "
    if login <> "" then
        sqlStr = sqlStr & " and login = '" & login & "' "
    end if
    sqlStr = sqlStr & "and (upper(login) like upper('%" & search & "%') or upper(fail) like upper('%" & search & "%') " &_
        "or login_log_id in (select login_log_id from login_log_detail where upper(login_log_detail_value) like upper('%" & search & "%'))) "&_
        "order by create_dt desc"
end if

set rs = conn.execute(sqlStr)
%>
<script>
    function changeLogin(ddl) {
        document.getElementById("form").submit()
    }
</script>

<form name="form" id="form" action="<%=request.servervariables("SCRIPT_NAME")%>" method="post">
<table border="0" cellspacing="0" cellpadding="0" class="adminText" width="100%">
    <tr>
        <td align="right">login:</td>
        <td width="15%">
            <%if isArray(arrLogin) then%>
            <select name="login" id="login" onchange="changeLogin(this);">
                <option></option>
                <%for i = lbound(arrLogin,2) to ubound(arrLogin,2)%>
                <option value="<%=arrLogin(0,i)%>"<%if login = arrLogin(0,i) then%> selected<%end if%>><%=arrLogin(0,i)%> - <%=arrLogin(1,i)%>&nbsp;<%=arrLogin(2,i)%></option>
                <%next%>
            </select>
            <%end if%>
        </td>
    </tr>
    <tr>
        <td align="right">search:</td>
        <td>
            <input type="text" name="search" id="search" value="<%=search%>"/> <input type="submit" value="Search" class="adminButton" />
        </td>
    </tr>
</table>
</form>
<%
if rs.bof and rs.eof then
%>
	<div align="center" class="adminText">No login activity to report on.</div>
<%
else
    arrLoginLog = rs.GetRows()
    totRecs = ubound(arrLoginLog,2) + 1
    maxPosition = ubound(arrLoginLog,2) + 1

    recStart = 0

    if request("start") <> "" then
        recStart = request("start")
    end if

    recEnd = (recStart + recsPerPage) - 1
    
    if recEnd > ubound(arrLoginLog,2) then
        recEnd = ubound(arrLoginLog,2)
    end if
    
    totRecs = ubound(arrLoginLog,2) + 1
    
    if (totRecs mod recsPerPage) = 0 then
        numPages = totRecs / recsPerPage
    else
        arrNumPages = split((totRecs / recsPerPage),".")
        numPages = cint(arrNumPages(0)) + 1
    end if
    
    pageNo = (recStart + recsPerPage) / recsPerPage
    no = 0
%>
<table border="0" cellspacing="0" cellpadding="0" class="adminText" width="100%">
	<tr class="adminTitleBar">
		<td></td>
		<td>Login</td>
		<td>Fail</td>
		<td>Login Date/Time</td>
		<td>Logout Date/Time</td>
	</tr>
<%
    for idx = recStart to recEnd
		switch = 1 - switch
		no = idx + 1
		if switch > 0 then
			lineClass = "activeLineItem1"
		else
			lineClass = "activeLineItem2"
		end if
%>
	<tr class="<%=lineClass%>">
		<td><%=no%>.</td>
		<td><a href="<%=request.servervariables("SCRIPT_NAME")%>?login_log_id=<%=arrLoginLog(0,idx)%>&start=<%=recStart%><%if login <> "" then%>&login=<%=login%><%end if%><%if search <> "" then%>&search=<%=search%><%end if%>" class="adminLink"><%=arrLoginLog(6,idx)%></a></td>
		<td><a href="<%=request.servervariables("SCRIPT_NAME")%>?login_log_id=<%=arrLoginLog(0,idx)%>&start=<%=recStart%><%if login <> "" then%>&login=<%=login%><%end if%><%if search <> "" then%>&search=<%=search%><%end if%>" class="adminLink"><%=arrLoginLog(4,idx)%></a></td>
		<td><%=arrLoginLog(5,idx)%></td>
		<td><%=arrLoginLog(7,idx)%></td>
	</tr>
<%
        response.flush
    next
%>
</table>

<div align="center" class="adminTitlebar">
<%
for i = 1 to numPages
    if i > 1 and i <= numPages and (((i-1) mod 10) <> 0) then
        response.Write "&nbsp;&#183;&nbsp;"
    end if
    if pageNo <> i then
        nextRecStart = (i-1) * recsPerPage
%>
    <a href="<%=request.servervariables("SCRIPT_NAME")%>?start=<%=nextRecStart%><%if login <> "" then%>&login=<%=login%><%end if%><%if search <> "" then%>&search=<%=search%><%end if%>" class="adminNavLink">
    <%
    end if
    response.Write i
    if pageNo <> i then
    %>
    </a>
    <%
    end if
    if (i mod 10) = 0 then
        response.Write "<br>"
    end if
next
%>
</div>
<%if totRecs > 0 then%>
<div align="center" class="lineItemsTotal">
    <%=recStart+1%>-<%=recEnd+1%> of <%=totRecs%> values
</div>
<%end if%>

<%
end if

rs.close
set rs = nothing

end sub

sub showDetails(loginLogID, startRec)

dim strList, login, search

login = request("login")
search = request("search")

strList = request.ServerVariables("SCRIPT_NAME") & "?start=" & startRec
    
if login <> "" then
    strList = strList & "&login=" & login
end if
if search <> "" then
    strList = strList & "&search=" & search
end if
%>

<div align="center" class="adminTitle">Login Details</div><br><br>

<table border="0" cellspacing="0" cellpadding="0" class="adminText" width="100%">

<%
sqlStr = "select * from login_log where login_log_id = "&loginLogID

set rs = conn.execute(sqlStr)
if rs.eof and rs.bof then
else
%>
<tr>
   	<td align="right" colspan="3">
		<input type="button" value="List" name="list" class="adminButton" onMouseOver="this.className='adminButtonHover'" onMouseOut="this.className='adminButton'" onClick="parent.location = '<%=strList%>'">
	</td>
</tr>
<tr>
	<td align="right">Login:</td>
	<td>&nbsp&nbsp&nbsp;</td>
	<td><%=rs("LOGIN")%>
</tr>
<tr>
	<td align="right">Fail:</td>
	<td>&nbsp&nbsp&nbsp;</td>
	<td><%=rs("FAIL")%>
</tr>
<tr>
	<td align="right">Login Date/Time:</td>
	<td>&nbsp&nbsp&nbsp;</td>
	<td><%=rs("CREATE_DT")%>
</tr>

</table>
<%
end if
rs.close
set rs = nothing

sqlStr = "select * from login_log_detail where login_log_id = "&loginLogID&" order by login_log_detail_key"
set rs = conn.execute(sqlStr)

if rs.eof and rs.bof then
else
%>
<br><br><div class="adminSubTitle" align="center" width="100%">Login Details</div><br><br>
<table border="0" cellspacing="0" cellpadding="0" class="adminText" width="100%">
<tr>
	<td width="35%">Key</td>
	<td>&nbsp&nbsp&nbsp;</td>
	<td width="60%">Value</td>
</tr>
<%do while not rs.eof
        switch = 1 - switch
        if switch > 0 then
                lineClass = "activeLineItem1"
        else
                lineClass = "activeLineItem2"
        end if
%>
<tr class="<%=lineClass%>">
	<td><%=rs("LOGIN_LOG_DETAIL_KEY")%></td>
	<td>&nbsp&nbsp&nbsp;</td>
	<td><%=rs("LOGIN_LOG_DETAIL_VALUE")%></td>
</tr>
<%
	rs.MoveNext
loop
end if
rs.close
set rs = nothing
%>
</table>
<%
end sub

conn.close
set conn = nothing
%>
<!--#include file="bottomshell.asp"-->