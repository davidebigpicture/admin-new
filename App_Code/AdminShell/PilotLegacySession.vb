Imports System
Imports System.Configuration
Imports System.Globalization
Imports System.Web

''' <summary>
''' Bridges pilot sign-in to legacy /admin/admin auth: sessionIDadmin cookie,
''' encrypted username/password cookies, and CacheManager-compatible Redis keys.
''' Credential encoding is delegated to ILegacyMembershipCredentialEncoder so the
''' legacy Blowfish adapter can be removed when global auth modernizes.
''' </summary>
Public NotInheritable Class PilotLegacySession
    Public Const AdminCookiePath As String = "/admin"
    Public Const SessionCookieName As String = "sessionIDadmin"
    Public Const UsernameCookieName As String = "username"
    Public Const PasswordCookieName As String = "password"
    Public Const AuthenticatedCookieName As String = "authenticated"
    Public Const DefaultTimeoutMinutes As Integer = 60

    Private Sub New()
    End Sub

    Public Shared Sub Ensure(context As HttpContext, user As PilotUser, password As String)
        If context Is Nothing OrElse user Is Nothing Then
            Return
        End If

        Dim timeoutMinutes = GetAdminTimeoutMinutes()
        Dim sessionId = GetOrCreateAdminSessionId(context)
        WriteAdminRedisSession(sessionId, user.UserName, timeoutMinutes)
        WriteCoreCookies(context, sessionId, timeoutMinutes)

        Dim encryptedUsername As String = Nothing
        Dim encryptedPassword As String = Nothing
        Try
            encryptedUsername = PilotLegacyMembershipCrypto.Encrypt(user.UserName)
            If Not String.IsNullOrEmpty(password) Then
                encryptedPassword = PilotLegacyMembershipCrypto.Encrypt(password)
            Else
                Dim existingPassword = context.Request.Cookies(PasswordCookieName)
                If existingPassword IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(existingPassword.Value) Then
                    encryptedPassword = existingPassword.Value
                End If
            End If
        Catch
            ' Redis + sessionIDadmin are already established. Credential cookies require the
            ' configured legacy encoder.
            Return
        End Try

        WriteCredentialCookies(context, encryptedUsername, encryptedPassword, timeoutMinutes)
    End Sub

    Public Shared Sub Refresh(context As HttpContext, user As PilotUser)
        If context Is Nothing OrElse user Is Nothing Then
            Return
        End If

        Dim timeoutMinutes = GetAdminTimeoutMinutes()
        Dim sessionId = GetOrCreateAdminSessionId(context)
        WriteAdminRedisSession(sessionId, user.UserName, timeoutMinutes)
        WriteCoreCookies(context, sessionId, timeoutMinutes)

        Dim encryptedUsername As String = Nothing
        Dim existingUsername = context.Request.Cookies(UsernameCookieName)
        If existingUsername IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(existingUsername.Value) Then
            encryptedUsername = existingUsername.Value
        Else
            Try
                encryptedUsername = PilotLegacyMembershipCrypto.Encrypt(user.UserName)
            Catch
                Return
            End Try
        End If

        Dim encryptedPassword As String = Nothing
        Dim existingPassword = context.Request.Cookies(PasswordCookieName)
        If existingPassword IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(existingPassword.Value) Then
            encryptedPassword = existingPassword.Value
        End If

        WriteCredentialCookies(context, encryptedUsername, encryptedPassword, timeoutMinutes)
    End Sub

    Public Shared Sub Clear(context As HttpContext)
        If context Is Nothing Then
            Return
        End If

        Dim expired = DateTime.UtcNow.AddYears(-1)
        ExpireCookie(context, SessionCookieName, expired)
        ExpireCookie(context, UsernameCookieName, expired)
        ExpireCookie(context, PasswordCookieName, expired)
        ExpireCookie(context, AuthenticatedCookieName, expired)
    End Sub

    ''' <summary>
    ''' Recognizes an active legacy /admin session from cookies set by login.pl / topshell.asp.
    ''' Used to bridge legacy sign-in into the pilot shell without a second login prompt.
    ''' </summary>
    Public Shared Function TryGetAuthenticatedLoginName(context As HttpContext, ByRef loginName As String) As Boolean
        loginName = Nothing
        If context Is Nothing Then
            Return False
        End If

        Return TryResolveLoginNameFromLegacyProof(
            GetCookieValue(context, AuthenticatedCookieName),
            GetCookieValue(context, UsernameCookieName),
            loginName)
    End Function

    Friend Shared Function TryResolveLoginNameFromLegacyProof(
        authenticatedValue As String,
        encryptedUsername As String,
        ByRef loginName As String) As Boolean

        loginName = Nothing
        If Not IsAuthenticatedCookieValue(authenticatedValue) Then
            Return False
        End If

        If String.IsNullOrWhiteSpace(encryptedUsername) Then
            Return False
        End If

        Dim decrypted As String = Nothing
        Try
            If Not PilotLegacyMembershipCrypto.TryDecrypt(encryptedUsername, decrypted) Then
                Return False
            End If
        Catch
            Return False
        End Try

        If String.IsNullOrWhiteSpace(decrypted) Then
            Return False
        End If

        loginName = decrypted.Trim()
        Return True
    End Function

    Friend Shared Function GetCookieValue(context As HttpContext, name As String) As String
        Dim cookie = context.Request.Cookies(name)
        If cookie Is Nothing Then
            Return String.Empty
        End If

        Return If(cookie.Value, String.Empty).Trim()
    End Function

    Friend Shared Function NormalizeSessionId(rawValue As String) As String
        If String.IsNullOrWhiteSpace(rawValue) Then
            Return String.Empty
        End If

        Dim normalized = rawValue.Trim().Replace("-", String.Empty)
        Return normalized.Replace("%2D", String.Empty).Replace("%2d", String.Empty)
    End Function

    Friend Shared Function CreateSessionId() As String
        Return Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)
    End Function

    Private Shared Sub WriteAdminRedisSession(sessionId As String, loginName As String, timeoutMinutes As Integer)
        Dim normalizedId = NormalizeSessionId(sessionId)
        If String.IsNullOrEmpty(normalizedId) OrElse String.IsNullOrWhiteSpace(loginName) Then
            Return
        End If

        Dim session = RedisService.CreateSession(normalizedId)
        session.SetDefaultExpiration(0, 0, timeoutMinutes, 0)
        session("LoginName") = loginName
        session("redisTimeout") = timeoutMinutes.ToString(CultureInfo.InvariantCulture)
        session("redisSessionID") = normalizedId
    End Sub

    Private Shared Function GetOrCreateAdminSessionId(context As HttpContext) As String
        Dim existing = context.Request.Cookies(SessionCookieName)
        Dim normalized = NormalizeSessionId(If(existing Is Nothing, Nothing, existing.Value))
        If Not String.IsNullOrEmpty(normalized) Then
            Return normalized
        End If

        Return CreateSessionId()
    End Function

    Private Shared Sub WriteCoreCookies(context As HttpContext, sessionId As String, timeoutMinutes As Integer)
        Dim expires = DateTime.UtcNow.AddMinutes(timeoutMinutes)
        WriteAdminCookie(context, SessionCookieName, NormalizeSessionId(sessionId), expires)
        WriteAdminCookie(context, AuthenticatedCookieName, "true", expires)
    End Sub

    Private Shared Sub WriteCredentialCookies(
        context As HttpContext,
        encryptedUsername As String,
        encryptedPassword As String,
        timeoutMinutes As Integer)

        Dim expires = DateTime.UtcNow.AddMinutes(timeoutMinutes)
        WriteAdminCookie(context, UsernameCookieName, encryptedUsername, expires)
        If Not String.IsNullOrEmpty(encryptedPassword) Then
            WriteAdminCookie(context, PasswordCookieName, encryptedPassword, expires)
        End If
    End Sub

    Private Shared Sub WriteAdminCookie(context As HttpContext, name As String, value As String, expiresUtc As DateTime)
        Dim cookie As New HttpCookie(name, value) With {
            .Expires = expiresUtc,
            .HttpOnly = True,
            .Secure = True,
            .Path = AdminCookiePath
        }
        context.Response.Cookies.Add(cookie)
    End Sub

    Private Shared Sub ExpireCookie(context As HttpContext, name As String, expiresUtc As DateTime)
        Dim cookie As New HttpCookie(name, String.Empty) With {
            .Expires = expiresUtc,
            .HttpOnly = True,
            .Secure = True,
            .Path = AdminCookiePath
        }
        context.Response.Cookies.Add(cookie)
    End Sub

    Private Shared Function IsAuthenticatedCookieValue(value As String) As Boolean
        Return String.Equals(If(value, String.Empty).Trim(), "true", StringComparison.OrdinalIgnoreCase)
    End Function

    Private Shared Function GetAdminTimeoutMinutes() As Integer
        Dim configured = If(ConfigurationManager.AppSettings("PilotLegacySessionTimeoutMinutes"), String.Empty).Trim()
        Dim timeoutMinutes As Integer
        If Integer.TryParse(configured, NumberStyles.Integer, CultureInfo.InvariantCulture, timeoutMinutes) AndAlso
            timeoutMinutes > 0 Then
            Return timeoutMinutes
        End If

        Return DefaultTimeoutMinutes
    End Function
End Class
