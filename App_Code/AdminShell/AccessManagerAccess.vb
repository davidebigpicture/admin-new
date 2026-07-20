Imports System

Public NotInheritable Class AccessManagerAccess
    Private Const AccessManagerPilotRoute As String = "managed/access-manager/index.aspx"

    Private Sub New()
    End Sub

    Public Shared Function CanOpenApp(user As PilotUser) As Boolean
        Return PilotAccess.CanAccess(user, PilotConfig.CombinePilot(AccessManagerPilotRoute))
    End Function

    Public Shared Function GetCapabilities(user As PilotUser) As AccessManagerCapabilities
        If user Is Nothing Then
            Return AccessManagerCapabilities.DenyAll()
        End If
        Return AccessManagerCapabilityResolver.Resolve(user.MemberId)
    End Function

    Public Shared Function CanManageSections(user As PilotUser) As Boolean
        Return GetCapabilities(user).CanManageSections
    End Function

    Public Shared Function CanManageScripts(user As PilotUser) As Boolean
        Return GetCapabilities(user).CanManageScripts
    End Function

    Public Shared Function CanManageMemberships(user As PilotUser) As Boolean
        Return GetCapabilities(user).CanManageMemberships
    End Function

    Public Shared Function CanManageGrants(user As PilotUser) As Boolean
        Return GetCapabilities(user).CanManageGrants
    End Function
End Class
