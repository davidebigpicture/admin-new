Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Security.Cryptography
Imports System.Text
Imports System.Web
Imports System.Web.Script.Serialization

Public NotInheritable Class PilotJsonApi
    Public Const CsrfSessionKey As String = "PilotApiCsrf"
    Public Const MaxRequestBytes As Integer = 65536
    Public Const MaxPrincipalSearchLimit As Integer = 50

    Private Sub New()
    End Sub

    Public Shared Sub PrepareJsonResponse(context As HttpContext)
        context.Response.ContentType = "application/json"
        context.Response.ContentEncoding = Encoding.UTF8
        context.Response.Cache.SetCacheability(HttpCacheability.NoCache)
        context.Response.Cache.SetNoStore()
        context.Response.AppendHeader("X-Content-Type-Options", "nosniff")
        context.Response.TrySkipIisCustomErrors = True
    End Sub

    Public Shared Function IssueCsrfToken(context As HttpContext) As String
        Dim bytes(31) As Byte
        Using generator = RandomNumberGenerator.Create()
            generator.GetBytes(bytes)
        End Using

        Dim token = Convert.ToBase64String(bytes)
        context.Session(CsrfSessionKey) = token
        Return token
    End Function

    Public Shared Function RequireCsrf(context As HttpContext) As Boolean
        Dim suppliedToken = context.Request.Headers("X-CSRF-Token")
        Dim expectedToken = TryCast(context.Session(CsrfSessionKey), String)
        If Not PilotLoginApiPolicy.IsValidCsrfToken(expectedToken, suppliedToken) Then
            Dim replacement = IssueCsrfToken(context)
            WriteError(context, 403, "The request expired. Refresh the page and try again.", replacement)
            Return False
        End If
        Return True
    End Function

    Public Shared Function RequireUser(context As HttpContext, ByRef user As PilotUser) As Boolean
        user = Nothing
        If Not PilotConfig.IsEnabledForHost(context.Request.Url.Host) Then
            WriteError(context, 404, "Not found.", Nothing)
            Return False
        End If

        If Not PilotAuth.TryGetCurrentUser(context, user) Then
            WriteError(
                context,
                401,
                "Authentication is required.",
                Nothing,
                New Dictionary(Of String, Object) From {
                    {"loginUrl", PilotConfig.LoginUrl}
                })
            Return False
        End If

        Return True
    End Function

    Public Shared Function ReadJsonBody(Of T)(context As HttpContext) As T
        If context.Request.ContentLength < 0 OrElse context.Request.ContentLength > MaxRequestBytes Then
            Throw New AdminShellValidationException("The request body is not valid.")
        End If

        Dim contentType = If(context.Request.ContentType, String.Empty)
        If Not contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase) Then
            Throw New AdminShellValidationException("The request body is not valid.")
        End If

        Using reader As New StreamReader(context.Request.InputStream, Encoding.UTF8)
            Dim json = reader.ReadToEnd()
            If String.IsNullOrWhiteSpace(json) Then
                Throw New AdminShellValidationException("The request body is not valid.")
            End If

            Dim serializer As New JavaScriptSerializer With {
                .MaxJsonLength = MaxRequestBytes
            }
            Return serializer.Deserialize(Of T)(json)
        End Using
    End Function

    Public Shared Sub WriteJson(context As HttpContext, statusCode As Integer, data As Object)
        context.Response.StatusCode = statusCode
        Dim payload As New Dictionary(Of String, Object) From {
            {"ok", True},
            {"data", data}
        }
        WriteRaw(context, payload)
    End Sub

    Public Shared Sub WriteError(
        context As HttpContext,
        statusCode As Integer,
        message As String,
        Optional csrfToken As String = Nothing,
        Optional data As Object = Nothing)

        context.Response.StatusCode = statusCode
        Dim payload As New Dictionary(Of String, Object) From {
            {"ok", False},
            {"error", message}
        }
        If Not String.IsNullOrEmpty(csrfToken) Then
            payload("csrfToken") = csrfToken
        End If
        If data IsNot Nothing Then
            payload("data") = data
        End If
        WriteRaw(context, payload)
    End Sub

    Public Shared Sub HandleServiceException(context As HttpContext, ex As Exception)
        Dim csrfToken As String = Nothing
        If context.Session IsNot Nothing Then
            csrfToken = IssueCsrfToken(context)
        End If

        Select Case True
            Case TypeOf ex Is AdminShellValidationException
                WriteError(context, 400, ex.Message, csrfToken)
            Case TypeOf ex Is AdminShellForbiddenException
                WriteError(context, 403, ex.Message, csrfToken)
            Case TypeOf ex Is AdminShellConcurrencyException
                WriteError(context, 409, ex.Message, csrfToken)
            Case TypeOf ex Is AdminShellServiceException
                WriteError(context, 400, ex.Message, csrfToken)
            Case Else
                Dim detail As String = Nothing
                Try
                    If PilotConfig.IsEnabledForHost(context.Request.Url.Host) Then
                        detail = ex.GetType().Name & ": " & ex.Message
                    End If
                Catch
                End Try

                If String.IsNullOrEmpty(detail) Then
                    WriteError(context, 503, "The service is temporarily unavailable.", csrfToken)
                Else
                    WriteError(
                        context,
                        503,
                        "The service is temporarily unavailable.",
                        csrfToken,
                        New Dictionary(Of String, Object) From {
                            {"detail", detail}
                        })
                End If
        End Select
    End Sub

    Public Shared Function CapPrincipalSearchLimit(requestedLimit As Integer) As Integer
        If requestedLimit <= 0 Then
            Return MaxPrincipalSearchLimit
        End If
        Return Math.Min(requestedLimit, MaxPrincipalSearchLimit)
    End Function

    Public Shared Function CanUseAccessManager(user As PilotUser) As Boolean
        Return AccessManagerAccess.CanUseWorkspace(user)
    End Function

    Public Shared Function SerializeCapabilities(caps As AccessManagerCapabilities) As Dictionary(Of String, Object)
        Return New Dictionary(Of String, Object) From {
            {"canManageSections", caps.CanManageSections},
            {"canManageScripts", caps.CanManageScripts},
            {"canManageMemberships", caps.CanManageMemberships},
            {"canManageGrants", caps.CanManageGrants}
        }
    End Function

    Public Shared Function SerializeRoutes() As IList(Of Dictionary(Of String, Object))
        Dim routes = PilotConfig.GetRoutes()
        Dim payload As New List(Of Dictionary(Of String, Object))()
        Dim routeIndex As Integer
        For routeIndex = 0 To routes.Count - 1
            Dim route = routes(routeIndex)
            payload.Add(New Dictionary(Of String, Object) From {
                {"path", PreferredNavigationPath(route.PilotPath)},
                {"label", route.NavLabel},
                {"canonicalPath", route.CanonicalPath}
            })
        Next
        Return payload
    End Function

    Public Shared Function SerializeShellPaths() As Dictionary(Of String, Object)
        Return New Dictionary(Of String, Object) From {
            {"pilotRoot", PilotConfig.PilotRootPath},
            {"globalAdminRoot", PilotConfig.GlobalAdminRootPath},
            {"loginUrl", PilotConfig.LoginUrl},
            {"logoutUrl", PilotConfig.LogoutUrl},
            {"managedBase", PilotConfig.CombinePilot("managed") & "/"},
            {"routes", SerializeRoutes()}
        }
    End Function

    Public Shared Function LoadMenuSections(user As PilotUser) As IList(Of PilotMenuSection)
        Try
            Return New PilotRepository().ListMenuSections(user.MemberId)
        Catch
            Return New List(Of PilotMenuSection)()
        End Try
    End Function

    Public Shared Function SerializeMenuSections(sections As IList(Of PilotMenuSection)) As IList(Of Dictionary(Of String, Object))
        Dim payload As New List(Of Dictionary(Of String, Object))()
        If sections Is Nothing Then
            Return payload
        End If

        For Each section As PilotMenuSection In sections
            Dim items As New List(Of Dictionary(Of String, Object))()
            If section.Items IsNot Nothing Then
                For Each item As PilotMenuItem In section.Items
                    items.Add(New Dictionary(Of String, Object) From {
                        {"ScriptId", item.ScriptId},
                        {"Title", item.Title},
                        {"Path", PreferredNavigationPath(ResolveMenuItemPath(item.Path))}
                    })
                Next
            End If

            payload.Add(New Dictionary(Of String, Object) From {
                {"SectionId", section.SectionId},
                {"Title", section.Title},
                {"Items", items}
            })
        Next

        Return payload
    End Function

    Public Shared Function ResolveMenuItemPath(scriptPath As String) As String
        If String.IsNullOrWhiteSpace(scriptPath) Then
            Return scriptPath
        End If

        Dim trimmed = scriptPath.Trim()
        Dim pilotPath As String = Nothing
        If PilotPolicy.TryResolvePilotPathByCanonical(
            trimmed,
            PilotConfig.RoutesConfig,
            PilotConfig.PilotRootPath,
            PilotConfig.GlobalAdminRootPath,
            pilotPath) Then
            Return pilotPath
        End If

        Dim canonicalCandidate As String = Nothing
        If Not trimmed.StartsWith("/", StringComparison.Ordinal) AndAlso
            PilotPathConfig.TryCombine(PilotConfig.GlobalAdminRootPath, trimmed, canonicalCandidate) AndAlso
            PilotPolicy.TryResolvePilotPathByCanonical(
                canonicalCandidate,
                PilotConfig.RoutesConfig,
                PilotConfig.PilotRootPath,
                PilotConfig.GlobalAdminRootPath,
                pilotPath) Then
            Return pilotPath
        End If

        Return trimmed
    End Function

    Private Shared Function PreferredNavigationPath(path As String) As String
        Const defaultDocument As String = "index.aspx"
        If String.IsNullOrWhiteSpace(path) Then
            Return path
        End If

        Dim suffixStart = path.IndexOfAny(New Char() {"?"c, "#"c})
        Dim pathPart = If(suffixStart < 0, path, path.Substring(0, suffixStart))
        Dim suffix = If(suffixStart < 0, String.Empty, path.Substring(suffixStart))
        If pathPart.EndsWith(defaultDocument, StringComparison.OrdinalIgnoreCase) Then
            Return pathPart.Substring(0, pathPart.Length - defaultDocument.Length) & suffix
        End If

        Return path
    End Function

    Private Shared Sub WriteRaw(context As HttpContext, payload As Object)
        Dim serializer As New JavaScriptSerializer With {
            .MaxJsonLength = Integer.MaxValue
        }
        context.Response.Write(serializer.Serialize(payload))
    End Sub
End Class
