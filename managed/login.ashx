<%@ WebHandler Language="VB" Class="PilotLoginHandler" %>
Imports System
Imports System.Collections.Generic
Imports System.Globalization
Imports System.IO
Imports System.Security.Cryptography
Imports System.Text
Imports System.Web
Imports System.Web.Script.Serialization
Imports System.Web.SessionState

Public Class PilotLoginRequest
    Public Property UserName As String
    Public Property Password As String
    Public Property ReturnUrl As String
End Class

Public Class PilotLoginHandler
    Implements IHttpHandler
    Implements IRequiresSessionState

    Private Const MaxSessionFailures As Integer = 5
    Private Const MaxRequestBytes As Integer = 8192

    Public Sub ProcessRequest(context As HttpContext) Implements IHttpHandler.ProcessRequest
        PrepareResponse(context)

        If Not PilotConfig.IsEnabledForHost(context.Request.Url.Host) Then
            WriteError(context, 404, "Not found.", Nothing)
            Return
        End If

        Select Case context.Request.HttpMethod.ToUpperInvariant()
            Case "GET"
                HandleGet(context)
            Case "POST"
                HandlePost(context)
            Case Else
                context.Response.AppendHeader("Allow", "GET, POST")
                WriteError(context, 405, "Only GET and POST are supported.", Nothing)
        End Select
    End Sub

    Private Shared Sub HandleGet(context As HttpContext)
        Try
            Dim currentUser As PilotUser = Nothing
            If PilotAuth.TryGetCurrentUser(context, currentUser) AndAlso
                PilotAccess.CanAccessDefault(currentUser) Then
                WriteJson(
                    context,
                    200,
                    New Dictionary(Of String, Object) From {
                        {"redirectUrl", PilotConfig.DefaultRoute}
                    })
                Return
            End If

            Dim csrfToken = IssueCsrfToken(context)
            WriteJson(
                context,
                200,
                New Dictionary(Of String, Object) From {
                    {"csrfToken", csrfToken}
                })
        Catch ex As Exception
            WriteError(context, 503, "The sign-in service is temporarily unavailable.", Nothing)
        End Try
    End Sub

    Private Shared Sub HandlePost(context As HttpContext)
        Dim replacementToken As String = Nothing

        Try
            If context.Request.ContentLength <= 0 OrElse context.Request.ContentLength > MaxRequestBytes OrElse
                Not If(context.Request.ContentType, String.Empty).StartsWith(
                    "application/json",
                    StringComparison.OrdinalIgnoreCase) Then
                replacementToken = IssueCsrfToken(context)
                WriteError(context, 400, "The sign-in request was not valid.", replacementToken)
                Return
            End If

            Dim suppliedToken = context.Request.Headers("X-CSRF-Token")
            Dim expectedToken = TryCast(context.Session("PilotLoginCsrf"), String)
            If Not PilotLoginApiPolicy.IsValidCsrfToken(expectedToken, suppliedToken) Then
                replacementToken = IssueCsrfToken(context)
                WriteError(context, 403, "The sign-in form expired. Refresh the page and try again.", replacementToken)
                Return
            End If

            replacementToken = IssueCsrfToken(context)
            Dim requestBody = ReadRequest(context)
            If requestBody Is Nothing Then
                WriteError(context, 400, "The sign-in request was not valid.", replacementToken)
                Return
            End If

            Dim failures = GetFailureCount(context)
            If failures >= MaxSessionFailures Then
                WriteError(
                    context,
                    429,
                    "Too many failed attempts in this browser session. Close the browser and try again later.",
                    replacementToken)
                Return
            End If

            Dim user As PilotUser = Nothing
            If Not PilotAuth.Authenticate(
                context,
                If(requestBody.UserName, String.Empty),
                If(requestBody.Password, String.Empty),
                user) Then
                RecordFailure(context, failures + 1)
                WriteError(context, 401, "The username or password is not valid for this pilot.", replacementToken)
                Return
            End If

            Dim destination = PilotLoginApiPolicy.ResolveReturnUrl(
                requestBody.ReturnUrl,
                PilotConfig.DefaultRoute,
                PilotConfig.RoutesConfig,
                PilotConfig.PilotRootPath,
                PilotConfig.GlobalAdminRootPath)
            Dim destinationPath = destination.Split("?"c)(0)
            If Not PilotAccess.CanAccess(user, destinationPath) Then
                RecordFailure(context, failures + 1)
                WriteError(context, 403, "This account is not authorized for the requested admin shell tool.", replacementToken)
                Return
            End If

            context.Session.Remove("PilotFailedAttempts")
            context.Session.Remove("PilotLoginCsrf")
            PilotAuth.SignIn(context, user)

            WriteJson(
                context,
                200,
                New Dictionary(Of String, Object) From {
                    {"redirectUrl", destination}
                })
        Catch ex As Exception
            If String.IsNullOrEmpty(replacementToken) Then
                replacementToken = IssueCsrfToken(context)
            End If
            WriteError(context, 503, "The sign-in service is temporarily unavailable.", replacementToken)
        End Try
    End Sub

    Private Shared Function ReadRequest(context As HttpContext) As PilotLoginRequest
        Using reader As New StreamReader(context.Request.InputStream, Encoding.UTF8)
            Dim json = reader.ReadToEnd()
            If String.IsNullOrWhiteSpace(json) Then
                Return Nothing
            End If

            Dim serializer As New JavaScriptSerializer With {
                .MaxJsonLength = MaxRequestBytes
            }
            Return serializer.Deserialize(Of PilotLoginRequest)(json)
        End Using
    End Function

    Private Shared Function IssueCsrfToken(context As HttpContext) As String
        Dim bytes(31) As Byte
        Using generator = RandomNumberGenerator.Create()
            generator.GetBytes(bytes)
        End Using

        Dim token = Convert.ToBase64String(bytes)
        context.Session("PilotLoginCsrf") = token
        Return token
    End Function

    Private Shared Function GetFailureCount(context As HttpContext) As Integer
        Dim value = context.Session("PilotFailedAttempts")
        If value Is Nothing Then
            Return 0
        End If

        Dim count As Integer
        If Integer.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), count) Then
            Return count
        End If
        Return 0
    End Function

    Private Shared Sub RecordFailure(context As HttpContext, count As Integer)
        context.Session("PilotFailedAttempts") = count
    End Sub

    Private Shared Sub PrepareResponse(context As HttpContext)
        context.Response.ContentType = "application/json"
        context.Response.ContentEncoding = Encoding.UTF8
        context.Response.Cache.SetCacheability(HttpCacheability.NoCache)
        context.Response.Cache.SetNoStore()
        context.Response.AppendHeader("X-Content-Type-Options", "nosniff")
        context.Response.TrySkipIisCustomErrors = True
    End Sub

    Private Shared Sub WriteError(
        context As HttpContext,
        statusCode As Integer,
        message As String,
        csrfToken As String)

        Dim payload As New Dictionary(Of String, Object) From {
            {"error", message}
        }
        If Not String.IsNullOrEmpty(csrfToken) Then
            payload("csrfToken") = csrfToken
        End If

        WriteJson(context, statusCode, payload)
    End Sub

    Private Shared Sub WriteJson(context As HttpContext, statusCode As Integer, payload As Object)
        context.Response.StatusCode = statusCode
        Dim serializer As New JavaScriptSerializer()
        context.Response.Write(serializer.Serialize(payload))
    End Sub

    Public ReadOnly Property IsReusable As Boolean Implements IHttpHandler.IsReusable
        Get
            Return False
        End Get
    End Property
End Class
