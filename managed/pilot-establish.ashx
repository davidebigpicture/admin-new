<%@ WebHandler Language="VB" Class="PilotLegacyEstablishHandler" %>
Imports System
Imports System.Web

''' <summary>
''' Establishes bp_admin_next from legacy /admin cookies. Called server-side from
''' pilot-bridge.asp (under /admin/admin) where those cookies are present.
''' </summary>
Public Class PilotLegacyEstablishHandler
    Implements IHttpHandler

    Public Sub ProcessRequest(context As HttpContext) Implements IHttpHandler.ProcessRequest
        context.Response.ContentType = "text/plain"
        context.Response.ContentEncoding = Text.Encoding.UTF8
        context.Response.Cache.SetCacheability(HttpCacheability.NoCache)
        context.Response.Cache.SetNoStore()

        If Not PilotConfig.IsEnabledForHost(context.Request.Url.Host) Then
            WriteResult(context, 404, "NOTFOUND")
            Return
        End If

        Dim returnUrl = PilotLoginApiPolicy.ResolveReturnUrl(
            context.Request.QueryString("returnUrl"),
            PilotConfig.DefaultRoute,
            PilotConfig.RoutesConfig,
            PilotConfig.PilotRootPath,
            PilotConfig.GlobalAdminRootPath)

        Try
            Dim user As PilotUser = Nothing
            If Not PilotAuth.TryGetCurrentUser(context, user) Then
                WriteResult(context, 401, "NOSESSION")
                Return
            End If

            Dim pilotPath = returnUrl
            Dim queryIndex = pilotPath.IndexOf("?"c)
            If queryIndex >= 0 Then
                pilotPath = pilotPath.Substring(0, queryIndex)
            End If

            If Not PilotAccess.CanAccess(user, pilotPath) Then
                WriteResult(context, 403, "DENY")
                Return
            End If

            WriteResult(context, 200, "OK")
        Catch ex As Exception
            WriteResult(context, 503, "UNAVAILABLE")
        End Try
    End Sub

    Private Shared Sub WriteResult(context As HttpContext, statusCode As Integer, value As String)
        context.Response.StatusCode = statusCode
        context.Response.TrySkipIisCustomErrors = True
        context.Response.Write(value)
    End Sub

    Public ReadOnly Property IsReusable As Boolean Implements IHttpHandler.IsReusable
        Get
            Return False
        End Get
    End Property
End Class
