<% pageTitle = "DB: Views (Pilot)"%>
<!--#include file="topshell.asp"-->
<!--#include virtual="/admin/admin/includes/tools.inc"-->
<%Response.Buffer = True%>

<%
dim scriptName, connOracle, fieldType, rsOrgSubTypeCodes, tableName, maxRows, maxPosition, rsWindowShadeCodes, rs, maxViewAll, rsGroupCodes
dim rsColumns, fieldArr, columnID, columnDetailGroupID, arrFields(10)

arrFields(0) = "COLUMN_TY"
arrFields(1) = "GROUP_CD"
arrFields(2) = "WINDOW_SHADE_CD"
arrFields(3) = "ROW_NUM"
arrFields(4) = "POSITION"
arrFields(5) = "VIEW_ALL_ORDER"
arrFields(6) = "MASS_UPDATE"
arrFields(7) = "EXPORT"
arrFields(8) = "MODIFY_VALUES"
arrFields(9) = "REPORT_BUILDER_DEFAULT"
arrFields(10) = "READONLY"

maxRows = 50
maxPosition = 10
maxViewAll = 0

scriptName = "views.asp"

Set connOracle = Server.CreateObject("ADODB.Connection") 
connOracle.Open Application("ConnectionString") 

'ShowQueryStringVariables
'ShowFormVariables

if request("action") = "add" then
	modify ""
elseif request("action") = "modify" then
	modify request("column_detail_group_id")
elseif request("action") = "deleteView" then
	doDelete
    reloadCache(connOracle)
	mainMenu
elseif request("action") = "doModify" then
	modifyField request("column_detail_group_id"), request("row_number"), request("position"), request("window_shade_cd"), request("view_all_order"), request("export"), request("mass_update"), request("modify_values"), request("report_builder_default"), request("readonly")
    reloadCache(connOracle)
	mainMenu
elseif request("action") = "doAdd" then
	fieldArr = split(request("column_id"),"|")
	fieldType = fieldArr(0)
	columnID = fieldArr(1)
	addField fieldType, columnID, request("group_cd"), request("org_sub_ty_cd"), request("window_shade_cd"), request("row_number"), request("position"), request("view_all_order"), request("export"), request("mass_update"), request("modify_values"), request("report_builder_default"), request("readonly")
    reloadCache(connOracle)
	mainMenu
elseIf request("action") = "updateVarious" then
	updateVarious request("column_detail_group_id"), request("new_val"), request("column")
    reloadCache(connOracle)
	mainMenu
elseif request("action") = "copy" then
    copy request("org_sub_ty_cd"), request("group_cd"), request("target_group")
    reloadCache(connOracle)
    redirectForm "views.asp?group_cd="&request("target_group")&"&org_sub_ty_cd="&request("org_sub_ty_cd")
elseif request("action") = "doExport" then
	doExport
	mainMenu
elseif request("action") = "finishImport" then
	finishImport
	mainMenu
elseif request("action") = "doImport" then
	doImport
elseif request("action") = "doReadOnly" then
	doReadOnly
    reloadCache(connOracle)
	mainMenu
else
	mainMenu
end if

if request("action")&"" <> "" then
    reloadCache(connOracle)
end if

sub doImport()
%>
<form name="form" id="form1" action="<%=scriptName%>" method="post">
<input type="hidden" name="action" id="Hidden1" value="finishImport" />
<div align="center" >
    Paste the export text into the box below and click "Submit".<br />
    <textarea rows="30" cols="150" name="importfields" id="importfields"></textarea>
    <br />
	<input type="submit" value="Submit" class="adminButton">

</div>
</form>
<%
end sub

sub finishImport

dim arrTextArea, i, j, k, arrImportFields, importTable

arrTextArea = split(request.Form("importfields"),vbcrlf)

set cSql = new cRunSql
cSql.Conn = connOracle
cSql.DisplayErrors = true

for i = lbound(arrTextArea) to ubound(arrTextArea)
    if arrTextArea(i)&"" <> "" and instr(arrTextArea(i)&"","|") then
        arrImportFields = split(arrTextArea(i),"|")
        if arrImportFields(2) = "DETAIL" then
            importTable = "ORG_COLUMN_DETAIL"
        else
            importTable = "MEMBERSHIP_COLUMN_DETAIL"
        end if
        cSql.SqlStr = "select column_id from " & importTable & " where column_desc = ? and org_sub_ty_cd = ?"
        cSql.AddParam(arrImportFields(0))
        cSql.AddParam(arrImportFields(1))
        set rs = cSql.Execute
        if rs.eof then
        else
            cSql.SqlStr = "insert into column_detail_group ("
            cSql.SqlStr = cSql.SqlStr & "column_id,"
            for k = lbound(arrFields) to ubound(arrFields)
                cSql.SqlStr = cSql.SqlStr & arrFields(k)
                if k < ubound(arrFields) then
                    cSql.SqlStr = cSql.SqlStr & ","
                end if
            next
            cSql.SqlStr = cSql.SqlStr & ") values ("
            cSql.SqlStr = cSql.SqlStr & rs("column_id") & ","
            for k = lbound(arrFields) to ubound(arrFields)
                j = k + 2
                cSql.SqlStr = cSql.SqlStr & "?"
                if k < ubound(arrFields) then
                    cSql.SqlStr = cSql.SqlStr & ","
                end if
                if k = 0 then
                    cSql.AddParam(replaceSpaces(arrImportFields(j)))
                else
                    cSql.AddParam(arrImportFields(j))
                end if
            next
            cSql.SqlStr = cSql.SqlStr & ")"
            'cSql.Debug
            cSql.Execute
            response.write arrImportFields(0) & " imported<br>"
        end if
    end if
next

set cSql = nothing

response.Write "<div align=""center"">Import complete</div>"

end sub


sub doExport()
dim maxExportRows
maxExportRows = 40
%>
<div align="center" >
    Copy the following contents and paste into the importer on the destination server.
    <br />
    <textarea rows="<%=maxExportRows%>" cols="75" name="exportfields" id="exportfields">
<%
dim appRenewCode, rsExport, i, numRows, columnID, arrCol

appRenewCode = request("app_renew_cd")

set cSql = new cRunSql
cSql.conn = connOracle
cSql.DisplayErrors = true
cSql.SqlStr = "select column_desc,org_sub_ty_cd,"
numRows = 0
for i = lbound(arrFields) to ubound(arrFields)
    cSql.SqlStr = cSql.SqlStr & "column_detail_group." & arrFields(i)
    if i < ubound(arrFields) then
        cSql.SqlStr = cSql.SqlStr & ","
    end if
next
cSql.SqlStr = cSql.SqlStr & " from org_column_detail, column_detail_group where org_column_detail.column_id = column_detail_group.column_id and column_detail_group.column_detail_group_id in ("
for i = 1 to request("deleteView").Count
    cSql.SqlStr = cSql.SqlStr & "?"
    cSql.AddParam(request("deleteView")(i))
    if i < request("deleteView").Count then
        cSql.SqlStr = cSql.SqlStr & ","
    end if
