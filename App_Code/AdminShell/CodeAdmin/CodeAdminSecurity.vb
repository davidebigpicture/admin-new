Imports System
Imports System.Web

Public NotInheritable Class CodeAdminAccess
    Private Sub New()
    End Sub

    Public Shared Function CanOpenApp(user As PilotUser) As Boolean
        Return PilotAccess.CanAccess(user, PilotConfig.CombinePilot(CodeAdminConstants.ManagedRoute))
    End Function
End Class
Public NotInheritable Class CodeAdminApiGuard
    Private Const AccessDeniedMessage As String = "You do not have permission to use Code Admin."

    Private Sub New()
    End Sub

    Public Shared Function RequireAuthorized(context As HttpContext, ByRef user As PilotUser) As Boolean
        Return AdminShellApiGuard.RequireAuthorized(context, AddressOf CodeAdminAccess.CanOpenApp, AccessDeniedMessage, user)
    End Function

    Public Shared Function RequireAuthorizedMutation(context As HttpContext, ByRef user As PilotUser) As Boolean
        Return AdminShellApiGuard.RequireAuthorizedMutation(context, AddressOf CodeAdminAccess.CanOpenApp, AccessDeniedMessage, user)
    End Function
End Class
