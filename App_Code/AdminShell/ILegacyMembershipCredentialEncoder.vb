''' <summary>
''' Encodes/decodes legacy admin membership cookie values (username, password).
''' Replace the registered implementation when global auth moves off legacy formats.
''' </summary>
Public Interface ILegacyMembershipCredentialEncoder
    ReadOnly Property SchemeId As String

    Function Encode(plaintext As String) As String

    Function TryDecode(encoded As String, ByRef plaintext As String) As Boolean
End Interface
