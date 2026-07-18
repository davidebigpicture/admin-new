Imports System
Imports System.Collections.Generic
Imports StackExchange.Redis

''' <summary>
''' Stateful session-scoped Redis wrapper that mirrors CacheManager's API.
''' Call Init(prefix) once, then all operations auto-prefix — drop-in replacement for:
'''   Dim session = Server.CreateObject("CacheManager")
'''   session.Init(sessionKey)
''' Becomes:
'''   Dim session As New RedisSession()
'''   session.Init(sessionKey)
''' Everything else stays the same: session("key") = value, value = session("key"), etc.
''' </summary>
Public Class RedisSession

    Private _prefix As String = String.Empty
    Private _expirationSeconds As Integer = RedisService.DEFAULT_SECONDS_TO_EXPIRE

    Private Sub EnsureInitialized()
        If String.IsNullOrEmpty(_prefix) Then
            Throw New InvalidOperationException("no cache key defined - operation not allowed.")
        End If
    End Sub

    ''' <summary>
    ''' Factory: create and init in one call.
    ''' </summary>
    Public Shared Function Create(prefix As String) As RedisSession
        Return RedisService.CreateSession(prefix)
    End Function

    ''' <summary>
    ''' Matches CacheManager.Init(prefix). Sets the namespace for all subsequent operations.
    ''' </summary>
    Public Sub Init(prefix As String)
        _prefix = RedisService.NormalizePrefix(prefix)
    End Sub

    ''' <summary>
    ''' Matches CacheManager.SetDefaultExpiration(days, hours, minutes, seconds).
    ''' Sets the TTL applied to all subsequent writes via the default indexer.
    ''' </summary>
    Public Sub SetDefaultExpiration(days As Integer, hours As Integer, minutes As Integer, seconds As Integer)
        _expirationSeconds = (days * 86400) + (hours * 3600) + (minutes * 60) + seconds
        If _expirationSeconds <= 0 Then _expirationSeconds = RedisService.DEFAULT_SECONDS_TO_EXPIRE
    End Sub

    ''' <summary>
    ''' Matches CacheManager.Add(key, value).
    ''' </summary>
    Public Sub Add(key As String, value As Object)
        EnsureInitialized()
        Item(key) = value
    End Sub

    ''' <summary>
    ''' Matches CacheManager.AddWithExpire(key, value, secondsToExpire).
    ''' </summary>
    Public Sub AddWithExpire(key As String, value As Object, secondsToExpire As Integer)
        EnsureInitialized()
        Dim strValue As String = RedisService.SerializeCacheManagerValue(value)
        Dim pairs As New List(Of KeyValuePair(Of String, String))()
        pairs.Add(New KeyValuePair(Of String, String)(key, strValue))
        RedisService.AddWithExpire(pairs, secondsToExpire, _prefix)
    End Sub

    ''' <summary>
    ''' Matches CacheManager.Get(key).
    ''' </summary>
    Public Function [Get](key As String) As Object
        EnsureInitialized()
        Return Item(key)
    End Function

    ''' <summary>
    ''' Default indexer — matches CacheManager's default property.
    '''   session("key") = "value"   ' write with auto-prefix + TTL
    '''   value = session("key")     ' read with auto-prefix; returns Object(,) for arrays
    ''' Returns Object to match COM behavior: native VB arrays for array values, String for scalars.
    ''' </summary>
    Default Public Property Item(key As String) As Object
        Get
            EnsureInitialized()
            Dim fullKey As String = RedisService.FullKey(_prefix, key)
            Dim result As RedisValue = RedisService.Db.StringGet(CType(fullKey, RedisKey))
            If Not result.HasValue Then Return String.Empty

            Dim rawString As String = result.ToString()
            If RedisService.IsCacheManagerArray(rawString) Then
                Dim deserialized As Object = RedisService.DeserializeCacheManagerValue(rawString)
                If deserialized IsNot Nothing Then Return deserialized
            End If
            Return rawString
        End Get
        Set(value As Object)
            EnsureInitialized()
            AddWithExpire(key, value, _expirationSeconds)
        End Set
    End Property

    ''' <summary>
    ''' String-typed getter for callers that know the value is a scalar string.
    ''' Avoids boxing/casting when you don't need array support.
    ''' </summary>
    Public Function GetString(key As String) As String
        EnsureInitialized()
        Dim fullKey As String = RedisService.FullKey(_prefix, key)
        Dim result As RedisValue = RedisService.Db.StringGet(CType(fullKey, RedisKey))
        If result.HasValue Then Return result.ToString()
        Return String.Empty
    End Function

    ''' <summary>
    ''' Matches CacheManager.GetCacheList(). Returns comma-separated key names (same format as COM).
    ''' </summary>
    Public Function GetCacheList() As String
        EnsureInitialized()
        Dim keys As List(Of String) = RedisService.GetValidatedCacheList(_prefix)
        Return String.Join(",", keys)
    End Function

    ''' <summary>
    ''' Matches CacheManager.Abandon(). Removes all tracked keys + the tracking SET.
    ''' </summary>
    Public Sub Abandon()
        EnsureInitialized()
        RedisService.Abandon(_prefix)
    End Sub

    ''' <summary>
    ''' Matches CacheManager.Remove(key). Removes a single key.
    ''' </summary>
    Public Sub Remove(key As String)
        EnsureInitialized()
        Dim keys As New List(Of String)()
        keys.Add(key)
        RedisService.Remove(keys, _prefix)
    End Sub

    ''' <summary>
    ''' Matches CacheManager.RemoveAll(). Nuclear delete via KEYS pattern.
    ''' </summary>
    Public Sub RemoveAll()
        EnsureInitialized()
        RedisService.RemoveAll(_prefix)
    End Sub

    ''' <summary>
    ''' Matches CacheManager.Exists(key). Checks if a single key exists.
    ''' </summary>
    Public Function Exists(key As String) As Boolean
        EnsureInitialized()
        Dim keys As New List(Of String)()
        keys.Add(key)
        Dim results = RedisService.Exists(keys, _prefix)
        Return results.Count > 0 AndAlso results(0).Exists
    End Function

    ''' <summary>
    ''' Matches CacheManager.SetExpiration(key, seconds) — or uses the default TTL.
    ''' </summary>
    Public Sub SetExpiration(key As String, Optional seconds As Integer = -1)
        EnsureInitialized()
        If seconds < 0 Then seconds = _expirationSeconds
        Dim keys As New List(Of String)()
        keys.Add(key)
        RedisService.SetExpiration(keys, seconds, _prefix)
    End Sub

    ''' <summary>
    ''' Convenience: load all session values into a Dictionary. No CacheManager equivalent
    ''' but useful for bulk operations like saveFormDataSession / logPageSubmit.
    ''' </summary>
    Public Function GetAllAsDictionary(Optional mgetBatchSize As Integer = 1000) As Dictionary(Of String, String)
        EnsureInitialized()
        Return RedisService.GetAllSessionAsDictionary(_prefix, mgetBatchSize)
    End Function

    ''' <summary>
    ''' Expose the bound prefix for callers that need it (e.g., building FORM_ keys).
    ''' </summary>
    Public ReadOnly Property Prefix As String
        Get
            Return _prefix
        End Get
    End Property

    ''' <summary>
    ''' Expose the current TTL in seconds.
    ''' </summary>
    Public ReadOnly Property ExpirationSeconds As Integer
        Get
            Return _expirationSeconds
        End Get
    End Property

End Class