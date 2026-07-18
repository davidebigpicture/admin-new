Imports System
Imports System.Configuration
Imports System.Collections.Generic
Imports System.Linq
Imports StackExchange.Redis

''' <summary>
''' Shared Redis service providing CacheManager-compatible operations via StackExchange.Redis.
''' Used by BatchRedis.ashx (HTTP adapter) and forms_contribute.aspx.vb (direct calls).
''' 
''' Key architecture:
'''   - CacheManager stores keys as lower(prefix:keyname)
'''   - A tracking SET at prefix:cachelist lists all short key names
'''   - We NEVER use SCAN/KEYS to discover keys — always read the tracking SET
''' </summary>
Public Class RedisService

    Private Const FALLBACK_REDIS_CONNECTION As String = "redis-development.albertsonconsulting.com:6379"
    Private Const CACHE_LIST_ADDITIONAL_SECONDS As Integer = 3600
    Public Const DEFAULT_SECONDS_TO_EXPIRE As Integer = 43200  ' 12 hours (matches CacheManager)
    Public Const DEFAULT_MULTI_KEY_BATCH_SIZE As Integer = 1000
    Public Const DEFAULT_MGET_BATCH_SIZE As Integer = DEFAULT_MULTI_KEY_BATCH_SIZE
    Public Const DEFAULT_KEY_DELETE_BATCH_SIZE As Integer = DEFAULT_MULTI_KEY_BATCH_SIZE
    Public Const DEFAULT_KEY_EXISTS_BATCH_SIZE As Integer = DEFAULT_MULTI_KEY_BATCH_SIZE
    Public Const DEFAULT_KEY_EXPIRE_BATCH_SIZE As Integer = DEFAULT_MULTI_KEY_BATCH_SIZE

    Private Const EXISTS_BATCH_LUA As String = "local result = {} for i = 1, #KEYS do result[i] = redis.call('exists', KEYS[i]) end return result"
    Private Const EXPIRE_BATCH_LUA As String = "local ttl = tonumber(ARGV[1]) local count = 0 for i = 1, #KEYS do if redis.call('expire', KEYS[i], ttl) == 1 then count = count + 1 end end return count"

    Private Shared _redis As ConnectionMultiplexer = Nothing
    Private Shared ReadOnly _syncRoot As New Object()

    ''' <summary>
    ''' Thread-safe lazy singleton for the Redis connection.
    ''' </summary>
    Private Shared ReadOnly Property Connection As ConnectionMultiplexer
        Get
            If _redis Is Nothing Then
                SyncLock _syncRoot
                    If _redis Is Nothing Then
                        Dim options As ConfigurationOptions = ConfigurationOptions.Parse(GetRedisConnectionString())
                        options.AbortOnConnectFail = False
                        _redis = ConnectionMultiplexer.Connect(options)
                    End If
                End SyncLock
            End If
            Return _redis
        End Get
    End Property

    ''' <summary>
    ''' The default Redis database instance.
    ''' </summary>
    Public Shared ReadOnly Property Db As IDatabase
        Get
            Return Connection.GetDatabase()
        End Get
    End Property

    ''' <summary>
    ''' The first Redis server endpoint (for server-level commands).
    ''' </summary>
    Public Shared ReadOnly Property Server As IServer
        Get
            Dim endpoint = Connection.GetEndPoints()(0)
            Return Connection.GetServer(endpoint)
        End Get
    End Property

    ' ──────────────────────────────────────────────
    '  Key helpers (CacheManager compatibility)
    ' ──────────────────────────────────────────────

    ''' <summary>
    ''' Build a fully-qualified Redis key matching CacheManager.GenerateFullKey().
    ''' </summary>
    Public Shared Function FullKey(prefix As String, key As String) As String
        Return (NormalizePrefix(prefix) & ":" & NormalizeShortKey(key)).ToLowerInvariant()
    End Function

    ''' <summary>
    ''' Return the cache list tracking SET key for a prefix.
    ''' </summary>
    Public Shared Function CacheListKey(prefix As String) As String
        Return NormalizePrefix(prefix) & ":cachelist"
    End Function

    ''' <summary>
    ''' Factory for a stateful session-scoped wrapper that mirrors CacheManager's Init() model.
    ''' </summary>
    Public Shared Function CreateSession(Optional prefix As String = Nothing) As RedisSession
        Dim session As New RedisSession()
        If Not String.IsNullOrWhiteSpace(prefix) Then
            session.Init(prefix)
        End If
        Return session
    End Function

    Private Shared Function GetRedisConnectionString() As String
        Dim configured As String = ConfigurationManager.AppSettings("RedisConnectionPrimary")
        If Not String.IsNullOrWhiteSpace(configured) Then
            Return configured.Trim()
        End If

        configured = ConfigurationManager.AppSettings("RedisConnection")
        If Not String.IsNullOrWhiteSpace(configured) Then
            Return configured.Trim()
        End If

        Return FALLBACK_REDIS_CONNECTION
    End Function

    Public Shared Function NormalizePrefix(prefix As String) As String
        If prefix Is Nothing Then Return String.Empty
        Return prefix.Trim().TrimEnd(":"c)
    End Function

    Public Shared Function NormalizeShortKey(key As String) As String
        If key Is Nothing Then Return String.Empty
        Return key.Trim().ToLowerInvariant()
    End Function

    Private Shared Function ResolveKey(key As String, Optional prefix As String = Nothing) As RedisKey
        prefix = NormalizePrefix(prefix)

        If String.IsNullOrWhiteSpace(key) Then
            Throw New ArgumentException("key is required", "key")
        End If

        If String.IsNullOrEmpty(prefix) Then
            Return CType(NormalizeShortKey(key), RedisKey)
        End If

        Return CType(FullKey(prefix, key), RedisKey)
    End Function

    Private Shared Function ToRedisValue(value As String) As RedisValue
        Return CType(If(value, String.Empty), RedisValue)
    End Function

    Private Shared Function ToRedisValues(values As IEnumerable(Of String)) As RedisValue()
        If values Is Nothing Then
            Return New RedisValue() {}
        End If

        Return values.Select(Function(v) ToRedisValue(v)).ToArray()
    End Function

    Private Shared Function ToRedisKeys(keys As IEnumerable(Of String), Optional prefix As String = Nothing) As RedisKey()
        If keys Is Nothing Then
            Return New RedisKey() {}
        End If

        prefix = NormalizePrefix(prefix)
        Return keys.Select(Function(k) ResolveKey(k, prefix)).ToArray()
    End Function

    Private Shared Sub TrackKey(prefix As String, key As String)
        TrackKeys(prefix, New String() {key})
    End Sub

    Private Shared Sub UntrackKey(prefix As String, key As String)
        UntrackKeys(prefix, New String() {key})
    End Sub

    Private Shared Sub ApplyExpiration(redisKey As RedisKey, seconds As Integer)
        If seconds > 0 Then
            Db.KeyExpire(redisKey, TimeSpan.FromSeconds(seconds))
        End If
    End Sub

    Private Shared Sub UntrackIfMissing(prefix As String, key As String, redisKey As RedisKey)
        prefix = NormalizePrefix(prefix)
        If String.IsNullOrEmpty(prefix) Then Return

        If Not Db.KeyExists(redisKey) Then
            UntrackKey(prefix, key)
        End If
    End Sub

    Private Shared Sub TrackKeys(prefix As String, keys As IEnumerable(Of String), Optional cacheListSecondsToExpire As Integer = 0)
        prefix = NormalizePrefix(prefix)
        If String.IsNullOrEmpty(prefix) OrElse keys Is Nothing Then Return

        Dim shortKeys As List(Of String) = keys.Where(Function(k) Not String.IsNullOrWhiteSpace(k)).Select(Function(k) NormalizeShortKey(k)).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
        If shortKeys.Count = 0 Then Return

        Dim cacheList As RedisKey = CType(CacheListKey(prefix), RedisKey)
        Db.SetAdd(cacheList, ToRedisValues(shortKeys))

        If cacheListSecondsToExpire > 0 Then
            Db.KeyExpire(cacheList, TimeSpan.FromSeconds(cacheListSecondsToExpire))
        End If
    End Sub

    Private Shared Sub UntrackKeys(prefix As String, keys As IEnumerable(Of String))
        prefix = NormalizePrefix(prefix)
        If String.IsNullOrEmpty(prefix) OrElse keys Is Nothing Then Return

        Dim shortKeys As List(Of String) = keys.Where(Function(k) Not String.IsNullOrWhiteSpace(k)).Select(Function(k) NormalizeShortKey(k)).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
        If shortKeys.Count = 0 Then Return

        Dim cacheList As RedisKey = CType(CacheListKey(prefix), RedisKey)
        Db.SetRemove(cacheList, ToRedisValues(shortKeys))
    End Sub

    Private Shared Function SanitizeBatchSize(requestedBatchSize As Integer, fallbackBatchSize As Integer) As Integer
        If requestedBatchSize <= 0 Then
            Return fallbackBatchSize
        End If

        Return requestedBatchSize
    End Function

    Private Shared Function DeleteKeysInChunks(keys As IList(Of RedisKey), Optional requestedBatchSize As Integer = DEFAULT_KEY_DELETE_BATCH_SIZE) As Long
        If keys Is Nothing OrElse keys.Count = 0 Then Return 0

        Dim batchSize As Integer = SanitizeBatchSize(requestedBatchSize, DEFAULT_KEY_DELETE_BATCH_SIZE)
        Dim removed As Long = 0

        For batchStart As Integer = 0 To keys.Count - 1 Step batchSize
            Dim currentBatchSize As Integer = Math.Min(batchSize, keys.Count - batchStart)
            Dim batchKeys(currentBatchSize - 1) As RedisKey

            For i As Integer = 0 To currentBatchSize - 1
                batchKeys(i) = keys(batchStart + i)
            Next

            removed += Db.KeyDelete(batchKeys)
        Next

        Return removed
    End Function

    Private Shared Function KeyExistsInChunks(keys As IList(Of RedisKey), requestedBatchSize As Integer) As List(Of Boolean)
        Dim results As New List(Of Boolean)()
        If keys Is Nothing OrElse keys.Count = 0 Then Return results

        Dim batchSize As Integer = SanitizeBatchSize(requestedBatchSize, DEFAULT_KEY_EXISTS_BATCH_SIZE)
        results.Capacity = keys.Count

        For batchStart As Integer = 0 To keys.Count - 1 Step batchSize
            Dim currentBatchSize As Integer = Math.Min(batchSize, keys.Count - batchStart)
            Dim batchKeys(currentBatchSize - 1) As RedisKey

            For i As Integer = 0 To currentBatchSize - 1
                batchKeys(i) = keys(batchStart + i)
            Next

            Dim batchResult As RedisResult = Db.ScriptEvaluate(EXISTS_BATCH_LUA, batchKeys, New RedisValue() {})
            Dim existsResults As RedisResult() = CType(batchResult, RedisResult())

            For i As Integer = 0 To existsResults.Length - 1
                results.Add(Convert.ToInt64(existsResults(i).ToString()) <> 0)
            Next
        Next

        Return results
    End Function

    Private Shared Function ExpireKeysInChunks(keys As IList(Of RedisKey), seconds As Integer, requestedBatchSize As Integer) As Integer
        If keys Is Nothing OrElse keys.Count = 0 Then Return 0

        Dim batchSize As Integer = SanitizeBatchSize(requestedBatchSize, DEFAULT_KEY_EXPIRE_BATCH_SIZE)
        Dim setCount As Integer = 0
        Dim args As RedisValue() = New RedisValue() {CType(seconds, RedisValue)}

        For batchStart As Integer = 0 To keys.Count - 1 Step batchSize
            Dim currentBatchSize As Integer = Math.Min(batchSize, keys.Count - batchStart)
            Dim batchKeys(currentBatchSize - 1) As RedisKey

            For i As Integer = 0 To currentBatchSize - 1
                batchKeys(i) = keys(batchStart + i)
            Next

            Dim batchResult As RedisResult = Db.ScriptEvaluate(EXPIRE_BATCH_LUA, batchKeys, args)
            setCount += Convert.ToInt32(batchResult.ToString())
        Next

        Return setCount
    End Function

    ' ──────────────────────────────────────────────
    '  Core operations
    ' ──────────────────────────────────────────────

    ''' <summary>
    ''' Batch write keys using MSET. No TTL.
    ''' Equivalent to CacheManager.Add(key, value) in batch.
    ''' </summary>
    ''' <param name="pairs">List of key/value tuples</param>
    ''' <param name="prefix">Optional CacheManager-style prefix</param>
    ''' <returns>True if MSET succeeded</returns>
    Public Shared Function Add(pairs As List(Of KeyValuePair(Of String, String)), Optional prefix As String = Nothing) As Boolean
        If pairs Is Nothing OrElse pairs.Count = 0 Then Return False

        prefix = NormalizePrefix(prefix)
        Dim usePrefix As Boolean = Not String.IsNullOrEmpty(prefix)
        Dim kvPairs(pairs.Count - 1) As KeyValuePair(Of RedisKey, RedisValue)
        Dim trackedKeys As New List(Of String)(pairs.Count)

        For i As Integer = 0 To pairs.Count - 1
            Dim key As String = pairs(i).Key
            If usePrefix Then
                trackedKeys.Add(key)
                key = FullKey(prefix, key)
            End If
            kvPairs(i) = New KeyValuePair(Of RedisKey, RedisValue)(CType(key, RedisKey), CType(If(pairs(i).Value, String.Empty), RedisValue))
        Next

        Dim success As Boolean = Db.StringSet(kvPairs)
        If success AndAlso usePrefix Then
            TrackKeys(prefix, trackedKeys)
        End If
        Return success
    End Function

    ''' <summary>
    ''' Batch write keys with per-key TTL using a pipelined batch.
    ''' Equivalent to CacheManager.AddWithExpire(key, value, seconds) in batch.
    ''' </summary>
    Public Shared Sub AddWithExpire(pairs As List(Of KeyValuePair(Of String, String)), Optional seconds As Integer = DEFAULT_SECONDS_TO_EXPIRE, Optional prefix As String = Nothing)
        If pairs Is Nothing OrElse pairs.Count = 0 Then Return

        If seconds <= 0 Then
            Add(pairs, prefix)
            Return
        End If

        prefix = NormalizePrefix(prefix)
        Dim usePrefix As Boolean = Not String.IsNullOrEmpty(prefix)
        Dim ttl As TimeSpan = TimeSpan.FromSeconds(seconds)
        Dim trackedKeys As New List(Of String)(pairs.Count)

        For Each pair In pairs
            Dim key As String = pair.Key
            If usePrefix Then
                trackedKeys.Add(key)
                key = FullKey(prefix, key)
            End If
            Db.StringSet(CType(key, RedisKey), CType(If(pair.Value, String.Empty), RedisValue), ttl)
        Next

        If usePrefix Then
            TrackKeys(prefix, trackedKeys, seconds + CACHE_LIST_ADDITIONAL_SECONDS)
        End If
    End Sub

    ''' <summary>
    ''' Batch read keys using MGET.
    ''' Equivalent to CacheManager.Get(key) in batch.
    ''' </summary>
    ''' <param name="keys">Fully-qualified Redis keys</param>
    ''' <returns>List of (key, exists, value) tuples</returns>
    Public Shared Function [Get](keys As List(Of String)) As List(Of KeyResult)
        Dim results As New List(Of KeyResult)()
        If keys Is Nothing OrElse keys.Count = 0 Then Return results

        Dim redisKeys(keys.Count - 1) As RedisKey
        For i As Integer = 0 To keys.Count - 1
            redisKeys(i) = CType(keys(i), RedisKey)
        Next

        Dim values As RedisValue() = Db.StringGet(redisKeys)

        For i As Integer = 0 To values.Length - 1
            results.Add(New KeyResult() With {
                .Key = keys(i),
                .Exists = values(i).HasValue,
                .Value = If(values(i).HasValue, values(i).ToString(), Nothing)
            })
        Next

        Return results
    End Function

    ''' <summary>
    ''' Batch delete keys using DEL.
    ''' Equivalent to CacheManager.Remove(key) in batch.
    ''' </summary>
    ''' <param name="keys">Short key names</param>
    ''' <param name="prefix">Optional CacheManager-style prefix</param>
    ''' <returns>Number of keys actually removed</returns>
    Public Shared Function Remove(keys As List(Of String), Optional prefix As String = Nothing) As Long
        If keys Is Nothing OrElse keys.Count = 0 Then Return 0

        prefix = NormalizePrefix(prefix)
        Dim usePrefix As Boolean = Not String.IsNullOrEmpty(prefix)
        Dim redisKeys(keys.Count - 1) As RedisKey
        For i As Integer = 0 To keys.Count - 1
            If usePrefix Then
                redisKeys(i) = CType(FullKey(prefix, keys(i)), RedisKey)
            Else
                redisKeys(i) = CType(keys(i).ToLowerInvariant(), RedisKey)
            End If
        Next

        Dim removed As Long = DeleteKeysInChunks(redisKeys)
        If usePrefix Then
            UntrackKeys(prefix, keys)
        End If
        Return removed
    End Function

    ''' <summary>
    ''' Remove keys matching simple prefix patterns by filtering the tracking SET.
    ''' No SCAN needed.
    ''' </summary>
    ''' <returns>List of (pattern, found, removed) results and total removed count</returns>
    Public Shared Function RemoveByPatterns(patterns As List(Of String), prefix As String) As PatternRemoveResult
        Dim result As New PatternRemoveResult()
        If patterns Is Nothing OrElse patterns.Count = 0 Then Return result

        prefix = NormalizePrefix(prefix)
        If String.IsNullOrEmpty(prefix) Then Throw New ArgumentException("prefix is required", "prefix")

        Dim fullPrefix As String = prefix.ToLowerInvariant() & ":"
        Dim setMembers As RedisValue() = Db.SetMembers(CType(CacheListKey(prefix), RedisKey))
        Dim removedShortKeys As New List(Of String)()
        Dim normalizedMembers As New List(Of KeyValuePair(Of String, String))(setMembers.Length)

        For Each member As RedisValue In setMembers
            Dim shortKey As String = member.ToString()
            normalizedMembers.Add(New KeyValuePair(Of String, String)(shortKey, shortKey.ToLowerInvariant()))
        Next

        For Each pattern As String In patterns
            Dim patternLower As String = If(pattern, String.Empty).ToLowerInvariant()
            Dim patternPrefix As String = If(patternLower.EndsWith("*"), patternLower.Substring(0, patternLower.Length - 1), patternLower)

            Dim matchingKeys As New List(Of RedisKey)()
            Dim matchingShortKeys As New List(Of String)()
            For Each member As KeyValuePair(Of String, String) In normalizedMembers
                Dim memberStr As String = member.Value
                If patternPrefix.Length = 0 OrElse memberStr.StartsWith(patternPrefix) Then
                    matchingKeys.Add(CType(fullPrefix & memberStr, RedisKey))
                    matchingShortKeys.Add(member.Key)
                End If
            Next

            Dim removed As Long = 0
            If matchingKeys.Count > 0 Then
                removed = DeleteKeysInChunks(matchingKeys)
                removedShortKeys.AddRange(matchingShortKeys)
            End If

            result.PatternResults.Add(New PatternResult() With {
                .Pattern = pattern,
                .Found = matchingKeys.Count,
                .Removed = removed
            })
            result.TotalRemoved += removed
        Next

        If removedShortKeys.Count > 0 Then
            UntrackKeys(prefix, removedShortKeys)
        End If

        Return result
    End Function

    ''' <summary>
    ''' Remove ALL keys in a namespace using server-side Lua script.
    ''' Matches CacheManager.RemoveAll() — uses KEYS inside Lua. Nuclear option.
    ''' Prefer Abandon() when possible.
    ''' </summary>
    Public Shared Function RemoveAll(prefix As String) As Integer
        prefix = NormalizePrefix(prefix)
        Dim mask As String = prefix.ToLowerInvariant() & ":*"
        Dim luaScript As String = "local keys = redis.call('keys', ARGV[1]) for i=1,#keys,5000 do redis.call('del', unpack(keys, i, math.min(i+4999, #keys))) end return #keys"
        Dim removed As Object = Db.ScriptEvaluate(luaScript, Nothing, New RedisValue() {CType(mask, RedisValue)})
        Db.KeyDelete(CType(CacheListKey(prefix), RedisKey))
        Return Convert.ToInt32(removed.ToString())
    End Function

    ''' <summary>
    ''' Remove all tracked keys + the tracking SET itself.
    ''' Faster than RemoveAll because it reads the SET instead of scanning.
    ''' Equivalent to CacheManager.Abandon() but batched (COM does serial Remove).
    ''' </summary>
    ''' <returns>Number of keys deleted (including the tracking SET)</returns>
    Public Shared Function Abandon(prefix As String) As Integer
        prefix = NormalizePrefix(prefix)
        Dim clKey As String = CacheListKey(prefix)
        Dim setMembers As RedisValue() = Db.SetMembers(CType(clKey, RedisKey))
        Dim fullPrefix As String = prefix.ToLowerInvariant() & ":"

        If setMembers.Length > 0 Then
            Dim keysToDelete As New List(Of RedisKey)(setMembers.Length + 1)
            For i As Integer = 0 To setMembers.Length - 1
                keysToDelete.Add(CType(fullPrefix & setMembers(i).ToString().ToLowerInvariant(), RedisKey))
            Next
            keysToDelete.Add(CType(clKey, RedisKey))
            DeleteKeysInChunks(keysToDelete)
        Else
            Db.KeyDelete(CType(clKey, RedisKey))
        End If

        Return setMembers.Length + 1
    End Function

    ''' <summary>
    ''' Check existence of multiple keys using chunked Lua-backed EXISTS calls.
    ''' Equivalent to CacheManager.Exists(key) in batch.
    ''' </summary>
    Public Shared Function Exists(keys As List(Of String), Optional prefix As String = Nothing, Optional batchSize As Integer = DEFAULT_KEY_EXISTS_BATCH_SIZE) As List(Of KeyExistsResult)
        Dim results As New List(Of KeyExistsResult)()
        If keys Is Nothing OrElse keys.Count = 0 Then Return results

        prefix = NormalizePrefix(prefix)
        Dim usePrefix As Boolean = Not String.IsNullOrEmpty(prefix)
        Dim redisKeys As RedisKey() = If(usePrefix, ToRedisKeys(keys, prefix), ToRedisKeys(keys))
        Dim existsResults As List(Of Boolean) = KeyExistsInChunks(redisKeys, batchSize)
        results.Capacity = keys.Count

        For i As Integer = 0 To keys.Count - 1
            results.Add(New KeyExistsResult() With {
                .Key = keys(i),
                .Exists = existsResults(i)
            })
        Next

        Return results
    End Function

    ''' <summary>
    ''' Set TTL on multiple keys using chunked Lua-backed EXPIRE calls.
    ''' Equivalent to CacheManager.SetExpiration(key, seconds) in batch.
    ''' </summary>
    ''' <returns>Number of keys where the TTL was actually set</returns>
    Public Shared Function SetExpiration(keys As List(Of String), Optional seconds As Integer = DEFAULT_SECONDS_TO_EXPIRE, Optional prefix As String = Nothing, Optional batchSize As Integer = DEFAULT_KEY_EXPIRE_BATCH_SIZE) As Integer
        If keys Is Nothing OrElse keys.Count = 0 Then Return 0

        prefix = NormalizePrefix(prefix)
        Dim usePrefix As Boolean = Not String.IsNullOrEmpty(prefix)
        Dim redisKeys As RedisKey() = If(usePrefix, ToRedisKeys(keys, prefix), ToRedisKeys(keys))
        Return ExpireKeysInChunks(redisKeys, seconds, batchSize)
    End Function

    ''' <summary>
    ''' Get all key names from the tracking SET (no existence validation).
    ''' Equivalent to CacheManager.GetCacheList() but returns a List instead of CSV.
    ''' Faster because it skips per-key Exists checks that the COM version does.
    ''' </summary>
    Public Shared Function GetCacheList(prefix As String) As List(Of String)
        prefix = NormalizePrefix(prefix)
        Dim clKey As String = CacheListKey(prefix)
        Dim setMembers As RedisValue() = Db.SetMembers(CType(clKey, RedisKey))
        Dim keys As New List(Of String)(setMembers.Length)
        For Each member As RedisValue In setMembers
            keys.Add(member.ToString())
        Next
        Return keys
    End Function

    ''' <summary>
    ''' CacheManager-compatible cache list: returns only keys that still exist.
    ''' Optionally removes stale names from the tracking SET while validating.
    ''' </summary>
    Public Shared Function GetValidatedCacheList(prefix As String, Optional cleanupStale As Boolean = True) As List(Of String)
        Dim keys As List(Of String) = GetCacheList(prefix)
        If keys.Count = 0 Then Return keys

        Dim existsResults As List(Of KeyExistsResult) = Exists(keys, prefix)
        Dim liveKeys As New List(Of String)(existsResults.Count)
        Dim staleKeys As New List(Of String)()

        For Each item In existsResults
            If item.Exists Then
                liveKeys.Add(item.Key)
            Else
                staleKeys.Add(item.Key)
            End If
        Next

        If cleanupStale AndAlso staleKeys.Count > 0 Then
            UntrackKeys(prefix, staleKeys)
        End If

        Return liveKeys
    End Function

    ''' <summary>
    ''' Get all tracked key names AND values for a namespace in one call.
    ''' Combines SetMembers + chunked MGET. No CacheManager equivalent.
    ''' </summary>
    ''' <param name="prefix">Session/namespace prefix (e.g. session key)</param>
    ''' <param name="mgetBatchSize">Max keys per MGET call (default DEFAULT_MGET_BATCH_SIZE)</param>
    ''' <returns>List of (shortKey, exists, value) results</returns>
    Public Shared Function GetAllSession(prefix As String, Optional mgetBatchSize As Integer = DEFAULT_MGET_BATCH_SIZE) As List(Of KeyResult)
        Dim results As New List(Of KeyResult)()
        prefix = NormalizePrefix(prefix)
        Dim fullPrefix As String = prefix.ToLowerInvariant() & ":"
        Dim clKey As String = CacheListKey(prefix)
        Dim batchSize As Integer = SanitizeBatchSize(mgetBatchSize, DEFAULT_MGET_BATCH_SIZE)

        Dim setMembers As RedisValue() = Db.SetMembers(CType(clKey, RedisKey))
        If setMembers.Length = 0 Then Return results
        results.Capacity = setMembers.Length

        For batchStart As Integer = 0 To setMembers.Length - 1 Step batchSize
            Dim currentBatchSize As Integer = Math.Min(batchSize, setMembers.Length - batchStart)
            Dim batchKeys(currentBatchSize - 1) As RedisKey

            For i As Integer = 0 To currentBatchSize - 1
                batchKeys(i) = CType(fullPrefix & setMembers(batchStart + i).ToString().ToLowerInvariant(), RedisKey)
            Next

            Dim batchValues As RedisValue() = Db.StringGet(batchKeys)

            For i As Integer = 0 To currentBatchSize - 1
                Dim memberIndex As Integer = batchStart + i
                results.Add(New KeyResult() With {
                    .Key = setMembers(memberIndex).ToString(),
                    .Exists = batchValues(i).HasValue,
                    .Value = If(batchValues(i).HasValue, batchValues(i).ToString(), Nothing)
                })
            Next
        Next

        Return results
    End Function

    ''' <summary>
    ''' Load all session values into a Dictionary(Of String, String) keyed by short name.
    ''' Convenience method for forms_contribute.aspx.vb Phase 1.
    ''' </summary>
    Public Shared Function GetAllSessionAsDictionary(prefix As String, Optional mgetBatchSize As Integer = DEFAULT_MGET_BATCH_SIZE) As Dictionary(Of String, String)
        Dim items = GetAllSession(prefix, mgetBatchSize)
        Dim dict As New Dictionary(Of String, String)(items.Count, StringComparer.OrdinalIgnoreCase)
        For Each item In items
            dict(item.Key) = If(item.Value, "")
        Next
        Return dict
    End Function

    ' ──────────────────────────────────────────────
    '  CacheManager serialization compatibility
    ' ──────────────────────────────────────────────

    ''' <summary>
    ''' Detects whether a raw Redis string value is a CacheManager-serialized array.
    ''' CacheManager (RedisComClient) stores arrays as JSON via a "MyTable" class with
    ''' properties "ArrayCollumns" (2D) and "ArrayCollumn" (1D). The presence of
    ''' "ArrayCollumn" in the string is the same marker CacheManager.Get() uses internally.
    ''' </summary>
    Public Shared Function IsCacheManagerArray(value As String) As Boolean
        If String.IsNullOrEmpty(value) Then Return False
        Return value.IndexOf("ArrayCollumn", StringComparison.OrdinalIgnoreCase) >= 0
    End Function

    Private Class CacheManagerSerializedArray
        Public Property ArrayCollumns As List(Of List(Of Object))
        Public Property ArrayCollumn As List(Of Object)
    End Class

    Private Shared Function NormalizeCacheManagerArrayValue(value As Object) As Object
        If value Is Nothing OrElse value Is DBNull.Value Then
            Return Nothing
        End If
        Return value
    End Function

    Private Shared Function NormalizeCacheManagerDeserializedValue(value As Object) As Object
        If value Is Nothing OrElse value Is DBNull.Value Then
            Return Nothing
        End If

        If TypeOf value Is Long Then
            Dim longValue As Long = CLng(value)
            If longValue >= Integer.MinValue AndAlso longValue <= Integer.MaxValue Then
                Return CInt(longValue)
            End If
        End If

        Return value
    End Function

    ''' <summary>
    ''' Serializes a scalar or array value using CacheManager-compatible rules.
    ''' Scalars are stored as strings; 1D/2D arrays are stored as MyTable-compatible JSON.
    ''' </summary>
    Public Shared Function SerializeCacheManagerValue(value As Object) As String
        If value Is Nothing Then Return String.Empty

        Dim arrayValue As Array = TryCast(value, Array)
        If arrayValue IsNot Nothing Then
            Return SerializeCacheManagerArray(arrayValue)
        End If

        Return Convert.ToString(value)
    End Function

    ''' <summary>
    ''' Serializes a 1D or 2D array in the same JSON shape used by CacheManager's MyTable class.
    ''' </summary>
    Public Shared Function SerializeCacheManagerArray(value As Array) As String
        If value Is Nothing Then Return String.Empty

        Dim payload As New CacheManagerSerializedArray() With {
            .ArrayCollumns = New List(Of List(Of Object))(),
            .ArrayCollumn = New List(Of Object)()
        }

        Select Case value.Rank
            Case 1
                For index As Integer = value.GetLowerBound(0) To value.GetUpperBound(0)
                    payload.ArrayCollumn.Add(NormalizeCacheManagerArrayValue(value.GetValue(index)))
                Next
            Case 2
                For col As Integer = value.GetLowerBound(0) To value.GetUpperBound(0)
                    Dim colData As New List(Of Object)()
                    For row As Integer = value.GetLowerBound(1) To value.GetUpperBound(1)
                        colData.Add(NormalizeCacheManagerArrayValue(value.GetValue(col, row)))
                    Next
                    payload.ArrayCollumns.Add(colData)
                Next
            Case Else
                Throw New NotSupportedException("CacheManager-compatible serialization supports only 1D and 2D arrays.")
        End Select

        Return New System.Web.Script.Serialization.JavaScriptSerializer().Serialize(payload)
    End Function

    ''' <summary>
    ''' Deserializes a CacheManager value into either a String, a 1D Object array, or a 2D Object array.
    ''' </summary>
    Public Shared Function DeserializeCacheManagerValue(value As String) As Object
        If Not IsCacheManagerArray(value) Then Return value

        Try
            Dim jss As New System.Web.Script.Serialization.JavaScriptSerializer()
            Dim parsed As Dictionary(Of String, Object) = jss.Deserialize(Of Dictionary(Of String, Object))(value)

            Dim columns As System.Collections.IList = Nothing
            If parsed.ContainsKey("ArrayCollumns") Then
                columns = TryCast(parsed("ArrayCollumns"), System.Collections.IList)
            End If

            If columns IsNot Nothing AndAlso columns.Count > 0 Then
                Dim firstCol As System.Collections.IList = TryCast(columns(0), System.Collections.IList)
                If firstCol Is Nothing Then Return Nothing

                Dim numCols As Integer = columns.Count
                Dim numRows As Integer = firstCol.Count
                Dim result As Array = Array.CreateInstance(GetType(Object), New Integer() {numCols, numRows})

                For col As Integer = 0 To numCols - 1
                    Dim colData As System.Collections.IList = TryCast(columns(col), System.Collections.IList)
                    If colData IsNot Nothing Then
                        For row As Integer = 0 To Math.Min(colData.Count, numRows) - 1
                            result.SetValue(NormalizeCacheManagerDeserializedValue(colData(row)), col, row)
                        Next
                    End If
                Next

                Return CType(result, Object(,))
            End If

            Dim items As System.Collections.IList = Nothing
            If parsed.ContainsKey("ArrayCollumn") Then
                items = TryCast(parsed("ArrayCollumn"), System.Collections.IList)
            End If

            If items IsNot Nothing Then
                Dim result As Array = Array.CreateInstance(GetType(Object), items.Count)
                For index As Integer = 0 To items.Count - 1
                    result.SetValue(NormalizeCacheManagerDeserializedValue(items(index)), index)
                Next
                Return CType(result, Object())
            End If

            Return Nothing
        Catch
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' Deserializes a CacheManager-serialized array string into a 2D Object array.
    ''' CacheManager serializes arrays as JSON: {"ArrayCollumns":[[col0],[col1],...],"ArrayCollumn":[]}
    ''' where ArrayCollumns(col)(row) maps to result(col, row).
    ''' Returns Nothing if the string is not a valid CacheManager array or deserialization fails.
    ''' </summary>
    Public Shared Function DeserializeCacheManagerArray(value As String) As Object(,)
        Dim deserialized As Object = DeserializeCacheManagerValue(value)
        Dim arrayValue As Array = TryCast(deserialized, Array)
        If arrayValue Is Nothing OrElse arrayValue.Rank <> 2 Then Return Nothing
        Return CType(deserialized, Object(,))
    End Function

    ' ──────────────────────────────────────────────
    '  Hash operations
    ' ──────────────────────────────────────────────

    ''' <summary>
    ''' Set multiple hash fields on a key.
    ''' Mirrors IDatabase.HashSet(key, HashEntry()).
    ''' </summary>
    Public Shared Sub HashSet(key As String, fields As IEnumerable(Of HashEntry), Optional prefix As String = Nothing, Optional seconds As Integer = 0)
        If fields Is Nothing Then Return

        Dim entries As HashEntry() = fields.ToArray()
        If entries.Length = 0 Then Return

        prefix = NormalizePrefix(prefix)
        Dim redisKey As RedisKey = ResolveKey(key, prefix)

        Db.HashSet(redisKey, entries)
        ApplyExpiration(redisKey, seconds)

        If Not String.IsNullOrEmpty(prefix) Then
            TrackKey(prefix, key)
        End If
    End Sub

    ''' <summary>
    ''' Set a single hash field.
    ''' Mirrors IDatabase.HashSet(key, field, value, when).
    ''' </summary>
    Public Shared Function HashSet(key As String, field As String, value As String, Optional prefix As String = Nothing, Optional condition As StackExchange.Redis.When = StackExchange.Redis.When.Always, Optional seconds As Integer = 0) As Boolean
        prefix = NormalizePrefix(prefix)
        Dim redisKey As RedisKey = ResolveKey(key, prefix)
        Dim wasAdded As Boolean = Db.HashSet(redisKey, CType(field, RedisValue), ToRedisValue(value), condition)

        If condition = StackExchange.Redis.When.Always OrElse wasAdded Then
            ApplyExpiration(redisKey, seconds)

            If Not String.IsNullOrEmpty(prefix) Then
                TrackKey(prefix, key)
            End If
        End If

        Return wasAdded
    End Function

    ''' <summary>
    ''' Get a single hash field.
    ''' Mirrors IDatabase.HashGet(key, field).
    ''' </summary>
    Public Shared Function HashGet(key As String, field As String, Optional prefix As String = Nothing) As RedisValue
        Return Db.HashGet(ResolveKey(key, prefix), CType(field, RedisValue))
    End Function

    ''' <summary>
    ''' Get multiple hash fields.
    ''' Mirrors IDatabase.HashGet(key, fields).
    ''' </summary>
    Public Shared Function HashGet(key As String, fields As IEnumerable(Of String), Optional prefix As String = Nothing) As RedisValue()
        Dim redisFields As RedisValue() = ToRedisValues(fields)
        If redisFields.Length = 0 Then Return New RedisValue() {}

        Return Db.HashGet(ResolveKey(key, prefix), redisFields)
    End Function

    ''' <summary>
    ''' Get all fields for a hash key.
    ''' Mirrors IDatabase.HashGetAll(key).
    ''' </summary>
    Public Shared Function HashGetAll(key As String, Optional prefix As String = Nothing) As HashEntry()
        Return Db.HashGetAll(ResolveKey(key, prefix))
    End Function

    ''' <summary>
    ''' Delete one or more hash fields.
    ''' Mirrors IDatabase.HashDelete.
    ''' </summary>
    Public Shared Function HashDelete(key As String, fields As IEnumerable(Of String), Optional prefix As String = Nothing) As Long
        Dim redisFields As RedisValue() = ToRedisValues(fields)
        If redisFields.Length = 0 Then Return 0

        prefix = NormalizePrefix(prefix)
        Dim redisKey As RedisKey = ResolveKey(key, prefix)
        Dim removed As Long = Db.HashDelete(redisKey, redisFields)

        If removed > 0 Then
            UntrackIfMissing(prefix, key, redisKey)
        End If

        Return removed
    End Function

    ''' <summary>
    ''' Delete a single hash field.
    ''' Mirrors IDatabase.HashDelete(key, field).
    ''' </summary>
    Public Shared Function HashDelete(key As String, field As String, Optional prefix As String = Nothing) As Boolean
        prefix = NormalizePrefix(prefix)
        Dim redisKey As RedisKey = ResolveKey(key, prefix)
        Dim removed As Boolean = Db.HashDelete(redisKey, CType(field, RedisValue))

        If removed Then
            UntrackIfMissing(prefix, key, redisKey)
        End If

        Return removed
    End Function

    ''' <summary>
    ''' Check whether a hash field exists.
    ''' Mirrors IDatabase.HashExists(key, field).
    ''' </summary>
    Public Shared Function HashExists(key As String, field As String, Optional prefix As String = Nothing) As Boolean
        Return Db.HashExists(ResolveKey(key, prefix), CType(field, RedisValue))
    End Function

    ''' <summary>
    ''' Get the number of fields in a hash.
    ''' Mirrors IDatabase.HashLength(key).
    ''' </summary>
    Public Shared Function HashLength(key As String, Optional prefix As String = Nothing) As Long
        Return Db.HashLength(ResolveKey(key, prefix))
    End Function

    ' ──────────────────────────────────────────────
    '  List operations
    ' ──────────────────────────────────────────────

    ''' <summary>
    ''' Push one or more values to the left side of a list.
    ''' Mirrors IDatabase.ListLeftPush.
    ''' </summary>
    Public Shared Function ListLeftPush(key As String, values As IEnumerable(Of String), Optional prefix As String = Nothing, Optional seconds As Integer = 0) As Long
        Dim redisValues As RedisValue() = ToRedisValues(values)
        If redisValues.Length = 0 Then Return 0

        prefix = NormalizePrefix(prefix)
        Dim redisKey As RedisKey = ResolveKey(key, prefix)
        Dim newLength As Long = Db.ListLeftPush(redisKey, redisValues)

        ApplyExpiration(redisKey, seconds)
        If Not String.IsNullOrEmpty(prefix) Then
            TrackKey(prefix, key)
        End If

        Return newLength
    End Function

    ''' <summary>
    ''' Push one or more values to the right side of a list.
    ''' Mirrors IDatabase.ListRightPush.
    ''' </summary>
    Public Shared Function ListRightPush(key As String, values As IEnumerable(Of String), Optional prefix As String = Nothing, Optional seconds As Integer = 0) As Long
        Dim redisValues As RedisValue() = ToRedisValues(values)
        If redisValues.Length = 0 Then Return 0

        prefix = NormalizePrefix(prefix)
        Dim redisKey As RedisKey = ResolveKey(key, prefix)
        Dim newLength As Long = Db.ListRightPush(redisKey, redisValues)

        ApplyExpiration(redisKey, seconds)
        If Not String.IsNullOrEmpty(prefix) Then
            TrackKey(prefix, key)
        End If

        Return newLength
    End Function

    ''' <summary>
    ''' Pop a value from the left side of a list.
    ''' Mirrors IDatabase.ListLeftPop.
    ''' </summary>
    Public Shared Function ListLeftPop(key As String, Optional prefix As String = Nothing) As RedisValue
        prefix = NormalizePrefix(prefix)
        Dim redisKey As RedisKey = ResolveKey(key, prefix)
        Dim value As RedisValue = Db.ListLeftPop(redisKey)

        If value.HasValue Then
            UntrackIfMissing(prefix, key, redisKey)
        End If

        Return value
    End Function

    ''' <summary>
    ''' Pop a value from the right side of a list.
    ''' Mirrors IDatabase.ListRightPop.
    ''' </summary>
    Public Shared Function ListRightPop(key As String, Optional prefix As String = Nothing) As RedisValue
        prefix = NormalizePrefix(prefix)
        Dim redisKey As RedisKey = ResolveKey(key, prefix)
        Dim value As RedisValue = Db.ListRightPop(redisKey)

        If value.HasValue Then
            UntrackIfMissing(prefix, key, redisKey)
        End If

        Return value
    End Function

    ''' <summary>
    ''' Read a range of list values.
    ''' Mirrors IDatabase.ListRange.
    ''' </summary>
    Public Shared Function ListRange(key As String, Optional start As Long = 0, Optional [stop] As Long = -1, Optional prefix As String = Nothing) As RedisValue()
        Return Db.ListRange(ResolveKey(key, prefix), start, [stop])
    End Function

    ''' <summary>
    ''' Remove occurrences of a list value.
    ''' Mirrors IDatabase.ListRemove.
    ''' </summary>
    Public Shared Function ListRemove(key As String, value As String, Optional count As Long = 0, Optional prefix As String = Nothing) As Long
        prefix = NormalizePrefix(prefix)
        Dim redisKey As RedisKey = ResolveKey(key, prefix)
        Dim removed As Long = Db.ListRemove(redisKey, ToRedisValue(value), count)

        If removed > 0 Then
            UntrackIfMissing(prefix, key, redisKey)
        End If

        Return removed
    End Function

    ''' <summary>
    ''' Trim a list to a specified range.
    ''' Mirrors IDatabase.ListTrim.
    ''' </summary>
    Public Shared Sub ListTrim(key As String, start As Long, [stop] As Long, Optional prefix As String = Nothing)
        prefix = NormalizePrefix(prefix)
        Dim redisKey As RedisKey = ResolveKey(key, prefix)

        Db.ListTrim(redisKey, start, [stop])
        UntrackIfMissing(prefix, key, redisKey)
    End Sub

    ''' <summary>
    ''' Get the length of a list.
    ''' Mirrors IDatabase.ListLength.
    ''' </summary>
    Public Shared Function ListLength(key As String, Optional prefix As String = Nothing) As Long
        Return Db.ListLength(ResolveKey(key, prefix))
    End Function

    ' ──────────────────────────────────────────────
    '  Set operations
    ' ──────────────────────────────────────────────

    ''' <summary>
    ''' Add one or more members to a set.
    ''' Mirrors IDatabase.SetAdd.
    ''' </summary>
    Public Shared Function SetAdd(key As String, members As IEnumerable(Of String), Optional prefix As String = Nothing, Optional seconds As Integer = 0) As Long
        Dim redisMembers As RedisValue() = ToRedisValues(members)
        If redisMembers.Length = 0 Then Return 0

        prefix = NormalizePrefix(prefix)
        Dim redisKey As RedisKey = ResolveKey(key, prefix)
        Dim added As Long = Db.SetAdd(redisKey, redisMembers)

        ApplyExpiration(redisKey, seconds)
        If Not String.IsNullOrEmpty(prefix) Then
            TrackKey(prefix, key)
        End If

        Return added
    End Function

    ''' <summary>
    ''' Get all members from a set.
    ''' Mirrors IDatabase.SetMembers.
    ''' </summary>
    Public Shared Function SetMembers(key As String, Optional prefix As String = Nothing) As RedisValue()
        Return Db.SetMembers(ResolveKey(key, prefix))
    End Function

    ''' <summary>
    ''' Remove one or more set members.
    ''' Mirrors IDatabase.SetRemove.
    ''' </summary>
    Public Shared Function SetRemove(key As String, members As IEnumerable(Of String), Optional prefix As String = Nothing) As Long
        Dim redisMembers As RedisValue() = ToRedisValues(members)
        If redisMembers.Length = 0 Then Return 0

        prefix = NormalizePrefix(prefix)
        Dim redisKey As RedisKey = ResolveKey(key, prefix)
        Dim removed As Long = Db.SetRemove(redisKey, redisMembers)

        If removed > 0 Then
            UntrackIfMissing(prefix, key, redisKey)
        End If

        Return removed
    End Function

    ''' <summary>
    ''' Test membership in a set.
    ''' Mirrors IDatabase.SetContains.
    ''' </summary>
    Public Shared Function SetContains(key As String, member As String, Optional prefix As String = Nothing) As Boolean
        Return Db.SetContains(ResolveKey(key, prefix), ToRedisValue(member))
    End Function

    ''' <summary>
    ''' Get the cardinality of a set.
    ''' Mirrors IDatabase.SetLength.
    ''' </summary>
    Public Shared Function SetLength(key As String, Optional prefix As String = Nothing) As Long
        Return Db.SetLength(ResolveKey(key, prefix))
    End Function

    ' ──────────────────────────────────────────────
    '  Sorted set operations
    ' ──────────────────────────────────────────────

    ''' <summary>
    ''' Add one or more members to a sorted set.
    ''' Mirrors IDatabase.SortedSetAdd.
    ''' </summary>
    Public Shared Function SortedSetAdd(key As String, entries As IEnumerable(Of SortedSetEntry), Optional prefix As String = Nothing, Optional seconds As Integer = 0) As Long
        If entries Is Nothing Then Return 0

        Dim sortedEntries As SortedSetEntry() = entries.ToArray()
        If sortedEntries.Length = 0 Then Return 0

        prefix = NormalizePrefix(prefix)
        Dim redisKey As RedisKey = ResolveKey(key, prefix)
        Dim added As Long = Db.SortedSetAdd(redisKey, sortedEntries)

        ApplyExpiration(redisKey, seconds)
        If Not String.IsNullOrEmpty(prefix) Then
            TrackKey(prefix, key)
        End If

        Return added
    End Function

    ''' <summary>
    ''' Add a single member to a sorted set.
    ''' Mirrors IDatabase.SortedSetAdd(key, member, score, when).
    ''' </summary>
    Public Shared Function SortedSetAdd(key As String, member As String, score As Double, Optional prefix As String = Nothing, Optional condition As StackExchange.Redis.When = StackExchange.Redis.When.Always, Optional seconds As Integer = 0) As Boolean
        prefix = NormalizePrefix(prefix)
        Dim redisKey As RedisKey = ResolveKey(key, prefix)
        Dim wasAdded As Boolean = Db.SortedSetAdd(redisKey, ToRedisValue(member), score, condition)

        If condition = StackExchange.Redis.When.Always OrElse wasAdded Then
            ApplyExpiration(redisKey, seconds)

            If Not String.IsNullOrEmpty(prefix) Then
                TrackKey(prefix, key)
            End If
        End If

        Return wasAdded
    End Function

    ''' <summary>
    ''' Read sorted set members by rank.
    ''' Mirrors IDatabase.SortedSetRangeByRank.
    ''' </summary>
    Public Shared Function SortedSetRangeByRank(key As String, Optional start As Long = 0, Optional [stop] As Long = -1, Optional order As Order = Order.Ascending, Optional prefix As String = Nothing) As RedisValue()
        Return Db.SortedSetRangeByRank(ResolveKey(key, prefix), start, [stop], order)
    End Function

    ''' <summary>
    ''' Read sorted set members and scores by score range.
    ''' Mirrors IDatabase.SortedSetRangeByScoreWithScores.
    ''' </summary>
    Public Shared Function SortedSetRangeByScoreWithScores(key As String, Optional start As Double = Double.NegativeInfinity, Optional [stop] As Double = Double.PositiveInfinity, Optional exclude As Exclude = Exclude.None, Optional order As Order = Order.Ascending, Optional skip As Long = 0, Optional take As Long = -1, Optional prefix As String = Nothing) As SortedSetEntry()
        Return Db.SortedSetRangeByScoreWithScores(ResolveKey(key, prefix), start, [stop], exclude, order, skip, take)
    End Function

    ''' <summary>
    ''' Remove one or more members from a sorted set.
    ''' Mirrors IDatabase.SortedSetRemove.
    ''' </summary>
    Public Shared Function SortedSetRemove(key As String, members As IEnumerable(Of String), Optional prefix As String = Nothing) As Long
        Dim redisMembers As RedisValue() = ToRedisValues(members)
        If redisMembers.Length = 0 Then Return 0

        prefix = NormalizePrefix(prefix)
        Dim redisKey As RedisKey = ResolveKey(key, prefix)
        Dim removed As Long = Db.SortedSetRemove(redisKey, redisMembers)

        If removed > 0 Then
            UntrackIfMissing(prefix, key, redisKey)
        End If

        Return removed
    End Function

    ''' <summary>
    ''' Get the rank of a member in a sorted set.
    ''' Mirrors IDatabase.SortedSetRank.
    ''' </summary>
    Public Shared Function SortedSetRank(key As String, member As String, Optional order As Order = Order.Ascending, Optional prefix As String = Nothing) As Nullable(Of Long)
        Return Db.SortedSetRank(ResolveKey(key, prefix), ToRedisValue(member), order)
    End Function

    ''' <summary>
    ''' Get the number of members in a sorted set.
    ''' Mirrors IDatabase.SortedSetLength.
    ''' </summary>
    Public Shared Function SortedSetLength(key As String, Optional prefix As String = Nothing) As Long
        Return Db.SortedSetLength(ResolveKey(key, prefix))
    End Function

    ''' <summary>
    ''' Get the type of a Redis key.
    ''' Useful when callers need to branch by underlying Redis data structure.
    ''' </summary>
    Public Shared Function GetKeyType(key As String, Optional prefix As String = Nothing) As RedisType
        Return Db.KeyType(ResolveKey(key, prefix))
    End Function

    ' ──────────────────────────────────────────────
    '  Result types
    ' ──────────────────────────────────────────────

    Public Class KeyResult
        Public Property Key As String
        Public Property Exists As Boolean
        Public Property Value As String
    End Class

    Public Class KeyExistsResult
        Public Property Key As String
        Public Property Exists As Boolean
    End Class

    Public Class PatternResult
        Public Property Pattern As String
        Public Property Found As Integer
        Public Property Removed As Long
    End Class

    Public Class PatternRemoveResult
        Public Property TotalRemoved As Long = 0
        Public Property PatternResults As New List(Of PatternResult)()
    End Class

End Class
