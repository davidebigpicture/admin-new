Imports System
Imports System.Collections.Generic
Imports System.Configuration
Imports System.Data.Odbc
Imports System.Globalization
Imports System.IO
Imports System.Net
Imports System.Text
Imports System.Web
Imports System.Web.Security

Public NotInheritable Class PilotConfig
    Private Const DevelopmentSiteItemKey As String = "AdminShell.IsDevelopmentSite"

    Private Sub New()
    End Sub

    Public Shared ReadOnly Property AllowedHost As String
        Get
            Return Setting("PilotAllowedHost")
        End Get
    End Property

    Public Shared ReadOnly Property PilotRootPath As String
        Get
            Return PilotPathConfig.NormalizeRoot(Setting("PilotRootPath"))
        End Get
    End Property

    Public Shared ReadOnly Property GlobalAdminRootPath As String
        Get
            Return PilotPathConfig.NormalizeRoot(Setting("GlobalAdminRootPath"))
        End Get
    End Property

    Public Shared ReadOnly Property CookiePath As String
        Get
            Return PilotPathConfig.CookiePathFromRoot(PilotRootPath)
        End Get
    End Property

    Public Shared ReadOnly Property DefaultRoute As String
        Get
            Return PilotPathConfig.Combine(PilotRootPath, Setting("PilotDefaultRoute"))
        End Get
    End Property

    Public Shared ReadOnly Property RoutesConfig As String
        Get
            Return Setting("PilotRoutes")
        End Get
    End Property

    Public Shared ReadOnly Property SessionTimeoutMinutes As Integer
        Get
            Dim configured = Setting("PilotLegacySessionTimeoutMinutes")
            Dim timeoutMinutes As Integer
            If Integer.TryParse(configured, NumberStyles.Integer, CultureInfo.InvariantCulture, timeoutMinutes) AndAlso
                timeoutMinutes > 0 Then
                Return timeoutMinutes
            End If
            Return PilotLegacySession.DefaultTimeoutMinutes
        End Get
    End Property

    Public Shared ReadOnly Property LoginUrl As String
        Get
            Return PilotPathConfig.Combine(PilotRootPath, "managed/login.html")
        End Get
    End Property

    Public Shared ReadOnly Property LogoutUrl As String
        Get
            Return PilotPathConfig.Combine(PilotRootPath, "managed/logout.ashx")
        End Get
    End Property

    Public Shared ReadOnly Property StylesheetUrl As String
        Get
            Return PilotPathConfig.Combine(GlobalAdminRootPath, "bpstyles.css") & "?ver=082919"
        End Get
    End Property

    Public Shared ReadOnly Property PasswordHashUrl As String
        Get
            Return Setting("PilotPasswordHashUrl")
        End Get
    End Property

    Public Shared ReadOnly Property BannerTitle As String
        Get
            Return Setting("PilotBannerTitle")
        End Get
    End Property

    Public Shared ReadOnly Property BannerType As String
        Get
            Return Setting("PilotBannerType")
        End Get
    End Property

    Public Shared ReadOnly Property IsDevelopmentSite As Boolean
        Get
            Dim context = HttpContext.Current
            If context Is Nothing OrElse context.Request Is Nothing Then
                Return False
            End If

            If context.Items.Contains(DevelopmentSiteItemKey) Then
                Return CBool(context.Items(DevelopmentSiteItemKey))
            End If

            Dim host = context.Request.Url.DnsSafeHost
            Dim configuredDevelopmentDomain = Convert.ToString(context.Application("DEV_DOMAIN"))
            Dim isDevelopmentHost =
                Not String.IsNullOrWhiteSpace(configuredDevelopmentDomain) AndAlso
                String.Equals(host, configuredDevelopmentDomain, StringComparison.OrdinalIgnoreCase)
            Dim isDevelopment = isDevelopmentHost OrElse
                host.IndexOf("dev", StringComparison.OrdinalIgnoreCase) >= 0

            context.Items(DevelopmentSiteItemKey) = isDevelopment
            Return isDevelopment
        End Get
    End Property

    Public Shared Function IsEnabledForHost(host As String) As Boolean
        Return PilotPolicy.IsAllowedHost(host, AllowedHost)
    End Function

    Public Shared Function IsPilotUser(userName As String) As Boolean
        Return PilotPolicy.IsListedUser(userName, Setting("PilotUsers"))
    End Function

    Public Shared Function IsPilotRoute(path As String) As Boolean
        Return PilotPolicy.IsPilotRoute(path, RoutesConfig, PilotRootPath, GlobalAdminRootPath)
    End Function

    Public Shared Function TryGetCanonicalPath(path As String, ByRef canonicalPath As String) As Boolean
        Return PilotPolicy.TryResolveCanonicalPath(path, RoutesConfig, PilotRootPath, GlobalAdminRootPath, canonicalPath)
    End Function

    Public Shared Function GetRoutes() As System.Collections.Generic.IList(Of PilotRouteMapping)
        Return PilotPolicy.ParseRoutes(RoutesConfig, PilotRootPath, GlobalAdminRootPath)
    End Function

    Public Shared Function CombinePilot(relativePath As String) As String
        Return PilotPathConfig.Combine(PilotRootPath, relativePath)
    End Function

    Public Shared Function CombineGlobal(relativePath As String) As String
        Return PilotPathConfig.Combine(GlobalAdminRootPath, relativePath)
    End Function

    Private Shared Function Setting(key As String) As String
        Return If(ConfigurationManager.AppSettings(key), String.Empty).Trim()
    End Function
