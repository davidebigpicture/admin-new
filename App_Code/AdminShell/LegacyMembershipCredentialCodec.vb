Imports System
Imports System.Configuration

''' <summary>
''' Application-facing codec for legacy membership cookies. Call sites depend on this
''' type, not on a specific algorithm.
''' </summary>
Public NotInheritable Class LegacyMembershipCredentialCodec
    Private Sub New()
    End Sub

    Public Shared Function Encode(plaintext As String) As String
        If String.IsNullOrWhiteSpace(plaintext) Then
            Return String.Empty
        End If

        Return LegacyMembershipCredentialEncoderFactory.Create().Encode(plaintext)
    End Function

    Public Shared Function TryDecode(encoded As String, ByRef plaintext As String) As Boolean
        plaintext = Nothing
        If String.IsNullOrWhiteSpace(encoded) Then
            Return False
        End If

        Return LegacyMembershipCredentialEncoderFactory.Create().TryDecode(encoded, plaintext)
    End Function

    Public Shared Function ResolveEncryptionKey() As String
        Dim configured = If(ConfigurationManager.AppSettings("PilotMembershipEncryptionKey"), String.Empty).Trim()
        If Not String.IsNullOrEmpty(configured) Then
            Return configured
        End If

        Throw New ConfigurationErrorsException(
            "PilotMembershipEncryptionKey is not configured for legacy membership cookie encoding.")
    End Function
End Class