next
cSql.SqlStr = cSql.SqlStr & ")"
cSql.SqlStr = cSql.SqlStr & " union select column_desc,org_sub_ty_cd,"
numRows = 0
for i = lbound(arrFields) to ubound(arrFields)
    cSql.SqlStr = cSql.SqlStr & "column_detail_group." & arrFields(i)
    if i < ubound(arrFields) then
        cSql.SqlStr = cSql.SqlStr & ","
    end if
next
cSql.SqlStr = cSql.SqlStr & " from membership_column_detail, column_detail_group where membership_column_detail.column_id = column_detail_group.column_id and column_detail_group.column_detail_group_id in ("
for i = 1 to request("deleteView").Count
    cSql.SqlStr = cSql.SqlStr & "?"
    cSql.AddParam(request("deleteView")(i))
    if i < request("deleteView").Count then
        cSql.SqlStr = cSql.SqlStr & ","
    end if
next
cSql.SqlStr = cSql.SqlStr & ")"
'cSql.Debug
'response.end
set rsExport = cSql.Execute

do while not rsExport.eof
    response.Write rsExport("column_desc") & "|"
    response.Write rsExport("org_sub_ty_cd") & "|"
    for i = lbound(arrFields) to ubound(arrFields)
        response.Write replace(rsExport(arrFields(i))&"",vbcrlf,"<br>")
        if i < ubound(arrFields) then
            response.Write "|"
        end if
    next
    response.Write vbcrlf
    numRows = numRows + 1
    rsExport.MoveNext
loop
numRows = numRows + 1
%>
    </textarea>
</div><br /><br />
<%if numRows < maxExportRows then%>
<script language="javascript">
    document.getElementById("exportfields").rows = "<%=numRows%>";
</script>
<%
end if
rsExport.close
set rsExport = nothing

set cSql = nothing
end sub

sub copy (orgSubTyCd, oldGroup, newGroup)

for i = 1 to request.form("deleteView").count
    sqlStr = "select column_id from column_detail_group where column_detail_group_id = " & request.form("deleteView")(i)
    set rs = connOracle.Execute(sqlStr)
    if rs.eof then
    else
        columnID = rs("column_id")
        sqlStr = "delete from column_detail_group where group_cd = '" &newGroup & "' and column_id = " & columnID
        connOracle.execute(sqlStr)

        columnDetailGroupID = getSequence("SEQ_COLUMN_ID")

        sqlStr = "insert into column_detail_group (column_detail_group_id, column_id, column_ty, group_cd, window_shade_cd, row_num, position, view_all_order, mass_update, export, readonly) " &_
            "select " & columnDetailGroupID & ",c.column_id, c.column_ty, '"&newGroup&"', c.window_shade_cd, c.row_num, c.position, c.view_all_order, c.mass_update, c.export, c.readonly " &_
            "from column_detail_group c, membership_column_detail m " &_
            "where m.column_id = c.column_id and c.column_ty = 'MEMBER' " &_
            "and c.group_cd = '"&oldGroup&"' " &_
            "and m.org_sub_ty_cd = '"&orgSubTyCd&"' " &_
            "and m.column_id = " & columnID & " " &_
            "union " &_
            "select " & columnDetailGroupID & ",c.column_id, c.column_ty, '"&newGroup&"', c.window_shade_cd, c.row_num, c.position, c.view_all_order, c.mass_update, c.export, c.readonly " &_
            "from column_detail_group c, org_column_detail m " &_
            "where m.column_id = c.column_id and c.column_ty = 'DETAIL' " &_
            "and c.group_cd = '"&oldGroup&"' " &_
            "and m.org_sub_ty_cd = '"&orgSubTyCd&"' " &_
            "and m.column_id = " & columnID
    
        connOracle.execute(sqlStr)

        sqlStr = "delete from p_g_a_finder where group_ty_cd = '"&newGroup&"' and permission_cd like '%876%' " &_
            "and permission_cd in (select 'MEMBERR876'||column_desc from membership_column_detail m, column_detail_group c " &_
            "where m.column_id = c.column_id and c.column_ty = 'MEMBER' " &_
            "and c.group_cd = '"&oldGroup&"' " &_
            "and m.org_sub_ty_cd = '"&orgSubTyCd&"' " &_
            "and m.column_id = " & columnID & " " &_
            "union " &_
            "select 'DETAIL876'||column_desc  " &_
            "from column_detail_group c, org_column_detail m " &_
            "where m.column_id = c.column_id and c.column_ty = 'DETAIL' " &_
            "and c.group_cd = '"&oldGroup&"' " &_
            "and m.column_id = " & columnID & " " &_
            "and m.org_sub_ty_cd = '"&orgSubTyCd&"') "
    
        connOracle.execute(sqlStr)

        sqlStr = "select readonly from column_detail_group where column_id = " & columnID 
        set rs = connOracle.execute(sqlStr)
        if rs.eof then
        else
            if rs("readonly")&"" = "" then
                sqlStr = "select action_cd from p_g_a_finder where group_ty_cd = '"&oldGroup&"' and permission_cd = " &_
                    "(select 'MEMBER876'||column_desc from membership_column_detail m, column_detail_group c " &_
                    "where m.column_id = c.column_id and c.column_ty = 'MEMBER' " &_
                    "and c.group_cd = '"&oldGroup&"' " &_
                    "and m.org_sub_ty_cd = '"&orgSubTyCd&"' " &_
                    "and m.column_id = " & columnID & " " &_
                    "union " &_
                    "select 'DETAIL876'||column_desc  " &_
                    "from column_detail_group c, org_column_detail m " &_
                    "where m.column_id = c.column_id and c.column_ty = 'DETAIL' " &_
                    "and c.group_cd = '"&oldGroup&"' " &_
                    "and m.column_id = " & columnID & " " &_
                    "and m.org_sub_ty_cd = '"&orgSubTyCd&"') "
                set rs = connOracle.execute(sqlStr)
                if rs.eof then
                else
                    readonly = "Y"
                    if rs("action_cd")&"" = "2" then
                        readonly = "N"
                    end if
                    sqlStr = "update column_detail_group set readonly = '" & readOnly & "' where column_detail_group_id = " & columnDetailGroupID
                    connOracle.execute(sqlstr)
                end if
            end if
        end if
    end if
    rs.close
    set rs = nothing
next

end sub

sub doDelete
dim i
for i = 1 to request("deleteView").Count
	sqlStr = "delete from column_detail_group where column_detail_group_id = "&request("deleteView")(i)
	connOracle.execute(sqlStr)
next
end sub

sub doReadOnly
dim i
for i = 1 to request("deleteView").Count
	sqlStr = "update column_detail_group set readonly = 'Y' where column_detail_group_id = "&request("deleteView")(i)
	connOracle.execute(sqlStr)
next
end sub

sub modify(columnDetailGroupID)

