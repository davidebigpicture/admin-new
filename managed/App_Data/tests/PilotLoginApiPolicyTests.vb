Imports System

Module PilotLoginApiPolicyTests
    Private _failures As Integer

    Private Const PilotRoot As String = "/dev/adminshell"
    Private Const GlobalRoot As String = "/admin/admin"
    Private Const DefaultRoute As String = "/dev/adminshell/views.asp"
    Private Const RoutesConfig As String =
        "views.asp=views.asp|Views;" &
        "loginlog.asp=loginlog.asp|Login Log;" &
        "sql_logs.asp=sql_logs.asp|SQL Logs;" &
        "sms_logs.asp=sms_logs.asp|SMS Logs"

    Function Main() As Integer
        TestReturnUrlResolution()
        TestCsrfValidation()

        If _failures = 0 Then
            Console.WriteLine("All PilotLoginApiPolicy tests passed.")
            Return 0
        End If

        Console.Error.WriteLine(_failures.ToString() & " PilotLoginApiPolicy test(s) failed.")
        Return 1
    End Function

    Private Sub TestReturnUrlResolution()
        AssertEqual(
            DefaultRoute & "?group_cd=STAFF",
            PilotLoginApiPolicy.ResolveReturnUrl(DefaultRoute & "?group_cd=STAFF", DefaultRoute, RoutesConfig, PilotRoot, GlobalRoot),
            "safe pilot URLs retain their query string")
        AssertEqual(
            "/dev/adminshell/sql_logs.asp",
            PilotLoginApiPolicy.ResolveReturnUrl("/dev/adminshell/sql_logs.asp", DefaultRoute, RoutesConfig, PilotRoot, GlobalRoot),
            "other configured pilot routes remain valid return URLs")
        AssertEqual(
            DefaultRoute,
            PilotLoginApiPolicy.ResolveReturnUrl("https://evil.example/admin", DefaultRoute, RoutesConfig, PilotRoot, GlobalRoot),
            "external URLs fall back to the configured default pilot route")
        AssertEqual(
            DefaultRoute,
            PilotLoginApiPolicy.ResolveReturnUrl("", DefaultRoute, RoutesConfig, PilotRoot, GlobalRoot),
            "blank URLs fall back to the configured default pilot route")
        AssertEqual(
            DefaultRoute,
            PilotLoginApiPolicy.ResolveReturnUrl("/dev/adminshell/unknown.asp", DefaultRoute, RoutesConfig, PilotRoot, GlobalRoot),
            "unknown pilot routes fall back to the configured default")
    End Sub

    Private Sub TestCsrfValidation()
        AssertTrue(PilotLoginApiPolicy.IsValidCsrfToken("token-value", "token-value"), "matching tokens pass")
        AssertFalse(PilotLoginApiPolicy.IsValidCsrfToken("token-value", "other-value"), "different tokens fail")
        AssertFalse(PilotLoginApiPolicy.IsValidCsrfToken("", "token-value"), "missing expected tokens fail")
        AssertFalse(PilotLoginApiPolicy.IsValidCsrfToken("token-value", Nothing), "missing supplied tokens fail")
    End Sub

    Private Sub AssertEqual(expected As String, actual As String, message As String)
        AssertTrue(String.Equals(expected, actual, StringComparison.Ordinal), message)
    End Sub

    Private Sub AssertTrue(value As Boolean, message As String)
        If Not value Then
            _failures += 1
            Console.Error.WriteLine("FAIL: " & message)
        End If
    End Sub

    Private Sub AssertFalse(value As Boolean, message As String)
        AssertTrue(Not value, message)
    End Sub
End Module
