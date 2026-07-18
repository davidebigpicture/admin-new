Imports System

Public NotInheritable Class CodeAdminAccess
    Private Sub New()
    End Sub

    Public Shared Function CanOpenApp(user As PilotUser) As Boolean
        Return PilotAccess.CanAccess(user, PilotConfig.CombinePilot(CodeAdminConstants.PilotRoute))
    End Function
End Class
