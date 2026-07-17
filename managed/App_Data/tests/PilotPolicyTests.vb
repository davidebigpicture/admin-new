Imports System

Module PilotPolicyTests
    Private _failures As Integer

    Private Const PilotRoot As String = "/dev/adminshell"
    Private Const GlobalRoot As String = "/admin/admin"
    Private Const RoutesConfig As String =
        "views.asp=views.asp|Views;" &
        "loginlog.asp=loginlog.asp|Login Log;" &
        "sql_logs.asp=sql_logs.asp|SQL Logs;" &
        "sms_logs.asp=sms_logs.asp|SMS Logs;" &
        "managed/access-manager/index.html=cgi-bin/accessadmin.pl|Access Manager"

    Function Main() As Integer
        TestAllowedHost()
        TestPilotUsers()
        TestPathConfig()
        TestPilotRoutes()
        TestReturnUrls()
        TestConstantTimeComparison()

        If _failures = 0 Then
            Console.WriteLine("All PilotPolicy tests passed.")
            Return 0
        End If

        Console.Error.WriteLine(_failures.ToString() & " PilotPolicy test(s) failed.")
        Return 1
    End Function

    Private Sub TestAllowedHost()
        AssertTrue(PilotPolicy.IsAllowedHost("DEV.SERVICES.WVBPS.WV.GOV", "dev.services.wvbps.wv.gov"), "host comparison ignores case")
        AssertFalse(PilotPolicy.IsAllowedHost("other.example.org", "dev.services.wvbps.wv.gov"), "other hosts are denied")
        AssertFalse(PilotPolicy.IsAllowedHost(Nothing, "dev.services.wvbps.wv.gov"), "missing hosts are denied")
    End Sub

    Private Sub TestPilotUsers()
        AssertTrue(PilotPolicy.IsListedUser("DHoffman", " pilot, dhoffman "), "allowlist ignores case and whitespace")
        AssertFalse(PilotPolicy.IsListedUser("other", "pilot,dhoffman"), "unlisted users are denied")
        AssertFalse(PilotPolicy.IsListedUser("", "dhoffman"), "blank users are denied")
        AssertFalse(PilotPolicy.IsListedUser("dhoffman", ""), "blank allowlists deny everyone")
    End Sub

    Private Sub TestPathConfig()
        AssertEqual("/dev/adminshell", PilotPathConfig.NormalizeRoot("/dev/adminshell/"), "roots trim trailing slash")
        AssertEqual("/dev/adminshell/views.asp", PilotPathConfig.Combine("/dev/adminshell", "views.asp"), "combine joins relative pilot paths")
        AssertEqual(
            "/admin/admin/cgi-bin/accessadmin.pl",
            PilotPathConfig.Combine("/admin/admin", "cgi-bin/accessadmin.pl"),
            "combine joins relative global paths")
        AssertFalse(PilotPathConfig.IsValidRelativePath("../escape"), "parent segments are rejected")
        AssertFalse(PilotPathConfig.IsValidRelativePath("/absolute"), "absolute relative paths are rejected")
        AssertFalse(PilotPathConfig.IsValidRelativePath("a?b"), "query strings are rejected")
        AssertEqual("/dev/adminshell", PilotPathConfig.CookiePathFromRoot("/dev/adminshell"), "cookie path follows pilot root")
    End Sub

    Private Sub TestPilotRoutes()
        Dim route As PilotRouteMapping = Nothing
        Dim canonicalPath As String = Nothing

        AssertTrue(
            PilotPolicy.TryResolveCanonicalPath("/DEV/ADMINSHELL/VIEWS.ASP", RoutesConfig, PilotRoot, GlobalRoot, canonicalPath),
            "views route matching ignores case")
        AssertEqual("/admin/admin/views.asp", canonicalPath, "views maps to its canonical ACL identity")

        AssertTrue(
            PilotPolicy.TryResolveCanonicalPath("/dev/adminshell/loginlog.asp", RoutesConfig, PilotRoot, GlobalRoot, canonicalPath),
            "loginlog is a configured pilot route")
        AssertEqual("/admin/admin/loginlog.asp", canonicalPath, "loginlog maps to its canonical ACL identity")

        AssertTrue(
            PilotPolicy.TryResolveCanonicalPath(
                "/dev/adminshell/managed/access-manager/index.html",
                RoutesConfig,
                PilotRoot,
                GlobalRoot,
                canonicalPath),
            "access manager SPA is a configured pilot route")
        AssertEqual("/admin/admin/cgi-bin/accessadmin.pl", canonicalPath, "access manager maps to accessadmin ACL")

        AssertTrue(
            PilotPolicy.TryResolveRoute("/Dev/AdminShell/LoginLog.asp", RoutesConfig, PilotRoot, GlobalRoot, route),
            "route resolution ignores case")
        AssertEqual("Login Log", route.NavLabel, "resolved route exposes its nav label")

        AssertFalse(
            PilotPolicy.IsPilotRoute("/admin/admin/views.asp", RoutesConfig, PilotRoot, GlobalRoot),
            "legacy canonical paths are not pilot routes")
        AssertFalse(
            PilotPolicy.IsPilotRoute("/dev/adminshell/unknown.asp", RoutesConfig, PilotRoot, GlobalRoot),
            "unknown pilot routes are denied")
        AssertFalse(
            PilotPolicy.IsPilotRoute(Nothing, RoutesConfig, PilotRoot, GlobalRoot),
            "missing routes are denied")
        AssertFalse(
            PilotPolicy.TryResolveCanonicalPath("/dev/adminshell/views.asp", "", PilotRoot, GlobalRoot, canonicalPath),
            "blank route config denies all routes")
        AssertEqual(
            0,
            PilotPolicy.ParseRoutes("=/admin/admin/views.asp|Views;foo|bar;../x=y|Z", PilotRoot, GlobalRoot).Count,
            "malformed config yields no routes")
    End Sub

    Private Sub TestReturnUrls()
        Const viewsPath As String = "/dev/adminshell/views.asp"
        Const sqlLogsPath As String = "/dev/adminshell/sql_logs.asp"

        AssertTrue(PilotPolicy.IsSafeReturnUrl(viewsPath, RoutesConfig, PilotRoot, GlobalRoot), "default pilot page is a safe return URL")
        AssertTrue(PilotPolicy.IsSafeReturnUrl(viewsPath & "?group_cd=STAFF", RoutesConfig, PilotRoot, GlobalRoot), "pilot query strings are preserved")
        AssertTrue(PilotPolicy.IsSafeReturnUrl(sqlLogsPath & "?action=viewFile", RoutesConfig, PilotRoot, GlobalRoot), "other configured pilot routes are safe return URLs")
        AssertFalse(PilotPolicy.IsSafeReturnUrl("//evil.example/admin", RoutesConfig, PilotRoot, GlobalRoot), "protocol-relative URLs are denied")
        AssertFalse(PilotPolicy.IsSafeReturnUrl("https://evil.example/admin", RoutesConfig, PilotRoot, GlobalRoot), "absolute URLs are denied")
        AssertFalse(PilotPolicy.IsSafeReturnUrl("/admin/admin/cgi-bin/login.pl", RoutesConfig, PilotRoot, GlobalRoot), "legacy routes are denied")
        AssertFalse(PilotPolicy.IsSafeReturnUrl("/dev/adminshell/unknown.asp", RoutesConfig, PilotRoot, GlobalRoot), "unknown pilot routes are denied as return URLs")
        AssertFalse(PilotPolicy.IsSafeReturnUrl("", RoutesConfig, PilotRoot, GlobalRoot), "blank return URLs are denied")
    End Sub

    Private Sub TestConstantTimeComparison()
        AssertTrue(PilotPolicy.ConstantTimeEquals("abc123", "abc123"), "equal hashes match")
        AssertFalse(PilotPolicy.ConstantTimeEquals("abc123", "abc124"), "different hashes do not match")
        AssertFalse(PilotPolicy.ConstantTimeEquals("short", "longer"), "different lengths do not match")
        AssertTrue(PilotPolicy.ConstantTimeEquals(Nothing, ""), "null and empty normalize consistently")
    End Sub

    Private Sub AssertEqual(expected As String, actual As String, message As String)
        AssertTrue(String.Equals(expected, actual, StringComparison.Ordinal), message)
    End Sub

    Private Sub AssertEqual(expected As Integer, actual As Integer, message As String)
        AssertTrue(expected = actual, message)
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
