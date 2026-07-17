Imports System
Imports System.Collections.Generic
Imports System.Text

Public Class PilotRouteMapping
    Public Property PilotPath As String
    Public Property CanonicalPath As String
    Public Property NavLabel As String
    Public Property RelativePilotPath As String
    Public Property RelativeCanonicalPath As String
End Class

Public NotInheritable Class PilotPolicy
    Private Sub New()
    End Sub

    Public Shared Function IsAllowedHost(host As String, allowedHost As String) As Boolean
        Return Not String.IsNullOrWhiteSpace(host) AndAlso
            Not String.IsNullOrWhiteSpace(allowedHost) AndAlso
            String.Equals(host.Trim(), allowedHost.Trim(), StringComparison.OrdinalIgnoreCase)
    End Function

    Public Shared Function IsListedUser(userName As String, configuredUsers As String) As Boolean
        If String.IsNullOrWhiteSpace(userName) OrElse String.IsNullOrWhiteSpace(configuredUsers) Then
            Return False
        End If

        For Each configuredUser As String In configuredUsers.Split(","c)
            If String.Equals(configuredUser.Trim(), userName.Trim(), StringComparison.OrdinalIgnoreCase) Then
                Return True
            End If
        Next

        Return False
    End Function

    ''' <summary>
    ''' Parses relative route mappings of the form:
    ''' relativePilot=relativeCanonical|Label;...
    ''' and expands them against the configured roots.
    ''' </summary>
    Public Shared Function ParseRoutes(
        routesConfig As String,
        pilotRootPath As String,
        globalAdminRootPath As String) As IList(Of PilotRouteMapping)

        Dim routes As New List(Of PilotRouteMapping)()
        If String.IsNullOrWhiteSpace(routesConfig) Then
            Return routes
        End If

        Dim pilotRoot As String = Nothing
        Dim globalRoot As String = Nothing
        Try
            pilotRoot = PilotPathConfig.NormalizeRoot(pilotRootPath)
            globalRoot = PilotPathConfig.NormalizeRoot(globalAdminRootPath)
        Catch ex As ArgumentException
            Return routes
        End Try

        For Each rawEntry As String In routesConfig.Split(";"c)
            Dim entry = rawEntry.Trim()
            If entry.Length = 0 Then
                Continue For
            End If

            Dim separatorIndex = entry.IndexOf("="c)
            If separatorIndex <= 0 OrElse separatorIndex >= entry.Length - 1 Then
                Continue For
            End If

            Dim relativePilot = entry.Substring(0, separatorIndex).Trim()
            Dim remainder = entry.Substring(separatorIndex + 1).Trim()
            Dim labelSeparatorIndex = remainder.IndexOf("|"c)
            If labelSeparatorIndex <= 0 OrElse labelSeparatorIndex >= remainder.Length - 1 Then
                Continue For
            End If

            Dim relativeCanonical = remainder.Substring(0, labelSeparatorIndex).Trim()
            Dim navLabel = remainder.Substring(labelSeparatorIndex + 1).Trim()
            If navLabel.Length = 0 Then
                Continue For
            End If

            Dim pilotPath As String = Nothing
            Dim canonicalPath As String = Nothing
            If Not PilotPathConfig.TryCombine(pilotRoot, relativePilot, pilotPath) OrElse
                Not PilotPathConfig.TryCombine(globalRoot, relativeCanonical, canonicalPath) Then
                Continue For
            End If

            routes.Add(New PilotRouteMapping With {
                .RelativePilotPath = relativePilot.Trim().Replace("\"c, "/"c),
                .RelativeCanonicalPath = relativeCanonical.Trim().Replace("\"c, "/"c),
                .PilotPath = pilotPath,
                .CanonicalPath = canonicalPath,
                .NavLabel = navLabel
            })
        Next

        Return routes
    End Function

    Public Shared Function TryResolveRoute(
        path As String,
        routesConfig As String,
        pilotRootPath As String,
        globalAdminRootPath As String,
        ByRef route As PilotRouteMapping) As Boolean

        route = Nothing
        If String.IsNullOrWhiteSpace(path) Then
            Return False
        End If

        Dim normalizedPath = path.Trim()
        For Each candidate As PilotRouteMapping In ParseRoutes(routesConfig, pilotRootPath, globalAdminRootPath)
            If String.Equals(normalizedPath, candidate.PilotPath, StringComparison.OrdinalIgnoreCase) Then
                route = candidate
                Return True
            End If
        Next

        Return False
    End Function

    Public Shared Function TryResolveCanonicalPath(
        path As String,
        routesConfig As String,
        pilotRootPath As String,
        globalAdminRootPath As String,
        ByRef canonicalPath As String) As Boolean

        canonicalPath = Nothing
        Dim route As PilotRouteMapping = Nothing
        If Not TryResolveRoute(path, routesConfig, pilotRootPath, globalAdminRootPath, route) Then
            Return False
        End If

        canonicalPath = route.CanonicalPath
        Return True
    End Function

    Public Shared Function TryResolvePilotPathByCanonical(
        canonicalPath As String,
        routesConfig As String,
        pilotRootPath As String,
        globalAdminRootPath As String,
        ByRef pilotPath As String) As Boolean

        pilotPath = Nothing
        If String.IsNullOrWhiteSpace(canonicalPath) Then
            Return False
        End If

        Dim normalizedPath = canonicalPath.Trim()
        For Each candidate As PilotRouteMapping In ParseRoutes(routesConfig, pilotRootPath, globalAdminRootPath)
            If String.Equals(normalizedPath, candidate.CanonicalPath, StringComparison.OrdinalIgnoreCase) Then
                pilotPath = candidate.PilotPath
                Return True
            End If
        Next

        Return False
    End Function

    Public Shared Function IsPilotRoute(
        path As String,
        routesConfig As String,
        pilotRootPath As String,
        globalAdminRootPath As String) As Boolean

        Dim route As PilotRouteMapping = Nothing
        Return TryResolveRoute(path, routesConfig, pilotRootPath, globalAdminRootPath, route)
    End Function

    Public Shared Function IsSafeReturnUrl(
        returnUrl As String,
        routesConfig As String,
        pilotRootPath As String,
        globalAdminRootPath As String) As Boolean

        If String.IsNullOrWhiteSpace(returnUrl) OrElse
            Not returnUrl.StartsWith("/", StringComparison.Ordinal) OrElse
            returnUrl.StartsWith("//", StringComparison.Ordinal) Then
            Return False
        End If

        Dim absoluteUrl As Uri = Nothing
        If Uri.TryCreate(returnUrl, UriKind.Absolute, absoluteUrl) Then
            Return False
        End If

        Dim path = returnUrl.Split("?"c)(0)
        Return IsPilotRoute(path, routesConfig, pilotRootPath, globalAdminRootPath)
    End Function

    Public Shared Function ConstantTimeEquals(leftValue As String, rightValue As String) As Boolean
        Dim leftBytes = Encoding.UTF8.GetBytes(If(leftValue, String.Empty))
        Dim rightBytes = Encoding.UTF8.GetBytes(If(rightValue, String.Empty))
        Dim difference As Integer = leftBytes.Length Xor rightBytes.Length
        Dim length = Math.Max(leftBytes.Length, rightBytes.Length)

        For index As Integer = 0 To length - 1
            Dim leftByte As Byte = If(index < leftBytes.Length, leftBytes(index), CByte(0))
            Dim rightByte As Byte = If(index < rightBytes.Length, rightBytes(index), CByte(0))
            difference = difference Or (leftByte Xor rightByte)
        Next

        Return difference = 0
    End Function
End Class
