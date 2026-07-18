Imports System
Imports System.Collections.Generic

Module AccessManagerServiceTests
    Private _failures As Integer

    Function Main() As Integer
        TestValidationRejectsBlankSectionName()
        TestCapabilityDenyForSections()
        TestServiceRejectsNoCapabilities()
        TestConcurrencyOnSectionUpdate()
        TestGrantCreateDedupesInactiveRow()
        TestPrincipalGrantLookup()
        TestHardDeleteRequiresConfirmFlag()
        TestSectionDeletePreviewReportsImpact()

        If _failures = 0 Then
            Console.WriteLine("All AccessManagerService tests passed.")
            Return 0
        End If

        Console.Error.WriteLine(_failures.ToString() & " AccessManagerService test(s) failed.")
        Return 1
    End Function

    Private Sub TestValidationRejectsBlankSectionName()
        Dim service = CreateService(AllCapabilities())
        Try
            service.CreateSection(New CreateSectionCommand With {
                .SectionName = "   ",
                .ParentId = 0
            })
            Fail("blank section names are rejected by the service")
        Catch ex As AccessManagerValidationException
            Pass("blank section names are rejected by the service")
        Catch ex As Exception
            Fail("blank section names are rejected by the service (threw " & ex.GetType().Name & ")")
        End Try
    End Sub

    Private Sub TestCapabilityDenyForSections()
        Dim service = CreateService(AllCapabilities(), Function(caps)
                                                           caps.CanManageSections = False
                                                           Return caps
                                                       End Function)
        AssertThrows(Of AccessManagerForbiddenException)(
            Sub() service.ListSections(False),
            "section list requires section capability")
    End Sub

    Private Sub TestServiceRejectsNoCapabilities()
        Try
            Dim service = New AccessManagerService(
                New PilotUser With {
                    .MemberId = 1001,
                    .UserName = "tester"
                },
                New FakeAccessManagerRepository(),
                AccessManagerCapabilities.DenyAll(),
                False)
            Fail("service rejects users with no access manager permissions")
        Catch ex As AccessManagerForbiddenException
            Pass("service rejects users with no access manager permissions")
        Catch ex As Exception
            Fail("service rejects users with no access manager permissions (threw " & ex.GetType().Name & ")")
        End Try
    End Sub

    Private Sub TestConcurrencyOnSectionUpdate()
        Dim repository As New FakeAccessManagerRepository()
        repository.Sections.Add(New AccessManagerSection With {
            .SectionId = 10,
            .ParentId = 0,
            .SectionName = "Logs",
            .Position = 1,
            .UpdateNo = 3,
            .Inactive = False
        })

        Dim service = CreateService(AllCapabilities(), Nothing, repository)
        repository.ForceConcurrencyOnNextUpdate = True

        AssertThrows(Of AccessManagerConcurrencyException)(
            Sub() service.UpdateSection(New UpdateSectionCommand With {
                .SectionId = 10,
                .SectionName = "Audit Logs",
                .ExpectedUpdateNo = 3
            }),
            "stale update numbers surface concurrency conflicts")
    End Sub

    Private Sub TestGrantCreateDedupesInactiveRow()
        Dim repository As New FakeAccessManagerRepository()
        repository.Grants.Add(New AccessManagerGrant With {
            .AccessId = 77,
            .SecureTy = AccessManagerConstants.SecureTypeSection,
            .SecureId = 5,
            .UserTy = AccessManagerConstants.PrincipalTypeUser,
            .UserId = 42,
            .Inactive = True,
            .UpdateNo = 2
        })
        repository.Sections.Add(New AccessManagerSection With {
            .SectionId = 5,
            .SectionName = "Tools",
            .ParentId = 0,
            .Inactive = False
        })
        repository.Principals.Add(New AccessManagerPrincipal With {
            .PrincipalTy = AccessManagerConstants.PrincipalTypeUser,
            .PrincipalId = 42,
            .DisplayName = "dhoffman"
        })

        Dim service = CreateService(AllCapabilities(), Nothing, repository)
        Dim grant = service.CreateGrant(New CreateGrantCommand With {
            .SecureTy = AccessManagerConstants.SecureTypeSection,
            .SecureId = 5,
            .PrincipalTy = AccessManagerConstants.PrincipalTypeUser,
            .PrincipalId = 42
        })

        AssertTrue(grant.AccessId = 77, "inactive grant rows are reactivated instead of duplicated")
        AssertTrue(Not grant.Inactive, "reactivated grants are active")
    End Sub

    Private Sub TestPrincipalGrantLookup()
        Dim repository As New FakeAccessManagerRepository()
        repository.Principals.Add(New AccessManagerPrincipal With {
            .PrincipalTy = AccessManagerConstants.PrincipalTypeGroup,
            .PrincipalId = 7,
            .DisplayName = "Administrators"
        })
        repository.Grants.Add(New AccessManagerGrant With {
            .AccessId = 1,
            .UserTy = AccessManagerConstants.PrincipalTypeGroup,
            .UserId = 7,
            .SecureTy = AccessManagerConstants.SecureTypeSection,
            .SecureId = 5
        })
        repository.Grants.Add(New AccessManagerGrant With {
            .AccessId = 2,
            .UserTy = AccessManagerConstants.PrincipalTypeUser,
            .UserId = 99,
            .SecureTy = AccessManagerConstants.SecureTypeScript,
            .SecureId = 6
        })

        Dim service = CreateService(AllCapabilities(), Nothing, repository)
        Dim grants = service.ListPrincipalGrants(
            AccessManagerConstants.PrincipalTypeGroup,
            7,
            False)
        AssertTrue(grants.Count = 1, "principal access lookup returns only the selected principal's grants")
        AssertTrue(grants(0).AccessId = 1, "principal access lookup returns the matching grant")
    End Sub

    Private Sub TestHardDeleteRequiresConfirmFlag()
        Dim repository As New FakeAccessManagerRepository()
        repository.Sections.Add(New AccessManagerSection With {
            .SectionId = 8,
            .SectionName = "Temp",
            .ParentId = 0,
            .UpdateNo = 1,
            .Inactive = False
        })

        Dim service = CreateService(AllCapabilities(), Nothing, repository)
        AssertThrows(Of AccessManagerValidationException)(
            Sub() service.HardDeleteSection(New HardDeleteSectionCommand With {
                .SectionId = 8,
                .ExpectedUpdateNo = 1,
                .Confirm = False
            }),
            "hard delete requires confirm=true")
    End Sub

    Private Sub TestSectionDeletePreviewReportsImpact()
        Dim repository As New FakeAccessManagerRepository()
        repository.Sections.Add(New AccessManagerSection With {
            .SectionId = 3,
            .SectionName = "Reports",
            .ParentId = 0,
            .UpdateNo = 4,
            .Inactive = False
        })
        repository.DeleteImpacts(3) = New AccessManagerDeleteImpact With {
            .TargetKind = "section",
            .TargetId = 3,
            .TargetLabel = "Reports",
            .AccessRowCount = 2,
            .SectionScriptRowCount = 5,
            .ChildSectionCount = 1
        }

        Dim service = CreateService(AllCapabilities(), Nothing, repository)
        Dim impact = service.PreviewDeleteSection(3)
        AssertTrue(impact.AccessRowCount = 2, "delete preview reports access row count")
        AssertTrue(impact.ChildSectionCount = 1, "delete preview reports child section count")
    End Sub

    Private Function CreateService(
        capabilities As AccessManagerCapabilities,
        Optional adjustCaps As Func(Of AccessManagerCapabilities, AccessManagerCapabilities) = Nothing,
        Optional repository As FakeAccessManagerRepository = Nothing) As AccessManagerService

        Dim repo = If(repository, New FakeAccessManagerRepository())
        Dim caps = capabilities
        If adjustCaps IsNot Nothing Then
            caps = adjustCaps(caps)
        End If
        repo.Capabilities = caps
        Return New AccessManagerService(New PilotUser With {
            .MemberId = 1001,
            .UserName = "tester"
        }, repo, caps, True)
    End Function

    Private Function AllCapabilities() As AccessManagerCapabilities
        Return New AccessManagerCapabilities With {
            .CanManageSections = True,
            .CanManageScripts = True,
            .CanManageMemberships = True,
            .CanManageGrants = True
        }
    End Function

    Private Sub AssertTrue(condition As Boolean, message As String)
        If condition Then
            Pass(message)
        Else
            Fail(message)
        End If
    End Sub

    Private Sub Pass(message As String)
        Console.WriteLine("PASS: " & message)
    End Sub

    Private Sub Fail(message As String)
        _failures += 1
        Console.Error.WriteLine("FAIL: " & message)
    End Sub

    Private Sub AssertThrows(Of TException As Exception)(action As Action, message As String)
        Try
            action()
            Fail(message)
        Catch ex As TException
            Pass(message)
        Catch ex As Exception
            Fail(message & " (threw " & ex.GetType().Name & ")")
        End Try
    End Sub
