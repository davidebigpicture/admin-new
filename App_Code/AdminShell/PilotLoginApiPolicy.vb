Imports System

Public NotInheritable Class PilotLoginApiPolicy
    Private Sub New()
    End Sub

    Public Shared Function ResolveReturnUrl(
        requestedReturnUrl As String,
        defaultPath As String,
        routesConfig As String,
        pilotRootPath As String,
        globalAdminRootPath As String) As String

        If PilotPolicy.IsSafeReturnUrl(requestedReturnUrl, routesConfig, pilotRootPath, globalAdminRootPath) Then
            Return requestedReturnUrl
        End If

        Return defaultPath
    End Function

    Public Shared Function IsValidCsrfToken(expectedToken As String, suppliedToken As String) As Boolean
        If String.IsNullOrWhiteSpace(expectedToken) OrElse String.IsNullOrWhiteSpace(suppliedToken) Then
            Return False
        End If

        Return PilotPolicy.ConstantTimeEquals(expectedToken, suppliedToken)
    End Function
End Class
