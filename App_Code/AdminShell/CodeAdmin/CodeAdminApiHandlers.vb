Imports System
Imports System.Collections.Generic
Imports System.Globalization
Imports System.Web
Imports System.Web.SessionState

Public Class CodeAdminPage
    Inherits ManagedToolPage

    Protected Overrides ReadOnly Property ToolTitle As String
        Get
            Return "Code Admin"
        End Get
    End Property

    Protected Overrides ReadOnly Property ToolSubtitle As String
        Get
            Return "Manage code classes and values"
        End Get
    End Property

    Protected Overrides ReadOnly Property AccessDeniedMessage As String
        Get
            Return "You do not have permission to use Code Admin."
        End Get
    End Property

    Protected Overrides Function CanOpenTool(user As PilotUser) As Boolean
        Return CodeAdminAccess.CanOpenApp(user)
    End Function
End Class

Public Class CodeAdminSessionHandler
    Implements IHttpHandler
    Implements IRequiresSessionState

    Public Sub ProcessRequest(context As HttpContext) Implements IHttpHandler.ProcessRequest
        PilotJsonApi.PrepareJsonResponse(context)

        If Not String.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase) Then
            context.Response.AppendHeader("Allow", "GET")
            PilotJsonApi.WriteError(context, 405, "Only GET is supported.", Nothing)
            Return
        End If

        Try
            Dim user As PilotUser = Nothing
            If Not CodeAdminApiGuard.RequireAuthorized(context, user) Then
                Return
            End If

            Dim sections = PilotJsonApi.LoadMenuSections(user)
            PilotJsonApi.WriteJson(
                context,
                200,
                New Dictionary(Of String, Object) From {
                    {"userName", user.UserName},
                    {"memberId", user.MemberId},
                    {"csrfToken", PilotJsonApi.IssueCsrfToken(context)},
                    {"menuSections", PilotJsonApi.SerializeMenuSections(sections)},
                    {"paths", PilotJsonApi.SerializeShellPaths()}
                })
        Catch ex As Exception
            PilotJsonApi.HandleServiceException(context, ex)
        End Try
    End Sub

    Public ReadOnly Property IsReusable As Boolean Implements IHttpHandler.IsReusable
        Get
            Return False
        End Get
    End Property
End Class
Public Class CodeAdminWorkspaceHandler
    Implements IHttpHandler
    Implements IRequiresSessionState

    Public Sub ProcessRequest(context As HttpContext) Implements IHttpHandler.ProcessRequest
        PilotJsonApi.PrepareJsonResponse(context)

        If Not String.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase) Then
            context.Response.AppendHeader("Allow", "GET")
            PilotJsonApi.WriteError(context, 405, "Only GET is supported.", Nothing)
            Return
        End If

        Try
            Dim user As PilotUser = Nothing
            If Not CodeAdminApiGuard.RequireAuthorized(context, user) Then
                Return
            End If

            Dim service As New CodeAdminService(user)
            PilotJsonApi.WriteJson(context, 200, SerializeWorkspace(service.GetWorkspace()))
        Catch ex As Exception
            PilotJsonApi.HandleServiceException(context, ex)
        End Try
    End Sub

    Private Shared Function SerializeWorkspace(workspace As CodeAdminWorkspace) As Dictionary(Of String, Object)
        Dim classes As New List(Of Dictionary(Of String, Object))()
        Dim fieldMetadata As New Dictionary(Of String, Object)(StringComparer.OrdinalIgnoreCase)
        Dim classIndex As Integer
        For classIndex = 0 To workspace.Classes.Count - 1
            Dim item = workspace.Classes(classIndex)
            classes.Add(New Dictionary(Of String, Object) From {
                {"codeClass", item.CodeClass},
                {"codeClassDesc", item.CodeClassDesc},
                {"edit", item.Edit}
            })
        Next

        If workspace.FieldMetadata IsNot Nothing Then
            For Each metadataEntry In workspace.FieldMetadata
                fieldMetadata(metadataEntry.Key) = SerializeDetailMetadata(metadataEntry.Value)
            Next
        End If

        Return New Dictionary(Of String, Object) From {
            {"classes", classes},
            {"defaultCodeClass", workspace.DefaultCodeClass},
            {"majorCode", workspace.MajorCode},
            {"fieldMetadata", fieldMetadata},
            {"showClassCodes", workspace.ShowClassCodes}
        }
    End Function

    Public Shared Function SerializeDetailMetadata(metadata As CodeAdminDetailMetadata) As Dictionary(Of String, Object)
        Dim fields As New List(Of Dictionary(Of String, Object))()
        Dim fieldIndex As Integer
        For fieldIndex = 0 To metadata.Fields.Count - 1
            Dim field = metadata.Fields(fieldIndex)
            Dim options As New List(Of Dictionary(Of String, Object))()
            Dim optionIndex As Integer
            For optionIndex = 0 To field.Options.Count - 1
                options.Add(New Dictionary(Of String, Object) From {{"value", field.Options(optionIndex).Value}, {"label", field.Options(optionIndex).Label}})
            Next
            fields.Add(New Dictionary(Of String, Object) From {
                {"key", field.Key},
                {"label", field.Label},
                {"controlType", field.ControlType},
                {"required", field.Required},
                {"options", options},
                {"section", field.Section},
                {"order", field.Order}
            })
        Next
        Return New Dictionary(Of String, Object) From {{"fields", fields}}
    End Function

    Public ReadOnly Property IsReusable As Boolean Implements IHttpHandler.IsReusable
        Get
            Return False
        End Get
    End Property
