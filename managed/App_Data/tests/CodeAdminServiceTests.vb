Imports System
Imports System.Collections.Generic

#If CODE_ADMIN_TEST Then
Public Class PilotUser
    Public Property MemberId As Integer
    Public Property UserName As String
End Class

Public NotInheritable Class CodeAdminAccess
    Private Sub New()
    End Sub

    Public Shared Function CanOpenApp(user As PilotUser) As Boolean
        Return False
    End Function
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
#End If

Module CodeAdminServiceTests
    Private _failures As Integer

    Function Main() As Integer
        TestCreateRejectsDuplicatePairs()
        TestWorkspaceIncludesClassMetadata()
        TestAppRenewMetadataRequiresPathAndRecordType()
        TestAppRenewCreateRequiresPathAndRecordType()
        TestWindowShadeMetadataUsesDefaultStateOptions()
        TestOrgSubTypeMetadataUsesLegacyOptions()
        TestOrganizationSpecificMetadataUsesLegacyLabels()
        TestRequiredLegacyFieldMappings()
        TestLookupOptionsAreHydratedAndValidateMembership()
        TestCredentialProductsUseResolvedOrganization()
        TestMultiSelectNormalizesToLegacyStorage()
        TestLicenseObjTypeUsesTransactionalRepositoryPath()
        TestLicenseObjTypeDeleteUsesTransactionalRepositoryPath()
        TestOtherDeletesUseGenericRepositoryPath()
        TestGetValueRejectsNonEditableClass()
        TestProtectedValueCannotBeUpdated()
        TestProtectedValueCannotBePositioned()
        TestProtectedValueCannotChangeLifecycle()
        TestStatusAcceptsAllLifecycleValues()
        TestStatusRejectsInvalidLifecycleValue()
        TestLegacyApplicationDbValueCanChangeStatus()
        TestLegacyApplicationDbValueCanBePositioned()
        TestLegacyApplicationDbValueCanBeUpdated()
        TestCreateRetainsStrictCodeValuePolicy()
        TestExistingCodeValueReferenceRequiresNonblankBoundedValue()
        TestPositionRequiresExistingValue()
        TestInactiveValueCannotBePositioned()
        TestArchivedValueCannotBePositioned()
        TestInactiveValueCannotBePositionedThroughPatch()
        TestOptionalValueLengthIsValidated()
        TestOptionalValuesArePassedToCreateRepositoryCommand()
        TestDeleteRequiresSelection()

        If _failures = 0 Then
            Console.WriteLine("All CodeAdminService tests passed.")
            Return 0
        End If

        Console.Error.WriteLine(_failures.ToString() & " CodeAdminService test(s) failed.")
        Return 1
    End Function

    Private Sub TestCreateRejectsDuplicatePairs()
        Dim repository As New FakeCodeAdminRepository()
        repository.Classes.Add(New CodeAdminClass With {
            .CodeClass = "WINDOW_SHADE_CD",
            .CodeClassDesc = "Window Shades",
            .Edit = True
        })
        repository.ExistingPairs("WINDOW_SHADE_CD|BLUE") = True

        Dim service = CreateService(repository)
        AssertThrows(Of AccessManagerValidationException)(
            Sub() service.CreateValue(New CreateCodeValueCommand With {
                .CodeClass = "WINDOW_SHADE_CD",
                .CodeValue = "BLUE",
                .CodeValueDesc = "Blue"
            }),
            "duplicate class/value pairs are rejected")
    End Sub

    Private Sub TestWorkspaceIncludesClassMetadata()
        Dim repository As New FakeCodeAdminRepository()
        repository.Classes.Add(New CodeAdminClass With {
            .CodeClass = "C_BUSINESS_TYPE",
            .CodeClassDesc = "Business Type",
            .Edit = True
        })

        Dim service = CreateService(repository)
        Dim workspace = service.GetWorkspace()

        Dim metadata = workspace.FieldMetadata("C_BUSINESS_TYPE")
        If workspace.Classes.Count = 1 AndAlso workspace.DefaultCodeClass = "C_BUSINESS_TYPE" AndAlso metadata.Fields.Count = 20 AndAlso FindField(metadata, "minorCode") Is Nothing Then
            Pass("workspace includes default class metadata")
        Else
            Fail("workspace includes default class metadata")
        End If
    End Sub

    Private Sub TestAppRenewMetadataRequiresPathAndRecordType()
        Dim metadata = CodeAdminFieldMetadataRegistry.Build("3900", "APP_RENEW_TYPE_CD")
        AssertField(metadata, "codeValueLongDesc", "Path", "textarea", True, "APP_RENEW_TYPE_CD requires Path")
        AssertField(metadata, "minorCode", "Record Type", "select", True, "APP_RENEW_TYPE_CD requires Record Type")
    End Sub

    Private Sub TestAppRenewCreateRequiresPathAndRecordType()
        Dim repository As New FakeCodeAdminRepository()
        repository.Classes.Add(New CodeAdminClass With {
            .CodeClass = "APP_RENEW_TYPE_CD",
            .CodeClassDesc = "Application Renewal Types",
            .Edit = True
        })
        Dim service = CreateService(repository)

        AssertThrows(Of AccessManagerValidationException)(
            Sub() service.CreateValue(New CreateCodeValueCommand With {
                .CodeClass = "APP_RENEW_TYPE_CD",
                .CodeValue = "INITIAL",
                .CodeValueDesc = "Initial"
            }),
            "APP_RENEW_TYPE_CD create requires Path")

        AssertThrows(Of AccessManagerValidationException)(
            Sub() service.CreateValue(New CreateCodeValueCommand With {
                .CodeClass = "APP_RENEW_TYPE_CD",
                .CodeValue = "INITIAL",
                .CodeValueDesc = "Initial",
                .CodeValueLongDesc = "/initial"
            }),
            "APP_RENEW_TYPE_CD create requires Record Type")
    End Sub

    Private Sub TestWindowShadeMetadataUsesDefaultStateOptions()
        Dim metadata = CodeAdminFieldMetadataRegistry.Build("3900", "WINDOW_SHADE_CD")
        Dim field = FindField(metadata, "minorCode")
        If field Is Nothing OrElse field.Label <> "Default State" OrElse field.ControlType <> "radio" OrElse field.Options.Count <> 2 OrElse field.Options(0).Value <> "block" OrElse field.Options(1).Value <> "none" Then
            Fail("WINDOW_SHADE_CD uses Open and Closed default-state radio options")
        Else
            Pass("WINDOW_SHADE_CD uses Open and Closed default-state radio options")
        End If
    End Sub

    Private Sub TestOrgSubTypeMetadataUsesLegacyOptions()
        Dim metadata = CodeAdminFieldMetadataRegistry.Build("3900", "ORG_SUB_TY_CD")
        AssertField(metadata, "optionValue1", "Hide in Portal", "radio", False, "ORG_SUB_TY_CD has Hide in Portal options")
        AssertField(metadata, "optionValue15", "Default Sort Order", "radio", False, "ORG_SUB_TY_CD has Default Sort Order options")
        AssertField(metadata, "optionValue14", "Default Sort Field", "select", False, "ORG_SUB_TY_CD uses dynamic column options")
    End Sub

    Private Sub TestOrganizationSpecificMetadataUsesLegacyLabels()
        Dim metadata = CodeAdminFieldMetadataRegistry.Build("825", "CredentialDefinitionIdnt")
        AssertField(metadata, "optionValue8", "Supervisor Type", "select", False, "org 825 CredentialDefinitionIdnt uses Supervisor Type options")
        AssertField(CodeAdminFieldMetadataRegistry.Build("5500", "LIC_TYPE"), "optionValue3", "Related Practioner License Type", "select", False, "org 5500 preserves legacy Practioner label")
    End Sub

    Private Sub TestLookupOptionsAreHydratedAndValidateMembership()
        Dim repository As New FakeCodeAdminRepository()
        repository.MajorCode = "5500"
        repository.Classes.Add(New CodeAdminClass With {.CodeClass = "LIC_TYPE", .CodeClassDesc = "License Type", .Edit = True})
        repository.LookupValues("APP_RENEW_TYPE_CD") = New List(Of CodeAdminFieldOption) From {New CodeAdminFieldOption With {.Value = "RENEW", .Label = "Renew"}}
        repository.LookupValues("LIC_TYPE") = New List(Of CodeAdminFieldOption) From {New CodeAdminFieldOption With {.Value = "RN", .Label = "Registered Nurse"}}
        Dim workspace = CreateService(repository).GetWorkspace()
        Dim metadata = workspace.FieldMetadata("LIC_TYPE")
        If FindField(metadata, "optionValue2").Options.Count = 0 AndAlso FindField(metadata, "optionValue3").Options.Count = 0 AndAlso repository.LookupCallCount = 0 Then
            Pass("workspace metadata avoids unselected lookup queries")
        Else
            Fail("workspace metadata avoids unselected lookup queries")
        End If
        metadata = CreateService(repository).GetDetailMetadata("LIC_TYPE", "")
        If FindField(metadata, "optionValue2").Options.Count = 1 AndAlso FindField(metadata, "optionValue3").Options.Count = 1 Then
            Pass("detail metadata includes server-owned lookup options")
        Else
            Fail("detail metadata includes server-owned lookup options")
        End If
        AssertThrows(Of AccessManagerValidationException)(Sub() CreateService(repository).CreateValue(New CreateCodeValueCommand With {.CodeClass = "LIC_TYPE", .CodeValue = "LPN", .CodeValueDesc = "Licensed", .OptionValue2 = "FORGED"}), "lookup fields reject values outside server options")
    End Sub

    Private Sub TestCredentialProductsUseResolvedOrganization()
        Dim repository As New FakeCodeAdminRepository()
        repository.MajorCode = "825"
        repository.Classes.Add(New CodeAdminClass With {.CodeClass = "CredentialDefinitionIdnt", .CodeClassDesc = "Credential Definition", .Edit = True})
        CreateService(repository).GetDetailMetadata("CredentialDefinitionIdnt", "")
        If repository.LastProductOrganizationId = "825" Then
            Pass("credential products use the resolved organization")
        Else
            Fail("credential products use the resolved organization")
        End If
    End Sub

    Private Sub TestRequiredLegacyFieldMappings()
        AssertField(CodeAdminFieldMetadataRegistry.Build("3900", "LicenseObjType"), "optionValue1", "Window Shades to Show", "multiselect", False, "3900 LicenseObjType maps window shades")
        AssertField(CodeAdminFieldMetadataRegistry.Build("3900", "LicenseObjType"), "optionValue2", "Groups with Access", "multiselect", False, "3900 LicenseObjType maps groups")
        AssertField(CodeAdminFieldMetadataRegistry.Build("3900", "LicenseObjType"), "optionValue3", "License Group", "select", False, "3900 LicenseObjType maps license group")
        AssertField(CodeAdminFieldMetadataRegistry.Build("3900", "APP_RENEW_TYPE_CD"), "optionValue1", "License Obj Types for Duplicate Checks", "multiselect", False, "3900 APP_RENEW_TYPE_CD maps duplicate license types")
        AssertField(CodeAdminFieldMetadataRegistry.Build("2500", "Shades"), "optionValue1", "Child Types to Show", "multiselect", False, "2500 Shades maps child types")
        AssertField(CodeAdminFieldMetadataRegistry.Build("2500", "Type_of_Pipe"), "optionValue1", "Description Class", "select", False, "2500 Type_of_Pipe maps code classes")
        AssertField(CodeAdminFieldMetadataRegistry.Build("225", "Business_Type"), "optionValue1", "Parent Value", "select", False, "225 Business_Type maps parent values")
        AssertField(CodeAdminFieldMetadataRegistry.Build("825", "Warning"), "optionValue1", "Application/Renewal", "select", False, "825 Warning maps warning forms")
        AssertField(CodeAdminFieldMetadataRegistry.Build("825", "CredentialDefinitionIdnt"), "optionValue1", "Renewal Product", "select", False, "825 credential maps renewal product")
        AssertField(CodeAdminFieldMetadataRegistry.Build("825", "CredentialDefinitionIdnt"), "optionValue3", "Credential Type", "select", False, "825 credential maps credential type")
        AssertField(CodeAdminFieldMetadataRegistry.Build("825", "CredentialDefinitionIdnt"), "optionValue6", "Application Product", "select", False, "825 credential maps application product")
        AssertField(CodeAdminFieldMetadataRegistry.Build("1900", "FAC_PREF"), "optionValue1", "Preferred Email Field", "select", False, "1900 FAC_PREF maps email fields")
        AssertField(CodeAdminFieldMetadataRegistry.Build("1900", "CERT_LVL_CD"), "optionValue5", "Send to Awaiting Updates", "radio", False, "1900 CERT_LVL_CD maps awaiting updates")
        AssertField(CodeAdminFieldMetadataRegistry.Build("1900", "FAC_TYPE"), "optionValue1", "Include in Registrants per Facility Type Report", "radio", False, "1900 FAC_TYPE maps report option")
        AssertField(CodeAdminFieldMetadataRegistry.Build("330", "ZIP_CODES"), "minorCode", "County", "select", False, "org 330 ZIP_CODES maps county lookup")
        If FindField(CodeAdminFieldMetadataRegistry.Build("3900", "ZIP_CODES"), "minorCode") Is Nothing Then
            Pass("non-330 ZIP_CODES has no county metadata")
        Else
            Fail("non-330 ZIP_CODES has no county metadata")
        End If
    End Sub

    Private Sub TestMultiSelectNormalizesToLegacyStorage()
        Dim repository As New FakeCodeAdminRepository()
        repository.Classes.Add(New CodeAdminClass With {.CodeClass = "LicenseObjType", .CodeClassDesc = "License Object", .Edit = True})
        repository.LookupValues("WINDOW_SHADE_CD") = New List(Of CodeAdminFieldOption) From {New CodeAdminFieldOption With {.Value = "A", .Label = "A"}, New CodeAdminFieldOption With {.Value = "B", .Label = "B"}}
        repository.LookupValues("GROUP_TY_CD") = New List(Of CodeAdminFieldOption)()
        repository.LookupValues("LicenseGroups") = New List(Of CodeAdminFieldOption)()
        CreateService(repository).CreateValue(New CreateCodeValueCommand With {.CodeClass = "LicenseObjType", .CodeValue = "LIC", .CodeValueDesc = "License", .OptionValue1 = "A,B,A"})
        If repository.LastCreated.OptionValue1 = "A, B" Then Pass("multiselect values normalize to legacy comma-space storage") Else Fail("multiselect values normalize to legacy comma-space storage")
    End Sub

    Private Sub TestLicenseObjTypeUsesTransactionalRepositoryPath()
        Dim repository As New FakeCodeAdminRepository()
        repository.Classes.Add(New CodeAdminClass With {.CodeClass = "LicenseObjType", .CodeClassDesc = "License Object", .Edit = True})
        repository.LookupValues("WINDOW_SHADE_CD") = New List(Of CodeAdminFieldOption)()
        repository.LookupValues("GROUP_TY_CD") = New List(Of CodeAdminFieldOption)()
        repository.LookupValues("LicenseGroups") = New List(Of CodeAdminFieldOption)()
        CreateService(repository).CreateValue(New CreateCodeValueCommand With {.CodeClass = "LicenseObjType", .CodeValue = "LIC", .CodeValueDesc = "License"})
        If repository.CreatedLicenseObjType Then Pass("org 3900 LicenseObjType uses transactional repository path") Else Fail("org 3900 LicenseObjType uses transactional repository path")
    End Sub

    Private Sub TestLicenseObjTypeDeleteUsesTransactionalRepositoryPath()
        Dim repository As New FakeCodeAdminRepository()
        repository.Classes.Add(New CodeAdminClass With {.CodeClass = "LicenseObjType", .CodeClassDesc = "License Object", .Edit = True})
        repository.Values.Add(New CodeAdminValue With {.CodeValueId = 71, .CodeClass = "LicenseObjType", .CodeValue = "LIC", .CodeValueDesc = "License"})

        CreateService(repository).DeleteValues(New DeleteCodeValuesCommand With {.CodeValueIds = New List(Of Integer) From {71}})

        If repository.LicenseObjTypeDeleteCount = 1 AndAlso repository.GenericDeleteCount = 0 Then
            Pass("org 3900 LicenseObjType delete uses transactional repository path")
        Else
            Fail("org 3900 LicenseObjType delete uses transactional repository path")
        End If
    End Sub

    Private Sub TestOtherDeletesUseGenericRepositoryPath()
        Dim nonLicenseRepository As New FakeCodeAdminRepository()
        nonLicenseRepository.Classes.Add(New CodeAdminClass With {.CodeClass = "WINDOW_SHADE_CD", .CodeClassDesc = "Window Shade", .Edit = True})
        nonLicenseRepository.Values.Add(New CodeAdminValue With {.CodeValueId = 72, .CodeClass = "WINDOW_SHADE_CD", .CodeValue = "OPEN", .CodeValueDesc = "Open"})
        CreateService(nonLicenseRepository).DeleteValues(New DeleteCodeValuesCommand With {.CodeValueIds = New List(Of Integer) From {72}})

        Dim otherOrganizationRepository As New FakeCodeAdminRepository()
        otherOrganizationRepository.MajorCode = "3901"
        otherOrganizationRepository.Classes.Add(New CodeAdminClass With {.CodeClass = "LicenseObjType", .CodeClassDesc = "License Object", .Edit = True})
        otherOrganizationRepository.Values.Add(New CodeAdminValue With {.CodeValueId = 73, .CodeClass = "LicenseObjType", .CodeValue = "LIC", .CodeValueDesc = "License"})
        CreateService(otherOrganizationRepository).DeleteValues(New DeleteCodeValuesCommand With {.CodeValueIds = New List(Of Integer) From {73}})

        If nonLicenseRepository.GenericDeleteCount = 1 AndAlso nonLicenseRepository.LicenseObjTypeDeleteCount = 0 AndAlso
           otherOrganizationRepository.GenericDeleteCount = 1 AndAlso otherOrganizationRepository.LicenseObjTypeDeleteCount = 0 Then
            Pass("non-3900 or non-LicenseObjType deletes use generic repository path")
        Else
            Fail("non-3900 or non-LicenseObjType deletes use generic repository path")
        End If
    End Sub

    Private Sub AssertField(metadata As CodeAdminDetailMetadata, key As String, label As String, controlType As String, required As Boolean, message As String)
        Dim field = FindField(metadata, key)
        If field Is Nothing OrElse field.Label <> label OrElse field.ControlType <> controlType OrElse field.Required <> required Then
            Fail(message)
        Else
            Pass(message)
        End If
    End Sub

    Private Function FindField(metadata As CodeAdminDetailMetadata, key As String) As CodeAdminFieldMetadata
        Dim fieldIndex As Integer
        For fieldIndex = 0 To metadata.Fields.Count - 1
            If String.Equals(metadata.Fields(fieldIndex).Key, key, StringComparison.OrdinalIgnoreCase) Then
                Return metadata.Fields(fieldIndex)
            End If
        Next
        Return Nothing
    End Function

    Private Sub TestGetValueRejectsNonEditableClass()
        Dim repository As New FakeCodeAdminRepository()
        repository.Classes.Add(New CodeAdminClass With {
            .CodeClass = "READ_ONLY_CLASS",
            .CodeClassDesc = "Read Only Class",
            .Edit = False
        })
        repository.Values.Add(New CodeAdminValue With {
            .CodeValueId = 44,
            .CodeClass = "READ_ONLY_CLASS",
            .CodeValue = "LOCKED",
            .CodeValueDesc = "Locked"
        })

        AssertThrows(Of AccessManagerForbiddenException)(
            Sub() CreateService(repository).GetValue(44),
            "detail reads reject non-editable classes")
    End Sub

    Private Sub TestProtectedValueCannotBeUpdated()
        Dim repository As New FakeCodeAdminRepository()
        repository.Values.Add(New CodeAdminValue With {
            .CodeValueId = 1,
            .CodeClass = "APPLICATION_DB",
            .CodeValue = "DEV_DOMAIN",
            .CodeValueDesc = "Dev Domain",
            .IsProtected = True
        })
        repository.Classes.Add(New CodeAdminClass With {
            .CodeClass = "APPLICATION_DB",
            .CodeClassDesc = "Application DB",
            .Edit = True
        })

        Dim service = CreateService(repository)
        AssertThrows(Of AccessManagerForbiddenException)(
            Sub() service.UpdateValue(New UpdateCodeValueCommand With {
                .CodeValueId = 1,
                .CodeClass = "APPLICATION_DB",
                .CodeValue = "DEV_DOMAIN",
                .CodeValueDesc = "Changed"
            }),
            "protected values cannot be updated")
    End Sub

    Private Sub TestProtectedValueCannotBePositioned()
        Dim repository = CreateProtectedValueRepository()
        Dim service = CreateService(repository)

        AssertThrows(Of AccessManagerForbiddenException)(
            Sub() service.SetPosition(New CodeValuePositionCommand With {
                .CodeClass = "APPLICATION_DB",
                .CodeValue = "DEV_DOMAIN",
                .NewPosition = 1
            }),
            "protected values cannot be repositioned")
    End Sub

    Private Sub TestProtectedValueCannotChangeLifecycle()
        Dim repository = CreateProtectedValueRepository()
        Dim service = CreateService(repository)
        Dim command As New CodeValueLifecycleCommand With {
            .CodeClass = "APPLICATION_DB",
            .CodeValue = "DEV_DOMAIN"
        }

        AssertThrows(Of AccessManagerForbiddenException)(
            Sub() service.ActivateValue(command),
            "protected values cannot be activated")
        AssertThrows(Of AccessManagerForbiddenException)(
            Sub() service.DeactivateValue(command),
            "protected values cannot be deactivated")
    End Sub

    Private Sub TestStatusAcceptsAllLifecycleValues()
        Dim repository As New FakeCodeAdminRepository()
        repository.MajorCode = String.Empty
        repository.Classes.Add(New CodeAdminClass With {.CodeClass = "WINDOW_SHADE_CD", .CodeClassDesc = "Window Shades", .Edit = True})
        repository.Values.Add(New CodeAdminValue With {.CodeValueId = 8, .CodeClass = "WINDOW_SHADE_CD", .CodeValue = "OPEN", .CodeValueDesc = "Open"})
        Dim service = CreateService(repository)

        service.SetStatus(New CodeValueLifecycleCommand With {.CodeClass = "WINDOW_SHADE_CD", .CodeValue = "OPEN", .Status = CodeAdminConstants.StatusActive})
        service.SetStatus(New CodeValueLifecycleCommand With {.CodeClass = "WINDOW_SHADE_CD", .CodeValue = "OPEN", .Status = CodeAdminConstants.StatusInactive})
        service.SetStatus(New CodeValueLifecycleCommand With {.CodeClass = "WINDOW_SHADE_CD", .CodeValue = "OPEN", .Status = CodeAdminConstants.StatusArchived})

        If String.Join(",", repository.Statuses) = "N,Y,A" Then
            Pass("status accepts and routes Active, Inactive, and Archived values")
        Else
            Fail("status accepts and routes Active, Inactive, and Archived values")
        End If
    End Sub

    Private Sub TestStatusRejectsInvalidLifecycleValue()
        Dim repository As New FakeCodeAdminRepository()
        repository.Classes.Add(New CodeAdminClass With {.CodeClass = "WINDOW_SHADE_CD", .CodeClassDesc = "Window Shades", .Edit = True})
        repository.Values.Add(New CodeAdminValue With {.CodeValueId = 9, .CodeClass = "WINDOW_SHADE_CD", .CodeValue = "OPEN", .CodeValueDesc = "Open"})

        AssertThrows(Of AccessManagerValidationException)(
            Sub() CreateService(repository).SetStatus(New CodeValueLifecycleCommand With {.CodeClass = "WINDOW_SHADE_CD", .CodeValue = "OPEN", .Status = "X"}),
            "status rejects values outside Active, Inactive, and Archived")
    End Sub

    Private Sub TestLegacyApplicationDbValueCanChangeStatus()
        Dim repository = CreateLegacyApplicationDbRepository()

        CreateService(repository).SetStatus(New CodeValueLifecycleCommand With {
            .CodeClass = "APPLICATION_DB",
            .CodeValue = "/db/customforms.js",
            .Status = CodeAdminConstants.StatusActive
        })

        If String.Join(",", repository.Statuses) = CodeAdminConstants.StatusActive Then
            Pass("legacy APPLICATION_DB value can be activated")
        Else
            Fail("legacy APPLICATION_DB value can be activated")
        End If
    End Sub

    Private Sub TestLegacyApplicationDbValueCanBePositioned()
        Dim repository = CreateLegacyApplicationDbRepository()

        CreateService(repository).SetPosition(New CodeValuePositionCommand With {
            .CodeClass = "APPLICATION_DB",
            .CodeValue = "/db/customforms.js",
            .NewPosition = 3
        })

        If repository.LastPositionedCodeValue = "/db/customforms.js" AndAlso repository.LastPositionedValue = 3 Then
            Pass("legacy APPLICATION_DB value can be positioned")
        Else
            Fail("legacy APPLICATION_DB value can be positioned")
        End If
    End Sub

    Private Sub TestLegacyApplicationDbValueCanBeUpdated()
        Dim repository = CreateLegacyApplicationDbRepository()

        CreateService(repository).UpdateValue(New UpdateCodeValueCommand With {
            .CodeValueId = 15,
            .CodeClass = "APPLICATION_DB",
            .CodeValue = "/db/customforms.js",
            .CodeValueDesc = "Custom forms"
        })

        If repository.LastUpdated IsNot Nothing AndAlso repository.LastUpdated.CodeValue = "/db/customforms.js" Then
            Pass("legacy APPLICATION_DB value can be updated")
        Else
            Fail("legacy APPLICATION_DB value can be updated")
        End If
    End Sub

    Private Sub TestCreateRetainsStrictCodeValuePolicy()
        Dim repository = CreateLegacyApplicationDbRepository()

        AssertThrows(Of AccessManagerValidationException)(
            Sub() CreateService(repository).CreateValue(New CreateCodeValueCommand With {
                .CodeClass = "APPLICATION_DB",
                .CodeValue = "/db/customforms.js",
                .CodeValueDesc = "Custom forms"
            }),
            "create retains strict code value naming policy")
    End Sub

    Private Sub TestExistingCodeValueReferenceRequiresNonblankBoundedValue()
        AssertThrows(Of AccessManagerValidationException)(
            Sub() CodeAdminValidation.ValidateExistingCodeValueReference(" "),
            "existing code value references require a value")
        AssertThrows(Of AccessManagerValidationException)(
            Sub() CodeAdminValidation.ValidateExistingCodeValueReference(New String("x"c, CodeAdminConstants.MaxCodeValueLength + 1)),
            "existing code value references enforce the maximum length")
    End Sub

    Private Sub TestPositionRequiresExistingValue()
        Dim repository As New FakeCodeAdminRepository()
        repository.Classes.Add(New CodeAdminClass With {
            .CodeClass = "WINDOW_SHADE_CD",
            .CodeClassDesc = "Window Shades",
            .Edit = True
        })
        Dim service = CreateService(repository)

        AssertThrows(Of AccessManagerValidationException)(
            Sub() service.SetPosition(New CodeValuePositionCommand With {
                .CodeClass = "WINDOW_SHADE_CD",
                .CodeValue = "MISSING",
                .NewPosition = 1
            }),
            "position requires an existing code value")
    End Sub

    Private Sub TestInactiveValueCannotBePositioned()
        Dim repository As New FakeCodeAdminRepository()
        repository.Classes.Add(New CodeAdminClass With {
            .CodeClass = "WINDOW_SHADE_CD",
            .CodeClassDesc = "Window Shades",
            .Edit = True
        })
        repository.Values.Add(New CodeAdminValue With {
            .CodeValueId = 2,
            .CodeClass = "WINDOW_SHADE_CD",
            .CodeValue = "CLOSED",
            .CodeValueDesc = "Closed",
            .Inactive = True
        })
        Dim service = CreateService(repository)

        AssertThrows(Of AccessManagerValidationException)(
            Sub() service.SetPosition(New CodeValuePositionCommand With {
                .CodeClass = "WINDOW_SHADE_CD",
                .CodeValue = "CLOSED",
                .NewPosition = 1
            }),
            "inactive values cannot be positioned")
    End Sub

    Private Sub TestArchivedValueCannotBePositioned()
        Dim repository As New FakeCodeAdminRepository()
        repository.Classes.Add(New CodeAdminClass With {.CodeClass = "WINDOW_SHADE_CD", .CodeClassDesc = "Window Shades", .Edit = True})
        repository.Values.Add(New CodeAdminValue With {.CodeValueId = 4, .CodeClass = "WINDOW_SHADE_CD", .CodeValue = "ARCHIVED", .CodeValueDesc = "Archived", .Status = CodeAdminConstants.StatusArchived, .Inactive = True})

        AssertThrows(Of AccessManagerValidationException)(
            Sub() CreateService(repository).SetPosition(New CodeValuePositionCommand With {.CodeClass = "WINDOW_SHADE_CD", .CodeValue = "ARCHIVED", .NewPosition = 1}),
            "archived values cannot be positioned")
    End Sub

    Private Sub TestInactiveValueCannotBePositionedThroughPatch()
        Dim repository As New FakeCodeAdminRepository()
        repository.Classes.Add(New CodeAdminClass With {
            .CodeClass = "WINDOW_SHADE_CD",
            .CodeClassDesc = "Window Shades",
            .Edit = True
        })
        repository.Values.Add(New CodeAdminValue With {
            .CodeValueId = 3,
            .CodeClass = "WINDOW_SHADE_CD",
            .CodeValue = "CLOSED",
            .CodeValueDesc = "Closed",
            .Inactive = True
        })

        AssertThrows(Of AccessManagerValidationException)(
            Sub() CreateService(repository).PatchValue(New PatchCodeValueCommand With {
                .CodeValueId = 3,
                .FieldName = "order_by",
                .FieldValue = "1"
            }),
            "inactive values cannot be positioned through patch")
    End Sub

    Private Sub TestOptionalValueLengthIsValidated()
        Dim repository As New FakeCodeAdminRepository()
        repository.Classes.Add(New CodeAdminClass With {
            .CodeClass = "WINDOW_SHADE_CD",
            .CodeClassDesc = "Window Shades",
            .Edit = True
        })

        AssertThrows(Of AccessManagerValidationException)(
            Sub() CreateService(repository).CreateValue(New CreateCodeValueCommand With {
                .CodeClass = "WINDOW_SHADE_CD",
                .CodeValue = "BLUE",
                .CodeValueDesc = "Blue",
                .OptionValue1 = New String("x"c, CodeAdminConstants.MaxOptionalValueLength + 1)
            }),
            "optional values longer than 1000 characters are rejected")
    End Sub

    Private Sub TestOptionalValuesArePassedToCreateRepositoryCommand()
        Dim repository As New FakeCodeAdminRepository()
        repository.Classes.Add(New CodeAdminClass With {
            .CodeClass = "C_BUSINESS_TYPE",
            .CodeClassDesc = "Business Type",
            .Edit = True
        })

        CreateService(repository).CreateValue(New CreateCodeValueCommand With {
            .CodeClass = "C_BUSINESS_TYPE",
            .CodeValue = "CORP",
            .CodeValueDesc = "Corporation",
            .FormDisplay = "Business form",
            .OptionValue1 = "Parent",
            .OptionValue17 = "Last optional value"
        })

        If repository.LastCreated Is Nothing OrElse
           repository.LastCreated.FormDisplay <> "Business form" OrElse
           repository.LastCreated.OptionValue1 <> "Parent" OrElse
           repository.LastCreated.OptionValue17 <> "Last optional value" Then
            Fail("optional values and form display are passed to create repository command")
        Else
            Pass("optional values and form display are passed to create repository command")
        End If
    End Sub

    Private Function CreateProtectedValueRepository() As FakeCodeAdminRepository
        Dim repository As New FakeCodeAdminRepository()
        repository.Values.Add(New CodeAdminValue With {
            .CodeValueId = 1,
            .CodeClass = "APPLICATION_DB",
            .CodeValue = "DEV_DOMAIN",
            .CodeValueDesc = "Dev Domain",
            .IsProtected = True
        })
        repository.Classes.Add(New CodeAdminClass With {
            .CodeClass = "APPLICATION_DB",
            .CodeClassDesc = "Application DB",
            .Edit = True
        })
        Return repository
    End Function

    Private Function CreateLegacyApplicationDbRepository() As FakeCodeAdminRepository
        Dim repository As New FakeCodeAdminRepository()
        repository.Classes.Add(New CodeAdminClass With {
            .CodeClass = "APPLICATION_DB",
            .CodeClassDesc = "Application DB",
            .Edit = True
        })
        repository.Values.Add(New CodeAdminValue With {
            .CodeValueId = 15,
            .CodeClass = "APPLICATION_DB",
            .CodeValue = "/db/customforms.js",
            .CodeValueDesc = "Custom forms"
        })
        Return repository
    End Function

    Private Sub TestDeleteRequiresSelection()
        Dim service = CreateService(New FakeCodeAdminRepository())
        AssertThrows(Of AccessManagerValidationException)(
            Sub() service.DeleteValues(New DeleteCodeValuesCommand With {
                .CodeValueIds = New List(Of Integer)()
            }),
            "delete requires at least one id")
    End Sub

    Private Function CreateService(repository As FakeCodeAdminRepository) As CodeAdminService
        Return New CodeAdminService(
            New PilotUser With {.MemberId = 42, .UserName = "tester"},
            repository,
            canOpenApp:=True)
    End Function

    Private Sub AssertThrows(Of TException As Exception)(action As Action, message As String)
        Try
            action()
            _failures += 1
            Console.Error.WriteLine("FAIL: " & message)
        Catch ex As TException
            If _failures = 0 Then
                Console.WriteLine("PASS: " & message)
            End If
        Catch ex As Exception
            _failures += 1
            Console.Error.WriteLine("FAIL: " & message & " (threw " & ex.GetType().Name & ")")
        End Try
    End Sub

    Private Sub Pass(message As String)
        Console.WriteLine("PASS: " & message)
    End Sub

    Private Sub Fail(message As String)
        _failures += 1
        Console.Error.WriteLine("FAIL: " & message)
    End Sub
