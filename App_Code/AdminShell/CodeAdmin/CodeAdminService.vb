Imports System
Imports System.Collections.Generic
Imports System.Configuration

Public Class CodeAdminService
    Private ReadOnly _repository As ICodeAdminRepository
    Private _majorCode As String
    Private ReadOnly _canOpenApp As Boolean

    Public Sub New(user As PilotUser, Optional repository As ICodeAdminRepository = Nothing, Optional canOpenApp As Boolean = False)
        If user Is Nothing Then
            Throw New AdminShellForbiddenException("Authentication is required.")
        End If

        _canOpenApp = canOpenApp OrElse CodeAdminAccess.CanOpenApp(user)
        If Not _canOpenApp Then
            Throw New AdminShellForbiddenException("You do not have permission to use Code Admin.")
        End If

        _repository = If(repository, New CodeAdminRepository())
    End Sub

    Private Function RequireMajorCode() As String
        If String.IsNullOrEmpty(_majorCode) Then
            _majorCode = _repository.ResolveMajorCode()
        End If
        Return _majorCode
    End Function

    Public Function GetWorkspace() As CodeAdminWorkspace
        Dim classes = _repository.ListEditableClasses()
        Dim defaultClass As String = String.Empty
        Dim fieldMetadata As New Dictionary(Of String, CodeAdminDetailMetadata)(StringComparer.OrdinalIgnoreCase)
        Dim majorCode = RequireMajorCode()
        Dim classIndex As Integer
        For classIndex = 0 To classes.Count - 1
            fieldMetadata(classes(classIndex).CodeClass) = CodeAdminFieldMetadataRegistry.Build(majorCode, classes(classIndex).CodeClass)
        Next
        If classes.Count > 0 Then
            defaultClass = classes(0).CodeClass
        End If

        Return New CodeAdminWorkspace With {
            .Classes = classes,
            .DefaultCodeClass = defaultClass,
            .MajorCode = majorCode,
            .FieldMetadata = fieldMetadata,
            .ShowClassCodes = String.Equals(
                If(ConfigurationManager.AppSettings("PilotShowDevDetails"), String.Empty).Trim(),
                "true",
                StringComparison.OrdinalIgnoreCase)
        }
    End Function

    Public Function ListValues(codeClass As String, search As String, start As Integer, pageSize As Integer) As CodeAdminValuePage
        CodeAdminValidation.ValidateCodeClass(codeClass)
        CodeAdminValidation.ValidateSearch(search)
        CodeAdminValidation.ValidatePageSize(pageSize)
        If start < 0 Then
            start = 0
        End If

        EnsureEditableClass(codeClass)

        Dim allValues = _repository.ListValues(codeClass, search)
        Dim pageItems As New List(Of CodeAdminValue)()
        Dim endIndex = Math.Min(start + pageSize, allValues.Count) - 1
        Dim itemIndex As Integer
        For itemIndex = start To endIndex
            pageItems.Add(allValues(itemIndex))
        Next

        Return New CodeAdminValuePage With {
            .Items = pageItems,
            .TotalCount = allValues.Count,
            .Start = start,
            .PageSize = pageSize,
            .CanDelete = CodeAdminValidation.ClassAllowsDelete(codeClass)
        }
    End Function

    Public Function GetValue(codeValueId As Integer) As CodeAdminValue
        Dim value = _repository.GetValueById(codeValueId)
        If value Is Nothing Then
            Throw New AdminShellValidationException("Code value was not found.")
        End If
        EnsureEditableClass(value.CodeClass)
        value.FieldMetadata = HydrateMetadata(CodeAdminFieldMetadataRegistry.Build(RequireMajorCode(), value.CodeClass), value)
        Return value
    End Function

    Public Function GetDetailMetadata(codeClass As String, codeValue As String) As CodeAdminDetailMetadata
        CodeAdminValidation.ValidateCodeClass(codeClass)
        EnsureEditableClass(codeClass)
        If Not String.IsNullOrWhiteSpace(codeValue) Then
            CodeAdminValidation.ValidateCodeValue(codeValue)
        End If
        Return HydrateMetadata(
            CodeAdminFieldMetadataRegistry.Build(RequireMajorCode(), codeClass),
            New CodeAdminValue With {.CodeClass = codeClass, .CodeValue = If(codeValue, String.Empty)})
    End Function

    Public Function CreateValue(command As CreateCodeValueCommand) As CodeAdminValue
        ValidateCreateCommand(command)
        NormalizeLookupCommand(command.CodeClass, command.GetOptionValues(), Sub(index, value) command.SetOptionValue(index, value))
        EnsureEditableClass(command.CodeClass)

        If _repository.ValuePairExists(command.CodeClass, command.CodeValue, Nothing) Then
            Throw New AdminShellValidationException("Class and value pair already exist.")
        End If

        Return _repository.CreateValue(command, BuildMutationContext(command.CodeClass))
    End Function

    Public Function UpdateValue(command As UpdateCodeValueCommand) As CodeAdminValue
        ValidateUpdateCommand(command)
        NormalizeLookupCommand(command.CodeClass, command.GetOptionValues(), Sub(index, value) command.SetOptionValue(index, value))
        EnsureEditableClass(command.CodeClass)

        Dim existing = GetValue(command.CodeValueId)
        If existing.IsProtected Then
            Throw New AdminShellForbiddenException("This code value cannot be edited.")
        End If

        Return _repository.UpdateValue(command, BuildMutationContext(command.CodeClass))
    End Function

    Public Function PatchValue(command As PatchCodeValueCommand) As CodeAdminValue
        If command Is Nothing OrElse command.CodeValueId <= 0 Then
            Throw New AdminShellValidationException("Code value id is required.")
        End If

        CodeAdminValidation.ValidatePatchField(command.FieldName, command.FieldValue)

        Dim existing = GetValue(command.CodeValueId)
        If existing.IsProtected Then
            Throw New AdminShellForbiddenException("This code value cannot be edited.")
        End If

        EnsureEditableClass(existing.CodeClass)

        If String.Equals(command.FieldName, "order_by", StringComparison.OrdinalIgnoreCase) Then
            Dim position = Integer.Parse(command.FieldValue, Globalization.CultureInfo.InvariantCulture)
            If existing.Inactive Then
                Throw New AdminShellValidationException("Inactive code values cannot be positioned.")
            End If
            _repository.SetPosition(existing.CodeClass, existing.CodeValue, position)
            Return GetValue(command.CodeValueId)
        End If

        Return _repository.PatchValue(command)
    End Function

    Public Function DeleteValues(command As DeleteCodeValuesCommand) As IList(Of CodeAdminDeleteResult)
        If command Is Nothing OrElse command.CodeValueIds Is Nothing OrElse command.CodeValueIds.Count = 0 Then
            Throw New AdminShellValidationException("Select at least one code value to delete.")
        End If

        Dim results As New List(Of CodeAdminDeleteResult)()
        Dim idIndex As Integer
        For idIndex = 0 To command.CodeValueIds.Count - 1
            Dim existing = _repository.GetValueById(command.CodeValueIds(idIndex))
            If existing IsNot Nothing Then
                EnsureEditableClass(existing.CodeClass)
            End If
            results.Add(_repository.DeleteValue(
                command.CodeValueIds(idIndex),
                If(existing Is Nothing, New CodeAdminMutationContext(), BuildMutationContext(existing.CodeClass))))
        Next
        Return results
    End Function

    Public Sub SetStatus(command As CodeValueLifecycleCommand)
        ValidateLifecycleCommand(command)
        Dim status = If(command.Status, String.Empty).Trim().ToUpperInvariant()
        If Not CodeAdminConstants.IsLifecycleStatus(status) Then
            Throw New AdminShellValidationException("Status must be Active, Inactive, or Archived.")
        End If
        EnsureEditableClass(command.CodeClass)
        RequireMutableValue(command.CodeClass, command.CodeValue)
        _repository.SetStatus(command.CodeClass, command.CodeValue, status)
    End Sub

    Public Sub ActivateValue(command As CodeValueLifecycleCommand)
        SetStatus(New CodeValueLifecycleCommand With {.CodeClass = command.CodeClass, .CodeValue = command.CodeValue, .Status = CodeAdminConstants.StatusActive})
    End Sub

    Public Sub DeactivateValue(command As CodeValueLifecycleCommand)
        SetStatus(New CodeValueLifecycleCommand With {.CodeClass = command.CodeClass, .CodeValue = command.CodeValue, .Status = CodeAdminConstants.StatusInactive})
    End Sub

    Public Sub SetPosition(command As CodeValuePositionCommand)
        If command Is Nothing Then
            Throw New AdminShellValidationException("Position request is not valid.")
        End If
        CodeAdminValidation.ValidateCodeClass(command.CodeClass)
        CodeAdminValidation.ValidateExistingCodeValueReference(command.CodeValue)
        If command.NewPosition < 1 Then
            Throw New AdminShellValidationException("Position must be a positive number.")
        End If
        EnsureEditableClass(command.CodeClass)
        Dim existing = RequireMutableValue(command.CodeClass, command.CodeValue)
        If existing.Inactive Then
            Throw New AdminShellValidationException("Inactive code values cannot be positioned.")
        End If
        _repository.SetPosition(command.CodeClass, command.CodeValue, command.NewPosition)
    End Sub

    Private Function RequireMutableValue(codeClass As String, codeValue As String) As CodeAdminValue
        Dim existing = _repository.GetValueByClassAndValue(codeClass, codeValue)
        If existing Is Nothing Then
            Throw New AdminShellValidationException("Code value was not found.")
        End If
        If existing.IsProtected Then
            Throw New AdminShellForbiddenException("This code value cannot be changed.")
        End If
        Return existing
    End Function

    Private Sub ValidateCreateCommand(command As CreateCodeValueCommand)
        If command Is Nothing Then
            Throw New AdminShellValidationException("Create request is not valid.")
        End If
        CodeAdminValidation.ValidateCodeClass(command.CodeClass)
        CodeAdminValidation.ValidateCodeValue(command.CodeValue)
        CodeAdminValidation.ValidateDescription(command.CodeValueDesc)
        CodeAdminValidation.ValidateLongDescription(command.CodeValueLongDesc)
        ValidateDetailFields(command.FormDisplay, command.GetOptionValues())
        ValidateRequiredDetailFields(command.CodeClass, command.CodeValueLongDesc, command.MinorCode)
        ValidateLookupFields(command.CodeClass, command.CodeValue, command.MinorCode, command.GetOptionValues(), Nothing)
    End Sub

    Private Sub ValidateUpdateCommand(command As UpdateCodeValueCommand)
        If command Is Nothing OrElse command.CodeValueId <= 0 Then
            Throw New AdminShellValidationException("Update request is not valid.")
        End If
        CodeAdminValidation.ValidateCodeClass(command.CodeClass)
        CodeAdminValidation.ValidateExistingCodeValueReference(command.CodeValue)
        CodeAdminValidation.ValidateDescription(command.CodeValueDesc)
        CodeAdminValidation.ValidateLongDescription(command.CodeValueLongDesc)
        ValidateDetailFields(command.FormDisplay, command.GetOptionValues())
        ValidateRequiredDetailFields(command.CodeClass, command.CodeValueLongDesc, command.MinorCode)
        ValidateLookupFields(command.CodeClass, command.CodeValue, command.MinorCode, command.GetOptionValues(), GetValue(command.CodeValueId))
    End Sub

    Private Sub ValidateRequiredDetailFields(codeClass As String, longDescription As String, minorCode As String)
        Dim organizationId = RequireMajorCode()
        If CodeAdminFieldMetadataRegistry.IsRequired(organizationId, codeClass, "codeValueLongDesc") AndAlso String.IsNullOrWhiteSpace(longDescription) Then
            Throw New AdminShellValidationException("Path is required.")
        End If
        If CodeAdminFieldMetadataRegistry.IsRequired(organizationId, codeClass, "minorCode") AndAlso String.IsNullOrWhiteSpace(minorCode) Then
            Throw New AdminShellValidationException("Record Type is required.")
        End If
    End Sub

    Private Shared Sub ValidateDetailFields(formDisplay As String, optionValues As IList(Of String))
        ValidateOptionalField(formDisplay, "Form display")
        Dim optionIndex As Integer
        For optionIndex = 0 To optionValues.Count - 1
            ValidateOptionalField(optionValues(optionIndex), "Optional value " & (optionIndex + 1).ToString(Globalization.CultureInfo.InvariantCulture))
        Next
    End Sub

    Private Shared Sub ValidateOptionalField(value As String, label As String)
        If value IsNot Nothing AndAlso value.Length > CodeAdminConstants.MaxOptionalValueLength Then
            Throw New AdminShellValidationException(label & " is too long.")
        End If
    End Sub

    Private Function BuildMutationContext(codeClass As String) As CodeAdminMutationContext
        Dim majorCode = RequireMajorCode()
        Return New CodeAdminMutationContext With {
            .MajorCode = majorCode,
            .RebuildLicenseObjTypeTables = String.Equals(majorCode, "3900", StringComparison.OrdinalIgnoreCase) AndAlso String.Equals(codeClass, "LicenseObjType", StringComparison.OrdinalIgnoreCase)
        }
    End Function

    Private Function HydrateMetadata(metadata As CodeAdminDetailMetadata, value As CodeAdminValue) As CodeAdminDetailMetadata
        Dim previousMetadataValue = _metadataValue
        _metadataValue = value
        Try
            Dim fieldIndex As Integer
            For fieldIndex = 0 To metadata.Fields.Count - 1
                Dim field = metadata.Fields(fieldIndex)
                If String.IsNullOrWhiteSpace(field.LookupSource) Then
                    Continue For
                End If
                field.Options = GetLookupOptions(field.LookupSource, GetFieldValue(value, field.Key))
            Next
            Return metadata
        Finally
            _metadataValue = previousMetadataValue
        End Try
    End Function

    Private Function GetLookupOptions(source As String, currentValue As String) As IList(Of CodeAdminFieldOption)
        If source.StartsWith("code-values:", StringComparison.OrdinalIgnoreCase) Then
            Dim parts = source.Split(":"c)
            Dim excluded = If(parts.Length > 3 AndAlso String.Equals(parts(2), "exclude", StringComparison.OrdinalIgnoreCase), parts(3), String.Empty)
            Return _repository.ListLookupCodeValues(parts(1), SplitMultiSelect(currentValue), excluded)
        End If
        If String.Equals(source, "code-classes", StringComparison.OrdinalIgnoreCase) Then
            Return _repository.ListLookupCodeClasses(currentValue)
        End If
        If String.Equals(source, "org-sub-type-columns", StringComparison.OrdinalIgnoreCase) Then
            Return If(String.IsNullOrWhiteSpace(GetCurrentOrgSubTypeCode()), New List(Of CodeAdminFieldOption)(), _repository.ListOrgSubTypeColumns(GetCurrentOrgSubTypeCode(), True))
        End If
        If String.Equals(source, "org-sub-type-sort-columns", StringComparison.OrdinalIgnoreCase) Then
            Return If(String.IsNullOrWhiteSpace(GetCurrentOrgSubTypeCode()), New List(Of CodeAdminFieldOption)(), _repository.ListOrgSubTypeColumns(GetCurrentOrgSubTypeCode(), False))
        End If
        If String.Equals(source, "fac-pref-email-fields", StringComparison.OrdinalIgnoreCase) Then
            Return _repository.ListFacPrefEmailFields()
        End If
        If String.Equals(source, "products", StringComparison.OrdinalIgnoreCase) Then
            Return _repository.ListProducts(RequireMajorCode())
        End If
        Throw New AdminShellValidationException("Code Admin lookup source is not supported.")
    End Function

    Private _metadataValue As CodeAdminValue

    Private Function GetCurrentOrgSubTypeCode() As String
        Return If(_metadataValue Is Nothing, String.Empty, _metadataValue.CodeValue)
    End Function

    Private Shared Function GetFieldValue(value As CodeAdminValue, key As String) As String
        If value Is Nothing Then
            Return String.Empty
        End If
        Select Case key
            Case "minorCode" : Return value.MinorCode
            Case "optionValue1" : Return value.OptionValue1
            Case "optionValue2" : Return value.OptionValue2
            Case "optionValue3" : Return value.OptionValue3
            Case "optionValue4" : Return value.OptionValue4
            Case "optionValue14" : Return value.OptionValue14
        End Select
        Return String.Empty
    End Function

    Private Sub ValidateLookupFields(codeClass As String, codeValue As String, minorCode As String, optionValues As IList(Of String), existing As CodeAdminValue)
        Dim metadataValue = existing
        If metadataValue Is Nothing AndAlso String.Equals(codeClass, "ORG_SUB_TY_CD", StringComparison.OrdinalIgnoreCase) Then
            metadataValue = New CodeAdminValue With {.CodeValue = codeValue}
        End If
        Dim metadata = HydrateMetadata(CodeAdminFieldMetadataRegistry.Build(RequireMajorCode(), codeClass), metadataValue)
        Dim fieldIndex As Integer
        For fieldIndex = 0 To metadata.Fields.Count - 1
            Dim field = metadata.Fields(fieldIndex)
            If String.IsNullOrWhiteSpace(field.LookupSource) AndAlso (field.Options Is Nothing OrElse field.Options.Count = 0) Then
                Continue For
            End If
            Dim submitted = If(String.Equals(field.Key, "minorCode", StringComparison.OrdinalIgnoreCase), minorCode, GetOptionValue(optionValues, field.Key))
            ValidateLookupValue(field, submitted)
        Next
    End Sub

    Private Shared Function GetOptionValue(optionValues As IList(Of String), key As String) As String
        If Not key.StartsWith("optionValue", StringComparison.OrdinalIgnoreCase) Then
            Return String.Empty
        End If
        Dim optionIndex As Integer
        If Not Integer.TryParse(key.Substring("optionValue".Length), optionIndex) OrElse optionIndex < 1 OrElse optionIndex > optionValues.Count Then
            Return String.Empty
        End If
        Return optionValues(optionIndex - 1)
    End Function

    Private Shared Sub ValidateLookupValue(field As CodeAdminFieldMetadata, submitted As String)
        If String.IsNullOrWhiteSpace(submitted) Then
            Return
        End If
        Dim validValues As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        Dim optionIndex As Integer
        For optionIndex = 0 To field.Options.Count - 1
            validValues.Add(field.Options(optionIndex).Value)
        Next
        Dim values = If(String.Equals(field.ControlType, "multiselect", StringComparison.OrdinalIgnoreCase), SplitMultiSelect(submitted), New String() {submitted.Trim()})
        Dim valueIndex As Integer
        For valueIndex = 0 To values.Count - 1
            If Not validValues.Contains(values(valueIndex)) Then
                Throw New AdminShellValidationException(field.Label & " contains an invalid selection.")
            End If
        Next
    End Sub

    Private Shared Function SplitMultiSelect(value As String) As IList(Of String)
        Dim results As New List(Of String)()
        For Each item In If(value, String.Empty).Split(New Char() {","c})
            Dim normalized = item.Trim()
            If normalized.Length > 0 AndAlso Not results.Contains(normalized) Then
                results.Add(normalized)
            End If
        Next
        Return results
    End Function

    Private Sub NormalizeLookupCommand(codeClass As String, optionValues As IList(Of String), setOptionValue As Action(Of Integer, String))
        Dim metadata = CodeAdminFieldMetadataRegistry.Build(RequireMajorCode(), codeClass)
        Dim fieldIndex As Integer
        For fieldIndex = 0 To metadata.Fields.Count - 1
            Dim field = metadata.Fields(fieldIndex)
            If Not String.Equals(field.ControlType, "multiselect", StringComparison.OrdinalIgnoreCase) OrElse Not field.Key.StartsWith("optionValue", StringComparison.OrdinalIgnoreCase) Then
                Continue For
            End If
            Dim optionIndex As Integer
            If Integer.TryParse(field.Key.Substring("optionValue".Length), optionIndex) AndAlso optionIndex >= 1 AndAlso optionIndex <= optionValues.Count Then
                setOptionValue(optionIndex, String.Join(", ", SplitMultiSelect(optionValues(optionIndex - 1)).ToArray()))
            End If
        Next
    End Sub

    Private Shared Sub ValidateLifecycleCommand(command As CodeValueLifecycleCommand)
        If command Is Nothing Then
            Throw New AdminShellValidationException("Lifecycle request is not valid.")
        End If
        CodeAdminValidation.ValidateCodeClass(command.CodeClass)
        CodeAdminValidation.ValidateExistingCodeValueReference(command.CodeValue)
    End Sub

    Private Sub EnsureEditableClass(codeClass As String)
        Dim codeClassRow = _repository.GetClass(codeClass)
        If codeClassRow Is Nothing OrElse Not codeClassRow.Edit Then
            Throw New AdminShellForbiddenException("This code class cannot be edited.")
        End If
    End Sub
End Class