End Class

Public Class CodeAdminValuesHandler
    Implements IHttpHandler
    Implements IRequiresSessionState

    Public Sub ProcessRequest(context As HttpContext) Implements IHttpHandler.ProcessRequest
        PilotJsonApi.PrepareJsonResponse(context)

        Select Case context.Request.HttpMethod.ToUpperInvariant()
            Case "GET"
                HandleGet(context)
            Case "POST"
                HandlePost(context)
            Case Else
                context.Response.AppendHeader("Allow", "GET, POST")
                PilotJsonApi.WriteError(context, 405, "Only GET and POST are supported.", Nothing)
        End Select
    End Sub

    Private Shared Sub HandleGet(context As HttpContext)
        Try
            Dim user As PilotUser = Nothing
            If Not CodeAdminApiGuard.RequireAuthorized(context, user) Then
                Return
            End If

            Dim service As New CodeAdminService(user)
            Dim codeClass = If(context.Request.QueryString("codeClass"), String.Empty).Trim()
            Dim search = If(context.Request.QueryString("search"), String.Empty).Trim()
            Dim start = ParsePositiveInt(context.Request.QueryString("start"), 0)
            Dim pageSize = CodeAdminConstants.DefaultPageSize

            If String.Equals(context.Request.QueryString("metadata"), "true", StringComparison.OrdinalIgnoreCase) Then
                Dim codeValue = If(context.Request.QueryString("codeValue"), String.Empty).Trim()
                PilotJsonApi.WriteJson(context, 200, CodeAdminWorkspaceHandler.SerializeDetailMetadata(service.GetDetailMetadata(codeClass, codeValue)))
                Return
            End If

            Dim idValue = context.Request.QueryString("id")
            Dim codeValueId As Integer
            If Integer.TryParse(idValue, NumberStyles.None, CultureInfo.InvariantCulture, codeValueId) AndAlso codeValueId > 0 Then
                PilotJsonApi.WriteJson(context, 200, SerializeValue(service.GetValue(codeValueId)))
                Return
            End If

            PilotJsonApi.WriteJson(context, 200, SerializeValuePage(service.ListValues(codeClass, search, start, pageSize)))
        Catch ex As Exception
            PilotJsonApi.HandleServiceException(context, ex)
        End Try
    End Sub

    Private Shared Sub HandlePost(context As HttpContext)
        Try
            Dim user As PilotUser = Nothing
            If Not CodeAdminApiGuard.RequireAuthorizedMutation(context, user) Then
                Return
            End If

            Dim service As New CodeAdminService(user)
            Dim action = If(context.Request.QueryString("action"), String.Empty).Trim().ToLowerInvariant()

            Select Case action
                Case "create"
                    Dim body = PilotJsonApi.ReadJsonBody(Of CreateCodeValueCommand)(context)
                    PilotJsonApi.WriteJson(context, 200, SerializeValue(service.CreateValue(body)))
                Case "update"
                    Dim body = PilotJsonApi.ReadJsonBody(Of UpdateCodeValueCommand)(context)
                    PilotJsonApi.WriteJson(context, 200, SerializeValue(service.UpdateValue(body)))
                Case "patch"
                    Dim body = PilotJsonApi.ReadJsonBody(Of PatchCodeValueCommand)(context)
                    PilotJsonApi.WriteJson(context, 200, SerializeValue(service.PatchValue(body)))
                Case "delete"
                    Dim body = PilotJsonApi.ReadJsonBody(Of DeleteCodeValuesCommand)(context)
                    PilotJsonApi.WriteJson(context, 200, SerializeDeleteResults(service.DeleteValues(body)))
                Case "activate"
                    Dim body = PilotJsonApi.ReadJsonBody(Of CodeValueLifecycleCommand)(context)
                    service.ActivateValue(body)
                    PilotJsonApi.WriteJson(context, 200, New Dictionary(Of String, Object) From {{"updated", True}})
                Case "deactivate"
                    Dim body = PilotJsonApi.ReadJsonBody(Of CodeValueLifecycleCommand)(context)
                    service.DeactivateValue(body)
                    PilotJsonApi.WriteJson(context, 200, New Dictionary(Of String, Object) From {{"updated", True}})
                Case "status"
                    Dim body = PilotJsonApi.ReadJsonBody(Of CodeValueLifecycleCommand)(context)
                    service.SetStatus(body)
                    PilotJsonApi.WriteJson(context, 200, New Dictionary(Of String, Object) From {{"updated", True}})
                Case "position"
                    Dim body = PilotJsonApi.ReadJsonBody(Of CodeValuePositionCommand)(context)
                    service.SetPosition(body)
                    PilotJsonApi.WriteJson(context, 200, New Dictionary(Of String, Object) From {{"updated", True}})
                Case Else
                    Throw New AdminShellValidationException("Action is not supported.")
            End Select
        Catch ex As Exception
            PilotJsonApi.HandleServiceException(context, ex)
        End Try
    End Sub

    Private Shared Function ParsePositiveInt(value As String, fallback As Integer) As Integer
        Dim parsed As Integer
        If Integer.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, parsed) Then
            Return parsed
        End If
        Return fallback
    End Function

    Private Shared Function SerializeValuePage(page As CodeAdminValuePage) As Dictionary(Of String, Object)
        Dim items As New List(Of Dictionary(Of String, Object))()
        Dim itemIndex As Integer
        For itemIndex = 0 To page.Items.Count - 1
            items.Add(SerializeValue(page.Items(itemIndex)))
        Next

        Return New Dictionary(Of String, Object) From {
            {"items", items},
            {"totalCount", page.TotalCount},
            {"start", page.Start},
            {"pageSize", page.PageSize},
            {"canDelete", page.CanDelete}
        }
    End Function

    Private Shared Function SerializeValue(value As CodeAdminValue) As Dictionary(Of String, Object)
        Dim result = New Dictionary(Of String, Object) From {
            {"codeValueId", value.CodeValueId},
            {"codeClass", value.CodeClass},
            {"codeValue", value.CodeValue},
            {"codeValueDesc", value.CodeValueDesc},
            {"codeValueLongDesc", value.CodeValueLongDesc},
            {"status", value.Status},
            {"inactive", value.Inactive},
            {"majorCode", value.MajorCode},
            {"minorCode", value.MinorCode},
            {"orderBy", value.OrderBy},
            {"formDisplay", value.FormDisplay},
            {"optionValue1", value.OptionValue1},
            {"optionValue2", value.OptionValue2},
            {"optionValue3", value.OptionValue3},
            {"optionValue4", value.OptionValue4},
            {"optionValue5", value.OptionValue5},
            {"optionValue6", value.OptionValue6},
            {"optionValue7", value.OptionValue7},
            {"optionValue8", value.OptionValue8},
            {"optionValue9", value.OptionValue9},
            {"optionValue10", value.OptionValue10},
            {"optionValue11", value.OptionValue11},
            {"optionValue12", value.OptionValue12},
            {"optionValue13", value.OptionValue13},
            {"optionValue14", value.OptionValue14},
            {"optionValue15", value.OptionValue15},
            {"optionValue16", value.OptionValue16},
            {"optionValue17", value.OptionValue17},
            {"isProtected", value.IsProtected}
        }
        If value.FieldMetadata IsNot Nothing Then
            result("fieldMetadata") = CodeAdminWorkspaceHandler.SerializeDetailMetadata(value.FieldMetadata)
        End If
        Return result
    End Function

    Private Shared Function SerializeDeleteResults(results As IList(Of CodeAdminDeleteResult)) As Dictionary(Of String, Object)
        Dim payload As New List(Of Dictionary(Of String, Object))()
        Dim resultIndex As Integer
        For resultIndex = 0 To results.Count - 1
            Dim result = results(resultIndex)
            payload.Add(New Dictionary(Of String, Object) From {
                {"deleted", result.Deleted},
                {"skippedInUse", result.SkippedInUse},
                {"message", result.Message}
            })
        Next
        Return New Dictionary(Of String, Object) From {{"results", payload}}
    End Function

    Public ReadOnly Property IsReusable As Boolean Implements IHttpHandler.IsReusable
        Get
            Return False
        End Get
    End Property
End Class
