Imports System
Imports System.Web

Public NotInheritable Class AdminShellApiGuard
    Private Sub New()
    End Sub

    Public Shared Function RequireAuthorized(
        context As HttpContext,
        accessPredicate As Func(Of PilotUser, Boolean),
        denialMessage As String,
        ByRef user As PilotUser) As Boolean

        user = Nothing

        If Not PilotConfig.IsEnabledForHost(context.Request.Url.Host) Then
            PilotJsonApi.WriteError(context, 404, "Not found.", Nothing)
            Return False
        End If

        If Not PilotJsonApi.RequireUser(context, user) Then
            Return False
        End If

        If Not accessPredicate(user) Then
            PilotJsonApi.WriteError(
                context,
                403,
                denialMessage,
                PilotJsonApi.IssueCsrfToken(context))
            Return False
        End If

        Return True
    End Function

    Public Shared Function RequireAuthorizedMutation(
        context As HttpContext,
        accessPredicate As Func(Of PilotUser, Boolean),
        denialMessage As String,
        ByRef user As PilotUser) As Boolean

        If Not RequireAuthorized(context, accessPredicate, denialMessage, user) Then
            Return False
        End If

        Return PilotJsonApi.RequireCsrf(context)
    End Function
End Class