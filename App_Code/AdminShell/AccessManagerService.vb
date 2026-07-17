Imports System
Imports System.Collections.Generic

Public Class AccessManagerService
    Private ReadOnly _repository As IAccessManagerRepository
    Private ReadOnly _capabilities As AccessManagerCapabilities
    Private ReadOnly _actingMemberId As Integer
    Private ReadOnly _canOpenApp As Boolean

    Public Sub New(
        user As PilotUser,
        Optional repository As IAccessManagerRepository = Nothing,
        Optional capabilities As AccessManagerCapabilities = Nothing,
        Optional canOpenApp As Boolean = False)

        If user Is Nothing Then
            Throw New AccessManagerForbiddenException("Authentication is required.")
        End If

        _actingMemberId = user.MemberId
        _repository = If(repository, New AccessManagerRepository())
        _capabilities = If(capabilities, AccessManagerCapabilityResolver.Resolve(user.MemberId))
        _canOpenApp = canOpenApp OrElse AccessManagerAccess.CanOpenApp(user)
    End Sub

    Public ReadOnly Property Capabilities As AccessManagerCapabilities
        Get
            Return _capabilities
        End Get
    End Property

    Public Function GetWorkspace() As AccessManagerWorkspace
        EnsureCanReadWorkspace()

        Dim scriptTypes = _repository.ListScriptTypes()
        Dim defaultScriptType As String = String.Empty
        If scriptTypes.Count > 0 Then
            defaultScriptType = scriptTypes(0).CodeValue
        End If

        Return New AccessManagerWorkspace With {
            .Capabilities = _capabilities,
            .Sections = _repository.ListSections(0, False),
            .ScriptTypes = scriptTypes,
            .DefaultScriptType = defaultScriptType
        }
    End Function

    Public Function ListSections(includeInactive As Boolean) As IList(Of AccessManagerSection)
        EnsureCanManageSections()
        Return _repository.ListSections(0, includeInactive)
    End Function

    Public Function GetSection(sectionId As Integer) As AccessManagerSection
        EnsureCanManageSections()
        Dim section = _repository.GetSection(sectionId)
        If section Is Nothing Then
            Throw New AccessManagerValidationException("Section was not found.")
        End If
        Return section
    End Function

    Public Function PreviewDeleteSection(sectionId As Integer) As AccessManagerDeleteImpact
        EnsureCanManageSections()
        Return _repository.GetSectionDeleteImpact(sectionId)
    End Function

    Public Function CreateSection(command As CreateSectionCommand) As AccessManagerSection
        EnsureCanManageSections()
        AccessManagerValidation.ValidateUniqueSectionName(
            command.SectionName,
            Nothing,
            Function(name, excludeId) _repository.SectionNameExists(name, excludeId))

        Return _repository.CreateSection(
            New CreateSectionCommand With {
                .SectionName = command.SectionName.Trim(),
                .ParentId = command.ParentId
            },
            _actingMemberId)
    End Function

    Public Function UpdateSection(command As UpdateSectionCommand) As AccessManagerSection
        EnsureCanManageSections()
        AccessManagerValidation.ValidateExpectedUpdateNo(command.ExpectedUpdateNo)
        AccessManagerValidation.ValidateUniqueSectionName(
            command.SectionName,
            command.SectionId,
            Function(name, excludeId) _repository.SectionNameExists(name, excludeId))

        Return _repository.UpdateSection(
            New UpdateSectionCommand With {
                .SectionId = command.SectionId,
                .SectionName = command.SectionName.Trim(),
                .ExpectedUpdateNo = command.ExpectedUpdateNo
            },
            _actingMemberId)
    End Function

    Public Sub ReorderSection(command As ReorderSectionCommand)
        EnsureCanManageSections()

        Dim sections = _repository.ListSections(0, True)
        Dim activeCount As Integer = 0
        Dim sectionIndex As Integer
        For sectionIndex = 0 To sections.Count - 1
            If Not sections(sectionIndex).Inactive Then
                activeCount += 1
            End If
        Next

        AccessManagerValidation.ValidateReorderPosition(command.NewPosition, activeCount)
        _repository.ReorderSection(command, _actingMemberId)
    End Sub

    Public Sub DeactivateSection(command As SectionLifecycleCommand)
        EnsureCanManageSections()
        AccessManagerValidation.ValidateExpectedUpdateNo(command.ExpectedUpdateNo)
        _repository.DeactivateSection(command, _actingMemberId)
    End Sub

    Public Sub ActivateSection(command As SectionLifecycleCommand)
        EnsureCanManageSections()
        AccessManagerValidation.ValidateExpectedUpdateNo(command.ExpectedUpdateNo)
        _repository.ActivateSection(command, _actingMemberId)
    End Sub

    Public Sub HardDeleteSection(command As HardDeleteSectionCommand)
        EnsureCanManageSections()
        AccessManagerValidation.ValidateHardDeleteConfirmed(command.Confirm)
        AccessManagerValidation.ValidateExpectedUpdateNo(command.ExpectedUpdateNo)

        Dim impact = _repository.GetSectionDeleteImpact(command.SectionId)
        If impact.ChildSectionCount > 0 Then
            Throw New AccessManagerValidationException("Section cannot be deleted while child sections exist.")
        End If

        _repository.HardDeleteSection(command, _actingMemberId)
    End Sub

    Public Function ListScripts(scriptTy As String, includeInactive As Boolean) As IList(Of AccessManagerScript)
        EnsureCanManageScripts()
        Dim scriptTypes = _repository.ListScriptTypes()
        AccessManagerValidation.ValidateScriptType(scriptTy, scriptTypes)
        Return _repository.ListScripts(scriptTy, includeInactive)
    End Function

    Public Function GetScript(scriptId As Integer) As AccessManagerScript
        EnsureCanManageScripts()
        Dim script = _repository.GetScript(scriptId)
        If script Is Nothing Then
            Throw New AccessManagerValidationException("Script was not found.")
        End If
        Return script
    End Function

    Public Function PreviewDeleteScript(scriptId As Integer) As AccessManagerDeleteImpact
        EnsureCanManageScripts()
        Return _repository.GetScriptDeleteImpact(scriptId)
    End Function

    Public Function CreateScript(command As CreateScriptCommand) As AccessManagerScript
        EnsureCanManageScripts()
        Dim scriptTypes = _repository.ListScriptTypes()
        AccessManagerValidation.ValidateScriptType(command.ScriptTy, scriptTypes)
        AccessManagerValidation.ValidateScriptTitle(command.Title)
        AccessManagerValidation.ValidateUniqueScriptName(
            command.ScriptName,
            Nothing,
            Function(name, excludeId) _repository.ScriptNameExists(name, excludeId))

        Return _repository.CreateScript(
            New CreateScriptCommand With {
                .ScriptTy = command.ScriptTy.Trim(),
                .ScriptName = command.ScriptName.Trim(),
                .Title = command.Title.Trim()
            },
            _actingMemberId)
    End Function

    Public Function UpdateScript(command As UpdateScriptCommand) As AccessManagerScript
        EnsureCanManageScripts()
        AccessManagerValidation.ValidateExpectedUpdateNo(command.ExpectedUpdateNo)
        Dim scriptTypes = _repository.ListScriptTypes()
        AccessManagerValidation.ValidateScriptType(command.ScriptTy, scriptTypes)
        AccessManagerValidation.ValidateScriptTitle(command.Title)
        AccessManagerValidation.ValidateUniqueScriptName(
            command.ScriptName,
            command.ScriptId,
            Function(name, excludeId) _repository.ScriptNameExists(name, excludeId))

        Return _repository.UpdateScript(
            New UpdateScriptCommand With {
                .ScriptId = command.ScriptId,
                .ScriptTy = command.ScriptTy.Trim(),
                .ScriptName = command.ScriptName.Trim(),
                .Title = command.Title.Trim(),
                .ExpectedUpdateNo = command.ExpectedUpdateNo
            },
            _actingMemberId)
    End Function

    Public Sub DeactivateScript(command As ScriptLifecycleCommand)
        EnsureCanManageScripts()
        AccessManagerValidation.ValidateExpectedUpdateNo(command.ExpectedUpdateNo)
        _repository.DeactivateScript(command, _actingMemberId)
    End Sub

    Public Sub ActivateScript(command As ScriptLifecycleCommand)
        EnsureCanManageScripts()
        AccessManagerValidation.ValidateExpectedUpdateNo(command.ExpectedUpdateNo)
        _repository.ActivateScript(command, _actingMemberId)
    End Sub

    Public Sub HardDeleteScript(command As HardDeleteScriptCommand)
        EnsureCanManageScripts()
        AccessManagerValidation.ValidateHardDeleteConfirmed(command.Confirm)
        AccessManagerValidation.ValidateExpectedUpdateNo(command.ExpectedUpdateNo)
        _repository.HardDeleteScript(command, _actingMemberId)
    End Sub

    Public Function ListSectionItems(sectionId As Integer, includeInactiveScripts As Boolean) As IList(Of AccessManagerSectionItem)
        EnsureCanManageMemberships()
        EnsureSectionExists(sectionId)
        Return _repository.ListSectionItems(sectionId, includeInactiveScripts)
    End Function

    Public Sub AddSectionScript(command As AddSectionScriptCommand)
        EnsureCanManageMemberships()
        EnsureSectionExists(command.SectionId)
        EnsureScriptExists(command.ScriptId)
        _repository.AddSectionScript(command, _actingMemberId)
    End Sub

    Public Sub RemoveSectionScript(command As RemoveSectionScriptCommand)
        EnsureCanManageMemberships()
        _repository.RemoveSectionScript(command, _actingMemberId)
    End Sub

    Public Sub ReorderSectionItem(command As ReorderSectionItemCommand)
        EnsureCanManageMemberships()
        Dim items = _repository.ListSectionItems(command.SectionId, True)
        AccessManagerValidation.ValidateReorderPosition(command.NewPosition, items.Count)
        _repository.ReorderSectionItem(command, _actingMemberId)
    End Sub

    Public Function ListGrants(secureTy As String, secureId As Integer, includeInactive As Boolean) As IList(Of AccessManagerGrant)
        EnsureCanManageGrants()
        AccessManagerValidation.ValidateGrantTarget(secureTy, secureId)
        Return _repository.ListGrants(secureTy.ToUpperInvariant(), secureId, includeInactive)
    End Function

    Public Function ListPrincipalGrants(principalTy As String, principalId As Integer, includeInactive As Boolean) As IList(Of AccessManagerGrant)
        EnsureCanManageGrants()
        AccessManagerValidation.ValidateGrantPrincipal(principalTy, principalId)
        EnsurePrincipalExists(principalTy, principalId)
        Return _repository.ListPrincipalGrants(principalTy.ToUpperInvariant(), principalId, includeInactive)
    End Function

    Public Function CreateGrant(command As CreateGrantCommand) As AccessManagerGrant
        EnsureCanManageGrants()
        AccessManagerValidation.ValidateGrantTarget(command.SecureTy, command.SecureId)
        AccessManagerValidation.ValidateGrantPrincipal(command.PrincipalTy, command.PrincipalId)
        EnsureGrantTargetExists(command.SecureTy, command.SecureId)
        EnsurePrincipalExists(command.PrincipalTy, command.PrincipalId)

        Return _repository.CreateOrReactivateGrant(
            New CreateGrantCommand With {
                .SecureTy = command.SecureTy.ToUpperInvariant(),
                .SecureId = command.SecureId,
                .PrincipalTy = command.PrincipalTy.ToUpperInvariant(),
                .PrincipalId = command.PrincipalId
            },
            _actingMemberId)
    End Function

    Public Sub DeactivateGrant(command As GrantLifecycleCommand)
        EnsureCanManageGrants()
        AccessManagerValidation.ValidateExpectedUpdateNo(command.ExpectedUpdateNo)
        _repository.DeactivateGrant(command, _actingMemberId)
    End Sub

    Public Sub ActivateGrant(command As GrantLifecycleCommand)
        EnsureCanManageGrants()
        AccessManagerValidation.ValidateExpectedUpdateNo(command.ExpectedUpdateNo)
        _repository.ActivateGrant(command, _actingMemberId)
    End Sub

    Public Function SearchPrincipals(query As String, principalTy As String, includeInactive As Boolean, limit As Integer) As IList(Of AccessManagerPrincipal)
        EnsureCanManageGrants()
        Return _repository.SearchPrincipals(query, principalTy, includeInactive, limit)
    End Function

    Public Function GetEffectiveAccess(query As EffectiveAccessQuery) As AccessManagerEffectiveAccess
        EnsureCanManageGrants()
        AccessManagerValidation.ValidateGrantPrincipal(query.PrincipalTy, query.PrincipalId)
        If query.ScriptId <= 0 Then
            Throw New AccessManagerValidationException("Script id is required.")
        End If

        Dim normalized = New EffectiveAccessQuery With {
            .PrincipalTy = query.PrincipalTy.ToUpperInvariant(),
            .PrincipalId = query.PrincipalId,
            .ScriptId = query.ScriptId
        }

        Dim result = _repository.GetEffectiveAccess(normalized)
        Dim principal = _repository.GetPrincipal(normalized.PrincipalTy, normalized.PrincipalId)
        If principal IsNot Nothing Then
            PopulatePrincipalLabels(result.DirectScriptGrants, principal)
            PopulatePrincipalLabels(result.DirectSectionGrants, principal)
            PopulatePrincipalLabels(result.InheritedSectionGrants, principal)
        End If

        Return result
    End Function

    Private Sub EnsureCanReadWorkspace()
        If Not _canOpenApp AndAlso Not _capabilities.CanReadWorkspace() Then
            Throw New AccessManagerForbiddenException("You do not have permission to use Access Manager.")
        End If
    End Sub

    Private Sub EnsureCanManageSections()
        If Not _capabilities.CanManageSections Then
            Throw New AccessManagerForbiddenException("You do not have permission to manage sections.")
        End If
    End Sub

    Private Sub EnsureCanManageScripts()
        If Not _capabilities.CanManageScripts Then
            Throw New AccessManagerForbiddenException("You do not have permission to manage scripts.")
        End If
    End Sub

    Private Sub EnsureCanManageMemberships()
        If Not _capabilities.CanManageMemberships Then
            Throw New AccessManagerForbiddenException("You do not have permission to manage section memberships.")
        End If
    End Sub

    Private Sub EnsureCanManageGrants()
        If Not _capabilities.CanManageGrants Then
            Throw New AccessManagerForbiddenException("You do not have permission to manage grants.")
        End If
    End Sub

    Private Sub EnsureSectionExists(sectionId As Integer)
        If _repository.GetSection(sectionId) Is Nothing Then
            Throw New AccessManagerValidationException("Section was not found.")
        End If
    End Sub

    Private Sub EnsureScriptExists(scriptId As Integer)
        If _repository.GetScript(scriptId) Is Nothing Then
            Throw New AccessManagerValidationException("Script was not found.")
        End If
    End Sub

    Private Sub EnsureGrantTargetExists(secureTy As String, secureId As Integer)
        If String.Equals(secureTy, AccessManagerConstants.SecureTypeSection, StringComparison.OrdinalIgnoreCase) Then
            EnsureSectionExists(secureId)
            Return
        End If
        EnsureScriptExists(secureId)
    End Sub

    Private Sub EnsurePrincipalExists(principalTy As String, principalId As Integer)
        Dim principal = _repository.GetPrincipal(principalTy.ToUpperInvariant(), principalId)
        If principal Is Nothing Then
            Throw New AccessManagerValidationException("Principal was not found.")
        End If
    End Sub

    Private Shared Sub PopulatePrincipalLabels(grants As IList(Of AccessManagerEffectiveGrant), principal As AccessManagerPrincipal)
        Dim grantIndex As Integer
        For grantIndex = 0 To grants.Count - 1
            grants(grantIndex).PrincipalLabel = principal.DisplayName
        Next
    End Sub
End Class
