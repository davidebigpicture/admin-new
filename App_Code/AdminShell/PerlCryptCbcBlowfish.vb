Imports System
Imports System.Collections.Generic
Imports System.Security.Cryptography
Imports System.Text

''' <summary>
''' Perl Crypt::CBC compatibility layer (Blowfish, padding=space, header=salt, encrypt_hex).
''' Kept isolated so it can be deleted when global auth stops using this format.
''' </summary>
Friend NotInheritable Class PerlCryptCbcBlowfish
    Private Const SaltHeader As String = "Salted__"
    Private Const BlockSize As Integer = 8
    Private Const KeySize As Integer = 56

    Private Sub New()
    End Sub

    Public Shared Function EncryptHex(passphrase As String, plaintext As String) As String
        Dim payload As Byte() = Encrypt(passphrase, plaintext)
        Return BytesToHex(payload)
    End Function

    Public Shared Function DecryptHex(passphrase As String, ciphertextHex As String) As String
        Return Encoding.ASCII.GetString(Decrypt(passphrase, HexToBytes(ciphertextHex)))
    End Function

    Friend Shared Function Encrypt(passphrase As String, plaintext As String, Optional fixedSalt As Byte() = Nothing) As Byte()
        If String.IsNullOrEmpty(passphrase) Then
            Throw New ArgumentException("passphrase is required", "passphrase")
        End If
        If plaintext Is Nothing Then
            plaintext = String.Empty
        End If

        Dim salt As Byte() = If(fixedSalt, GenerateSalt())
        If salt.Length <> 8 Then
            Throw New ArgumentException("salt must be exactly 8 bytes", "fixedSalt")
        End If

        Dim keyMaterial = DeriveSaltedKeyAndIv(passphrase, salt)
        Dim cipher As New BlowfishBlockCipher(keyMaterial.Key)
        Dim padded = ApplySpacePadding(Encoding.ASCII.GetBytes(plaintext))
        Dim encryptedBody = EncryptCbc(cipher, keyMaterial.Iv, padded)

        Dim header As Byte() = Encoding.ASCII.GetBytes(SaltHeader)
        Dim result As Byte() = New Byte(header.Length + salt.Length + encryptedBody.Length - 1) {}
        Buffer.BlockCopy(header, 0, result, 0, header.Length)
        Buffer.BlockCopy(salt, 0, result, header.Length, salt.Length)
        Buffer.BlockCopy(encryptedBody, 0, result, header.Length + salt.Length, encryptedBody.Length)
        Return result
    End Function

    Friend Shared Function Decrypt(passphrase As String, payload As Byte()) As Byte()
        If String.IsNullOrEmpty(passphrase) Then
            Throw New ArgumentException("passphrase is required", "passphrase")
        End If
        If payload Is Nothing OrElse payload.Length <= 16 Then
            Throw New ArgumentException("ciphertext is too short", "payload")
        End If

        Dim header As String = Encoding.ASCII.GetString(payload, 0, 8)
        If Not String.Equals(header, SaltHeader, StringComparison.Ordinal) Then
            Throw New ArgumentException("ciphertext does not begin with a valid salt header", "payload")
        End If

        Dim salt As Byte() = New Byte(7) {}
        Buffer.BlockCopy(payload, 8, salt, 0, 8)
        Dim ciphertext As Byte() = New Byte(payload.Length - 17) {}
        Buffer.BlockCopy(payload, 16, ciphertext, 0, ciphertext.Length)

        Dim keyMaterial = DeriveSaltedKeyAndIv(passphrase, salt)
        Dim cipher As New BlowfishBlockCipher(keyMaterial.Key)
        Dim padded = DecryptCbc(cipher, keyMaterial.Iv, ciphertext)
        Return RemoveSpacePadding(padded)
    End Function

    Friend Shared Function DeriveSaltedKeyAndIv(passphrase As String, salt As Byte()) As KeyIvMaterial
        If salt Is Nothing OrElse salt.Length <> 8 Then
            Throw New ArgumentException("salt must be exactly 8 bytes", "salt")
        End If

        Dim passBytes As Byte() = Encoding.ASCII.GetBytes(passphrase)
        Dim desiredLength As Integer = KeySize + BlockSize
        Dim data As New List(Of Byte)(desiredLength)
        Dim previous As Byte() = New Byte() {}

        Using md5 As MD5 = MD5.Create()
            While data.Count < desiredLength
                Dim input As Byte() = New Byte(previous.Length + passBytes.Length + salt.Length - 1) {}
                If previous.Length > 0 Then
                    Buffer.BlockCopy(previous, 0, input, 0, previous.Length)
                End If
                Buffer.BlockCopy(passBytes, 0, input, previous.Length, passBytes.Length)
                Buffer.BlockCopy(salt, 0, input, previous.Length + passBytes.Length, salt.Length)
                previous = md5.ComputeHash(input)
                data.AddRange(previous)
            End While
        End Using

        Dim key As Byte() = New Byte(KeySize - 1) {}
        Dim iv As Byte() = New Byte(BlockSize - 1) {}
        Buffer.BlockCopy(data.ToArray(), 0, key, 0, KeySize)
        Buffer.BlockCopy(data.ToArray(), KeySize, iv, 0, BlockSize)
        Return New KeyIvMaterial(key, iv)
    End Function

    Friend Shared Function ApplySpacePadding(data As Byte()) As Byte()
        Dim remainder As Integer = data.Length Mod BlockSize
        Dim padCount As Integer = If(remainder = 0, BlockSize, BlockSize - remainder)
        Dim padded As Byte() = New Byte(data.Length + padCount - 1) {}
        Buffer.BlockCopy(data, 0, padded, 0, data.Length)
        For i As Integer = data.Length To padded.Length - 1
            padded(i) = 32
        Next
        Return padded
    End Function

    Friend Shared Function RemoveSpacePadding(data As Byte()) As Byte()
        Dim endIndex As Integer = data.Length
        While endIndex > 0 AndAlso data(endIndex - 1) = 32
            endIndex -= 1
        End While

        Dim trimmed As Byte() = New Byte(endIndex - 1) {}
        If endIndex > 0 Then
            Buffer.BlockCopy(data, 0, trimmed, 0, endIndex)
        End If
        Return trimmed
    End Function

    Private Shared Function EncryptCbc(cipher As BlowfishBlockCipher, iv As Byte(), data As Byte()) As Byte()
        Dim output As Byte() = New Byte(data.Length - 1) {}
        Dim currentIv As Byte() = CType(iv.Clone(), Byte())

        For offset As Integer = 0 To data.Length - 1 Step BlockSize
            Dim block As Byte() = New Byte(BlockSize - 1) {}
            Buffer.BlockCopy(data, offset, block, 0, BlockSize)
            XorBlock(block, currentIv)
            cipher.EncryptBlock(block, 0)
            Buffer.BlockCopy(block, 0, output, offset, BlockSize)
            Buffer.BlockCopy(block, 0, currentIv, 0, BlockSize)
        Next

        Return output
    End Function

    Private Shared Function DecryptCbc(cipher As BlowfishBlockCipher, iv As Byte(), data As Byte()) As Byte()
        Dim output As Byte() = New Byte(data.Length - 1) {}
        Dim currentIv As Byte() = CType(iv.Clone(), Byte())

        For offset As Integer = 0 To data.Length - 1 Step BlockSize
            Dim block As Byte() = New Byte(BlockSize - 1) {}
            Buffer.BlockCopy(data, offset, block, 0, BlockSize)
            Dim nextIv As Byte() = CType(block.Clone(), Byte())
            cipher.DecryptBlock(block, 0)
            XorBlock(block, currentIv)
            Buffer.BlockCopy(block, 0, output, offset, BlockSize)
            currentIv = nextIv
        Next

        Return output
    End Function

    Private Shared Sub XorBlock(block As Byte(), iv As Byte())
        For i As Integer = 0 To BlockSize - 1
            block(i) = CByte(block(i) Xor iv(i))
        Next
    End Sub

    Private Shared Function GenerateSalt() As Byte()
        Dim salt As Byte() = New Byte(7) {}
        Using rng As RandomNumberGenerator = RandomNumberGenerator.Create()
            rng.GetBytes(salt)
        End Using
        Return salt
    End Function

    Private Shared Function BytesToHex(data As Byte()) As String
        Dim builder As New StringBuilder(data.Length * 2)
        For Each value As Byte In data
            builder.Append(value.ToString("X2"))
        Next
        Return builder.ToString()
    End Function

    Private Shared Function HexToBytes(hex As String) As Byte()
        If String.IsNullOrWhiteSpace(hex) Then
            Return New Byte() {}
        End If

        Dim normalized = hex.Trim()
        If normalized.Length Mod 2 <> 0 Then
            Throw New ArgumentException("hex length must be even", "hex")
        End If

        Dim data As Byte() = New Byte((normalized.Length \ 2) - 1) {}
        For i As Integer = 0 To data.Length - 1
            data(i) = Convert.ToByte(normalized.Substring(i * 2, 2), 16)
        Next
        Return data
    End Function

    Friend NotInheritable Class KeyIvMaterial
        Public ReadOnly Key As Byte()
        Public ReadOnly Iv As Byte()

        Public Sub New(key As Byte(), iv As Byte())
            Me.Key = key
            Me.Iv = iv
        End Sub
    End Class
End Class