End Class

Public Class PilotUser
    Public Property MemberId As Integer
    Public Property UserName As String
    Public Property PasswordHash As String
    Public Property Salt As String
    Public Property AccountLocked As Boolean
    Public Property Inactive As Boolean
End Class

Public Class PilotRepository
    Private ReadOnly _connectionString As String

    Public Sub New()
        Dim configured = ConfigurationManager.ConnectionStrings("ConnectionStringB")
        If configured Is Nothing OrElse String.IsNullOrWhiteSpace(configured.ConnectionString) Then
            Throw New ConfigurationErrorsException("ConnectionStringB is not configured for the admin shell pilot.")
        End If
        _connectionString = configured.ConnectionString
    End Sub

    Public Function FindUserByName(userName As String) As PilotUser
        Const sql As String =
            "select member_id,user_name,password_hash,salt,account_locked,inactive " &
            "from member_login where user_name = ?"

        Using connection As New OdbcConnection(_connectionString),
              command As New OdbcCommand(sql, connection)
            command.Parameters.Add("@user_name", OdbcType.VarChar, 255).Value = userName
            connection.Open()

            Using reader = command.ExecuteReader()
                If Not reader.Read() Then
                    Return Nothing
                End If

                Return ReadUser(reader)
            End Using
        End Using
    End Function

    Public Function FindActiveUserById(memberId As Integer) As PilotUser
        Const sql As String =
            "select member_id,user_name,password_hash,salt,account_locked,inactive " &
            "from member_login where member_id = ?"

        Using connection As New OdbcConnection(_connectionString),
              command As New OdbcCommand(sql, connection)
            command.Parameters.Add("@member_id", OdbcType.Int).Value = memberId
            connection.Open()

            Using reader = command.ExecuteReader()
                If Not reader.Read() Then
                    Return Nothing
                End If

                Dim user = ReadUser(reader)
                If user.Inactive OrElse user.AccountLocked Then
                    Return Nothing
                End If
                Return user
            End Using
        End Using
    End Function

    Public Function HasScriptAccess(memberId As Integer, scriptPath As String) As Boolean
        Const sql As String =
            "select count(*) from (" &
            " select a.access_id from script s" &
            " join access a on a.secure_id = s.script_id and a.secure_ty = 'SCRI'" &
            " where lower(s.script_name) = lower(?) and s.inactive = 'N' and a.inactive = 'N'" &
            " and ((a.user_ty = 'USER' and a.user_id = ?)" &
            " or (a.user_ty = 'GROU' and exists (" &
            " select 1 from group_member gm join `group` g on g.group_id = gm.group_id" &
            " where gm.member_id = ? and gm.group_id = a.user_id and g.inactive = 'N')))" &
            " union" &
            " select a.access_id from script s" &
            " join section_script ss on ss.script_id = s.script_id" &
            " join section sn on sn.section_id = ss.section_id" &
            " join access a on a.secure_id = sn.section_id and a.secure_ty = 'SECT'" &
            " where lower(s.script_name) = lower(?) and s.inactive = 'N' and sn.inactive = 'N' and a.inactive = 'N'" &
            " and ((a.user_ty = 'USER' and a.user_id = ?)" &
            " or (a.user_ty = 'GROU' and exists (" &
            " select 1 from group_member gm join `group` g on g.group_id = gm.group_id" &
            " where gm.member_id = ? and gm.group_id = a.user_id and g.inactive = 'N')))" &
            ") pilot_access"

        Using connection As New OdbcConnection(_connectionString),
              command As New OdbcCommand(sql, connection)
            command.Parameters.Add("@script_user", OdbcType.VarChar, 512).Value = scriptPath
            command.Parameters.Add("@member_user", OdbcType.Int).Value = memberId
            command.Parameters.Add("@group_member_user", OdbcType.Int).Value = memberId
            command.Parameters.Add("@script_section", OdbcType.VarChar, 512).Value = scriptPath
            command.Parameters.Add("@member_section", OdbcType.Int).Value = memberId
            command.Parameters.Add("@group_member_section", OdbcType.Int).Value = memberId
            connection.Open()
            Return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0
        End Using
    End Function

    Public Function ListMenuSections(memberId As Integer) As IList(Of PilotMenuSection)
        Const sql As String =
            "select sn.section_id,sn.section,sc.script_id,sc.script_name,sc.title " &
            "from section sn " &
            "join section_script ss on ss.section_id = sn.section_id " &
            "join script sc on sc.script_id = ss.script_id " &
            "where sn.inactive = 'N' and sc.inactive = 'N' " &
            "and exists (" &
            " select 1 from access a where a.inactive = 'N' and a.permission_cd = 'G' " &
            " and ((a.secure_ty = 'SCRI' and a.secure_id = sc.script_id) " &
            "   or (a.secure_ty = 'SECT' and a.secure_id = sn.section_id)) " &
            " and ((a.user_ty = 'USER' and a.user_id = ?) " &
            "   or (a.user_ty = 'GROU' and exists (" &
            "     select 1 from group_member gm join `group` g on g.group_id = gm.group_id " &
            "     where gm.member_id = ? and gm.group_id = a.user_id and g.inactive = 'N')))" &
            ") order by sn.position,sn.section,ss.position,sc.title"

        Dim sections As New List(Of PilotMenuSection)()
        Dim byId As New Dictionary(Of Integer, PilotMenuSection)()
        Using connection As New OdbcConnection(_connectionString),
              command As New OdbcCommand(sql, connection)
            command.Parameters.Add("@member_user", OdbcType.Int).Value = memberId
            command.Parameters.Add("@member_group", OdbcType.Int).Value = memberId
            connection.Open()
            Using reader = command.ExecuteReader()
                While reader.Read()
                    Dim sectionId = Convert.ToInt32(reader("section_id"), CultureInfo.InvariantCulture)
                    Dim menuSection As PilotMenuSection = Nothing
                    If Not byId.TryGetValue(sectionId, menuSection) Then
                        menuSection = New PilotMenuSection With {
                            .SectionId = sectionId,
                            .Title = DbString(reader("section")),
                            .Items = New List(Of PilotMenuItem)()
                        }
                        byId(sectionId) = menuSection
                        sections.Add(menuSection)
                    End If
                    menuSection.Items.Add(New PilotMenuItem With {
                        .ScriptId = Convert.ToInt32(reader("script_id"), CultureInfo.InvariantCulture),
                        .Title = DbString(reader("title")),
                        .Path = DbString(reader("script_name"))
                    })
                End While
            End Using
        End Using
        Return sections
    End Function

    Private Shared Function ReadUser(reader As OdbcDataReader) As PilotUser
        Return New PilotUser With {
            .MemberId = Convert.ToInt32(reader("member_id"), CultureInfo.InvariantCulture),
            .UserName = Convert.ToString(reader("user_name"), CultureInfo.InvariantCulture),
            .PasswordHash = DbString(reader("password_hash")),
            .Salt = DbString(reader("salt")),
            .AccountLocked = FlagIsTrue(reader("account_locked")),
            .Inactive = FlagIsTrue(reader("inactive"))
        }
    End Function

    Private Shared Function DbString(value As Object) As String
        If value Is Nothing OrElse Convert.IsDBNull(value) Then
            Return String.Empty
        End If
        Return Convert.ToString(value, CultureInfo.InvariantCulture)
    End Function

    Private Shared Function FlagIsTrue(value As Object) As Boolean
        Dim flag = DbString(value)
        Return String.Equals(flag, "Y", StringComparison.OrdinalIgnoreCase) OrElse
            String.Equals(flag, "1", StringComparison.OrdinalIgnoreCase) OrElse
            String.Equals(flag, "true", StringComparison.OrdinalIgnoreCase)
    End Function
