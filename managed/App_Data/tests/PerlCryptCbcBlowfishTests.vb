Imports System
Imports System.Text

Module PerlCryptCbcBlowfishTests
    Private _failures As Integer
    Private Const TestKey As String = "aciencryptionkey"

    Function Main() As Integer
        TestSpacePadding()
        TestSaltedKeyDerivation()
        TestRoundTripWithFixedSalt()
        TestHexRoundTrip()

        If _failures = 0 Then
            Console.WriteLine("All PerlCryptCbcBlowfish tests passed.")
            Return 0
        End If

        Console.Error.WriteLine(_failures.ToString() & " PerlCryptCbcBlowfish test(s) failed.")
        Return 1
    End Function

    Private Sub TestSpacePadding()
        Dim padded = PerlCryptCbcBlowfish.ApplySpacePadding(Encoding.ASCII.GetBytes("abc"))
        AssertEqual(8, padded.Length, "short values are padded to one block")
        AssertEqual(CByte(32), padded(7), "space padding uses ASCII space")

        Dim exact = PerlCryptCbcBlowfish.ApplySpacePadding(Encoding.ASCII.GetBytes("12345678"))
        AssertEqual(16, exact.Length, "aligned values still receive a full padding block")
    End Sub

    Private Sub TestSaltedKeyDerivation()
        Dim salt As Byte() = New Byte() {0, 0, 0, 0, 0, 0, 0, 0}
        Dim material = PerlCryptCbcBlowfish.DeriveSaltedKeyAndIv(TestKey, salt)
        AssertEqual(56, material.Key.Length, "derived blowfish key length is 56 bytes")
        AssertEqual(8, material.Iv.Length, "derived iv length is 8 bytes")
    End Sub

    Private Sub TestRoundTripWithFixedSalt()
        Dim salt As Byte() = New Byte() {1, 2, 3, 4, 5, 6, 7, 8}
        Dim encrypted = PerlCryptCbcBlowfish.Encrypt(TestKey, "dhoffman", salt)
        Dim decrypted = PerlCryptCbcBlowfish.Decrypt(TestKey, encrypted)
        AssertEqual("dhoffman", Encoding.ASCII.GetString(decrypted), "encrypt/decrypt round trip succeeds")
    End Sub

    Private Sub TestHexRoundTrip()
        Dim salt As Byte() = New Byte() {8, 7, 6, 5, 4, 3, 2, 1}
        Dim encrypted = PerlCryptCbcBlowfish.Encrypt(TestKey, "pilot", salt)
        Dim hex = ToHex(encrypted)
        Dim roundTrip = PerlCryptCbcBlowfish.Decrypt(TestKey, FromHex(hex))
        AssertEqual("pilot", Encoding.ASCII.GetString(roundTrip), "hex-wrapped payloads round trip")
    End Sub

    Private Function ToHex(data As Byte()) As String
        Dim builder As New StringBuilder(data.Length * 2)
        For Each value As Byte In data
            builder.Append(value.ToString("X2"))
        Next
        Return builder.ToString()
    End Function

    Private Function FromHex(hex As String) As Byte()
        Dim data As Byte() = New Byte((hex.Length \ 2) - 1) {}
        For i As Integer = 0 To data.Length - 1
            data(i) = Convert.ToByte(hex.Substring(i * 2, 2), 16)
        Next
        Return data
    End Function

    Private Sub AssertEqual(expected As Integer, actual As Integer, message As String)
        If expected <> actual Then
            _failures += 1
            Console.Error.WriteLine("FAIL: " & message & " (expected " & expected.ToString() & ", got " & actual.ToString() & ")")
        End If
    End Sub

    Private Sub AssertEqual(expected As Byte, actual As Byte, message As String)
        If expected <> actual Then
            _failures += 1
            Console.Error.WriteLine("FAIL: " & message)
        End If
    End Sub

    Private Sub AssertEqual(expected As String, actual As String, message As String)
        If Not String.Equals(expected, actual, StringComparison.Ordinal) Then
            _failures += 1
            Console.Error.WriteLine("FAIL: " & message & " (expected '" & expected & "', got '" & actual & "')")
        End If
    End Sub
End Module
