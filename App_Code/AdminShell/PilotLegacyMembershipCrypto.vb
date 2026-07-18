Imports System

''' <summary>
''' Thin wrapper kept for call-site stability. Delegates to LegacyMembershipCredentialCodec.
''' </summary>
Public NotInheritable Class PilotLegacyMembershipCrypto
    Private Sub New()
    End Sub

    Public Shared Function Encrypt(plaintext As String) As String
        Return LegacyMembershipCredentialCodec.Encode(plaintext)
    End Function

    Public Shared Function TryDecrypt(encoded As String, ByRef plaintext As String) As Boolean
        Return LegacyMembershipCredentialCodec.TryDecode(encoded, plaintext)
    End Function
End Class