dim fieldType, rsColumnDetails
dim rsGroupCodes, groupCode, rsColumnDetail, rsPGA
dim orgSubTypeCode, windowShadeCode, rowNumber, position, i, required, viewAll, rsCodeValue, export, massUpdate, modifyValues, reportBuilderDefault, readOnly

sqlStr = getCodeValuesO("GROUP_TY_CD")
set rsGroupCodes = connOracle.execute(sqlStr)

sqlStr = getCodeValuesO("ORG_SUB_TY_CD")
set rsOrgSubTypeCodes = connOracle.execute(sqlStr)

sqlStr = getCodeValuesO("WINDOW_SHADE_CD")
set rsWindowShadeCodes = connOracle.execute(sqlStr)

if len(columnDetailGroupID) > 0 then
	sqlStr = getColumnDetailGroup(columnDetailGroupID)
	set rs = connOracle.execute(sqlStr)
	columnID = rs("COLUMN_ID")
	groupCode = rs("GROUP_CD")
	windowShadecode = rs("WINDOW_SHADE_CD")
	rowNumber = rs("ROW_NUM")
	position = rs("POSITION")
        viewAll = rs("VIEW_ALL_ORDER")
        export = rs("EXPORT")
        massUpdate = rs("MASS_UPDATE")
	groupCode = rs("GROUP_CD")
	reportBuilderDefault = rs("REPORT_BUILDER_DEFAULT")
	modifyValues = rs("MODIFY_VALUES")
	if rs("COLUMN_TY") = "MEMBER" then
		sqlStr = getMembershipColumnDetail(rs("COLUMN_ID"))
	else
		sqlStr = getOrgColumnDetail(rs("COLUMN_ID"))
	end if
	set rsColumnDetail = connOracle.execute(sqlStr)
	if rsColumnDetail.BOF and rsColumnDetail.EOF then
		orgSubTypeCode = ""
	else
		orgSubTypeCode = rsColumnDetail("ORG_SUB_TY_CD")
	end if
    if rs("READONLY")&"" <> "" then
        readOnly = rs("READONLY")
    else
	    sqlStr = "select ACTION_CD from p_g_a_finder where page_cd = 'MASTERVIEW' and group_ty_cd = '"&groupCode&"' and org_id = '"&Application("ORG_ID")&"' and permission_cd = '"&rs("COLUMN_TY")&"876"&rsColumnDetail("COLUMN_DESC")&"'"
	    set rsPGA = connOracle.execute(sqlStr)
	    if rsPGA.EOF and rsPGA.BOF then
		    permissionCode = ""
            readOnly = "N"
	    else
            if rsPGA("ACTION_CD")&"" = "2" then
                readOnly = "N"
            else
                readOnly = "Y"
            end if
	    end if
    end if
else
	columnID = ""
	groupCode = request.form("group_cd")
	orgSubTypeCode = request.form("org_sub_ty_cd")
	windowShadeCode = request.form("window_shade_cd")
	rowNumber = ""
	position = ""
        viewAll = ""
        export = "Y"
        massUpdate = "N"
        reportBuilderDefault = ""
	sqlStr = getColumnsNotInAView(orgSubTypeCode,windowShadeCode,groupCode)
	set rsColumns = connOracle.execute(sqlStr)
	maxViewAll = maxViewAll + 1
	permissionCode = "2"
	modifyValues = ""
    readOnly = "N"
end if

sqlStr = getColumnDetailGroups(orgSubTypeCode, "", groupCode)
set rsColumnDetails = connOracle.execute(sqlStr)

if rsColumnDetails.EOF and rsColumnDetails.BOF then
else
	do while not rsColumnDetails.EOF
		maxViewAll = maxViewAll + 1
		rsColumnDetails.MoveNext
	loop
end if

%>

<script language="Javascript">
<!--


// loadForm
function loadForm() {
	document.form.action.value="add";
	document.form.submit();
}


// verify
function verify() {
	var retval = true;

	var errMsg = "";

	if (document.form.column_id.value == "")
		errMsg += " - Field is required.\n";
		
<%if len(columnID) > 0 then%>
<%else%>
	if (document.form.group_cd.value == "")
		errMsg += " - Group is required.\n";
		
	if (document.form.org_sub_ty_cd.value == "")
		errMsg += " - Record Type is required.\n";
		
<%end if%>	
	if (document.form.row_number.value == "")
		errMsg += " - Row Number is required.\n";
		
	if (document.form.position.value == "")
		errMsg += " - Position is required.\n";
		
	if (document.getElementById("export").value == "")
		errMsg += " - Export is required.\n";
		
	if (document.form.mass_update.value == "")
		errMsg += " - Mass Update is required.\n";
		
	if (document.form.readonly.value == "")
		errMsg += " - Read Only is required.\n";
		
	if (errMsg != "") {
		var errorMsg = "Please, correct the follow error(s):\n\n";
		errorMsg += errMsg;

		alert(errorMsg);
		retval = false;
	}

	return retval;
}
-->
</script>

<form action="<%=scriptName%>" method="post" name="form" id="form">
<input type="hidden" name="action" value="<%if request("action") = "modify" then%>doModify<%else%>doAdd<%end if%>">
<%if len(columnDetailGroupID) > 0 then%>
<input type="hidden" name="column_detail_group_id" value="<%=columnDetailGroupID%>">
<input type="hidden" name="org_sub_ty_cd" value="<%=orgSubTypeCode%>">
<input type="hidden" name="group_cd" value="<%=groupCode%>">
<%end if%>

<table cellspacing="2" cellpadding="2" border="0" width="100%">
<tr> 
     <td colspan="3" align="center" class="adminSubTitle"><%if request("action") = "modify" then%>Modify<%else%>Add<%end if%> Field</td>
</tr>
<tr>
	<td align="center" colspan="3" class="adminText">
		<table width="100%" border="0" cellspacing="0" cellpadding="1">
		<tr>
			<td align="left">
				<input type="button" value="Cancel" name="cancel" class="adminButton" onMouseOver="this.className='adminButtonHover'" onMouseOut="this.className='adminButton'" onClick="parent.location='<%=scriptName%>?record_type=<%=orgSubTypeCode%>'">
			</td>
			<td align="right">
				<input type="button" value="List" name="list" class="adminButton" onMouseOver="this.className='adminButtonHover'" onMouseOut="this.className='adminButton'" onClick="parent.location='<%=scriptName%>?record_type=<%=orgSubTypeCode%>'">
			</td>
		</tr>
		</table>
		<hr size="1" color="#000000">
	</td>
</tr>
	
<%if len(columnDetailGroupID) > 0 then%>
<tr class="adminText">
	<td align="right" valign="middle" class="adminText">
	Field:
	</td>
	<td>
		<%if rsColumnDetail.EOF and rsColumnDetail.BOF then%><%else%><%=rsColumnDetail("COLUMN_RPT_DESC")%><%end if%>
	</td>
