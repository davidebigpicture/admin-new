Imports System
Imports System.Collections.Generic
Imports System.Configuration

Public Class CodeAdminService
    Private ReadOnly _repository As ICodeAdminRepository
    Private _majorCode As String
    Private ReadOnly _canOpenApp As Boolean

    Public Sub New(user As PilotUser, Optional repository As ICodeAdminRepository = Nothing, Optional canOpenApp As Boolean = False)
        If user Is Nothing Then
            Throw New AccessManagerForbiddenException("Authentication is required.")
        End If

        _canOpenApp = canOpenApp OrElse CodeAdminAccess.CanOpenApp(user)
        If Not _canOpenApp Then
            Throw New AccessManagerForbiddenException("You do not have permission to use Code Admin.")
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
        If classes.Count > 0 Then
            defaultClass = classes(0).CodeClass
        End If

        Return New CodeAdminWorkspace With {
            .Classes = classes,
            .DefaultCodeClass = defaultClass,
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
            Throw New AccessManagerValidationException("Code value was not found.")
        End If
        Return value
    End Function

    Public Function CreateValue(command As CreateCodeValueCommand) As CodeAdminValue
        ValidateCreateCommand(command)
        EnsureEditableClass(command.CodeClass)

        If _repository.ValuePairExists(command.CodeClass, command.CodeValue, Nothing) Then
            Throw New AccessManagerValidationException("Class and value pair already exist.")
        End If

        Return _repository.CreateValue(command, RequireMajorCode())
    End Function

    Public Function UpdateValue(command As UpdateCodeValueCommand) As CodeAdminValue
        ValidateUpdateCommand(command)
        EnsureEditableClass(command.CodeClass)

        Dim existing = GetValue(command.CodeValueId)
        If existing.IsProtected Then
            Throw New AccessManagerForbiddenException("This code value cannot be edited.")
        End If

        Return _repository.UpdateValue(command)
    End Function

    Public Function PatchValue(command As PatchCodeValueCommand) As CodeAdminValue
        If command Is Nothing OrElse command.CodeValueId <= 0 Then
            Throw New AccessManagerValidationException("Code value id is required.")
        End If

        CodeAdminValidation.ValidatePatchField(command.FieldName, command.FieldValue)

        Dim existing = GetValue(command.CodeValueId)
        If existing.IsProtected Then
            Throw New AccessManagerForbiddenException("This code value cannot be edited.")
        End If

        EnsureEditableClass(existing.CodeClass)

        If String.Equals(command.FieldName, "order_by", StringComparison.OrdinalIgnoreCase) Then
            Dim position = Integer.Parse(command.FieldValue, Globalization.CultureInfo.InvariantCulture)
            _repository.SetPosition(existing.CodeClass, existing.CodeValue, position)
            Return GetValue(command.CodeValueId)
        End If

        Return _repository.PatchValue(command)
    End Function

    Public Function DeleteValues(command As DeleteCodeValuesCommand) As IList(Of CodeAdminDeleteResult)
        If command Is Nothing OrElse command.CodeValueIds Is Nothing OrElse command.CodeValueIds.Count = 0 Then
            Throw New AccessManagerValidationException("Select at least one code value to delete.")
        End If

        Dim results As New List(Of CodeAdminDeleteResult)()
        Dim idIndex As Integer
        For idIndex = 0 To command.CodeValueIds.Count - 1
            Dim existing = _repository.GetValueById(command.CodeValueIds(idIndex))
            If existing IsNot Nothing Then
                EnsureEditableClass(existing.CodeClass)
            End If
            results.Add(_repository.DeleteValue(command.CodeValueIds(idIndex)))
        Next
        Return results
    End Function

    Public Sub ActivateValue(command As CodeValueLifecycleCommand)
        ValidateLifecycleCommand(command)
        EnsureEditableClass(command.CodeClass)
        _repository.ActivateValue(command.CodeClass, command.CodeValue, RequireMajorCode())
    End Sub

    Public Sub DeactivateValue(command As CodeValueLifecycleCommand)
        ValidateLifecycleCommand(command)
        EnsureEditableClass(command.CodeClass)
        _repository.DeactivateValue(command.CodeClass, command.CodeValue, RequireMajorCode())
    End Sub

    Public Sub SetPosition(command As CodeValuePositionCommand)
        If command Is Nothing Then
            Throw New AccessManagerValidationException("Position request is not valid.")
        End If
        CodeAdminValidation.ValidateCodeClass(command.CodeClass)
        CodeAdminValidation.ValidateCodeValue(command.CodeValue)
        If command.NewPosition < 1 Then
            Throw New AccessManagerValidationException("Position must be a positive number.")
        End If
        EnsureEditableClass(command.CodeClass)
        _repository.SetPosition(command.CodeClass, command.CodeValue, command.NewPosition)
    End Sub

    Private Shared Sub ValidateCreateCommand(command As CreateCodeValueCommand)
        If command Is Nothing Then
            Throw New AccessManagerValidationException("Create request is not valid.")
        End If
        CodeAdminValidation.ValidateCodeClass(command.CodeClass)
        CodeAdminValidation.ValidateCodeValue(command.CodeValue)
        CodeAdminValidation.ValidateDescription(command.CodeValueDesc)
        CodeAdminValidation.ValidateLongDescription(command.CodeValueLongDesc)
    End Sub

    Private Shared Sub ValidateUpdateCommand(command As UpdateCodeValueCommand)
        If command Is Nothing OrElse command.CodeValueId <= 0 Then
            Throw New AccessManagerValidationException("Update request is not valid.")
        End If
        CodeAdminValidation.ValidateCodeClass(command.CodeClass)
        CodeAdminValidation.ValidateCodeValue(command.CodeValue)
        CodeAdminValidation.ValidateDescription(command.CodeValueDesc)
        CodeAdminValidation.ValidateLongDescription(command.CodeValueLongDesc)
    End Sub

    Private Shared Sub ValidateLifecycleCommand(command As CodeValueLifecycleCommand)
        If command Is Nothing Then
            Throw New AccessManagerValidationException("Lifecycle request is not valid.")
        End If
        CodeAdminValidation.ValidateCodeClass(command.CodeClass)
        CodeAdminValidation.ValidateCodeValue(command.CodeValue)
    End Sub

    Private Sub EnsureEditableClass(codeClass As String)
        Dim codeClassRow = _repository.GetClass(codeClass)
        If codeClassRow Is Nothing OrElse Not codeClassRow.Edit Then
            Throw New AccessManagerForbiddenException("This code class cannot be edited.")
        End If
    End Sub
End Class
