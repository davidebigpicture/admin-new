Imports System
Imports System.Collections.Generic
Imports System.Web
Imports System.Web.SessionState

Public Class PilotSessionHandler
    Implements IHttpHandler
    Implements IRequiresSessionState

    Public Sub ProcessRequest(context As HttpContext) Implements IHttpHandler.ProcessRequest
        PilotJsonApi.PrepareJsonResponse(context)

        If Not PilotConfig.IsEnabledForHost(context.Request.Url.Host) Then
            PilotJsonApi.WriteError(context, 404, "Not found.", Nothing)
            Return
        End If

        If Not String.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase) Then
            context.Response.AppendHeader("Allow", "GET")
            PilotJsonApi.WriteError(context, 405, "Only GET is supported.", Nothing)
            Return
        End If

        Try
            Dim user As PilotUser = Nothing
            If Not PilotJsonApi.RequireUser(context, user) Then
                Return
            End If

            Dim sections = PilotJsonApi.LoadMenuSections(user)
            PilotJsonApi.WriteJson(
                context,
                200,
                New Dictionary(Of String, Object) From {
                    {"userName", user.UserName},
                    {"memberId", user.MemberId},
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
