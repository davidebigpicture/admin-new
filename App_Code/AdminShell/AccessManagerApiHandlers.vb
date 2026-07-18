Imports System
Imports System.Collections.Generic
Imports System.Globalization
Imports System.Web
Imports System.Web.SessionState

Public Class AccessManagerSessionHandler
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
            If Not AccessManagerApiGuard.RequireAuthorized(context, user) Then
                Return
            End If

            Dim capabilities = AccessManagerAccess.GetCapabilities(user)
            Dim sections = PilotJsonApi.LoadMenuSections(user)
            PilotJsonApi.WriteJson(
                context,
                200,
                New Dictionary(Of String, Object) From {
                    {"userName", user.UserName},
                    {"memberId", user.MemberId},
                    {"capabilities", PilotJsonApi.SerializeCapabilities(capabilities)},
                    {"menuSections", PilotJsonApi.SerializeMenuSections(sections)},
                    {"csrfToken", PilotJsonApi.IssueCsrfToken(context)},
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

Public Class AccessManagerWorkspaceHandler
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
            If Not AccessManagerApiGuard.RequireAuthorized(context, user) Then
                Return
            End If

            Dim service As New AccessManagerService(user)
            Dim workspace = service.GetWorkspace()
            Dim payload As New Dictionary(Of String, Object) From {
                {"capabilities", PilotJsonApi.SerializeCapabilities(workspace.Capabilities)},
                {"sections", workspace.Sections},
                {"scriptTypes", workspace.ScriptTypes},
                {"defaultScriptType", workspace.DefaultScriptType}
            }

            Dim sectionIdValue = context.Request.QueryString("sectionId")
            Dim sectionId As Integer
            If Integer.TryParse(sectionIdValue, NumberStyles.None, CultureInfo.InvariantCulture, sectionId) AndAlso sectionId > 0 Then
                AppendSectionDetail(service, payload, sectionId)
            End If

            PilotJsonApi.WriteJson(context, 200, payload)
        Catch ex As Exception
            PilotJsonApi.HandleServiceException(context, ex)
        End Try
    End Sub

    Private Shared Sub AppendSectionDetail(service As AccessManagerService, payload As Dictionary(Of String, Object), sectionId As Integer)
        Dim detail As New Dictionary(Of String, Object)()
        Dim caps = service.Capabilities

        If caps.CanManageSections Then
            detail("section") = service.GetSection(sectionId)
        End If

        If caps.CanManageMemberships Then
            detail("items") = service.ListSectionItems(sectionId, True)
        End If

        If caps.CanManageGrants Then
            detail("grants") = service.ListGrants(AccessManagerConstants.SecureTypeSection, sectionId, True)
        End If

        If detail.Count > 0 Then
            payload("sectionDetail") = detail
        End If
    End Sub

    Public ReadOnly Property IsReusable As Boolean Implements IHttpHandler.IsReusable
        Get
            Return False
        End Get
    End Property
End Class

Public Class AccessManagerSectionsHandler
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
            If Not AccessManagerApiGuard.RequireAuthorized(context, user) Then
                Return
            End If

            Dim service As New AccessManagerService(user)
            Dim idValue = context.Request.QueryString("id")
            Dim sectionId As Integer
            If Integer.TryParse(idValue, NumberStyles.None, CultureInfo.InvariantCulture, sectionId) AndAlso sectionId > 0 Then
                PilotJsonApi.WriteJson(context, 200, service.GetSection(sectionId))
                Return
            End If

            Dim includeInactive = String.Equals(
                context.Request.QueryString("includeInactive"),
                "true",
                StringComparison.OrdinalIgnoreCase)
            PilotJsonApi.WriteJson(context, 200, service.ListSections(includeInactive))
        Catch ex As Exception
            PilotJsonApi.HandleServiceException(context, ex)
        End Try
    End Sub

    Private Shared Sub HandlePost(context As HttpContext)
        Try
            Dim user As PilotUser = Nothing
            If Not AccessManagerApiGuard.RequireAuthorizedMutation(context, user) Then
                Return
            End If

            Dim service As New AccessManagerService(user)
            Dim action = If(context.Request.QueryString("action"), String.Empty).Trim().ToLowerInvariant()

            Select Case action
                Case "create"
                    Dim body = PilotJsonApi.ReadJsonBody(Of CreateSectionCommand)(context)
                    PilotJsonApi.WriteJson(context, 200, service.CreateSection(body))
                Case "update"
                    Dim body = PilotJsonApi.ReadJsonBody(Of UpdateSectionCommand)(context)
                    PilotJsonApi.WriteJson(context, 200, service.UpdateSection(body))
                Case "reorder"
                    Dim body = PilotJsonApi.ReadJsonBody(Of ReorderSectionCommand)(context)
                    service.ReorderSection(body)
                    PilotJsonApi.WriteJson(context, 200, New Dictionary(Of String, Object) From {{"updated", True}})
                Case "activate"
                    Dim body = PilotJsonApi.ReadJsonBody(Of SectionLifecycleCommand)(context)
                    service.ActivateSection(body)
                    PilotJsonApi.WriteJson(context, 200, New Dictionary(Of String, Object) From {{"updated", True}})
                Case "deactivate"
                    Dim body = PilotJsonApi.ReadJsonBody(Of SectionLifecycleCommand)(context)
                    service.DeactivateSection(body)
                    PilotJsonApi.WriteJson(context, 200, New Dictionary(Of String, Object) From {{"updated", True}})
                Case "deletepreview"
                    Dim body = PilotJsonApi.ReadJsonBody(Of SectionLifecycleCommand)(context)
                    PilotJsonApi.WriteJson(context, 200, service.PreviewDeleteSection(body.SectionId))
                Case "delete"
                    Dim body = PilotJsonApi.ReadJsonBody(Of HardDeleteSectionCommand)(context)
                    service.HardDeleteSection(body)
                    PilotJsonApi.WriteJson(context, 200, New Dictionary(Of String, Object) From {{"deleted", True}})
                Case "additem"
                    Dim body = PilotJsonApi.ReadJsonBody(Of AddSectionScriptCommand)(context)
                    service.AddSectionScript(body)
                    PilotJsonApi.WriteJson(context, 200, New Dictionary(Of String, Object) From {{"updated", True}})
                Case "removeitem"
                    Dim body = PilotJsonApi.ReadJsonBody(Of RemoveSectionScriptCommand)(context)
                    service.RemoveSectionScript(body)
                    PilotJsonApi.WriteJson(context, 200, New Dictionary(Of String, Object) From {{"updated", True}})
                Case "reorderitem"
                    Dim body = PilotJsonApi.ReadJsonBody(Of ReorderSectionItemCommand)(context)
                    service.ReorderSectionItem(body)
                    PilotJsonApi.WriteJson(context, 200, New Dictionary(Of String, Object) From {{"updated", True}})
                Case Else
                    PilotJsonApi.WriteError(context, 400, "Unknown action.", PilotJsonApi.IssueCsrfToken(context))
            End Select
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

Public Class AccessManagerScriptsHandler
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
            If Not AccessManagerApiGuard.RequireAuthorized(context, user) Then
                Return
            End If

            Dim service As New AccessManagerService(user)
            Dim idValue = context.Request.QueryString("id")
            Dim scriptId As Integer
            If Integer.TryParse(idValue, NumberStyles.None, CultureInfo.InvariantCulture, scriptId) AndAlso scriptId > 0 Then
                PilotJsonApi.WriteJson(context, 200, service.GetScript(scriptId))
                Return
            End If

            If String.Equals(context.Request.QueryString("filters"), "true", StringComparison.OrdinalIgnoreCase) Then
                Dim workspace = service.GetWorkspace()
                PilotJsonApi.WriteJson(
                    context,
                    200,
                    New Dictionary(Of String, Object) From {
                        {"scriptTypes", workspace.ScriptTypes},
                        {"defaultScriptType", workspace.DefaultScriptType}
                    })
                Return
            End If

            Dim scriptTy = If(context.Request.QueryString("scriptTy"), String.Empty)
            Dim includeInactive = String.Equals(
                context.Request.QueryString("includeInactive"),
                "true",
                StringComparison.OrdinalIgnoreCase)
            PilotJsonApi.WriteJson(context, 200, service.ListScripts(scriptTy, includeInactive))
        Catch ex As Exception
            PilotJsonApi.HandleServiceException(context, ex)
        End Try
    End Sub

    Private Shared Sub HandlePost(context As HttpContext)
        Try
            Dim user As PilotUser = Nothing
            If Not AccessManagerApiGuard.RequireAuthorizedMutation(context, user) Then
                Return
            End If

            Dim service As New AccessManagerService(user)
            Dim action = If(context.Request.QueryString("action"), String.Empty).Trim().ToLowerInvariant()

            Select Case action
                Case "create"
                    Dim body = PilotJsonApi.ReadJsonBody(Of CreateScriptCommand)(context)
                    PilotJsonApi.WriteJson(context, 200, service.CreateScript(body))
                Case "update"
                    Dim body = PilotJsonApi.ReadJsonBody(Of UpdateScriptCommand)(context)
                    PilotJsonApi.WriteJson(context, 200, service.UpdateScript(body))
                Case "activate"
                    Dim body = PilotJsonApi.ReadJsonBody(Of ScriptLifecycleCommand)(context)
                    service.ActivateScript(body)
                    PilotJsonApi.WriteJson(context, 200, New Dictionary(Of String, Object) From {{"updated", True}})
                Case "deactivate"
                    Dim body = PilotJsonApi.ReadJsonBody(Of ScriptLifecycleCommand)(context)
                    service.DeactivateScript(body)
                    PilotJsonApi.WriteJson(context, 200, New Dictionary(Of String, Object) From {{"updated", True}})
                Case "deletepreview"
                    Dim body = PilotJsonApi.ReadJsonBody(Of ScriptLifecycleCommand)(context)
                    PilotJsonApi.WriteJson(context, 200, service.PreviewDeleteScript(body.ScriptId))
                Case "delete"
                    Dim body = PilotJsonApi.ReadJsonBody(Of HardDeleteScriptCommand)(context)
                    service.HardDeleteScript(body)
                    PilotJsonApi.WriteJson(context, 200, New Dictionary(Of String, Object) From {{"deleted", True}})
                Case Else
                    PilotJsonApi.WriteError(context, 400, "Unknown action.", PilotJsonApi.IssueCsrfToken(context))
            End Select
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

Public Class AccessManagerPrincipalsHandler
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
            If Not AccessManagerApiGuard.RequireAuthorized(context, user) Then
                Return
            End If

            Dim service As New AccessManagerService(user)
            Dim query = If(context.Request.QueryString("q"), String.Empty)
            Dim principalTy = If(context.Request.QueryString("ty"), String.Empty)
            Dim limitValue = context.Request.QueryString("limit")
            Dim requestedLimit As Integer
            If Not Integer.TryParse(limitValue, NumberStyles.None, CultureInfo.InvariantCulture, requestedLimit) Then
                requestedLimit = PilotJsonApi.MaxPrincipalSearchLimit
            End If

            Dim includeInactive = String.Equals(
                context.Request.QueryString("includeInactive"),
                "true",
                StringComparison.OrdinalIgnoreCase)
            Dim results = service.SearchPrincipals(
                query,
                principalTy,
                includeInactive,
                PilotJsonApi.CapPrincipalSearchLimit(requestedLimit))
            PilotJsonApi.WriteJson(context, 200, results)
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

Public Class AccessManagerGrantsHandler
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
            If Not AccessManagerApiGuard.RequireAuthorized(context, user) Then
                Return
            End If

            Dim service As New AccessManagerService(user)
            If String.Equals(context.Request.QueryString("effective"), "true", StringComparison.OrdinalIgnoreCase) Then
                Dim principalTy = If(context.Request.QueryString("principalTy"), String.Empty)
                Dim principalIdValue = context.Request.QueryString("principalId")
                Dim scriptIdValue = context.Request.QueryString("scriptId")
                Dim principalId As Integer
                Dim scriptId As Integer
                If Not Integer.TryParse(principalIdValue, NumberStyles.None, CultureInfo.InvariantCulture, principalId) OrElse
                    Not Integer.TryParse(scriptIdValue, NumberStyles.None, CultureInfo.InvariantCulture, scriptId) Then
                    Throw New AccessManagerValidationException("Principal and script ids are required.")
                End If

                PilotJsonApi.WriteJson(
                    context,
                    200,
                    service.GetEffectiveAccess(New EffectiveAccessQuery With {
                        .PrincipalTy = principalTy,
                        .PrincipalId = principalId,
                        .ScriptId = scriptId
                    }))
                Return
            End If

            If String.Equals(context.Request.QueryString("principal"), "true", StringComparison.OrdinalIgnoreCase) Then
                Dim principalTy = If(context.Request.QueryString("principalTy"), String.Empty)
                Dim principalIdValue = context.Request.QueryString("principalId")
                Dim principalId As Integer
                If Not Integer.TryParse(principalIdValue, NumberStyles.None, CultureInfo.InvariantCulture, principalId) Then
                    Throw New AccessManagerValidationException("Principal id is required.")
                End If

                Dim includePrincipalInactive = String.Equals(
                    context.Request.QueryString("includeInactive"),
                    "true",
                    StringComparison.OrdinalIgnoreCase)
                PilotJsonApi.WriteJson(
                    context,
                    200,
                    service.ListPrincipalGrants(principalTy, principalId, includePrincipalInactive))
                Return
            End If

            Dim secureTy = If(context.Request.QueryString("secureTy"), String.Empty)
            Dim secureIdValue = context.Request.QueryString("secureId")
            Dim secureId As Integer
            If Not Integer.TryParse(secureIdValue, NumberStyles.None, CultureInfo.InvariantCulture, secureId) Then
                Throw New AccessManagerValidationException("Secure id is required.")
            End If

            Dim includeInactive = String.Equals(
                context.Request.QueryString("includeInactive"),
                "true",
                StringComparison.OrdinalIgnoreCase)
            PilotJsonApi.WriteJson(context, 200, service.ListGrants(secureTy, secureId, includeInactive))
        Catch ex As Exception
            PilotJsonApi.HandleServiceException(context, ex)
        End Try
    End Sub

    Private Shared Sub HandlePost(context As HttpContext)
        Try
            Dim user As PilotUser = Nothing
            If Not AccessManagerApiGuard.RequireAuthorizedMutation(context, user) Then
                Return
            End If

            Dim service As New AccessManagerService(user)
            Dim action = If(context.Request.QueryString("action"), String.Empty).Trim().ToLowerInvariant()

            Select Case action
                Case "create"
                    Dim body = PilotJsonApi.ReadJsonBody(Of CreateGrantCommand)(context)
                    PilotJsonApi.WriteJson(context, 200, service.CreateGrant(body))
                Case "activate"
                    Dim body = PilotJsonApi.ReadJsonBody(Of GrantLifecycleCommand)(context)
                    service.ActivateGrant(body)
                    PilotJsonApi.WriteJson(context, 200, New Dictionary(Of String, Object) From {{"updated", True}})
                Case "deactivate"
                    Dim body = PilotJsonApi.ReadJsonBody(Of GrantLifecycleCommand)(context)
                    service.DeactivateGrant(body)
                    PilotJsonApi.WriteJson(context, 200, New Dictionary(Of String, Object) From {{"updated", True}})
                Case Else
                    PilotJsonApi.WriteError(context, 400, "Unknown action.", PilotJsonApi.IssueCsrfToken(context))
            End Select
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