End Module

Friend Class FakeAccessManagerRepository
    Implements IAccessManagerRepository

    Public Property Capabilities As AccessManagerCapabilities
    Public Property ForceConcurrencyOnNextUpdate As Boolean
    Public ReadOnly Sections As New List(Of AccessManagerSection)()
    Public ReadOnly Scripts As New List(Of AccessManagerScript)()
    Public ReadOnly ScriptTypes As New List(Of AccessManagerScriptType) From {
        New AccessManagerScriptType With {.CodeValue = "ASP", .CodeValueDesc = "ASP"}
    }
    Public ReadOnly SectionItems As New List(Of AccessManagerSectionItem)()
    Public ReadOnly Grants As New List(Of AccessManagerGrant)()
    Public ReadOnly Principals As New List(Of AccessManagerPrincipal)()
    Public ReadOnly DeleteImpacts As New Dictionary(Of Integer, AccessManagerDeleteImpact)()

    Public Function ListSections(parentId As Integer, includeInactive As Boolean) As IList(Of AccessManagerSection) Implements IAccessManagerRepository.ListSections
        Dim results As New List(Of AccessManagerSection)()
        For Each section In Sections
            If section.ParentId = parentId AndAlso (includeInactive OrElse Not section.Inactive) Then
                results.Add(section)
            End If
        Next
        Return results
    End Function

    Public Function GetSection(sectionId As Integer) As AccessManagerSection Implements IAccessManagerRepository.GetSection
        For Each section In Sections
            If section.SectionId = sectionId Then
                Return section
            End If
        Next
        Return Nothing
    End Function

    Public Function SectionNameExists(sectionName As String, excludeSectionId As Integer?) As Boolean Implements IAccessManagerRepository.SectionNameExists
        For Each section In Sections
            If String.Equals(section.SectionName, sectionName, StringComparison.OrdinalIgnoreCase) AndAlso
                (Not excludeSectionId.HasValue OrElse section.SectionId <> excludeSectionId.Value) Then
                Return True
            End If
        Next
        Return False
    End Function

    Public Function ListScriptTypes() As IList(Of AccessManagerScriptType) Implements IAccessManagerRepository.ListScriptTypes
        Return ScriptTypes
    End Function

    Public Function ListScripts(scriptTy As String, includeInactive As Boolean) As IList(Of AccessManagerScript) Implements IAccessManagerRepository.ListScripts
        Dim results As New List(Of AccessManagerScript)()
        For Each script In Scripts
            If String.Equals(script.ScriptTy, scriptTy, StringComparison.OrdinalIgnoreCase) AndAlso
                (includeInactive OrElse Not script.Inactive) Then
                results.Add(script)
            End If
        Next
        Return results
    End Function

    Public Function GetScript(scriptId As Integer) As AccessManagerScript Implements IAccessManagerRepository.GetScript
        For Each script In Scripts
            If script.ScriptId = scriptId Then
                Return script
            End If
        Next
        Return Nothing
    End Function

    Public Function ScriptNameExists(scriptName As String, excludeScriptId As Integer?) As Boolean Implements IAccessManagerRepository.ScriptNameExists
        For Each script In Scripts
            If String.Equals(script.ScriptName, scriptName, StringComparison.OrdinalIgnoreCase) AndAlso
                (Not excludeScriptId.HasValue OrElse script.ScriptId <> excludeScriptId.Value) Then
                Return True
            End If
        Next
        Return False
    End Function

    Public Function ListSectionItems(sectionId As Integer, includeInactiveScripts As Boolean) As IList(Of AccessManagerSectionItem) Implements IAccessManagerRepository.ListSectionItems
        Dim results As New List(Of AccessManagerSectionItem)()
        For Each item In SectionItems
            If item.SectionId = sectionId AndAlso (includeInactiveScripts OrElse Not item.ScriptInactive) Then
                results.Add(item)
            End If
        Next
        Return results
    End Function

    Public Function SectionScriptExists(sectionId As Integer, scriptId As Integer) As Boolean Implements IAccessManagerRepository.SectionScriptExists
        For Each item In SectionItems
            If item.SectionId = sectionId AndAlso item.ScriptId = scriptId Then
                Return True
            End If
        Next
        Return False
    End Function

    Public Function ListGrants(secureTy As String, secureId As Integer, includeInactive As Boolean) As IList(Of AccessManagerGrant) Implements IAccessManagerRepository.ListGrants
        Dim results As New List(Of AccessManagerGrant)()
        For Each grant In Grants
            If String.Equals(grant.SecureTy, secureTy, StringComparison.OrdinalIgnoreCase) AndAlso
                grant.SecureId = secureId AndAlso (includeInactive OrElse Not grant.Inactive) Then
                results.Add(grant)
            End If
        Next
        Return results
    End Function

    Public Function ListPrincipalGrants(principalTy As String, principalId As Integer, includeInactive As Boolean) As IList(Of AccessManagerGrant) Implements IAccessManagerRepository.ListPrincipalGrants
        Dim results As New List(Of AccessManagerGrant)()
        For Each grant In Grants
            If String.Equals(grant.UserTy, principalTy, StringComparison.OrdinalIgnoreCase) AndAlso
                grant.UserId = principalId AndAlso (includeInactive OrElse Not grant.Inactive) Then
                results.Add(grant)
            End If
        Next
        Return results
    End Function

    Public Function GetGrant(accessId As Integer) As AccessManagerGrant Implements IAccessManagerRepository.GetGrant
        For Each grant In Grants
            If grant.AccessId = accessId Then
                Return grant
            End If
        Next
        Return Nothing
    End Function

    Public Function FindGrant(secureTy As String, secureId As Integer, principalTy As String, principalId As Integer) As AccessManagerGrant Implements IAccessManagerRepository.FindGrant
        For Each grant In Grants
            If String.Equals(grant.SecureTy, secureTy, StringComparison.OrdinalIgnoreCase) AndAlso
                grant.SecureId = secureId AndAlso
                String.Equals(grant.UserTy, principalTy, StringComparison.OrdinalIgnoreCase) AndAlso
                grant.UserId = principalId Then
                Return grant
            End If
        Next
        Return Nothing
    End Function

    Public Function SearchPrincipals(query As String, principalTy As String, includeInactive As Boolean, limit As Integer) As IList(Of AccessManagerPrincipal) Implements IAccessManagerRepository.SearchPrincipals
        Return Principals
    End Function

    Public Function GetPrincipal(principalTy As String, principalId As Integer) As AccessManagerPrincipal Implements IAccessManagerRepository.GetPrincipal
        For Each principal In Principals
            If String.Equals(principal.PrincipalTy, principalTy, StringComparison.OrdinalIgnoreCase) AndAlso
                principal.PrincipalId = principalId Then
                Return principal
            End If
        Next
        Return Nothing
    End Function

    Public Function GetSectionDeleteImpact(sectionId As Integer) As AccessManagerDeleteImpact Implements IAccessManagerRepository.GetSectionDeleteImpact
        If DeleteImpacts.ContainsKey(sectionId) Then
            Return DeleteImpacts(sectionId)
        End If
        Return New AccessManagerDeleteImpact With {
            .TargetId = sectionId,
            .AccessRowCount = 0,
            .SectionScriptRowCount = 0,
            .ChildSectionCount = 0
        }
    End Function

    Public Function GetScriptDeleteImpact(scriptId As Integer) As AccessManagerDeleteImpact Implements IAccessManagerRepository.GetScriptDeleteImpact
        Return New AccessManagerDeleteImpact With {.TargetId = scriptId}
    End Function

    Public Function GetEffectiveAccess(query As EffectiveAccessQuery) As AccessManagerEffectiveAccess Implements IAccessManagerRepository.GetEffectiveAccess
        Return New AccessManagerEffectiveAccess With {
            .ScriptId = query.ScriptId,
            .PrincipalTy = query.PrincipalTy,
            .PrincipalId = query.PrincipalId,
            .DirectScriptGrants = New List(Of AccessManagerEffectiveGrant)(),
            .DirectSectionGrants = New List(Of AccessManagerEffectiveGrant)(),
            .InheritedSectionGrants = New List(Of AccessManagerEffectiveGrant)()
        }
    End Function

    Public Function CreateSection(command As CreateSectionCommand, actingMemberId As Integer) As AccessManagerSection Implements IAccessManagerRepository.CreateSection
        Dim section As New AccessManagerSection With {
            .SectionId = Sections.Count + 1,
            .ParentId = command.ParentId,
            .SectionName = command.SectionName,
            .UpdateNo = 1,
            .Inactive = False
        }
        Sections.Add(section)
        Return section
    End Function

    Public Function UpdateSection(command As UpdateSectionCommand, actingMemberId As Integer) As AccessManagerSection Implements IAccessManagerRepository.UpdateSection
        If ForceConcurrencyOnNextUpdate Then
            ForceConcurrencyOnNextUpdate = False
            Throw New AccessManagerConcurrencyException("Section was changed by another user.")
        End If

        Dim section = GetSection(command.SectionId)
        section.SectionName = command.SectionName
        section.UpdateNo += 1
        Return section
    End Function

    Public Sub ReorderSection(command As ReorderSectionCommand, actingMemberId As Integer) Implements IAccessManagerRepository.ReorderSection
    End Sub

    Public Sub DeactivateSection(command As SectionLifecycleCommand, actingMemberId As Integer) Implements IAccessManagerRepository.DeactivateSection
    End Sub

    Public Sub ActivateSection(command As SectionLifecycleCommand, actingMemberId As Integer) Implements IAccessManagerRepository.ActivateSection
    End Sub

    Public Sub HardDeleteSection(command As HardDeleteSectionCommand, actingMemberId As Integer) Implements IAccessManagerRepository.HardDeleteSection
        Sections.RemoveAll(Function(section) section.SectionId = command.SectionId)
    End Sub

    Public Function CreateScript(command As CreateScriptCommand, actingMemberId As Integer) As AccessManagerScript Implements IAccessManagerRepository.CreateScript
        Dim script As New AccessManagerScript With {
            .ScriptId = Scripts.Count + 1,
            .ScriptTy = command.ScriptTy,
            .ScriptName = command.ScriptName,
            .Title = command.Title,
            .UpdateNo = 1,
            .Inactive = False
        }
        Scripts.Add(script)
        Return script
    End Function

    Public Function UpdateScript(command As UpdateScriptCommand, actingMemberId As Integer) As AccessManagerScript Implements IAccessManagerRepository.UpdateScript
        Return GetScript(command.ScriptId)
    End Function

    Public Sub DeactivateScript(command As ScriptLifecycleCommand, actingMemberId As Integer) Implements IAccessManagerRepository.DeactivateScript
    End Sub

    Public Sub ActivateScript(command As ScriptLifecycleCommand, actingMemberId As Integer) Implements IAccessManagerRepository.ActivateScript
    End Sub

    Public Sub HardDeleteScript(command As HardDeleteScriptCommand, actingMemberId As Integer) Implements IAccessManagerRepository.HardDeleteScript
    End Sub

    Public Sub AddSectionScript(command As AddSectionScriptCommand, actingMemberId As Integer) Implements IAccessManagerRepository.AddSectionScript
    End Sub

    Public Sub RemoveSectionScript(command As RemoveSectionScriptCommand, actingMemberId As Integer) Implements IAccessManagerRepository.RemoveSectionScript
    End Sub

    Public Sub ReorderSectionItem(command As ReorderSectionItemCommand, actingMemberId As Integer) Implements IAccessManagerRepository.ReorderSectionItem
    End Sub

    Public Function CreateOrReactivateGrant(command As CreateGrantCommand, actingMemberId As Integer) As AccessManagerGrant Implements IAccessManagerRepository.CreateOrReactivateGrant
        Dim existing = FindGrant(command.SecureTy, command.SecureId, command.PrincipalTy, command.PrincipalId)
        If existing IsNot Nothing Then
            existing.Inactive = False
            existing.UpdateNo += 1
            Return existing
        End If

        Dim grant As New AccessManagerGrant With {
            .AccessId = Grants.Count + 100,
            .SecureTy = command.SecureTy,
            .SecureId = command.SecureId,
            .UserTy = command.PrincipalTy,
            .UserId = command.PrincipalId,
            .Inactive = False,
            .UpdateNo = 1
        }
        Grants.Add(grant)
        Return grant
    End Function

    Public Sub DeactivateGrant(command As GrantLifecycleCommand, actingMemberId As Integer) Implements IAccessManagerRepository.DeactivateGrant
    End Sub

    Public Sub ActivateGrant(command As GrantLifecycleCommand, actingMemberId As Integer) Implements IAccessManagerRepository.ActivateGrant
    End Sub
End Class
