Imports System

''' <summary>
''' Legacy global-admin membership cookie encoder (Perl Crypt::CBC Blowfish).
''' Delete this class when PilotLegacyCredentialEncoder moves to a modern scheme.
''' </summary>
Public NotInheritable Class PerlCryptCbcBlowfishCredentialEncoder
    Implements ILegacyMembershipCredentialEncoder

    Private ReadOnly _passphrase As String

    Public Sub New()
        _passphrase = LegacyMembershipCredentialCodec.ResolveEncryptionKey()
    End Sub

    Public ReadOnly Property SchemeId As String Implements ILegacyMembershipCredentialEncoder.SchemeId
        Get
            Return LegacyMembershipCredentialEncoderFactory.PerlCryptCbcBlowfishScheme
        End Get
    End Property

    Public Function Encode(plaintext As String) As String Implements ILegacyMembershipCredentialEncoder.Encode
        Return PerlCryptCbcBlowfish.EncryptHex(_passphrase, plaintext)
    End Function

    Public Function TryDecode(encoded As String, ByRef plaintext As String) As Boolean Implements ILegacyMembershipCredentialEncoder.TryDecode
        plaintext = Nothing
        Try
            plaintext = PerlCryptCbcBlowfish.DecryptHex(_passphrase, encoded)
            Return True
        Catch
            Return False
        End Try
    End Function
End Class
