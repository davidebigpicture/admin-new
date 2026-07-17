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

Public Class AccessManagerServiceException
    Inherits Exception

    Public Sub New(message As String)
        MyBase.New(message)
    End Sub
End Class

Public Class AccessManagerForbiddenException
    Inherits AccessManagerServiceException

    Public Sub New(message As String)
        MyBase.New(message)
    End Sub
End Class

Public Class AccessManagerValidationException
    Inherits AccessManagerServiceException

    Public Sub New(message As String)
        MyBase.New(message)
    End Sub
End Class

Public Class AccessManagerConcurrencyException
    Inherits AccessManagerServiceException

    Public Sub New(message As String)
        MyBase.New(message)
    End Sub
End Class
