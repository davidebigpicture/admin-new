Imports System
Imports System.Text.RegularExpressions

Public NotInheritable Class CodeAdminValidation
    Private Shared ReadOnly CodeValuePattern As New Regex(
        "^[a-zA-Z0-9_\-]+$",
        RegexOptions.Compiled Or RegexOptions.CultureInvariant)

    Private Shared ReadOnly PatchableFields As String() = {
        "code_value_desc",
        "code_value_long_desc",
        "inactive",
        "order_by"
    }

    Private Sub New()
    End Sub

    Public Shared Sub ValidateCodeClass(codeClass As String)
        Dim trimmed = NormalizeRequiredToken(codeClass, "Code class", CodeAdminConstants.MaxCodeValueLength)
        If trimmed.IndexOf("'"c) >= 0 Then
            Throw New AccessManagerValidationException("Code class is not valid.")
        End If
    End Sub

    Public Shared Sub ValidateCodeValue(codeValue As String)
        Dim trimmed = NormalizeRequiredToken(codeValue, "Code value", CodeAdminConstants.MaxCodeValueLength)
        If Not CodeValuePattern.IsMatch(trimmed) Then
            Throw New AccessManagerValidationException("Code value must contain only letters, numbers, hyphens, or underscores.")
        End If
    End Sub

    Public Shared Sub ValidateDescription(description As String)
        NormalizeRequiredToken(description, "Description", CodeAdminConstants.MaxDescriptionLength)
    End Sub

    Public Shared Sub ValidateLongDescription(description As String)
        If description Is Nothing Then
            Return
        End If
        If description.Length > CodeAdminConstants.MaxLongDescriptionLength Then
            Throw New AccessManagerValidationException("Long description is too long.")
        End If
    End Sub

    Public Shared Sub ValidateSearch(search As String)
        If String.IsNullOrWhiteSpace(search) Then
            Return
        End If
        If search.Trim().Length > CodeAdminConstants.MaxSearchLength Then
            Throw New AccessManagerValidationException("Search text is too long.")
        End If
    End Sub

    Public Shared Sub ValidatePageSize(pageSize As Integer)
        If pageSize < 1 OrElse pageSize > CodeAdminConstants.MaxPageSize Then
            Throw New AccessManagerValidationException("Rows per page is out of range.")
        End If
    End Sub

    Public Shared Sub ValidatePatchField(fieldName As String, fieldValue As String)
        Dim normalized = NormalizeRequiredToken(fieldName, "Field name", 50).ToLowerInvariant()
        Dim allowed As Boolean = False
        Dim fieldIndex As Integer
        For fieldIndex = 0 To PatchableFields.Length - 1
            If String.Equals(PatchableFields(fieldIndex), normalized, StringComparison.Ordinal) Then
                allowed = True
                Exit For
            End If
        Next

        If Not allowed Then
            Throw New AccessManagerValidationException("Field cannot be updated inline.")
        End If

        Select Case normalized
            Case "code_value_desc"
                ValidateDescription(fieldValue)
            Case "code_value_long_desc"
                ValidateLongDescription(fieldValue)
            Case "inactive"
                If Not String.Equals(fieldValue, "Y", StringComparison.OrdinalIgnoreCase) AndAlso
                    Not String.Equals(fieldValue, "N", StringComparison.OrdinalIgnoreCase) Then
                    Throw New AccessManagerValidationException("Inactive must be Y or N.")
                End If
            Case "order_by"
                Dim position As Integer
                If Not Integer.TryParse(fieldValue, position) OrElse position < 1 Then
                    Throw New AccessManagerValidationException("Position must be a positive number.")
                End If
        End Select
    End Sub

    Public Shared Function ClassAllowsDelete(codeClass As String) As Boolean
        Return Not String.Equals(codeClass, CodeAdminConstants.ProtectedClassGroupType, StringComparison.OrdinalIgnoreCase) AndAlso
            Not String.Equals(codeClass, CodeAdminConstants.ProtectedClassApplicationDb, StringComparison.OrdinalIgnoreCase)
    End Function

    Public Shared Function ValueIsProtected(codeValue As String) As Boolean
        Return String.Equals(codeValue, CodeAdminConstants.ProtectedValueDevDomain, StringComparison.OrdinalIgnoreCase)
    End Function

    Public Shared Function ValidateSqlIdentifier(identifier As String) As String
        If String.IsNullOrWhiteSpace(identifier) OrElse
            Not Regex.IsMatch(identifier, "^[A-Za-z][A-Za-z0-9_]*$", RegexOptions.CultureInvariant) Then
            Throw New AccessManagerServiceException("Invalid column reference.")
        End If
        Return identifier
    End Function

    Private Shared Function NormalizeRequiredToken(value As String, label As String, maxLength As Integer) As String
        If String.IsNullOrWhiteSpace(value) Then
            Throw New AccessManagerValidationException(label & " is required.")
        End If

        Dim trimmed = value.Trim()
        If trimmed.Length > maxLength Then
            Throw New AccessManagerValidationException(label & " is too long.")
        End If
        Return trimmed
    End Function
End Class
