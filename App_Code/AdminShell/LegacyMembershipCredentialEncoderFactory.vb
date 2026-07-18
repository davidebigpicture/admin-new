Imports System
Imports System.Configuration

''' <summary>
''' Selects the legacy membership credential encoder from configuration.
''' Today: perl-crypt-cbc-blowfish (global compatibility).
''' Tomorrow: swap PilotLegacyCredentialEncoder to a modern scheme and delete the Blowfish adapter.
''' </summary>
Public NotInheritable Class LegacyMembershipCredentialEncoderFactory
    Public Const PerlCryptCbcBlowfishScheme As String = "perl-crypt-cbc-blowfish"
    Public Const DisabledScheme As String = "disabled"

    Private Shared _cachedEncoder As ILegacyMembershipCredentialEncoder
    Private Shared ReadOnly _syncRoot As New Object()

    Private Sub New()
    End Sub

    Public Shared Function Create() As ILegacyMembershipCredentialEncoder
        If _cachedEncoder IsNot Nothing Then
            Return _cachedEncoder
        End If

        SyncLock _syncRoot
            If _cachedEncoder Is Nothing Then
                _cachedEncoder = CreateInstance(ResolveSchemeId())
            End If
            Return _cachedEncoder
        End SyncLock
    End Function

    Friend Shared Sub ResetForTests()
        SyncLock _syncRoot
            _cachedEncoder = Nothing
        End SyncLock
    End Sub

    Private Shared Function ResolveSchemeId() As String
        Dim configured = If(ConfigurationManager.AppSettings("PilotLegacyCredentialEncoder"), String.Empty).Trim()
        If String.IsNullOrEmpty(configured) Then
            Return PerlCryptCbcBlowfishScheme
        End If
        Return configured.ToLowerInvariant()
    End Function

    Private Shared Function CreateInstance(schemeId As String) As ILegacyMembershipCredentialEncoder
        Select Case schemeId
            Case PerlCryptCbcBlowfishScheme
                Return New PerlCryptCbcBlowfishCredentialEncoder()
            Case DisabledScheme
                Return New DisabledLegacyMembershipCredentialEncoder()
            Case Else
                Throw New ConfigurationErrorsException(
                    "Unsupported PilotLegacyCredentialEncoder value '" & schemeId & "'.")
        End Select
    End Function
End Class

''' <summary>
''' Placeholder for a future modern encoder. Legacy cookie bridging is skipped.
''' </summary>
Friend NotInheritable Class DisabledLegacyMembershipCredentialEncoder
    Implements ILegacyMembershipCredentialEncoder

    Public ReadOnly Property SchemeId As String Implements ILegacyMembershipCredentialEncoder.SchemeId
        Get
            Return LegacyMembershipCredentialEncoderFactory.DisabledScheme
        End Get
    End Property

    Public Function Encode(plaintext As String) As String Implements ILegacyMembershipCredentialEncoder.Encode
        Throw New InvalidOperationException(
            "Legacy membership credential encoding is disabled (PilotLegacyCredentialEncoder=disabled).")
    End Function

    Public Function TryDecode(encoded As String, ByRef plaintext As String) As Boolean Implements ILegacyMembershipCredentialEncoder.TryDecode
        plaintext = Nothing
        Return False
    End Function
End Class
