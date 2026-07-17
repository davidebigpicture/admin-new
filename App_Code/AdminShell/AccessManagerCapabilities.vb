Imports System
Imports System.Configuration

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
