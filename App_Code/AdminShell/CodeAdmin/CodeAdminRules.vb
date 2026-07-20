Imports System
Imports System.Text.RegularExpressions

Public NotInheritable Class CodeAdminConstants
    Private Sub New()
    End Sub

    Public Const ManagedRoute As String = "managed/code-admin/index.aspx"
    Public Const CanonicalRoute As String = "cgi-bin/codeadminO.pl"
    Public Const StatusActive As String = "N"
    Public Const StatusInactive As String = "Y"
    Public Const StatusArchived As String = "A"
    Public Const InactiveYes As String = StatusInactive
    Public Const InactiveNo As String = StatusActive
    Public Const ProtectedValueDevDomain As String = "DEV_DOMAIN"
    Public Const ProtectedClassGroupType As String = "GROUP_TY_CD"
    Public Const ProtectedClassApplicationDb As String = "APPLICATION_DB"
    Public Const DefaultPageSize As Integer = 200
    Public Const MaxPageSize As Integer = 500
    Public Const MaxSearchLength As Integer = 100
    Public Const MaxCodeValueLength As Integer = 50
    Public Const MaxDescriptionLength As Integer = 1000
    Public Const MaxLongDescriptionLength As Integer = 4000
    Public Const MaxOptionalValueLength As Integer = 1000

    Public Shared Function IsLifecycleStatus(value As String) As Boolean
        Return String.Equals(value, StatusActive, StringComparison.OrdinalIgnoreCase) OrElse
            String.Equals(value, StatusInactive, StringComparison.OrdinalIgnoreCase) OrElse
            String.Equals(value, StatusArchived, StringComparison.OrdinalIgnoreCase)
    End Function
End Class
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
            Throw New AdminShellValidationException("Code class is not valid.")
        End If
    End Sub

    Public Shared Sub ValidateCodeValue(codeValue As String)
        Dim trimmed = NormalizeRequiredToken(codeValue, "Code value", CodeAdminConstants.MaxCodeValueLength)
        If Not CodeValuePattern.IsMatch(trimmed) Then
            Throw New AdminShellValidationException("Code value must contain only letters, numbers, hyphens, or underscores.")
        End If
    End Sub

    Public Shared Sub ValidateExistingCodeValueReference(codeValue As String)
        NormalizeRequiredToken(codeValue, "Code value", CodeAdminConstants.MaxCodeValueLength)
    End Sub

    Public Shared Sub ValidateDescription(description As String)
        NormalizeRequiredToken(description, "Description", CodeAdminConstants.MaxDescriptionLength)
    End Sub

    Public Shared Sub ValidateLongDescription(description As String)
        If description Is Nothing Then
            Return
        End If
        If description.Length > CodeAdminConstants.MaxLongDescriptionLength Then
            Throw New AdminShellValidationException("Long description is too long.")
        End If
    End Sub

    Public Shared Sub ValidateSearch(search As String)
        If String.IsNullOrWhiteSpace(search) Then
            Return
        End If
        If search.Trim().Length > CodeAdminConstants.MaxSearchLength Then
            Throw New AdminShellValidationException("Search text is too long.")
        End If
    End Sub

    Public Shared Sub ValidatePageSize(pageSize As Integer)
        If pageSize < 1 OrElse pageSize > CodeAdminConstants.MaxPageSize Then
            Throw New AdminShellValidationException("Rows per page is out of range.")
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
            Throw New AdminShellValidationException("Field cannot be updated inline.")
        End If

        Select Case normalized
            Case "code_value_desc"
                ValidateDescription(fieldValue)
            Case "code_value_long_desc"
                ValidateLongDescription(fieldValue)
            Case "inactive"
                If Not String.Equals(fieldValue, "Y", StringComparison.OrdinalIgnoreCase) AndAlso Not String.Equals(fieldValue, "N", StringComparison.OrdinalIgnoreCase) Then
                    Throw New AdminShellValidationException("Inactive must be Y or N.")
                End If
            Case "order_by"
                Dim position As Integer
                If Not Integer.TryParse(fieldValue, position) OrElse position < 1 Then
                    Throw New AdminShellValidationException("Position must be a positive number.")
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
        If String.IsNullOrWhiteSpace(identifier) OrElse Not Regex.IsMatch(identifier, "^[A-Za-z][A-Za-z0-9_]*$", RegexOptions.CultureInvariant) Then
            Throw New AdminShellServiceException("Invalid column reference.")
        End If
        Return identifier
    End Function

    Private Shared Function NormalizeRequiredToken(value As String, label As String, maxLength As Integer) As String
        If String.IsNullOrWhiteSpace(value) Then
            Throw New AdminShellValidationException(label & " is required.")
        End If

        Dim trimmed = value.Trim()
        If trimmed.Length > maxLength Then
            Throw New AdminShellValidationException(label & " is too long.")
        End If
        Return trimmed
    End Function
End Class
