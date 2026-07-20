Imports System
Imports System.Collections.Generic
Imports System.Text.RegularExpressions

Public NotInheritable Class AccessManagerValidation
    Private Shared ReadOnly InvalidPathPattern As New Regex(
        "(\.\.|\\|\/\/|\?|#)",
        RegexOptions.Compiled Or RegexOptions.CultureInvariant)

    Private Sub New()
    End Sub

    Public Shared Sub ValidateSectionName(sectionName As String)
        Dim trimmed = NormalizeRequiredName(sectionName, "Section name", 50)
        If trimmed.IndexOf("|"c) >= 0 Then
            Throw New AdminShellValidationException("Section name cannot contain '|'.")
        End If
    End Sub

    Public Shared Sub ValidateScriptName(scriptName As String)
        Dim trimmed = NormalizeRequiredName(scriptName, "Script path", 512)
        If trimmed.StartsWith("/", StringComparison.Ordinal) = False Then
            Throw New AdminShellValidationException("Script path must start with '/'.")
        End If
        If InvalidPathPattern.IsMatch(trimmed) Then
            Throw New AdminShellValidationException("Script path is malformed.")
        End If
    End Sub

    Public Shared Sub ValidateScriptTitle(title As String)
        NormalizeRequiredName(title, "Script title", 255)
    End Sub

    Public Shared Sub ValidateScriptType(scriptTy As String, allowedTypes As IList(Of AccessManagerScriptType))
        Dim trimmed = NormalizeRequiredToken(scriptTy, "Script type")
        If allowedTypes Is Nothing OrElse allowedTypes.Count = 0 Then
            Throw New AdminShellValidationException("No script types are configured.")
        End If

        Dim found As Boolean = False
        Dim typeIndex As Integer
        For typeIndex = 0 To allowedTypes.Count - 1
            Dim candidate = allowedTypes(typeIndex)
            If String.Equals(candidate.CodeValue, trimmed, StringComparison.OrdinalIgnoreCase) Then
                found = True
                Exit For
            End If
        Next

        If Not found Then
            Throw New AdminShellValidationException("Script type is not valid.")
        End If
    End Sub

    Public Shared Sub ValidateUniqueSectionName(
        sectionName As String,
        excludeSectionId As Integer?,
        exists As Func(Of String, Integer?, Boolean))

        ValidateSectionName(sectionName)
        If exists(sectionName.Trim(), excludeSectionId) Then
            Throw New AdminShellValidationException("Section name must be unique.")
        End If
    End Sub

    Public Shared Sub ValidateUniqueScriptName(
        scriptName As String,
        excludeScriptId As Integer?,
        exists As Func(Of String, Integer?, Boolean))

        ValidateScriptName(scriptName)
        If exists(scriptName.Trim(), excludeScriptId) Then
            Throw New AdminShellValidationException("Script path must be unique.")
        End If
    End Sub

    Public Shared Sub ValidateReorderPosition(newPosition As Integer, itemCount As Integer)
        If newPosition < 1 OrElse newPosition > itemCount Then
            Throw New AdminShellValidationException("Position is out of range.")
        End If
    End Sub

    Public Shared Sub ValidateGrantTarget(secureTy As String, secureId As Integer)
        Dim normalized = NormalizeRequiredToken(secureTy, "Secure type")
        If Not String.Equals(normalized, AccessManagerConstants.SecureTypeSection, StringComparison.OrdinalIgnoreCase) AndAlso
            Not String.Equals(normalized, AccessManagerConstants.SecureTypeScript, StringComparison.OrdinalIgnoreCase) Then
            Throw New AdminShellValidationException("Secure type must be SECT or SCRI.")
        End If

        If secureId <= 0 Then
            Throw New AdminShellValidationException("Secure id is required.")
        End If
    End Sub

    Public Shared Sub ValidateGrantPrincipal(principalTy As String, principalId As Integer)
        Dim normalized = NormalizeRequiredToken(principalTy, "Principal type")
        If Not String.Equals(normalized, AccessManagerConstants.PrincipalTypeUser, StringComparison.OrdinalIgnoreCase) AndAlso
            Not String.Equals(normalized, AccessManagerConstants.PrincipalTypeGroup, StringComparison.OrdinalIgnoreCase) Then
            Throw New AdminShellValidationException("Principal type must be USER or GROU.")
        End If

        If principalId <= 0 Then
            Throw New AdminShellValidationException("Principal id is required.")
        End If
    End Sub

    Public Shared Sub ValidateHardDeleteConfirmed(confirm As Boolean)
        If Not confirm Then
            Throw New AdminShellValidationException("Hard delete requires confirm=true.")
        End If
    End Sub

    Public Shared Sub ValidateExpectedUpdateNo(expectedUpdateNo As Integer)
        If expectedUpdateNo < 0 Then
            Throw New AdminShellValidationException("Expected update number is invalid.")
        End If
    End Sub

    Private Shared Function NormalizeRequiredName(value As String, fieldLabel As String, maxLength As Integer) As String
        If String.IsNullOrWhiteSpace(value) Then
            Throw New AdminShellValidationException(fieldLabel & " is required.")
        End If

        Dim trimmed = value.Trim()
        If trimmed.Length > maxLength Then
            Throw New AdminShellValidationException(fieldLabel & " cannot exceed " & maxLength.ToString() & " characters.")
        End If

        Return trimmed
    End Function

    Private Shared Function NormalizeRequiredToken(value As String, fieldLabel As String) As String
        If String.IsNullOrWhiteSpace(value) Then
            Throw New AdminShellValidationException(fieldLabel & " is required.")
        End If
        Return value.Trim().ToUpperInvariant()
    End Function
End Class