End Class

Public Class PilotPasswordHasher
    Public Function Verify(password As String, salt As String, storedHash As String) As Boolean
        If String.IsNullOrEmpty(password) OrElse String.IsNullOrEmpty(salt) OrElse String.IsNullOrEmpty(storedHash) Then
            Return False
        End If

        Dim request = DirectCast(WebRequest.Create(PilotConfig.PasswordHashUrl), HttpWebRequest)
        request.Method = "POST"
        request.ContentType = "application/x-www-form-urlencoded"
        request.Timeout = 10000
        request.ReadWriteTimeout = 10000

        Dim payload = "action=hash&password=" & HttpUtility.UrlEncode(password) &
            "&salt=" & HttpUtility.UrlEncode(salt)
        Dim body = Encoding.UTF8.GetBytes(payload)
        request.ContentLength = body.Length

        Using requestStream = request.GetRequestStream()
            requestStream.Write(body, 0, body.Length)
        End Using

        Dim calculatedHash As String
        Using response = DirectCast(request.GetResponse(), HttpWebResponse),
              responseStream = response.GetResponseStream(),
              reader As New StreamReader(responseStream, Encoding.UTF8)
            calculatedHash = reader.ReadToEnd().Trim()
        End Using

        Return PilotPolicy.ConstantTimeEquals(calculatedHash, storedHash.Trim())
    End Function
