<%@ WebHandler Language="VB" Class="PilotChromeHandler" %>
Imports System
Imports System.Web

Public Class PilotChromeHandler
    Implements IHttpHandler

    Public Sub ProcessRequest(context As HttpContext) Implements IHttpHandler.ProcessRequest
        context.Response.ContentType = "text/html"
        context.Response.ContentEncoding = Text.Encoding.UTF8
        context.Response.Cache.SetCacheability(HttpCacheability.NoCache)
        context.Response.Cache.SetNoStore()

        Dim requestedPath = context.Request.QueryString("script")
        If Not PilotConfig.IsPilotRoute(requestedPath) Then
            Deny(context, 404)
            Return
        End If

        Try
            Dim user As PilotUser = Nothing
            If Not PilotAuth.TryGetCurrentUser(context, user) Then
                Deny(context, 401)
                Return
            End If

            If Not PilotAccess.CanAccess(user, requestedPath) Then
                Deny(context, 403)
                Return
            End If

            Select Case If(context.Request.QueryString("part"), String.Empty).ToLowerInvariant()
                Case "header"
                    context.Response.Write(
                        PilotShell.RenderHeader(
                            user,
                            context.Request.QueryString("title"),
                            requestedPath))
                Case "footer"
                    context.Response.Write(PilotShell.RenderFooter())
                Case Else
                    Deny(context, 400)
            End Select
        Catch ex As Exception
            Deny(context, 503)
        End Try
    End Sub

    Private Shared Sub Deny(context As HttpContext, statusCode As Integer)
        context.Response.StatusCode = statusCode
        context.Response.TrySkipIisCustomErrors = True
    End Sub

    Public ReadOnly Property IsReusable As Boolean Implements IHttpHandler.IsReusable
        Get
            Return False
        End Get
    End Property
End Class