</tr>
	<%
	sqlStr = getCodeValueO("GROUP_TY_CD",groupCode)
	set rsCodeValue = connOracle.execute(sqlStr)
	%>
<tr class="adminText">
	<td align="right" valign="middle" class="adminText">
	Group:
	</td>
	<td>
		<%if rsCodeValue.BOF and rsCodeValue.EOF then%><%=groupCode%><%else%><%=rsCodeValue("CODE_VALUE_DESC")%><%end if%>
	</td>
</tr>
	<%
	rsCodeValue.close
	set rsCodeValue = nothing
	%>
	<%
	if orgSubTypeCode <> "" then
		sqlStr = getCodeValueO("ORG_SUB_TY_CD",orgSubTypeCode)
		set rsCodeValue = connOracle.execute(sqlStr)
	%>
<tr class="adminText">
	<td align="right" valign="middle" class="adminText">
	<a href="javascript:manageCodesO('ORG_SUB_TY_CD');" class="adminLink">Record Type</a>:
	</td>
	<td>
		<%if rsCodeValue.BOF and rsCodeValue.EOF then%><%=groupCode%><%else%><%=rsCodeValue("CODE_VALUE_DESC")%><%end if%>
	</td>
</tr>
	<%
		rsCodeValue.close
		set rsCodeValue = nothing
	end if
	%>

<%else%>
<tr class="adminText">
	<td align="right" valign="middle" class="adminText">
	Field<span class="adminRequiredField">*</span>:
	</td>
	<td>
		<select name="column_id" class="adminText">
			<option value=""></option>
			<%do while not rsColumns.eof%>
				<option value="<%=rsColumns("TYPE")%>|<%=rsColumns("COLUMN_ID")%>"<% if rsColumns("TYPE")&"|"&rsColumns("COLUMN_ID") = request("column_id") then%> selected<%end if%>><%=rsColumns("COLUMN_RPT_DESC")%></option>
			<%
				rsColumns.MoveNext
			loop
			%>
		</select>
	</td>
</tr>

<tr class="adminText">
	<td align="right" valign="middle" class="adminText">
	<a href="javascript:manageCodesO('GROUP_TY_CD');" class="adminLink">Group</a><span class="adminRequiredField">*</span>:
	</td>
	<td>
		<select name="group_cd" class="adminText" onChange="loadForm()">
			<option value=""></option>
			<%do while not rsGroupCodes.eof%>
				<option value="<%=rsGroupCodes("CODE_VALUE")%>"<%if groupCode = rsGroupCodes("CODE_VALUE") then%> selected<%end if%>><%=rsGroupCodes("CODE_VALUE_DESC")%></option>
			<%
				rsGroupCodes.MoveNext
			loop
			%>
		</select>
	</td>
</tr>

<tr class="adminText">
	<td align="right" valign="middle" class="adminText">
	<a href="javascript:manageCodesO('ORG_SUB_TY_CD');" class="adminLink">Record Type</a><span class="adminRequiredField">*</span>:
	</td>
	<td>
		<select name="org_sub_ty_cd" class="adminText" onChange="loadForm()">
			<%do while not rsOrgSubTypeCodes.eof%>
				<option value="<%=rsOrgSubTypeCodes("CODE_VALUE")%>"<%if orgSubTypeCode = rsOrgSubTypeCodes("CODE_VALUE") then%> selected<%end if%>><%=rsOrgSubTypeCodes("CODE_VALUE_DESC")%></option>
			<%
				rsOrgSubTypeCodes.MoveNext
			loop
			%>
		</select>
	</td>
</tr>

<%end if%>
<tr class="adminText">
	<td align="right" valign="middle" class="adminText">
		<a href="javascript:manageCodesO('WINDOW_SHADE_CD');" class="adminLink">Window Shade<a href="javascript:manageCodesO('ORG_SUB_TY_CD');" class="adminLink">:
	</td>
	<td>
		<select name="window_shade_cd" class="adminText"<%if len(columnDetailGroupID) > 0 then%><%else%> onChange="loadForm()"<%end if%>>
			<option value=""></option>
			<%do while not rsWindowShadeCodes.eof%>
				<option value="<%=rsWindowShadeCodes("CODE_VALUE")%>"<%if windowShadeCode = rsWindowShadeCodes("CODE_VALUE") then%> selected<%end if%>><%=rsWindowShadeCodes("CODE_VALUE_DESC")%></option>
			<%
				rsWindowShadeCodes.MoveNext
			loop
			%>
		</select>
	</td>
</tr>

<tr class="adminText">
	<td align="right" valign="middle" class="adminText">
	Row Number<span class="adminRequiredField">*</span>:
	</td>
	<td>
		<select name="row_number" class="adminText">
			<%for i = 1 to maxRows%>
				<option value="<%=i%>"<%if len(columnDetailGroupID) > 0 then%><%if not isNull(rowNumber) then%><%if CINT(rowNumber) = i then%> selected<%end if%><%end if%><%end if%>><%=i%></option>
			<%
			next
			%>
		</select>
	</td>
</tr>

<tr class="adminText">
	<td align="right" valign="middle" class="adminText">
	Position<span class="adminRequiredField">*</span>:
	</td>
	<td>
		<select name="position" class="adminText">
			<%for i = 1 to maxPosition%>
				<option value="<%=i%>"<%if len(columnDetailGroupID) > 0 then%><%if not isNull(position) then%><%if CINT(position) = i then%> selected<%end if%><%end if%><%end if%>><%=i%></option>
			<%
			next
			%>
		</select>
	</td>
</tr>

<tr class="adminText">
	<td align="right" valign="middle" class="adminText">
	View All Order:
	</td>
	<td>
		<select name="view_all_order" class="adminText">
                    <option value=""></option>
			<%for i = 1 to maxViewAll%>
				<option value="<%=i%>"<%if len(columnDetailGroupID) > 0 then%><%if not isNull(viewAll) then%><%if CINT(viewAll) = i then%> selected<%end if%><%end if%><%end if%>><%=i%></option>
			<%
			next
			%>
		</select>
	</td>
</tr>

<tr class="adminText">
	<td align="right" valign="middle" class="adminText">
	Export<span class="adminRequiredField">*</span>:
	</td>
	<td>
		<select name="export" class="adminText">
			<option value="N"<%if export = "N" then%> selected<%end if%>>N</option>
			<option value="Y"<%if export = "Y" then%> selected<%end if%>>Y</option>
		</select>
	</td>
</tr>