End Module

Friend Class FakeCodeAdminRepository
    Implements ICodeAdminRepository

    Public Classes As New List(Of CodeAdminClass)()
    Public Values As New List(Of CodeAdminValue)()
    Public ExistingPairs As New Dictionary(Of String, Boolean)(StringComparer.OrdinalIgnoreCase)
    Public LookupValues As New Dictionary(Of String, IList(Of CodeAdminFieldOption))(StringComparer.OrdinalIgnoreCase)
    Public LookupCallCount As Integer
    Public LastProductOrganizationId As String
    Public LastCreated As CreateCodeValueCommand
    Public CreatedLicenseObjType As Boolean
    Public GenericDeleteCount As Integer
    Public LicenseObjTypeDeleteCount As Integer
    Public Statuses As New List(Of String)()
    Public LastUpdated As UpdateCodeValueCommand
    Public LastPositionedCodeValue As String
    Public LastPositionedValue As Integer
    Public MajorCode As String = "3900"

    Public Function ResolveMajorCode() As String Implements ICodeAdminRepository.ResolveMajorCode
        Return MajorCode
    End Function

    Public Function ListEditableClasses() As IList(Of CodeAdminClass) Implements ICodeAdminRepository.ListEditableClasses
        Return Classes
    End Function

    Public Function GetClass(codeClass As String) As CodeAdminClass Implements ICodeAdminRepository.GetClass
        Dim classIndex As Integer
        For classIndex = 0 To Classes.Count - 1
            If String.Equals(Classes(classIndex).CodeClass, codeClass, StringComparison.OrdinalIgnoreCase) Then
                Return Classes(classIndex)
            End If
        Next
        Return Nothing
    End Function

    Public Function ListValues(codeClass As String, search As String) As IList(Of CodeAdminValue) Implements ICodeAdminRepository.ListValues
        Return Values
    End Function

    Public Function ListLookupCodeValues(codeClass As String, selectedValues As IList(Of String), excludeValue As String) As IList(Of CodeAdminFieldOption) Implements ICodeAdminRepository.ListLookupCodeValues
        LookupCallCount += 1
        If LookupValues.ContainsKey(codeClass) Then Return LookupValues(codeClass)
        Return New List(Of CodeAdminFieldOption)()
    End Function

    Public Function ListLookupCodeClasses(selectedValue As String) As IList(Of CodeAdminFieldOption) Implements ICodeAdminRepository.ListLookupCodeClasses
        Dim results As New List(Of CodeAdminFieldOption)()
        For Each item In Classes
            results.Add(New CodeAdminFieldOption With {.Value = item.CodeClass, .Label = item.CodeClassDesc})
        Next
        Return results
    End Function

    Public Function ListOrgSubTypeColumns(orgSubTypeCode As String, includeFunctionFields As Boolean) As IList(Of CodeAdminFieldOption) Implements ICodeAdminRepository.ListOrgSubTypeColumns
        Return New List(Of CodeAdminFieldOption) From {New CodeAdminFieldOption With {.Value = "EMAIL", .Label = "Email"}}
    End Function

    Public Function ListFacPrefEmailFields() As IList(Of CodeAdminFieldOption) Implements ICodeAdminRepository.ListFacPrefEmailFields
        Return New List(Of CodeAdminFieldOption)()
    End Function

    Public Function ListProducts(organizationId As String) As IList(Of CodeAdminFieldOption) Implements ICodeAdminRepository.ListProducts
        LastProductOrganizationId = organizationId
        Return New List(Of CodeAdminFieldOption)()
    End Function

    Public Function GetValueById(codeValueId As Integer) As CodeAdminValue Implements ICodeAdminRepository.GetValueById
        Dim valueIndex As Integer
        For valueIndex = 0 To Values.Count - 1
            If Values(valueIndex).CodeValueId = codeValueId Then
                Return Values(valueIndex)
            End If
        Next
        Return Nothing
    End Function

    Public Function GetValueByClassAndValue(codeClass As String, codeValue As String) As CodeAdminValue Implements ICodeAdminRepository.GetValueByClassAndValue
        Dim valueIndex As Integer
        For valueIndex = 0 To Values.Count - 1
            If String.Equals(Values(valueIndex).CodeClass, codeClass, StringComparison.OrdinalIgnoreCase) AndAlso
               String.Equals(Values(valueIndex).CodeValue, codeValue, StringComparison.OrdinalIgnoreCase) Then
                Return Values(valueIndex)
            End If
        Next
        Return Nothing
    End Function

    Public Function ValuePairExists(codeClass As String, codeValue As String, excludeId As Integer?) As Boolean Implements ICodeAdminRepository.ValuePairExists
        Return ExistingPairs.ContainsKey(codeClass & "|" & codeValue)
    End Function

    Public Function CreateValue(command As CreateCodeValueCommand, majorCode As String) As CodeAdminValue Implements ICodeAdminRepository.CreateValue
        LastCreated = command
        Return New CodeAdminValue With {
            .CodeValueId = 99,
            .CodeClass = command.CodeClass,
            .CodeValue = command.CodeValue,
            .CodeValueDesc = command.CodeValueDesc,
            .FormDisplay = command.FormDisplay,
            .OptionValue1 = command.OptionValue1,
            .OptionValue17 = command.OptionValue17
        }
    End Function

    Public Function CreateLicenseObjTypeValue(command As CreateCodeValueCommand, majorCode As String) As CodeAdminValue Implements ICodeAdminRepository.CreateLicenseObjTypeValue
        CreatedLicenseObjType = True
        Return CreateValue(command, majorCode)
    End Function

    Public Function UpdateValue(command As UpdateCodeValueCommand) As CodeAdminValue Implements ICodeAdminRepository.UpdateValue
        LastUpdated = command
        Return GetValueById(command.CodeValueId)
    End Function

    Public Function UpdateLicenseObjTypeValue(command As UpdateCodeValueCommand, majorCode As String) As CodeAdminValue Implements ICodeAdminRepository.UpdateLicenseObjTypeValue
        Throw New NotImplementedException()
    End Function

    Public Function PatchValue(command As PatchCodeValueCommand) As CodeAdminValue Implements ICodeAdminRepository.PatchValue
        Throw New NotImplementedException()
    End Function

    Public Function DeleteValue(codeValueId As Integer) As CodeAdminDeleteResult Implements ICodeAdminRepository.DeleteValue
        GenericDeleteCount += 1
        Return New CodeAdminDeleteResult With {.Deleted = True}
    End Function

    Public Function DeleteLicenseObjTypeValue(codeValueId As Integer, majorCode As String) As CodeAdminDeleteResult Implements ICodeAdminRepository.DeleteLicenseObjTypeValue
        LicenseObjTypeDeleteCount += 1
        Return New CodeAdminDeleteResult With {.Deleted = True}
    End Function

    Public Sub SetStatus(codeClass As String, codeValue As String, status As String) Implements ICodeAdminRepository.SetStatus
        Statuses.Add(status)
    End Sub

    Public Sub SetPosition(codeClass As String, codeValue As String, newPosition As Integer) Implements ICodeAdminRepository.SetPosition
        LastPositionedCodeValue = codeValue
        LastPositionedValue = newPosition
    End Sub
End Class
