Imports System
Imports System.Collections.Generic

Public NotInheritable Class AccessManagerConstants
    Private Sub New()
    End Sub

    Public Const PermissionGrant As String = "G"
    Public Const SecureTypeSection As String = "SECT"
    Public Const SecureTypeScript As String = "SCRI"
    Public Const PrincipalTypeUser As String = "USER"
    Public Const PrincipalTypeGroup As String = "GROU"
    Public Const InactiveYes As String = "Y"
    Public Const InactiveNo As String = "N"
    Public Const ScriptTypeCodeClass As String = "script_ty"
End Class

Public Class AccessManagerSection
    Public Property SectionId As Integer
    Public Property ParentId As Integer
    Public Property SectionName As String
    Public Property Position As Integer?
    Public Property ModifyBy As Integer
    Public Property ModifyDt As DateTime?
    Public Property CreateBy As Integer
    Public Property CreateDt As DateTime?
    Public Property UpdateNo As Integer
    Public Property Inactive As Boolean
End Class

Public Class AccessManagerScript
    Public Property ScriptId As Integer
    Public Property ScriptTy As String
    Public Property ScriptName As String
    Public Property Title As String
    Public Property ModifyBy As Integer
    Public Property ModifyDt As DateTime?
    Public Property CreateBy As Integer
    Public Property CreateDt As DateTime?
    Public Property UpdateNo As Integer
    Public Property Inactive As Boolean
End Class

Public Class AccessManagerSectionItem
    Public Property SectionId As Integer
    Public Property ScriptId As Integer
    Public Property Position As Integer?
    Public Property ScriptTy As String
    Public Property ScriptName As String
    Public Property Title As String
    Public Property UpdateNo As Integer
    Public Property ScriptInactive As Boolean
End Class

Public Class AccessManagerGrant
    Public Property AccessId As Integer
    Public Property PermissionCd As String
    Public Property SecureId As Integer
    Public Property SecureTy As String
    Public Property UserId As Integer
    Public Property UserTy As String
    Public Property ModifyBy As Integer
    Public Property ModifyDt As DateTime?
    Public Property CreateBy As Integer
    Public Property CreateDt As DateTime?
    Public Property UpdateNo As Integer
    Public Property Inactive As Boolean
    Public Property SecureLabel As String
    Public Property PrincipalLabel As String
End Class

Public Class AccessManagerPrincipal
    Public Property PrincipalTy As String
    Public Property PrincipalId As Integer
    Public Property DisplayName As String
    Public Property UserName As String
    Public Property Inactive As Boolean
End Class

Public Class AccessManagerScriptType
    Public Property CodeValue As String
    Public Property CodeValueDesc As String
    Public Property OrderBy As Integer?
End Class

Public Class AccessManagerDeleteImpact
    Public Property TargetKind As String
    Public Property TargetId As Integer
    Public Property TargetLabel As String
    Public Property AccessRowCount As Integer
    Public Property SectionScriptRowCount As Integer
    Public Property ChildSectionCount As Integer
End Class

Public Class AccessManagerCapabilities
    Public Property CanManageSections As Boolean
    Public Property CanManageScripts As Boolean
    Public Property CanManageMemberships As Boolean
    Public Property CanManageGrants As Boolean
    Public Property SectionCapabilityPath As String
    Public Property ScriptCapabilityPath As String
    Public Property MembershipCapabilityPath As String
    Public Property GrantCapabilityPath As String

    Public Shared Function DenyAll() As AccessManagerCapabilities
        Return New AccessManagerCapabilities()
    End Function

    Public Function CanReadWorkspace() As Boolean
        Return CanManageSections OrElse CanManageScripts OrElse CanManageMemberships OrElse CanManageGrants
    End Function
End Class

Public Class AccessManagerWorkspace
    Public Property Capabilities As AccessManagerCapabilities
    Public Property Sections As IList(Of AccessManagerSection)
    Public Property ScriptTypes As IList(Of AccessManagerScriptType)
    Public Property DefaultScriptType As String
End Class

Public Class AccessManagerEffectiveGrant
    Public Property AccessId As Integer
    Public Property Source As String
    Public Property SectionId As Integer?
    Public Property SectionName As String
    Public Property SecureTy As String
    Public Property SecureId As Integer
    Public Property PrincipalTy As String
    Public Property PrincipalId As Integer
    Public Property PrincipalLabel As String
    Public Property Inactive As Boolean
End Class

Public Class AccessManagerEffectiveAccess
    Public Property ScriptId As Integer
    Public Property ScriptName As String
    Public Property PrincipalTy As String
    Public Property PrincipalId As Integer
    Public Property DirectSectionGrants As IList(Of AccessManagerEffectiveGrant)
    Public Property DirectScriptGrants As IList(Of AccessManagerEffectiveGrant)
    Public Property InheritedSectionGrants As IList(Of AccessManagerEffectiveGrant)
    Public Property HasEffectiveAccess As Boolean
End Class

Public Class CreateSectionCommand
    Public Property SectionName As String
    Public Property ParentId As Integer
End Class

Public Class UpdateSectionCommand
    Public Property SectionId As Integer
    Public Property SectionName As String
    Public Property ExpectedUpdateNo As Integer
End Class

Public Class ReorderSectionCommand
    Public Property SectionId As Integer
    Public Property NewPosition As Integer
    Public Property ExpectedUpdateNo As Integer
End Class

Public Class SectionLifecycleCommand
    Public Property SectionId As Integer
    Public Property ExpectedUpdateNo As Integer