<tr class="adminText">
	<td align="right" valign="middle" class="adminText">
	Mass Update<span class="adminRequiredField">*</span>:
	</td>
	<td>
	<%if len(columnDetailGroupID) > 0 then%>
		<%if rsColumnDetail.EOF and rsColumnDetail.BOF then%>
			<select name="mass_update" class="adminText">
				<option value="N"<%if massUpdate = "N" then%> selected<%end if%>>N</option>
				<option value="Y"<%if massUpdate = "Y" then%> selected<%end if%>>Y</option>
			</select>
		<%elseif rsColumnDetail("COLUMN_DESC") = "CREATE_DT" then%>
			<input type="hidden" name="mass_update" id="mass_update" value="N">
			N
		<%else%>
			<select name="mass_update" class="adminText">
				<option value="N"<%if massUpdate = "N" then%> selected<%end if%>>N</option>
				<option value="Y"<%if massUpdate = "Y" then%> selected<%end if%>>Y</option>
			</select>
		<%end if%>
	<%else%>
			<select name="mass_update" class="adminText">
				<option value="N"<%if massUpdate = "N" then%> selected<%end if%>>N</option>
				<option value="Y"<%if massUpdate = "Y" then%> selected<%end if%>>Y</option>
			</select>
	<%end if%>
	</td>
</tr>

<tr class="adminText">
	<td align="right" valign="middle" class="adminText">
	Read Only<span class="adminRequiredField">*</span>:
	</td>
	<td>
	<%if len(columnDetailGroupID) > 0 then%>
		<%if rsColumnDetail.EOF and rsColumnDetail.BOF then%>
			<select name="readonly" class="adminText">
				<option value=""></option>
				<option value="N"<%if readOnly = "N" then%> selected<%end if%>>N</option>
				<option value="Y"<%if readOnly = "Y" then%> selected<%end if%>>Y</option>
			</select>
		<%elseif rsColumnDetail("COLUMN_DESC") = "CREATE_DT" then%>
			<input type="hidden" name="readonly" id="readonly" value="1">
			Y
		<%else%>
			<select name="readonly" class="adminText">
				<option value=""></option>
				<option value="N"<%if readOnly = "N" then%> selected<%end if%>>N</option>
				<option value="Y"<%if readOnly = "Y" then%> selected<%end if%>>Y</option>
			</select>
		<%end if%>
	<%else%>
			<select name="readonly" class="adminText">
				<option value=""></option>
				<option value="N"<%if readOnly = "N" then%> selected<%end if%>>N</option>
				<option value="Y"<%if readOnly = "Y" then%> selected<%end if%>>Y</option>
			</select>
	<%end if%>
	</td>
</tr>
<%
if len(columnDetailGroupID) > 0 then
	if rs("COLUMN_TY") = "DETAIL" then
		sqlStr = getOrgColumnDetail(rs("COLUMN_ID"))
	else
		sqlStr = getMembershipColumnDetail(rs("COLUMN_ID"))
	end if
	set rs2 = connOracle.execute(sqlStr)
	if rs2("CODE_CLASS")&"" <> "" then
%>
<tr class="adminText">
	<td align="right" valign="middle" class="adminText">
	Edit Available Values:
	</td>
	<td>
		<select name="modify_values" class="adminText">
			<option value="N"<%if modifyValues = "N" then%> selected<%end if%>>N</option>
			<option value="Y"<%if modifyValues = "Y" or modifyValues = "" then%> selected<%end if%>>Y</option>
		</select>
	</td>
</tr>
<%
	end if
	rs2.close
	set rs2 = nothing
end if
%>

<tr class="adminText">
	<td align="right" valign="middle" class="adminText">
	Report Builder Default Field<span class="adminRequiredField">*</span>:
	</td>
	<td>
		<select name="report_builder_default" class="adminText">
			<option value="N"<%if reportBuilderDefault = "N" then%> selected<%end if%>>N</option>
			<option value="Y"<%if reportBuilderDefault = "Y" then%> selected<%end if%>>Y</option>
		</select>
	</td>
</tr>

<tr class="adminText">
	<td colspan="2" align="center">
		<input type="submit" value="Record" onClick="return verify();" class="adminButton" onMouseOver="this.className='adminButtonHover'" onMouseOut="this.className='adminButton'">
	</td>
</tr>

<tr class="adminText">
	<td colspan="2" align="center">
		<span class="adminRequiredField">*</span>indicates a required field
	</td>
</tr>
</form>

<%
if len(columnDetailGroupID) > 0 then
	rs.close
	set rs = nothing
	rsColumnDetail.close
	set rsColumnDetail = nothing
else
	rsColumns.close
	set rsColumns = nothing
end if
rsGroupCodes.close
set rsGroupCodes = nothing
rsOrgSubTypeCodes.close
set rsOrgSubTypeCodes = nothing
rsWindowShadeCodes.close
set rsWindowShadeCodes = nothing
rsColumnDetails.close
set rsColumnDetails = nothing
%>
<%end sub%>

<%sub mainMenu%>

<script src="/admin/admin/js/jquery.jeditable.js" type="text/javascript" charset="utf-8"></script>
<%
dim switch, lineClass, lineCounter, rsCodeValue, orgSubTypeCode, windowShadeCode, i, groupCode, rsColumnDetail, rsPGA, readOnly, arrWindowShades, strWindowShade

sqlStr = getCodeValuesO("ORG_SUB_TY_CD")
set rsOrgSubTypeCodes = connOracle.execute(sqlStr)
orgSubTypeCode = session("ORG_SUB_TY_CD")

if rsOrgSubTypeCodes.eof and rsOrgSubTypeCodes.bof then
else
    if orgSubTypeCode = "" then
	orgSubTypeCode = rsOrgSubTypeCodes("code_value")
    end if
    rsOrgSubTypeCodes.MoveFirst
end if

if len(request("org_sub_ty_cd")) > 0 then
    orgSubTypeCode = request("org_sub_ty_cd")
end if

session("ORG_SUB_TY_CD") = orgSubTypeCode

sqlStr = getCodeValuesO("WINDOW_SHADE_CD")
set rsWindowShadeCodes = connOracle.execute(sqlStr)
if rsWindowShadeCodes.eof and rsWindowShadeCodes.bof then
else
	arrWindowShades = rsWindowShadeCodes.GetRows
end if

if len(request("window_shade_cd")) > 0 then
	windowShadeCode = request("window_shade_cd")
end if

sqlStr = getCodeValuesO("GROUP_TY_CD")
set rsGroupCodes = connOracle.execute(sqlStr)

if rsGroupCodes.eof and rsGroupCodes.bof then
else
	groupCode = rsGroupCodes("code_value")
	rsGroupCodes.MoveFirst
end if

if len(request("group_cd")) > 0 then
	groupCode = request("group_cd")
end if

sqlStr = getColumnDetailGroups(orgSubTypeCode, windowShadeCode, groupCode)
set rs = connOracle.execute(sqlStr)

if rs.EOF and rs.BOF then
else
	do while not rs.EOF
		maxViewAll = maxViewAll + 1
		rs.MoveNext
	loop
	rs.MoveFirst
end if

rs.close
set rs = nothing

set rs = connOracle.execute(sqlStr)

dim jeditDetails