End Class

Public NotInheritable Class PilotAuth
    Private Const CookieName As String = "bp_admin_next"

    Private Sub New()
    End Sub

    Public Shared Function Authenticate(context As HttpContext, userName As String, password As String, ByRef user As PilotUser) As Boolean
        user = Nothing
        If Not PilotConfig.IsEnabledForHost(context.Request.Url.Host) OrElse
            Not PilotConfig.IsPilotUser(userName) Then
            Return False
        End If

        Dim repository As New PilotRepository()
        Dim candidate = repository.FindUserByName(userName.Trim())
        If candidate Is Nothing OrElse candidate.Inactive OrElse candidate.AccountLocked Then
            Return False
        End If

        Dim hasher As New PilotPasswordHasher()
        If Not hasher.Verify(password, candidate.Salt, candidate.PasswordHash) Then
            Return False
        End If

        user = candidate
        Return True
    End Function

    Public Shared Sub SignIn(context As HttpContext, user As PilotUser, Optional password As String = Nothing)
        Dim cookiePath = PilotConfig.CookiePath
        Dim issued = DateTime.UtcNow
        Dim ticket As New FormsAuthenticationTicket(
            2,
            user.UserName,
            issued,
            issued.AddMinutes(60),
            False,
            user.MemberId.ToString(CultureInfo.InvariantCulture),
            cookiePath)

        WriteTicketCookie(context, ticket, cookiePath)

        Try
            PilotLegacySession.Ensure(context, user, password)
        Catch ex As Exception
            ' Pilot sign-in still succeeds when legacy credential encoding is unavailable.
        End Try
    End Sub

    Public Shared Function TryGetCurrentUser(context As HttpContext, ByRef user As PilotUser) As Boolean
        user = Nothing
        If Not PilotConfig.IsEnabledForHost(context.Request.Url.Host) Then
            Return False
        End If

        Dim cookie = context.Request.Cookies(CookieName)
        If cookie Is Nothing OrElse String.IsNullOrWhiteSpace(cookie.Value) Then
            Return TrySignInFromLegacySession(context, user)
        End If

        Dim ticket As FormsAuthenticationTicket
        Try
            ticket = FormsAuthentication.Decrypt(cookie.Value)
        Catch ex As Exception
            Return TrySignInFromLegacySession(context, user)
        End Try

        If ticket Is Nothing OrElse ticket.Expired OrElse Not PilotConfig.IsPilotUser(ticket.Name) Then
            Return TrySignInFromLegacySession(context, user)
        End If

        Dim memberId As Integer
        If Not Integer.TryParse(ticket.UserData, NumberStyles.None, CultureInfo.InvariantCulture, memberId) Then
            Return False
        End If

        Dim repository As New PilotRepository()
        user = repository.FindActiveUserById(memberId)
        If user Is Nothing OrElse Not String.Equals(user.UserName, ticket.Name, StringComparison.OrdinalIgnoreCase) Then
            user = Nothing
            Return False
        End If

        Dim renewed = FormsAuthentication.RenewTicketIfOld(ticket)
        If Not renewed.Expiration.Equals(ticket.Expiration) Then
            WriteTicketCookie(context, renewed, PilotConfig.CookiePath)
        End If

        Try
            PilotLegacySession.Refresh(context, user)
        Catch ex As Exception
        End Try

        Return True
    End Function

    Public Shared Sub SignOut(context As HttpContext)
        Dim expired As New HttpCookie(CookieName, String.Empty) With {
            .Expires = DateTime.UtcNow.AddYears(-1),
            .HttpOnly = True,
            .Secure = True,
            .Path = PilotConfig.CookiePath,
            .SameSite = SameSiteMode.Lax
        }
        context.Response.Cookies.Add(expired)
        PilotLegacySession.Clear(context)
    End Sub

    Public Shared Function IsSafeReturnUrl(returnUrl As String) As Boolean
        Return PilotPolicy.IsSafeReturnUrl(
            returnUrl,
            PilotConfig.RoutesConfig,
            PilotConfig.PilotRootPath,
            PilotConfig.GlobalAdminRootPath)
    End Function

    Private Shared Function TrySignInFromLegacySession(context As HttpContext, ByRef user As PilotUser) As Boolean
        user = Nothing
        Dim loginName As String = Nothing
        If Not PilotLegacySession.TryGetAuthenticatedLoginName(context, loginName) Then
            Return False
        End If

        If Not PilotConfig.IsPilotUser(loginName) Then
            Return False
        End If

        Dim repository As New PilotRepository()
        user = repository.FindUserByName(loginName)
        If user Is Nothing OrElse user.Inactive OrElse user.AccountLocked Then
            user = Nothing
            Return False
        End If

        SignIn(context, user, Nothing)
        Return True
    End Function

    Private Shared Sub WriteTicketCookie(context As HttpContext, ticket As FormsAuthenticationTicket, cookiePath As String)
        Dim cookie As New HttpCookie(CookieName, FormsAuthentication.Encrypt(ticket)) With {
            .Expires = ticket.Expiration,
            .HttpOnly = True,
            .Secure = True,
            .Path = cookiePath,
            .SameSite = SameSiteMode.Lax
        }
        context.Response.Cookies.Add(cookie)
    End Sub
End Class

Public NotInheritable Class PilotAccess
    Private Sub New()
    End Sub

    Public Shared Function CanAccess(user As PilotUser, pilotPath As String) As Boolean
        If user Is Nothing Then
            Return False
        End If

        Dim canonicalPath As String = Nothing
        If Not PilotConfig.TryGetCanonicalPath(pilotPath, canonicalPath) Then
            Return False
        End If

        Dim repository As New PilotRepository()
        If repository.HasScriptAccess(user.MemberId, canonicalPath) Then
            Return True
        End If

        Return Not String.Equals(canonicalPath, pilotPath, StringComparison.OrdinalIgnoreCase) AndAlso
            repository.HasScriptAccess(user.MemberId, pilotPath)
    End Function

    Public Shared Function CanAccessDefault(user As PilotUser) As Boolean
        Return CanAccess(user, PilotConfig.DefaultRoute)
    End Function

    Public Shared Function CanAccessAccessManager(user As PilotUser) As Boolean
        Return AccessManagerAccess.CanOpenApp(user)
    End Function
End Class
