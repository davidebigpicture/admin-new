Imports System

Module PilotLegacySessionTests
    Private _failures As Integer

    Function Main() As Integer
        TestNormalizeSessionId()
        TestLegacyLoginProof()
        TestCreateSessionId()
        TestRedisServiceFullKey()

        If _failures = 0 Then
            Console.WriteLine("All PilotLegacySession tests passed.")
            Return 0
        End If

        Console.Error.WriteLine(_failures.ToString() & " PilotLegacySession test(s) failed.")
        Return 1
    End Function

    Private Sub TestNormalizeSessionId()
        AssertEqual(
            "abc123def456",
            PilotLegacySession.NormalizeSessionId("abc123-def456"),
            "session ids drop dashes")
        AssertEqual(
            "abc123def456",
            PilotLegacySession.NormalizeSessionId("abc123%2Ddef456"),
            "session ids drop encoded dashes")
        AssertEqual(String.Empty, PilotLegacySession.NormalizeSessionId("   "), "blank ids normalize to empty")
    End Sub

    Private Sub TestLegacyLoginProof()
        Dim loginName As String = Nothing
        AssertFalse(
            PilotLegacySession.TryResolveLoginNameFromLegacyProof("false", "abc", loginName),
            "rejects non-authenticated flag")
        AssertFalse(
            PilotLegacySession.TryResolveLoginNameFromLegacyProof("true", "", loginName),
            "rejects missing username cookie")
        AssertFalse(
            PilotLegacySession.TryResolveLoginNameFromLegacyProof("true", "not-valid-hex", loginName),
            "rejects undecryptable username cookie")
    End Sub

    Private Sub TestCreateSessionId()
        Dim sessionId = PilotLegacySession.CreateSessionId()
        AssertEqual(32, sessionId.Length, "session ids are 32 characters")
        AssertTrue(sessionId.IndexOf("-"c) < 0, "session ids omit dashes")
    End Sub

    Private Sub TestRedisServiceFullKey()
        AssertEqual(
            "abc123:loginname",
            RedisService.FullKey("abc123", "LoginName"),
            "redis keys match cachemanager casing")
        AssertEqual(
            "abc123:cachelist",
            RedisService.CacheListKey("abc123"),
            "cache list keys follow cachemanager convention")
    End Sub

    Private Sub AssertEqual(expected As String, actual As String, message As String)
        If Not String.Equals(expected, actual, StringComparison.Ordinal) Then
            _failures += 1
            Console.Error.WriteLine("FAIL: " & message & " (expected '" & expected & "', got '" & actual & "')")
        End If
    End Sub

    Private Sub AssertEqual(expected As Integer, actual As Integer, message As String)
        If expected <> actual Then
            _failures += 1
            Console.Error.WriteLine("FAIL: " & message & " (expected " & expected.ToString() & ", got " & actual.ToString() & ")")
        End If
    End Sub

    Private Sub AssertTrue(condition As Boolean, message As String)
        If Not condition Then
            _failures += 1
            Console.Error.WriteLine("FAIL: " & message)
        End If
    End Sub

    Private Sub AssertFalse(condition As Boolean, message As String)
        AssertTrue(Not condition, message)
    End Sub
End Module