jeditDetails = "'" & Application("ADMIN_URL") & "/admin/ajax.asp?action=updateView', {" & vbcrlf &_
        "indicator: 'Saving...'," & vbcrlf &_
        "cancel: '<button class=""adminButton"" type=""cancel"" onMouseOver=""this.className=\'adminButtonHover\'"" onMouseOut=""this.className=\'adminButton\'"" >Cancel</button>'," & vbcrlf &_
        "submit: '<button class=""adminButton"" type=""submit"" onMouseOver=""this.className=\'adminButtonHover\'"" onMouseOut=""this.className=\'adminButton\'"" >Save</button>'," & vbcrlf &_
        "tooltip: 'Click to edit...'," & vbcrlf &_
        "width: '100%'"

dim strWindowShades
if isArray(arrWindowShades) then
    strWindowShades = "'':'',"
    for k = lbound(arrWindowShades,2) to ubound(arrWindowShades,2)
        strWindowShades = strWindowShades & "'" & arrWindowShades(2,k) & "':'" & arrWindowShades(3,k)& "'"
        if k < ubound(arrWindowShades,2) then   
            strWindowShades = strWindowShades & ","
        end if
    next
end if

dim strRow
strRow = "'':'',"
for k = 1 to maxRows
    strRow = strRow & "'" & k & "':'" & k & "'"
    if k < maxRows then   
        strRow = strRow & ","
    end if
next

dim strPos
strPos = "'':'',"
for k = 1 to maxPosition
    strPos = strPos & "'" & k & "':'" & k & "'"
    if k < maxPosition then   
        strPos = strPos & ","
    end if
next

dim strViewAll
if maxViewAll > 0 then
    strViewAll = "'':'',"
    for k = 1 to maxViewAll
        strViewAll = strViewAll & "'" & k & "':'" & k & "'"
        if k < maxViewAll then   
            strViewAll = strViewAll & ","
        end if
    next
end if
%>

<script language="Javascript">
<!--

// loadForm
function loadForm() {
	document.form.submit();
}

// doAdd
function doAdd() {
	document.form.action.value = "add"
	document.form.submit();
}

// updateVarious
function updateVarious(column, columnDetailGroupID, newVal) {
	document.location = '<%=scriptName%>?action=updateVarious&column=' + column + '&column_detail_group_id=' + columnDetailGroupID + '&new_val=' + newVal + '&window_shade_cd=' + document.form.window_shade_cd.value + '&org_sub_ty_cd=' + document.form.org_sub_ty_cd.value+ '&group_cd=' + document.form.group_cd.value;
}

function doExport() {
    var isChecked = false;

    if (document.form.deleteView.length) {
        for (x=0; x<document.form.deleteView.length; x++) {
            if (document.form.deleteView[x].checked) {
                isChecked = true;
            }
        }
    } else {
        if (document.form.deleteView.checked)
            isChecked = true;
    }

    if (isChecked) {
        document.form.action.value = "doExport";
        document.form.submit();
    } else {
        alert("You didn't select any fields. Click the check box next to the fields you want to export.");
    }
}

function doImport() {
    document.form.action.value = "doImport";
    document.form.submit();
}


    // doDelete
function doDelete() {
        var isChecked = false; 
        var multiChecked = false;
                        
        if (document.form.deleteView.length) { 
                for (x=0; x<document.form.deleteView.length; x++) {
                        if (document.form.deleteView[x].checked) {
                                if (isChecked) {
                                        multiChecked = true;
                                } else {
                                        isChecked = true;
                                }
                        }
                }
        } else {
                if (document.form.deleteView.checked)
                                isChecked = true;
        }
           
        if (multiChecked) {
                if (confirm("Are you sure you want to permanently delete these fields from this view?")) {
                        document.form.action.value = "deleteView"; 
                        document.form.submit();
                }
        } else if (isChecked) {
			confirmMsg = "Are you sure you want to permanently delete this field from this view?"
		
			if (confirm(confirmMsg)) {
				document.form.action.value = "deleteView";
				document.form.submit();
			}               
        } else {
            alert("You didn't select any fields. Click the check box next to the fields you want to select.");
        }
}

// doReadOnly
function doReadOnly() {
    var isChecked = false; 
                        
    if (document.form.deleteView.length) { 
        for (x=0; x<document.form.deleteView.length; x++) {
            if (document.form.deleteView[x].checked)
                isChecked = true;
        }
    } else {
        if (document.form.deleteView.checked)
            isChecked = true;
    }

    if (!isChecked) {        
        alert("You didn't select any fields. Click the check box next to the fields you want to select.");
    } else {
        document.form.action.value = "doReadOnly";
        document.form.submit();
    }
}

// doCopy
function doCopy() {
    var isChecked = false; 
    if (document.form.deleteView.length) { 
        for (x=0; x<document.form.deleteView.length; x++) {
            if (document.form.deleteView[x].checked) {
                if (isChecked) {
                } else {
                    isChecked = true;
                }
            }
        }
    } else {
        if (document.form.deleteView.checked)
            isChecked = true;
    }

    if (isChecked) {
        if (document.form.target_group.value == "") {
            alert("No group selected to copy to.");
            return;
        }
		document.form.action.value = "copy";
		document.form.submit();
    } else {
        alert("You didn't select any fields. Click the check box next to the fields you want to select.");
    }
}

// toggleAll
function toggleAll() {

        if (document.form.deleteViews.checked) {
                checkStat = true;
        } else {
                checkStat = false;
        }

        if (document.form.deleteView.length) {
			for (x=0; x<document.form.deleteView.length; x++) {
				document.form.deleteView[x].checked = checkStat;
			}
        } else {
                document.form.deleteView.checked = checkStat;
        }
}

var dataWindowShades = {<%=strWindowShades%>};
var dataRow = {<%=strRow%>};
var dataPos = {<%=strPos%>};
var dataViewAll = {<%=strViewAll%>};

$(document).ready(function() {
    $('.edit_yn').editable(<%=jeditDetails%>,
        type: 'select',
        data: "{'Y':'Y','N':'N'}"
    });
    $('.edit_windowshade').editable(<%=jeditDetails%>,
        type: 'select',
        data: dataWindowShades,
        callback: function(value, settings) {
            $(this).html(dataWindowShades[value]);
        }
    });
    $('.edit_row').editable(<%=jeditDetails%>,
        type: 'select',
        data: dataRow,
        callback: function(value, settings) {
            $(this).html(dataRow[value]);
        }
    });
    $('.edit_pos').editable(<%=jeditDetails%>,
        type: 'select',
        data: dataPos,
        callback: function(value, settings) {
            $(this).html(dataPos[value]);
        }
    });
    $('.edit_viewall').editable(<%=jeditDetails%>,
        type: 'select',
        data: dataViewAll,
        callback: function(value, settings) {
            $(this).html(dataViewAll[value]);
        }
    });
});
-->
</script>
<form name="form" id="form" action="<%=scriptName%>" method="post">
<input type="hidden" name="action" value="">
<table width="100%" border="0" cellpadding="2" cellspacing="0">
<tr>
	<td colspan="10" align="right" class="adminText">
		<table border="0" cellpadding="2" cellspacing="0">
			<tr class="adminText">
				<td align="right">
					<a href="javascript:manageCodesO('ORG_SUB_TY_CD');" class="adminLink">Record Type</a>:
				</td>
				<td>
					<select name="org_sub_ty_cd" class="adminText" onChange="loadForm()">
