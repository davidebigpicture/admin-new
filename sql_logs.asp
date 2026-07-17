<% pageTitle = "SQL Logs (Pilot)"%>
<%Response.Buffer = True%>
<!--#include file="topshell.asp"-->
<!--#include virtual="/admin/admin/includes/tools.inc"-->
<%
dim path, arrPath, fs, lineClass, switch
dim file, TS, strArray, strLine, fileContents, arrFile, idx, idxArr, logContents

path = "\\amznfsxanseai3p.private.aciportal.com\share\logs" 

arrPath = split(request.servervariables("APPL_PHYSICAL_PATH")&"","\")
if ubound(arrPath) > 2 then
	path = path & "\" & arrPath(3)
end if
path = path & "\sql"
path = replace(path,"\\","\")

if request("action") = "viewFile" then
    viewFile
elseif request("action") = "viewEntry" then
    viewEntry
else
    menu
end if
%>
<!--#include file="bottomshell.asp"-->
<%
sub viewEntry
file = path & "\" & request("file")
idx = cint(request("entry"))
set fs = CreateObject("Scripting.FileSystemObject")
Set TS = fs.OpenTextFile(file, 1, false)
If Not TS.AtEndOfStream  Then  
    fileContents = TS.ReadAll
    arrFile = split(fileContents,vbcrlf)
    strLine = arrFile(idx)
    strArray = split(strLine, "|")
%>
<h1>SQL Log - View File</h1>
<div align="right"><input type="button" value="List" name="list" class="adminButton" onClick="parent.location = '<%=request.servervariables("SCRIPT_NAME")%>?action=viewFile&file=<%=request.querystring("file")%>'"></div>
<table border="0" cellpadding="0" cellspacing="0" align="center" class="controlsDetail">
    <tbody>
        <tr>
            <td align="right">Date/Time:</td>
            <td>&nbsp;</td>
            <td><%=strArray(0)%></td>
        </tr>
        <tr>
            <td align="right">Script:</td>
            <td>&nbsp;</td>
            <td><%=replace(strArray(1),"script=","")%></td>
        </tr>
        <tr>
            <td align="right">Error Number:</td>
            <td>&nbsp;</td>
            <td><%strArray(2)%></td>
        </tr>
        <tr>
            <td align="right">Error Description:</td>
            <td>&nbsp;</td>
            <td><%=strArray(3)%></td>
        </tr>
        <tr>
            <td align="right">SQL:</td>
            <td>&nbsp;</td>
            <td><%=strArray(4)%></td>
        </tr>
    </tbody>
</table>
<%
end if

set TS = nothing
set fs = nothing
end sub

sub viewFile
%>
<h1>SQL Log - View File</h1>
<div align="right"><input type="button" value="List" name="list" class="adminButton" onClick="parent.location = '<%=request.servervariables("SCRIPT_NAME")%>'"></div>
<table border="0" cellpadding="0" cellspacing="0" align="center" width="50%" class="list">
    <thead>
        <tr class="adminTitlebar">
            <th>Date/Time</th>
            <th>Script</th>
            <th>Error Number</th>
            <th>Error Message</th>
        </tr>
    </thead>
    <tbody>
<%
file = path & "\" & request("file")

switch = 0
set fs = CreateObject("Scripting.FileSystemObject")
Set TS = fs.OpenTextFile(file, 1, false)
If Not TS.AtEndOfStream  Then  
    fileContents = TS.ReadAll
    arrFile = split(fileContents,vbcrlf)
    for idx = lbound(arrFile) to ubound(arrFile)
	    strLine = arrFile(idx)
        if strLine&"" <> "" then
    	    switch = 1 - switch
		    if switch > 0 then
			    lineClass = "activeLineItem1"
		    else
			    lineClass = "activeLineItem2"
		    end if
	        strArray = split(strLine, "|")
%>
        <tr class="<%=lineClass%>">
            <td><a href="sql_logs.asp?action=viewEntry&file=<%=request.querystring("file")%>&entry=<%=idx%>"><%=strArray(0)%></a></td>
            <td><%=strArray(1)%></td>
            <td><%=strArray(2)%></td>
            <td><%=strArray(3)%></td>
            <%
            response.write "</tr>"
        end if
        response.flush
    next
end if

set TS = nothing
set fs = nothing
%>
    </tbody>
</table>
<%
end sub 

Sub menu

dim folder, file, item, url, fileCount, files

%>
<h1>SQL Logs - List</h1>
<table border="0" cellpadding="0" cellspacing="0" align="center" width="50%" class="list">
    <thead>
        <tr class="adminTitlebar">
            <th></th>
            <th>File</th>
            <th>Last Modified</th>
        </tr>
    </thead>
    <tbody>
<%
fileCount = 0
switch = 0
set fs = CreateObject("Scripting.FileSystemObject")
if fs.FolderExists(path) then
    set folder = fs.GetFolder(path)
    set files = folder.files
    for each item in SortFiles(files)
	    if left(item.name,4) = "sql_" and right(item.name,4) = ".log" then
    	    switch = 1 - switch
		    if switch > 0 then
			    lineClass = "activeLineItem1"
		    else
			    lineClass = "activeLineItem2"
		    end if
		    fileCount = fileCount + 1
%>
	<tr class="<%=lineClass%>">
        <td><%=fileCount%></td>
        <td><a href="sql_logs.asp?action=viewFile&file=<%=item.name%>"><%=item.name%></a></td>
        <td><%=timeZoneAdjust(item.DateLastModified)%></td>
    </tr>
<%
            response.flush
        end if
    next
    set files = nothing
    set folder = nothing
end if

set fs = nothing
%>
    </tbody>
</table>
<%
end sub
%>

<%
Function SortFiles(files)
  ReDim sorted(files.Count - 1)
  Dim file, i, j
  i = 0
  For Each file in files
    Set sorted(i) = file
    i = i + 1
  Next
  For i = 0 to files.Count - 2
    For j = i + 1 to files.Count - 1
      If sorted(i).DateLastModified < sorted(j).DateLastModified Then
        Dim tmp
        Set tmp = sorted(i)
        Set sorted(i) = sorted(j)
        Set sorted(j) = tmp
     End If
    Next
  Next
  SortFiles = sorted
End Function
%>