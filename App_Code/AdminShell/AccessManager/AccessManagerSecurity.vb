Imports System
Imports System.Configuration
Imports System.Web

Public NotInheritable Class AccessManagerAccess
    Private Const AccessManagerRoute As String = "managed/access-manager/index.aspx"

    Private Sub New()
    End Sub

    Public Shared Function CanOpenApp(user As PilotUser) As Boolean
        Return PilotAccess.CanAccess(user, PilotConfig.CombinePilot(AccessManagerRoute))
    End Function

    Public Shared Function CanUseWorkspace(user As PilotUser) As Boolean
        Return CanOpenApp(user) OrElse GetCapabilities(user).CanReadWorkspace()
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
Public NotInheritable Class AccessManagerCapabilityResolver
    Private Sub New()
    End Sub

    Public Shared Function Resolve(memberId As Integer) As AccessManagerCapabilities
        Dim repository As New PilotRepository()
        Return Resolve(memberId, repository)
    End Function

    Public Shared Function Resolve(memberId As Integer, repository As PilotRepository) As AccessManagerCapabilities
        If memberId <= 0 Then
            Return AccessManagerCapabilities.DenyAll()
        End If

        Dim sectionPath = ResolveCanonicalPath("AccessManagerSectionCapability")
        Dim scriptPath = ResolveCanonicalPath("AccessManagerScriptCapability")
        Dim membershipPath = ResolveCanonicalPath("AccessManagerMembershipCapability")
        Dim grantPath = ResolveCanonicalPath("AccessManagerGrantCapability")

        Return New AccessManagerCapabilities With {
            .CanManageSections = HasCapability(repository, memberId, sectionPath),
            .CanManageScripts = HasCapability(repository, memberId, scriptPath),
            .CanManageMemberships = HasCapability(repository, memberId, membershipPath),
            .CanManageGrants = HasCapability(repository, memberId, grantPath),
            .SectionCapabilityPath = sectionPath,
            .ScriptCapabilityPath = scriptPath,
            .MembershipCapabilityPath = membershipPath,
            .GrantCapabilityPath = grantPath
        }
    End Function

    Private Shared Function ResolveCanonicalPath(settingKey As String) As String
        Dim relativePath = If(ConfigurationManager.AppSettings(settingKey), String.Empty).Trim()
        If relativePath.Length = 0 Then
            Return String.Empty
        End If

        Try
            Return PilotConfig.CombineGlobal(relativePath)
        Catch ex As ArgumentException
            Return String.Empty
        End Try
    End Function

    Private Shared Function HasCapability(repository As PilotRepository, memberId As Integer, canonicalPath As String) As Boolean
        If String.IsNullOrWhiteSpace(canonicalPath) Then
            Return False
        End If
        Return repository.HasScriptAccess(memberId, canonicalPath)
    End Function
End Class

Public NotInheritable Class AccessManagerApiGuard
    Private Const AccessDeniedMessage As String = "You do not have permission to use Access Manager."

    Private Sub New()
    End Sub

    Public Shared Function RequireAuthorized(context As HttpContext, ByRef user As PilotUser) As Boolean
        Return AdminShellApiGuard.RequireAuthorized(context, AddressOf AccessManagerAccess.CanUseWorkspace, AccessDeniedMessage, user)
    End Function

    Public Shared Function RequireAuthorizedMutation(context As HttpContext, ByRef user As PilotUser) As Boolean
        Return AdminShellApiGuard.RequireAuthorizedMutation(context, AddressOf AccessManagerAccess.CanUseWorkspace, AccessDeniedMessage, user)
    End Function
End Class