<%do while not rsOrgSubTypeCodes.eof%>
						<option value="<%=rsOrgSubTypeCodes("code_value")%>"<%if orgSubTypeCode = rsOrgSubTypeCodes("code_value") then%> selected<%end if%>><%=rsOrgSubTypeCodes("code_value_desc")%></option>
	<%rsOrgSubTypeCodes.MoveNext%>
<%loop%>
					</select>
				</td>
			</tr>
			<tr class="adminText">
				<td align="right">
					<a href="javascript:manageCodesO('GROUP_TY_CD');" class="adminLink">Group</a>:
				</td>
				<td>
					<select name="group_cd" class="adminText" onChange="loadForm()">
<%do while not rsGroupCodes.eof%>
						<option value="<%=rsGroupCodes("code_value")%>"<%if groupCode = rsGroupCodes("code_value") then%> selected<%end if%>><%=rsGroupCodes("code_value_desc")%></option>
	<%rsGroupCodes.MoveNext%>
<%loop%>
					</select>

				</td>
			</tr>
			<tr class="adminText">
				<td>
					<a href="javascript:manageCodesO('WINDOW_SHADE_CD');" class="adminLink">Window Shade</a>:
				</td>
				<td>
					<select name="window_shade_cd" class="adminText" onChange="loadForm()">
						<option value="">-- All --</option>
	<%
    if isArray(arrWindowShades) then
        strWindowShades = "'':'',"
        for k = lbound(arrWindowShades,2) to ubound(arrWindowShades,2)
    %>
						<option value="<%=arrWindowShades(2,k)%>"<%if windowShadeCode = arrWindowShades(2,k) then%> selected<%end if%>><%=arrWindowShades(3,k)%></option>
    <%
        next
	end if
	%>
					</select>

				</td>
			</tr>
		</table>
	</td>
</tr>
<tr>
	<td>
	<input type="button" value="Add" onClick="doAdd()" class="adminButton" onMouseOver="this.className='adminButtonHover'" onMouseOut="this.className='adminButton'">
	</td>
	<td align="center">
	<input type="button" value="Delete" onClick="doDelete()" class="adminButton" onMouseOver="this.className='adminButtonHover'" onMouseOut="this.className='adminButton'">
	<input type="button" value="Read Only" onClick="doReadOnly()" class="adminButton" onMouseOver="this.className='adminButtonHover'" onMouseOut="this.className='adminButton'">
	</td>
	<td colspan="8">
<%
dim rsNewGroups
sqlStr = "select code_value,CODE_VALUE_DESC from code_value where code_class = 'GROUP_TY_CD' and code_value <> '"&groupCode&"' and code_value not in " &_
    "(select c.group_cd from column_detail_group c, membership_column_detail m where m.column_id = c.column_id and c.column_ty = 'MEMBER' and m.org_sub_ty_cd = '"&orgSubTypeCode&"' and GROUP_CD = '"&groupCode&"' " &_
    "union " &_
    "select c.group_cd from column_detail_group c, org_column_detail m where m.column_id = c.column_id and c.column_ty = 'DETAIL' and m.org_sub_ty_cd = '"&orgSubTypeCode&"' and GROUP_CD = '"&groupCode&"') "
set rsNewGroups = connOracle.execute(sqlStr)
if rsNewGroups.eof and rsNewGroups.BOF then
else
%>
<input type="button" value = "Copy to Group" class="adminButton" onMouseOver="this.className='adminButtonHover'" onMouseOut="this.className='adminButton'" onClick="doCopy();" title="Copy all fields for this group and record type to another group">
<select name="target_group" id="target_group" class="adminText">
    <option value=""></option>
<%do while not rsNewGroups.EOF%>
    <option value="<%=rsNewGroups("code_value")%>"><%=rsNewGroups("CODE_VALUE_DESC")%></option>
<%
    rsNewGroups.MoveNext
loop
%>
</select>
<%
end if
rsNewGroups.close
set rsNewGroups = nothing
%>
        <input type="button" value="Export" onClick="doExport()" class="adminButton" >
        <input type="button" value="Import" onClick="doImport()" class="adminButton">
	</td>
</tr>
<tr class="adminTitlebar">
	<td>&nbsp;
	
	</td>
	<td align="center">
	<%if not rs.eof then%>
		<input type="checkbox" name="deleteViews" value="all" onClick="toggleAll()">
	<%end if%>
	</td>
	<td>
	Column Description
	</td>
	<td>
	Window Shade
	</td>
	<td align="center">
	Row
	</td>
	<td align="center">
	Position
	</td>
	<td align="center">
	View All Order
	</td>
	<td align="center">
	Export
	</td>
	<td align="center">
	Mass Update
	</td>
	<td align="center">
	Read Only
	</td>
</tr>
<%
switch = 0
lineCounter = 0
do while not rs.EOF
	if rs("COLUMN_TY") = "MEMBER" then
		sqlStr = getMembershipColumnDetail(rs("COLUMN_ID"))
	else
		sqlStr = getOrgColumnDetail(rs("COLUMN_ID"))
	end if
	set rsColumnDetail = connOracle.execute(sqlStr)
	switch = 1 - switch
	lineCounter = lineCounter + 1
	if switch > 0 then
		lineClass = "activeLineItem1"
	else
		lineClass = "activeLineItem2"
	end if

    if rs("READONLY")&"" <> "" then
        readonly = rs("READONLY")
    else
	    sqlStr = "select ACTION_CD from p_g_a_finder where page_cd = 'MASTERVIEW' and group_ty_cd = '"&groupCode&"' and org_id = '"&Application("ORG_ID")&"' and permission_cd = '"&rs("COLUMN_TY")&"876"&rsColumnDetail("COLUMN_DESC")&"'"

	    set rsPGA = connOracle.execute(sqlStr)
	    if rsPGA.EOF and rsPGA.BOF then
		    readOnly = "Y"
	    else
            if rsPGA("ACTION_CD")&"" = "2" then
                readOnly = "N"
            else
                readOnly = "Y"
            end if
	    end if
	    rsPGA.close
	    set rsPGA = nothing
    end if
