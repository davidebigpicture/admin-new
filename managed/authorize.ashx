<%@ WebHandler Language="VB" Class="PilotAuthorizeHandler" %>
Imports System
Imports System.Web

Public Class PilotAuthorizeHandler
    Implements IHttpHandler

    Public Sub ProcessRequest(context As HttpContext) Implements IHttpHandler.ProcessRequest
        context.Response.ContentType = "text/plain"
        context.Response.ContentEncoding = Text.Encoding.UTF8
        context.Response.Cache.SetCacheability(HttpCacheability.NoCache)
        context.Response.Cache.SetNoStore()

        Dim requestedPath = context.Request.QueryString("script")
        If Not PilotConfig.IsPilotRoute(requestedPath) Then
            WriteResult(context, 404, "UNKNOWN")
            Return
        End If

        Try
            Dim user As PilotUser = Nothing
            If Not PilotAuth.TryGetCurrentUser(context, user) Then
                WriteResult(context, 401, "NOSESSION")
                Return
            End If

            If Not PilotAccess.CanAccess(user, requestedPath) Then
                WriteResult(context, 403, "DENY")
                Return
            End If

            WriteResult(context, 200, "OK|" & HttpUtility.UrlEncode(user.UserName))
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
