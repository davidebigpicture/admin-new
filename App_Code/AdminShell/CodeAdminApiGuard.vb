Imports System.Web

Public NotInheritable Class CodeAdminApiGuard
    Private Sub New()
    End Sub

    Public Shared Function RequireAuthorized(context As HttpContext, ByRef user As PilotUser) As Boolean
        user = Nothing

        If Not PilotConfig.IsEnabledForHost(context.Request.Url.Host) Then
            PilotJsonApi.WriteError(context, 404, "Not found.", Nothing)
            Return False
        End If

        If Not PilotJsonApi.RequireUser(context, user) Then
            Return False
        End If

        If Not CodeAdminAccess.CanOpenApp(user) Then
            PilotJsonApi.WriteError(
                context,
                403,
                "You do not have permission to use Code Admin.",
                PilotJsonApi.IssueCsrfToken(context))
            Return False
        End If

        Return True
    End Function

    Public Shared Function RequireAuthorizedMutation(context As HttpContext, ByRef user As PilotUser) As Boolean
        If Not RequireAuthorized(context, user) Then
            Return False
        End If

        If Not PilotJsonApi.RequireCsrf(context) Then
            Return False
        End If

        Return True
    End Function
End Class