%>
<tr class="<%=lineClass%>">
	<td align="center" class="adminText">
		<%=lineCounter%>.
	</td>
	<td align="center">
		<input type="checkbox" value="<%=rs("COLUMN_DETAIL_GROUP_ID")%>" id="deleteView" name="deleteView">
	</td>
	<td align="left" class="adminText">
	<a href="<%=scriptName%>?action=modify&column_detail_group_id=<%=rs("COLUMN_DETAIL_GROUP_ID")%>" class="adminLink"><%=rsColumnDetail("COLUMN_RPT_DESC")%></a>
	</td>
	<td align="left" class="adminText">
	    <%
	    strWindowShade = ""
	    if isArray(arrWindowShades) then
    	    for j = lbound(arrWindowShades,2) to ubound(arrWindowShades,2)
    	        if arrWindowShades(2,j) = rs("WINDOW_SHADE_CD") then
    	            strWindowShade = arrWindowShades(3,j)
    	        end if
    	    next
    	end if
	    %>
		<span class="edit_windowshade jEditableLink" style="display: inline" id="window_shade_cd|<%=rs("COLUMN_DETAIL_GROUP_ID")%>"><%=strWindowShade%></span>
	</td>
	<td align="center">
		<span class="edit_row jEditableLink" style="display: inline" id="row_num|<%=rs("COLUMN_DETAIL_GROUP_ID")%>"><%=rs("ROW_NUM")%></span>
	</td>
	<td align="center">
		<span class="edit_pos jEditableLink" style="display: inline" id="position|<%=rs("COLUMN_DETAIL_GROUP_ID")%>"><%=rs("POSITION")%></span>
	</td>
	<td align="center">
		<span class="edit_viewall jEditableLink" style="display: inline" id="view_all_order|<%=rs("COLUMN_DETAIL_GROUP_ID")%>"><%=rs("VIEW_ALL_ORDER")%></span>
	</td>
	<td align="center">
        <span class="edit_yn jEditableLink" style="display: inline" id="export|<%=rs("COLUMN_DETAIL_GROUP_ID")%>"><%=rs("EXPORT")%></span>
	</td>
	<td align="center">
        <span class="edit_yn jEditableLink" style="display: inline" id="mass_update|<%=rs("COLUMN_DETAIL_GROUP_ID")%>"><%=rs("MASS_UPDATE")%></span>
	</td>
	<td align="center">
		<%if rsColumnDetail("COLUMN_DESC") = "CREATE_DT" then%>
            Y
		<%else%>
        <span class="edit_yn jEditableLink" style="display: inline" id="readonly|<%=rs("COLUMN_DETAIL_GROUP_ID")%>"><%=readOnly%></span>
        <%end if%>
	</td>
</tr>	
<%
	rsColumnDetail.close
	set rsColumnDetail = nothing
    rs.MoveNext
    response.flush
Loop
%>
</table>
</form>

<%
rs.close
set rs = nothing
rsOrgSubTypeCodes.close
set rsOrgSubTypeCodes = nothing
rsWindowShadeCodes.close
set rsWindowShadeCodes = nothing
%>
<%end sub%>

<%
'~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
Sub addField(columnType,columnID, groupCode,orgSubTypeCode, windowShadeCode, rowNum, position, viewAllOrder, export, massUpdate, modifyValues, reportBuilderDefault, readOnly)

	columnDetailGroupID = getSequence("SEQ_COLUMN_ID")

    sqlStr = "delete from column_detail_group where column_ty = '"&columnType&"' and column_id = '"&columnID&"' and group_cd = '" & groupCode&"'"
	connOracle.Execute(sqlStr)

	sqlStr = "INSERT INTO column_detail_group (COLUMN_DETAIL_GROUP_ID, COLUMN_TY,COLUMN_ID,GROUP_CD,WINDOW_SHADE_CD, ROW_NUM, POSITION, VIEW_ALL_ORDER, EXPORT, MASS_UPDATE, MODIFY_VALUES, REPORT_BUILDER_DEFAULT, READONLY) VALUES "&_
	"("&columnDetailGroupID&",'"&columnType&"','"&columnID&"','"&groupCode&"','"&windowShadeCode&"',"&rowNum&","&position&",'"&viewAllOrder&"','"&export&"','"&massUpdate&"','"&modifyValues&"','"&reportBuilderDefault&"','"&readOnly&"')"

	connOracle.Execute(sqlStr)
End Sub

'~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
Sub modifyField(columnDetailGroupID,rowNum, position, windowShadeCode, viewAllOrder, export, massUpdate, modifyValues, reportBuilderDefault, readOnly)
	'The purpose of this subroutine is to add a MEMBER_DETAIL field.

    dim rsColumnDetailGroup, columnType, columnDesc, permission, rs
	
	sqlStr = "update column_detail_group set "&_
				" WINDOW_SHADE_CD = '"&windowShadeCode&"', " &_
				" ROW_NUM = "&rowNum&", " &_
				" POSITION = "&position&", " &_
				" VIEW_ALL_ORDER = '"&viewAllOrder&"', " &_
				" EXPORT = '"&export&"', " &_
				" MASS_UPDATE = '"&massUpdate&"', " &_
				" MODIFY_VALUES = '"&modifyValues&"', " &_
				" REPORT_BUILDER_DEFAULT = '"&reportBuilderDefault&"', " &_
				" READONLY = '"&readOnly&"' " &_
				" WHERE COLUMN_DETAIL_GROUP_ID = "&columnDetailGroupID
		
	connOracle.Execute(sqlStr)

    sqlStr = "select * from column_Detail_group where column_detail_group_id = '"&columnDetailGroupID&"'"
    set rsColumnDetailGroup = connOracle.execute(sqlStr)
    if rsColumnDetailGroup.EOF and rsColumnDetailGroup.BOF then
    else
	    columnType = rsColumnDetailGroup("COLUMN_TY")
	    if columnType = "MEMBER" then
		    tableName = "MEMBERSHIP_COLUMN_DETAIL"
	    else
		    tableName= "ORG_COLUMN_DETAIL"
	    end if
	    sqlStr = "select * from "&tableName&" where column_id = '"&rsColumnDetailGroup("COLUMN_ID")&"'"
	    set rs = connOracle.execute(sqlStr)
	    if rs.EOF and rs.BOF then
	    else
		    columnDesc = rs("COLUMN_DESC")
	    end if
	    rs.close
	    set rs = nothing
        permission = columnType&"876"&columnDesc
        sqlStr = "delete from p_g_a_finder where permission_cd = '" & permission & "' and group_ty_cd = '"&request("group_cd")&"'"
        connOracle.Execute(sqlStr)
    end if
    rsColumnDetailGroup.close
    set rsColumnDetailGroup = nothing
End Sub

'~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
Sub updateVarious(id,value,column)

	sqlStr = "UPDATE COLUMN_DETAIL_GROUP "&_
	"SET "&column&" = '"&value&"' "&_
	"WHERE COLUMN_DETAIL_GROUP_ID = "&id&""

	connOracle.Execute(sqlStr)
End Sub

'~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
Function replaceTick(dirty)

If dirty <> "" then
	replaceTick = replace(dirty, "'", "''")
Else
	replaceTick = ""
End If


End Function

Function replaceSpaces(dirty)

If dirty <> "" then
	replaceSpaces = replace(dirty, " ", "_")
Else
	replaceSpaces = ""
End If


End Function

%>

<%
connOracle.close
set connOracle = nothing
%>
<!--#include file="bottomshell.asp"-->