Imports System.Collections.Generic

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