End Class

Public Class HardDeleteSectionCommand
    Public Property SectionId As Integer
    Public Property ExpectedUpdateNo As Integer
    Public Property Confirm As Boolean
End Class

Public Class CreateScriptCommand
    Public Property ScriptTy As String
    Public Property ScriptName As String
    Public Property Title As String
End Class

Public Class UpdateScriptCommand
    Public Property ScriptId As Integer
    Public Property ScriptTy As String
    Public Property ScriptName As String
    Public Property Title As String
    Public Property ExpectedUpdateNo As Integer
End Class

Public Class ScriptLifecycleCommand
    Public Property ScriptId As Integer
    Public Property ExpectedUpdateNo As Integer
End Class

Public Class HardDeleteScriptCommand
    Public Property ScriptId As Integer
    Public Property ExpectedUpdateNo As Integer
    Public Property Confirm As Boolean
End Class

Public Class AddSectionScriptCommand
    Public Property SectionId As Integer
    Public Property ScriptId As Integer
End Class

Public Class RemoveSectionScriptCommand
    Public Property SectionId As Integer
    Public Property ScriptId As Integer
End Class

Public Class ReorderSectionItemCommand
    Public Property SectionId As Integer
    Public Property ScriptId As Integer
    Public Property NewPosition As Integer
End Class

Public Class CreateGrantCommand
    Public Property SecureTy As String
    Public Property SecureId As Integer
    Public Property PrincipalTy As String
    Public Property PrincipalId As Integer
End Class

Public Class GrantLifecycleCommand
    Public Property AccessId As Integer
    Public Property ExpectedUpdateNo As Integer
End Class

Public Class EffectiveAccessQuery
    Public Property PrincipalTy As String
    Public Property PrincipalId As Integer
    Public Property ScriptId As Integer
End Class

Public Interface IAccessManagerRepository
    Function ListSections(parentId As Integer, includeInactive As Boolean) As IList(Of AccessManagerSection)
    Function GetSection(sectionId As Integer) As AccessManagerSection
    Function SectionNameExists(sectionName As String, excludeSectionId As Integer?) As Boolean

    Function ListScriptTypes() As IList(Of AccessManagerScriptType)
    Function ListScripts(scriptTy As String, includeInactive As Boolean) As IList(Of AccessManagerScript)
    Function GetScript(scriptId As Integer) As AccessManagerScript
    Function ScriptNameExists(scriptName As String, excludeScriptId As Integer?) As Boolean

    Function ListSectionItems(sectionId As Integer, includeInactiveScripts As Boolean) As IList(Of AccessManagerSectionItem)
    Function SectionScriptExists(sectionId As Integer, scriptId As Integer) As Boolean

    Function ListGrants(secureTy As String, secureId As Integer, includeInactive As Boolean) As IList(Of AccessManagerGrant)
    Function ListPrincipalGrants(principalTy As String, principalId As Integer, includeInactive As Boolean) As IList(Of AccessManagerGrant)
    Function GetGrant(accessId As Integer) As AccessManagerGrant
    Function FindGrant(secureTy As String, secureId As Integer, principalTy As String, principalId As Integer) As AccessManagerGrant

    Function SearchPrincipals(query As String, principalTy As String, includeInactive As Boolean, limit As Integer) As IList(Of AccessManagerPrincipal)
    Function GetPrincipal(principalTy As String, principalId As Integer) As AccessManagerPrincipal

    Function GetSectionDeleteImpact(sectionId As Integer) As AccessManagerDeleteImpact
    Function GetScriptDeleteImpact(scriptId As Integer) As AccessManagerDeleteImpact
    Function GetEffectiveAccess(query As EffectiveAccessQuery) As AccessManagerEffectiveAccess

    Function CreateSection(command As CreateSectionCommand, actingMemberId As Integer) As AccessManagerSection
    Function UpdateSection(command As UpdateSectionCommand, actingMemberId As Integer) As AccessManagerSection
    Sub ReorderSection(command As ReorderSectionCommand, actingMemberId As Integer)
    Sub DeactivateSection(command As SectionLifecycleCommand, actingMemberId As Integer)
    Sub ActivateSection(command As SectionLifecycleCommand, actingMemberId As Integer)
    Sub HardDeleteSection(command As HardDeleteSectionCommand, actingMemberId As Integer)

    Function CreateScript(command As CreateScriptCommand, actingMemberId As Integer) As AccessManagerScript
    Function UpdateScript(command As UpdateScriptCommand, actingMemberId As Integer) As AccessManagerScript
    Sub DeactivateScript(command As ScriptLifecycleCommand, actingMemberId As Integer)
    Sub ActivateScript(command As ScriptLifecycleCommand, actingMemberId As Integer)
    Sub HardDeleteScript(command As HardDeleteScriptCommand, actingMemberId As Integer)

    Sub AddSectionScript(command As AddSectionScriptCommand, actingMemberId As Integer)
    Sub RemoveSectionScript(command As RemoveSectionScriptCommand, actingMemberId As Integer)
    Sub ReorderSectionItem(command As ReorderSectionItemCommand, actingMemberId As Integer)

    Function CreateOrReactivateGrant(command As CreateGrantCommand, actingMemberId As Integer) As AccessManagerGrant
    Sub DeactivateGrant(command As GrantLifecycleCommand, actingMemberId As Integer)
    Sub ActivateGrant(command As GrantLifecycleCommand, actingMemberId As Integer)
End Interface
