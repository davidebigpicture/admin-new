Imports System
Imports System.Text.RegularExpressions

''' <summary>
''' Central composition of relocatable pilot and global-admin virtual roots.
''' Moving the pilot from /dev/adminshell to /admin/... should only require
''' changing PilotRootPath / GlobalAdminRootPath in managed/web.config.
''' </summary>
Public NotInheritable Class PilotPathConfig
    Private Shared ReadOnly InvalidRelativePattern As New Regex(
        "(\.\.|\\|\/\/|\?|#)",
        RegexOptions.Compiled Or RegexOptions.CultureInvariant)

    Private Sub New()
    End Sub

    Public Shared Function NormalizeRoot(path As String) As String
        If String.IsNullOrWhiteSpace(path) Then
            Throw New ArgumentException("Root path is required.", "path")
        End If

        Dim trimmed = path.Trim().Replace("\"c, "/"c)
        If trimmed.Length = 0 OrElse Not trimmed.StartsWith("/", StringComparison.Ordinal) Then
            Throw New ArgumentException("Root path must be an absolute virtual path.", "path")
        End If
        If trimmed.StartsWith("//", StringComparison.Ordinal) OrElse
            trimmed.Contains("?") OrElse
            trimmed.Contains("#") OrElse
            trimmed.Contains("..") Then
            Throw New ArgumentException("Root path is malformed.", "path")
        End If

        While trimmed.Contains("//")
            trimmed = trimmed.Replace("//", "/")
        End While

        If trimmed.Length > 1 AndAlso trimmed.EndsWith("/", StringComparison.Ordinal) Then
            trimmed = trimmed.TrimEnd("/"c)
        End If

        Return trimmed
    End Function

    Public Shared Function IsValidRelativePath(path As String) As Boolean
        If String.IsNullOrWhiteSpace(path) Then
            Return False
        End If

        Dim trimmed = path.Trim().Replace("\"c, "/"c)
        If trimmed.Length = 0 OrElse
            trimmed.StartsWith("/", StringComparison.Ordinal) OrElse
            trimmed.StartsWith("./", StringComparison.Ordinal) OrElse
            trimmed.EndsWith("/", StringComparison.Ordinal) OrElse
            InvalidRelativePattern.IsMatch(trimmed) Then
            Return False
        End If

        Return True
    End Function

    Public Shared Function Combine(rootPath As String, relativePath As String) As String
        Dim root = NormalizeRoot(rootPath)
        If Not IsValidRelativePath(relativePath) Then
            Throw New ArgumentException("Relative path is malformed.", "relativePath")
        End If

        Dim relative = relativePath.Trim().Replace("\"c, "/"c)
        While relative.Contains("//")
            relative = relative.Replace("//", "/")
        End While

        Return root & "/" & relative
    End Function

    Public Shared Function TryCombine(rootPath As String, relativePath As String, ByRef absolutePath As String) As Boolean
        absolutePath = Nothing
        Try
            absolutePath = Combine(rootPath, relativePath)
            Return True
        Catch ex As ArgumentException
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Cookie path is the configured pilot root so a future move updates
    ''' cookie scope without source changes.
    ''' </summary>
    Public Shared Function CookiePathFromRoot(pilotRootPath As String) As String
        Return NormalizeRoot(pilotRootPath)
    End Function
End Class
