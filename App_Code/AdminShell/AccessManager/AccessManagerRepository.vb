Imports System
Imports System.Collections.Generic
Imports System.Configuration
Imports System.Data.Odbc
Imports System.Globalization

Public Class AccessManagerRepository
    Implements IAccessManagerRepository

    Private ReadOnly _connectionString As String

    Public Sub New()
        Dim configured = ConfigurationManager.ConnectionStrings("ConnectionStringB")
        If configured Is Nothing OrElse String.IsNullOrWhiteSpace(configured.ConnectionString) Then
            Throw New ConfigurationErrorsException("ConnectionStringB is not configured for Access Manager.")
        End If
        _connectionString = configured.ConnectionString
    End Sub

    Public Function ListSections(parentId As Integer, includeInactive As Boolean) As IList(Of AccessManagerSection) Implements IAccessManagerRepository.ListSections
        Dim inactiveClause = BuildInactiveClause("s.inactive", includeInactive)
        Dim sql =
            "select s.section_id,s.parent_id,s.section,s.position,s.modify_by,s.modify_dt," &
            "s.create_by,s.create_dt,s.update_no,s.inactive " &
            "from section s where s.parent_id = ? " & inactiveClause &
            " order by s.inactive, s.position, s.section"

        Dim results As New List(Of AccessManagerSection)()
        Using connection As New OdbcConnection(_connectionString),
              command As New OdbcCommand(sql, connection)
            command.Parameters.Add("@parent_id", OdbcType.Int).Value = parentId
            connection.Open()
            Using reader = command.ExecuteReader()
                While reader.Read()
                    results.Add(ReadSection(reader))
                End While
            End Using
        End Using
        Return results
    End Function

    Public Function GetSection(sectionId As Integer) As AccessManagerSection Implements IAccessManagerRepository.GetSection
        Const sql As String =
            "select s.section_id,s.parent_id,s.section,s.position,s.modify_by,s.modify_dt," &
            "s.create_by,s.create_dt,s.update_no,s.inactive from section s where s.section_id = ?"

        Using connection As New OdbcConnection(_connectionString),
              command As New OdbcCommand(sql, connection)
            command.Parameters.Add("@section_id", OdbcType.Int).Value = sectionId
            connection.Open()
            Using reader = command.ExecuteReader()
                If Not reader.Read() Then
                    Return Nothing
                End If
                Return ReadSection(reader)
            End Using
        End Using
    End Function

    Public Function SectionNameExists(sectionName As String, excludeSectionId As Integer?) As Boolean Implements IAccessManagerRepository.SectionNameExists
        Dim sql = "select count(*) from section where lower(section) = lower(?)"
        If excludeSectionId.HasValue Then
            sql &= " and section_id <> ?"
        End If

        Using connection As New OdbcConnection(_connectionString),
              command As New OdbcCommand(sql, connection)
            command.Parameters.Add("@section", OdbcType.VarChar, 50).Value = sectionName
            If excludeSectionId.HasValue Then
                command.Parameters.Add("@exclude_id", OdbcType.Int).Value = excludeSectionId.Value
            End If
            connection.Open()
            Return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0
        End Using
    End Function

    Public Function ListScriptTypes() As IList(Of AccessManagerScriptType) Implements IAccessManagerRepository.ListScriptTypes
        Const sql As String =
            "select code_value,code_value_desc,position from code_value " &
            "where code_class = ? and inactive = 'N' order by position, code_value_desc, code_value"

        Dim results As New List(Of AccessManagerScriptType)()
        Using connection As New OdbcConnection(_connectionString),
              command As New OdbcCommand(sql, connection)
            command.Parameters.Add("@code_class", OdbcType.VarChar, 50).Value = AccessManagerConstants.ScriptTypeCodeClass
            connection.Open()
            Using reader = command.ExecuteReader()
                While reader.Read()
                    results.Add(New AccessManagerScriptType With {
                        .CodeValue = AdminShellData.StringValue(reader("code_value")),
                        .CodeValueDesc = AdminShellData.StringValue(reader("code_value_desc")),
                        .OrderBy = AdminShellData.NullableInt(reader("position"))
                    })
                End While
            End Using
        End Using
        Return results
    End Function

    Public Function ListScripts(scriptTy As String, includeInactive As Boolean) As IList(Of AccessManagerScript) Implements IAccessManagerRepository.ListScripts
        Dim inactiveClause = BuildInactiveClause("s.inactive", includeInactive)
        Dim sql =
            "select s.script_id,s.script_ty,s.script_name,s.title,s.modify_by,s.modify_dt," &
            "s.create_by,s.create_dt,s.update_no,s.inactive from script s " &
            "where s.script_ty = ? " & inactiveClause & " order by s.inactive, s.title, s.script_name"

        Dim results As New List(Of AccessManagerScript)()
        Using connection As New OdbcConnection(_connectionString),
              command As New OdbcCommand(sql, connection)
            command.Parameters.Add("@script_ty", OdbcType.VarChar, 20).Value = scriptTy
            connection.Open()
            Using reader = command.ExecuteReader()
                While reader.Read()
                    results.Add(ReadScript(reader))
                End While
            End Using
        End Using
        Return results
    End Function

    Public Function GetScript(scriptId As Integer) As AccessManagerScript Implements IAccessManagerRepository.GetScript
        Const sql As String =
            "select s.script_id,s.script_ty,s.script_name,s.title,s.modify_by,s.modify_dt," &
            "s.create_by,s.create_dt,s.update_no,s.inactive from script s where s.script_id = ?"

        Using connection As New OdbcConnection(_connectionString),
              command As New OdbcCommand(sql, connection)
            command.Parameters.Add("@script_id", OdbcType.Int).Value = scriptId
            connection.Open()
            Using reader = command.ExecuteReader()
                If Not reader.Read() Then
                    Return Nothing
                End If
                Return ReadScript(reader)
            End Using
        End Using
    End Function

    Public Function ScriptNameExists(scriptName As String, excludeScriptId As Integer?) As Boolean Implements IAccessManagerRepository.ScriptNameExists
        Dim sql = "select count(*) from script where lower(script_name) = lower(?)"
        If excludeScriptId.HasValue Then
            sql &= " and script_id <> ?"
        End If

        Using connection As New OdbcConnection(_connectionString),
              command As New OdbcCommand(sql, connection)
            command.Parameters.Add("@script_name", OdbcType.VarChar, 512).Value = scriptName
            If excludeScriptId.HasValue Then
                command.Parameters.Add("@exclude_id", OdbcType.Int).Value = excludeScriptId.Value
            End If
            connection.Open()
            Return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0
        End Using
    End Function

    Public Function ListSectionItems(sectionId As Integer, includeInactiveScripts As Boolean) As IList(Of AccessManagerSectionItem) Implements IAccessManagerRepository.ListSectionItems
        Dim inactiveClause = String.Empty
        If Not includeInactiveScripts Then
            inactiveClause = " and sc.inactive = 'N'"
        End If

        Dim sql =
            "select ss.section_id,ss.script_id,ss.position,sc.script_ty,sc.script_name,sc.title," &
            "sc.update_no,sc.inactive from section_script ss " &
            "join script sc on sc.script_id = ss.script_id " &
            "where ss.section_id = ?" & inactiveClause & " order by ss.position, sc.title"

        Dim results As New List(Of AccessManagerSectionItem)()
        Using connection As New OdbcConnection(_connectionString),
              command As New OdbcCommand(sql, connection)
            command.Parameters.Add("@section_id", OdbcType.Int).Value = sectionId
            connection.Open()
            Using reader = command.ExecuteReader()
                While reader.Read()
                    results.Add(ReadSectionItem(reader))
                End While
            End Using
        End Using
        Return results
    End Function

    Public Function SectionScriptExists(sectionId As Integer, scriptId As Integer) As Boolean Implements IAccessManagerRepository.SectionScriptExists
        Const sql As String = "select count(*) from section_script where section_id = ? and script_id = ?"
        Using connection As New OdbcConnection(_connectionString),
              command As New OdbcCommand(sql, connection)
            command.Parameters.Add("@section_id", OdbcType.Int).Value = sectionId
            command.Parameters.Add("@script_id", OdbcType.Int).Value = scriptId
            connection.Open()
            Return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0
        End Using
    End Function

    Public Function ListGrants(secureTy As String, secureId As Integer, includeInactive As Boolean) As IList(Of AccessManagerGrant) Implements IAccessManagerRepository.ListGrants
        Dim inactiveClause = BuildInactiveClause("a.inactive", includeInactive)
        Dim sql =
            "select a.access_id,a.permission_cd,a.secure_id,a.secure_ty,a.user_id,a.user_ty," &
            "a.modify_by,a.modify_dt,a.create_by,a.create_dt,a.update_no,a.inactive from access a " &
            "where a.secure_ty = ? and a.secure_id = ? and a.permission_cd = 'G' " & inactiveClause &
            " order by a.inactive, a.user_ty, a.user_id"

        Dim results As New List(Of AccessManagerGrant)()
        Using connection As New OdbcConnection(_connectionString),
              command As New OdbcCommand(sql, connection)
            command.Parameters.Add("@secure_ty", OdbcType.VarChar, 4).Value = secureTy
            command.Parameters.Add("@secure_id", OdbcType.Int).Value = secureId
            connection.Open()
            Using reader = command.ExecuteReader()
                While reader.Read()
                    Dim grant = ReadGrant(reader)
                    PopulateGrantLabels(connection, grant)
                    results.Add(grant)
                End While
            End Using
        End Using
        Return results
    End Function

    Public Function ListPrincipalGrants(principalTy As String, principalId As Integer, includeInactive As Boolean) As IList(Of AccessManagerGrant) Implements IAccessManagerRepository.ListPrincipalGrants
        Dim inactiveClause = BuildInactiveClause("a.inactive", includeInactive)
        Dim sql =
            "select a.access_id,a.permission_cd,a.secure_id,a.secure_ty,a.user_id,a.user_ty," &
            "a.modify_by,a.modify_dt,a.create_by,a.create_dt,a.update_no,a.inactive from access a " &
            "where a.user_ty = ? and a.user_id = ? and a.permission_cd = 'G' " & inactiveClause &
            " order by a.inactive, a.secure_ty, a.secure_id"

        Dim results As New List(Of AccessManagerGrant)()
        Using connection As New OdbcConnection(_connectionString),
              command As New OdbcCommand(sql, connection)
            command.Parameters.Add("@user_ty", OdbcType.VarChar, 4).Value = principalTy
            command.Parameters.Add("@user_id", OdbcType.Int).Value = principalId
            connection.Open()
            Using reader = command.ExecuteReader()
                While reader.Read()
                    Dim grant = ReadGrant(reader)
                    PopulateGrantLabels(connection, grant)
                    results.Add(grant)
                End While
            End Using
        End Using
        Return results
    End Function

    Public Function GetGrant(accessId As Integer) As AccessManagerGrant Implements IAccessManagerRepository.GetGrant
        Const sql As String =
            "select a.access_id,a.permission_cd,a.secure_id,a.secure_ty,a.user_id,a.user_ty," &
            "a.modify_by,a.modify_dt,a.create_by,a.create_dt,a.update_no,a.inactive from access a where a.access_id = ?"

        Using connection As New OdbcConnection(_connectionString),
              command As New OdbcCommand(sql, connection)
            command.Parameters.Add("@access_id", OdbcType.Int).Value = accessId
            connection.Open()
            Using reader = command.ExecuteReader()
                If Not reader.Read() Then
                    Return Nothing
                End If
                Dim grant = ReadGrant(reader)
                PopulateGrantLabels(connection, grant)
                Return grant
            End Using
        End Using
    End Function

    Public Function FindGrant(secureTy As String, secureId As Integer, principalTy As String, principalId As Integer) As AccessManagerGrant Implements IAccessManagerRepository.FindGrant
        Const sql As String =
            "select a.access_id,a.permission_cd,a.secure_id,a.secure_ty,a.user_id,a.user_ty," &
            "a.modify_by,a.modify_dt,a.create_by,a.create_dt,a.update_no,a.inactive from access a " &
            "where a.secure_ty = ? and a.secure_id = ? and a.user_ty = ? and a.user_id = ? and a.permission_cd = 'G'"

        Using connection As New OdbcConnection(_connectionString),
              command As New OdbcCommand(sql, connection)
            command.Parameters.Add("@secure_ty", OdbcType.VarChar, 4).Value = secureTy
            command.Parameters.Add("@secure_id", OdbcType.Int).Value = secureId
            command.Parameters.Add("@user_ty", OdbcType.VarChar, 4).Value = principalTy
            command.Parameters.Add("@user_id", OdbcType.Int).Value = principalId
            connection.Open()
            Using reader = command.ExecuteReader()
                If Not reader.Read() Then
                    Return Nothing
                End If
                Dim grant = ReadGrant(reader)
                PopulateGrantLabels(connection, grant)
                Return grant
            End Using
        End Using
    End Function

    Public Function SearchPrincipals(query As String, principalTy As String, includeInactive As Boolean, limit As Integer) As IList(Of AccessManagerPrincipal) Implements IAccessManagerRepository.SearchPrincipals
        Dim results As New List(Of AccessManagerPrincipal)()
        Dim normalizedQuery = If(query, String.Empty).Trim()
        Dim effectiveLimit = If(limit <= 0, 50, Math.Min(limit, 200))

        Using connection As New OdbcConnection(_connectionString)
            connection.Open()
            If String.IsNullOrEmpty(principalTy) OrElse String.Equals(principalTy, AccessManagerConstants.PrincipalTypeUser, StringComparison.OrdinalIgnoreCase) Then
                AppendUserPrincipals(connection, results, normalizedQuery, includeInactive, effectiveLimit)
            End If
            If results.Count < effectiveLimit AndAlso
                (String.IsNullOrEmpty(principalTy) OrElse String.Equals(principalTy, AccessManagerConstants.PrincipalTypeGroup, StringComparison.OrdinalIgnoreCase)) Then
                AppendGroupPrincipals(connection, results, normalizedQuery, includeInactive, effectiveLimit - results.Count)
            End If
        End Using

        Return results
    End Function

    Public Function GetPrincipal(principalTy As String, principalId As Integer) As AccessManagerPrincipal Implements IAccessManagerRepository.GetPrincipal
        Using connection As New OdbcConnection(_connectionString)
            connection.Open()
            If String.Equals(principalTy, AccessManagerConstants.PrincipalTypeUser, StringComparison.OrdinalIgnoreCase) Then
                Return ReadUserPrincipal(connection, principalId)
            End If
            If String.Equals(principalTy, AccessManagerConstants.PrincipalTypeGroup, StringComparison.OrdinalIgnoreCase) Then
                Return ReadGroupPrincipal(connection, principalId)
            End If
        End Using
        Return Nothing
    End Function

    Public Function GetSectionDeleteImpact(sectionId As Integer) As AccessManagerDeleteImpact Implements IAccessManagerRepository.GetSectionDeleteImpact
        Dim section = GetSection(sectionId)
        If section Is Nothing Then
            Throw New AdminShellValidationException("Section was not found.")
        End If

        Using connection As New OdbcConnection(_connectionString)
            connection.Open()
            Return New AccessManagerDeleteImpact With {
                .TargetKind = AccessManagerConstants.SecureTypeSection,
                .TargetId = sectionId,
                .TargetLabel = section.SectionName,
                .AccessRowCount = CountRows(connection, "select count(*) from access where secure_ty = 'SECT' and secure_id = ?", sectionId),
                .SectionScriptRowCount = CountRows(connection, "select count(*) from section_script where section_id = ?", sectionId),
                .ChildSectionCount = CountRows(connection, "select count(*) from section where parent_id = ?", sectionId)
            }
        End Using
    End Function

    Public Function GetScriptDeleteImpact(scriptId As Integer) As AccessManagerDeleteImpact Implements IAccessManagerRepository.GetScriptDeleteImpact
        Dim script = GetScript(scriptId)
        If script Is Nothing Then
            Throw New AdminShellValidationException("Script was not found.")
        End If

        Using connection As New OdbcConnection(_connectionString)
            connection.Open()
            Return New AccessManagerDeleteImpact With {
                .TargetKind = AccessManagerConstants.SecureTypeScript,
                .TargetId = scriptId,
                .TargetLabel = script.ScriptName,
                .AccessRowCount = CountRows(connection, "select count(*) from access where secure_ty = 'SCRI' and secure_id = ?", scriptId),
                .SectionScriptRowCount = CountRows(connection, "select count(*) from section_script where script_id = ?", scriptId),
                .ChildSectionCount = 0
            }
        End Using
    End Function

    Public Function GetEffectiveAccess(query As EffectiveAccessQuery) As AccessManagerEffectiveAccess Implements IAccessManagerRepository.GetEffectiveAccess
        Dim script = GetScript(query.ScriptId)
        If script Is Nothing Then
            Throw New AdminShellValidationException("Script was not found.")
        End If

        Dim result As New AccessManagerEffectiveAccess With {
            .ScriptId = script.ScriptId,
            .ScriptName = script.ScriptName,
            .PrincipalTy = query.PrincipalTy.ToUpperInvariant(),
            .PrincipalId = query.PrincipalId,
            .DirectSectionGrants = New List(Of AccessManagerEffectiveGrant)(),
            .DirectScriptGrants = New List(Of AccessManagerEffectiveGrant)(),
            .InheritedSectionGrants = New List(Of AccessManagerEffectiveGrant)()
        }

        Using connection As New OdbcConnection(_connectionString)
            connection.Open()
            LoadDirectScriptGrants(connection, result)
            LoadInheritedSectionGrants(connection, result)
        End Using

        result.HasEffectiveAccess =
            HasActiveGrant(result.DirectScriptGrants) OrElse
            HasActiveGrant(result.DirectSectionGrants) OrElse
            HasActiveGrant(result.InheritedSectionGrants)

        Return result
    End Function

    Public Function CreateSection(command As CreateSectionCommand, actingMemberId As Integer) As AccessManagerSection Implements IAccessManagerRepository.CreateSection
        Using connection As New OdbcConnection(_connectionString)
            connection.Open()
            Using transaction = connection.BeginTransaction()
                Dim nextPosition = GetMaxPosition(connection, transaction, "select max(position) from section where inactive = 'N'") + 1
                If nextPosition <= 0 Then
                    nextPosition = 1
                End If

                Const sql As String =
                    "insert into section (section,parent_id,position,modify_by,modify_dt,create_by,create_dt,update_no,inactive) " &
                    "values (?,?,?,?,now(),?,now(),0,'N')"
                Using insertCommand As New OdbcCommand(sql, connection, transaction)
                    insertCommand.Parameters.Add("@section", OdbcType.VarChar, 50).Value = command.SectionName
                    insertCommand.Parameters.Add("@parent_id", OdbcType.Int).Value = command.ParentId
                    insertCommand.Parameters.Add("@position", OdbcType.Int).Value = nextPosition
                    insertCommand.Parameters.Add("@modify_by", OdbcType.Int).Value = actingMemberId
                    insertCommand.Parameters.Add("@create_by", OdbcType.Int).Value = actingMemberId
                    insertCommand.ExecuteNonQuery()
                End Using

                Dim sectionId = GetLastInsertId(connection, transaction)
                transaction.Commit()
                Return GetSection(sectionId)
            End Using
        End Using
    End Function

    Public Function UpdateSection(command As UpdateSectionCommand, actingMemberId As Integer) As AccessManagerSection Implements IAccessManagerRepository.UpdateSection
        Const sql As String =
            "update section set section = ?, modify_by = ?, modify_dt = now(), update_no = update_no + 1 " &
            "where section_id = ? and update_no = ?"

        Dim affected = ExecuteNonQuery(sql,
            New OdbcParameter("@section", OdbcType.VarChar, 50) With {.Value = command.SectionName},
            New OdbcParameter("@modify_by", OdbcType.Int) With {.Value = actingMemberId},
            New OdbcParameter("@section_id", OdbcType.Int) With {.Value = command.SectionId},
            New OdbcParameter("@update_no", OdbcType.Int) With {.Value = command.ExpectedUpdateNo})

        If affected = 0 Then
            ThrowConcurrency("Section")
        End If

        Return GetSection(command.SectionId)
    End Function

    Public Sub ReorderSection(command As ReorderSectionCommand, actingMemberId As Integer) Implements IAccessManagerRepository.ReorderSection
        Using connection As New OdbcConnection(_connectionString)
            connection.Open()
            Using transaction = connection.BeginTransaction()
                Dim orderedIds = ListPositionedSectionIds(connection, transaction)
                MoveIdInOrder(orderedIds, command.SectionId, command.NewPosition)
                RenumberSectionPositions(connection, transaction, orderedIds, actingMemberId)
                transaction.Commit()
            End Using
        End Using
    End Sub

    Public Sub DeactivateSection(command As SectionLifecycleCommand, actingMemberId As Integer) Implements IAccessManagerRepository.DeactivateSection
        Using connection As New OdbcConnection(_connectionString)
            connection.Open()
            Using transaction = connection.BeginTransaction()
                ClearSectionPosition(connection, transaction, command.SectionId, actingMemberId)
                Dim orderedIds = ListPositionedSectionIds(connection, transaction)
                RenumberSectionPositions(connection, transaction, orderedIds, actingMemberId)

                Const sql As String =
                    "update section set inactive = 'Y', modify_by = ?, modify_dt = now(), update_no = update_no + 1 " &
                    "where section_id = ? and update_no = ? and inactive = 'N'"
                Dim affected = ExecuteNonQuery(connection, transaction, sql,
                    New OdbcParameter("@modify_by", OdbcType.Int) With {.Value = actingMemberId},
                    New OdbcParameter("@section_id", OdbcType.Int) With {.Value = command.SectionId},
                    New OdbcParameter("@update_no", OdbcType.Int) With {.Value = command.ExpectedUpdateNo})
                If affected = 0 Then
                    ThrowConcurrency("Section")
                End If
                transaction.Commit()
            End Using
        End Using
    End Sub

    Public Sub ActivateSection(command As SectionLifecycleCommand, actingMemberId As Integer) Implements IAccessManagerRepository.ActivateSection
        Using connection As New OdbcConnection(_connectionString)
            connection.Open()
            Using transaction = connection.BeginTransaction()
                Dim nextPosition = GetMaxPosition(connection, transaction, "select max(position) from section where inactive = 'N'") + 1
                If nextPosition <= 0 Then
                    nextPosition = 1
                End If

                Const sql As String =
                    "update section set inactive = 'N', position = ?, modify_by = ?, modify_dt = now(), update_no = update_no + 1 " &
                    "where section_id = ? and update_no = ? and inactive = 'Y'"
                Dim affected = ExecuteNonQuery(connection, transaction, sql,
                    New OdbcParameter("@position", OdbcType.Int) With {.Value = nextPosition},
                    New OdbcParameter("@modify_by", OdbcType.Int) With {.Value = actingMemberId},
                    New OdbcParameter("@section_id", OdbcType.Int) With {.Value = command.SectionId},
                    New OdbcParameter("@update_no", OdbcType.Int) With {.Value = command.ExpectedUpdateNo})
                If affected = 0 Then
                    ThrowConcurrency("Section")
                End If
                transaction.Commit()
            End Using
        End Using
    End Sub

    Public Sub HardDeleteSection(command As HardDeleteSectionCommand, actingMemberId As Integer) Implements IAccessManagerRepository.HardDeleteSection
        Using connection As New OdbcConnection(_connectionString)
            connection.Open()
            Using transaction = connection.BeginTransaction()
                EnsureExists(connection, transaction, "select section_id from section where section_id = ?", command.SectionId, "Section")

                ExecuteNonQuery(connection, transaction,
                    "delete from access where secure_ty = 'SECT' and secure_id = ?",
                    New OdbcParameter("@secure_id", OdbcType.Int) With {.Value = command.SectionId})
                ExecuteNonQuery(connection, transaction,
                    "delete from section_script where section_id = ?",
                    New OdbcParameter("@section_id", OdbcType.Int) With {.Value = command.SectionId})

                ClearSectionPosition(connection, transaction, command.SectionId, actingMemberId)
                Dim orderedIds = ListPositionedSectionIds(connection, transaction)
                RenumberSectionPositions(connection, transaction, orderedIds, actingMemberId)

                Dim affected = ExecuteNonQuery(connection, transaction,
                    "delete from section where section_id = ? and update_no = ?",
                    New OdbcParameter("@section_id", OdbcType.Int) With {.Value = command.SectionId},
                    New OdbcParameter("@update_no", OdbcType.Int) With {.Value = command.ExpectedUpdateNo})
                If affected = 0 Then
                    ThrowConcurrency("Section")
                End If

                transaction.Commit()
            End Using
        End Using
    End Sub

    Public Function CreateScript(command As CreateScriptCommand, actingMemberId As Integer) As AccessManagerScript Implements IAccessManagerRepository.CreateScript
        Const sql As String =
            "insert into script (script_ty,script_name,title,modify_by,modify_dt,create_by,create_dt,update_no,inactive) " &
            "values (?,?,?,?,now(),?,now(),0,'N')"

        Using connection As New OdbcConnection(_connectionString)
            connection.Open()
            Using transaction = connection.BeginTransaction()
                Using insertCommand As New OdbcCommand(sql, connection, transaction)
                    insertCommand.Parameters.Add("@script_ty", OdbcType.VarChar, 20).Value = command.ScriptTy
                    insertCommand.Parameters.Add("@script_name", OdbcType.VarChar, 512).Value = command.ScriptName
                    insertCommand.Parameters.Add("@title", OdbcType.VarChar, 255).Value = command.Title
                    insertCommand.Parameters.Add("@modify_by", OdbcType.Int).Value = actingMemberId
                    insertCommand.Parameters.Add("@create_by", OdbcType.Int).Value = actingMemberId
                    insertCommand.ExecuteNonQuery()
                End Using
                Dim scriptId = GetLastInsertId(connection, transaction)
                transaction.Commit()
                Return GetScript(scriptId)
            End Using
        End Using
    End Function

    Public Function UpdateScript(command As UpdateScriptCommand, actingMemberId As Integer) As AccessManagerScript Implements IAccessManagerRepository.UpdateScript
        Const sql As String =
            "update script set script_ty = ?, script_name = ?, title = ?, modify_by = ?, modify_dt = now(), update_no = update_no + 1 " &
            "where script_id = ? and update_no = ?"

        Dim affected = ExecuteNonQuery(sql,
            New OdbcParameter("@script_ty", OdbcType.VarChar, 20) With {.Value = command.ScriptTy},
            New OdbcParameter("@script_name", OdbcType.VarChar, 512) With {.Value = command.ScriptName},
            New OdbcParameter("@title", OdbcType.VarChar, 255) With {.Value = command.Title},
            New OdbcParameter("@modify_by", OdbcType.Int) With {.Value = actingMemberId},
            New OdbcParameter("@script_id", OdbcType.Int) With {.Value = command.ScriptId},
            New OdbcParameter("@update_no", OdbcType.Int) With {.Value = command.ExpectedUpdateNo})

        If affected = 0 Then
            ThrowConcurrency("Script")
        End If

        Return GetScript(command.ScriptId)
    End Function

    Public Sub DeactivateScript(command As ScriptLifecycleCommand, actingMemberId As Integer) Implements IAccessManagerRepository.DeactivateScript
        Const sql As String =
            "update script set inactive = 'Y', modify_by = ?, modify_dt = now(), update_no = update_no + 1 " &
            "where script_id = ? and update_no = ? and inactive = 'N'"

        Dim affected = ExecuteNonQuery(sql,
            New OdbcParameter("@modify_by", OdbcType.Int) With {.Value = actingMemberId},
            New OdbcParameter("@script_id", OdbcType.Int) With {.Value = command.ScriptId},
            New OdbcParameter("@update_no", OdbcType.Int) With {.Value = command.ExpectedUpdateNo})

        If affected = 0 Then
            ThrowConcurrency("Script")
        End If
    End Sub

    Public Sub ActivateScript(command As ScriptLifecycleCommand, actingMemberId As Integer) Implements IAccessManagerRepository.ActivateScript
        Const sql As String =
            "update script set inactive = 'N', modify_by = ?, modify_dt = now(), update_no = update_no + 1 " &
            "where script_id = ? and update_no = ? and inactive = 'Y'"

        Dim affected = ExecuteNonQuery(sql,
            New OdbcParameter("@modify_by", OdbcType.Int) With {.Value = actingMemberId},
            New OdbcParameter("@script_id", OdbcType.Int) With {.Value = command.ScriptId},
            New OdbcParameter("@update_no", OdbcType.Int) With {.Value = command.ExpectedUpdateNo})

        If affected = 0 Then
            ThrowConcurrency("Script")
        End If
    End Sub

    Public Sub HardDeleteScript(command As HardDeleteScriptCommand, actingMemberId As Integer) Implements IAccessManagerRepository.HardDeleteScript
        Using connection As New OdbcConnection(_connectionString)
            connection.Open()
            Using transaction = connection.BeginTransaction()
                EnsureExists(connection, transaction, "select script_id from script where script_id = ?", command.ScriptId, "Script")

                ExecuteNonQuery(connection, transaction,
                    "delete from access where secure_ty = 'SCRI' and secure_id = ?",
                    New OdbcParameter("@secure_id", OdbcType.Int) With {.Value = command.ScriptId})
                ExecuteNonQuery(connection, transaction,
                    "delete from section_script where script_id = ?",
                    New OdbcParameter("@script_id", OdbcType.Int) With {.Value = command.ScriptId})

                Dim affected = ExecuteNonQuery(connection, transaction,
                    "delete from script where script_id = ? and update_no = ?",
                    New OdbcParameter("@script_id", OdbcType.Int) With {.Value = command.ScriptId},
                    New OdbcParameter("@update_no", OdbcType.Int) With {.Value = command.ExpectedUpdateNo})
                If affected = 0 Then
                    ThrowConcurrency("Script")
                End If

                transaction.Commit()
            End Using
        End Using
    End Sub

    Public Sub AddSectionScript(command As AddSectionScriptCommand, actingMemberId As Integer) Implements IAccessManagerRepository.AddSectionScript
        Using connection As New OdbcConnection(_connectionString)
            connection.Open()
            Using transaction = connection.BeginTransaction()
                If SectionScriptExists(connection, transaction, command.SectionId, command.ScriptId) Then
                    Throw New AdminShellValidationException("Script is already assigned to this section.")
                End If

                Dim nextPosition = GetMaxPosition(connection, transaction,
                    "select max(position) from section_script where section_id = ?", command.SectionId) + 1
                If nextPosition <= 0 Then
                    nextPosition = 1
                End If

                Const sql As String =
                    "insert into section_script (section_id,script_id,position,modify_by,modify_dt,create_by,create_dt,update_no) " &
                    "values (?,?,?,?,now(),?,now(),0)"
                ExecuteNonQuery(connection, transaction, sql,
                    New OdbcParameter("@section_id", OdbcType.Int) With {.Value = command.SectionId},
                    New OdbcParameter("@script_id", OdbcType.Int) With {.Value = command.ScriptId},
                    New OdbcParameter("@position", OdbcType.Int) With {.Value = nextPosition},
                    New OdbcParameter("@modify_by", OdbcType.Int) With {.Value = actingMemberId},
                    New OdbcParameter("@create_by", OdbcType.Int) With {.Value = actingMemberId})

                transaction.Commit()
            End Using
        End Using
    End Sub

    Public Sub RemoveSectionScript(command As RemoveSectionScriptCommand, actingMemberId As Integer) Implements IAccessManagerRepository.RemoveSectionScript
        Using connection As New OdbcConnection(_connectionString)
            connection.Open()
            Using transaction = connection.BeginTransaction()
                ExecuteNonQuery(connection, transaction,
                    "delete from section_script where section_id = ? and script_id = ?",
                    New OdbcParameter("@section_id", OdbcType.Int) With {.Value = command.SectionId},
                    New OdbcParameter("@script_id", OdbcType.Int) With {.Value = command.ScriptId})

                Dim orderedIds = ListPositionedSectionScriptIds(connection, transaction, command.SectionId)
                RenumberSectionScriptPositions(connection, transaction, command.SectionId, orderedIds, actingMemberId)
                transaction.Commit()
            End Using
        End Using
    End Sub

    Public Sub ReorderSectionItem(command As ReorderSectionItemCommand, actingMemberId As Integer) Implements IAccessManagerRepository.ReorderSectionItem
        Using connection As New OdbcConnection(_connectionString)
            connection.Open()
            Using transaction = connection.BeginTransaction()
                Dim orderedIds = ListPositionedSectionScriptIds(connection, transaction, command.SectionId)
                MoveIdInOrder(orderedIds, command.ScriptId, command.NewPosition)
                RenumberSectionScriptPositions(connection, transaction, command.SectionId, orderedIds, actingMemberId)
                transaction.Commit()
            End Using
        End Using
    End Sub

    Public Function CreateOrReactivateGrant(command As CreateGrantCommand, actingMemberId As Integer) As AccessManagerGrant Implements IAccessManagerRepository.CreateOrReactivateGrant
        Dim existing = FindGrant(command.SecureTy, command.SecureId, command.PrincipalTy, command.PrincipalId)
        If existing IsNot Nothing Then
            If existing.Inactive Then
                ActivateGrant(New GrantLifecycleCommand With {
                    .AccessId = existing.AccessId,
                    .ExpectedUpdateNo = existing.UpdateNo
                }, actingMemberId)
                Return GetGrant(existing.AccessId)
            End If
            Return existing
        End If

        Const sql As String =
            "insert into access (permission_cd,secure_id,secure_ty,user_id,user_ty,modify_by,modify_dt,create_by,create_dt,update_no,inactive) " &
            "values ('G',?,?,?,?,?,now(),?,now(),0,'N')"

        Using connection As New OdbcConnection(_connectionString)
            connection.Open()
            Using transaction = connection.BeginTransaction()
                Using insertCommand As New OdbcCommand(sql, connection, transaction)
                    insertCommand.Parameters.Add("@secure_id", OdbcType.Int).Value = command.SecureId
                    insertCommand.Parameters.Add("@secure_ty", OdbcType.VarChar, 4).Value = command.SecureTy
                    insertCommand.Parameters.Add("@user_id", OdbcType.Int).Value = command.PrincipalId
                    insertCommand.Parameters.Add("@user_ty", OdbcType.VarChar, 4).Value = command.PrincipalTy
                    insertCommand.Parameters.Add("@modify_by", OdbcType.Int).Value = actingMemberId
                    insertCommand.Parameters.Add("@create_by", OdbcType.Int).Value = actingMemberId
                    insertCommand.ExecuteNonQuery()
                End Using
                Dim accessId = GetLastInsertId(connection, transaction)
                transaction.Commit()
                Return GetGrant(accessId)
            End Using
        End Using
    End Function

    Public Sub DeactivateGrant(command As GrantLifecycleCommand, actingMemberId As Integer) Implements IAccessManagerRepository.DeactivateGrant
        Const sql As String =
            "update access set inactive = 'Y', modify_by = ?, modify_dt = now(), update_no = update_no + 1 " &
            "where access_id = ? and update_no = ? and inactive = 'N'"

        Dim affected = ExecuteNonQuery(sql,
            New OdbcParameter("@modify_by", OdbcType.Int) With {.Value = actingMemberId},
            New OdbcParameter("@access_id", OdbcType.Int) With {.Value = command.AccessId},
            New OdbcParameter("@update_no", OdbcType.Int) With {.Value = command.ExpectedUpdateNo})

        If affected = 0 Then
            ThrowConcurrency("Grant")
        End If
    End Sub

    Public Sub ActivateGrant(command As GrantLifecycleCommand, actingMemberId As Integer) Implements IAccessManagerRepository.ActivateGrant
        Const sql As String =
            "update access set inactive = 'N', modify_by = ?, modify_dt = now(), update_no = update_no + 1 " &
            "where access_id = ? and update_no = ? and inactive = 'Y'"

        Dim affected = ExecuteNonQuery(sql,
            New OdbcParameter("@modify_by", OdbcType.Int) With {.Value = actingMemberId},
            New OdbcParameter("@access_id", OdbcType.Int) With {.Value = command.AccessId},
            New OdbcParameter("@update_no", OdbcType.Int) With {.Value = command.ExpectedUpdateNo})

        If affected = 0 Then
            ThrowConcurrency("Grant")
        End If
    End Sub

    Private Shared Function BuildInactiveClause(columnName As String, includeInactive As Boolean) As String
        If includeInactive Then
            Return String.Empty
        End If
        Return " and " & columnName & " = 'N' "
    End Function

    Private Shared Function ReadSection(reader As OdbcDataReader) As AccessManagerSection
        Return New AccessManagerSection With {
            .SectionId = Convert.ToInt32(reader("section_id"), CultureInfo.InvariantCulture),
            .ParentId = Convert.ToInt32(reader("parent_id"), CultureInfo.InvariantCulture),
            .SectionName = AdminShellData.StringValue(reader("section")),
            .Position = AdminShellData.NullableInt(reader("position")),
            .ModifyBy = Convert.ToInt32(reader("modify_by"), CultureInfo.InvariantCulture),
            .ModifyDt = AdminShellData.NullableDate(reader("modify_dt")),
            .CreateBy = Convert.ToInt32(reader("create_by"), CultureInfo.InvariantCulture),
            .CreateDt = AdminShellData.NullableDate(reader("create_dt")),
            .UpdateNo = Convert.ToInt32(reader("update_no"), CultureInfo.InvariantCulture),
            .Inactive = FlagIsTrue(reader("inactive"))
        }
    End Function

    Private Shared Function ReadScript(reader As OdbcDataReader) As AccessManagerScript
        Return New AccessManagerScript With {
            .ScriptId = Convert.ToInt32(reader("script_id"), CultureInfo.InvariantCulture),
            .ScriptTy = AdminShellData.StringValue(reader("script_ty")),
            .ScriptName = AdminShellData.StringValue(reader("script_name")),
            .Title = AdminShellData.StringValue(reader("title")),
            .ModifyBy = Convert.ToInt32(reader("modify_by"), CultureInfo.InvariantCulture),
            .ModifyDt = AdminShellData.NullableDate(reader("modify_dt")),
            .CreateBy = Convert.ToInt32(reader("create_by"), CultureInfo.InvariantCulture),
            .CreateDt = AdminShellData.NullableDate(reader("create_dt")),
            .UpdateNo = Convert.ToInt32(reader("update_no"), CultureInfo.InvariantCulture),
            .Inactive = FlagIsTrue(reader("inactive"))
        }
    End Function

    Private Shared Function ReadSectionItem(reader As OdbcDataReader) As AccessManagerSectionItem
        Return New AccessManagerSectionItem With {
            .SectionId = Convert.ToInt32(reader("section_id"), CultureInfo.InvariantCulture),
            .ScriptId = Convert.ToInt32(reader("script_id"), CultureInfo.InvariantCulture),
            .Position = AdminShellData.NullableInt(reader("position")),
            .ScriptTy = AdminShellData.StringValue(reader("script_ty")),
            .ScriptName = AdminShellData.StringValue(reader("script_name")),
            .Title = AdminShellData.StringValue(reader("title")),
            .UpdateNo = Convert.ToInt32(reader("update_no"), CultureInfo.InvariantCulture),
            .ScriptInactive = FlagIsTrue(reader("inactive"))
        }
    End Function

    Private Shared Function ReadGrant(reader As OdbcDataReader) As AccessManagerGrant
        Return New AccessManagerGrant With {
            .AccessId = Convert.ToInt32(reader("access_id"), CultureInfo.InvariantCulture),
            .PermissionCd = AdminShellData.StringValue(reader("permission_cd")),
            .SecureId = Convert.ToInt32(reader("secure_id"), CultureInfo.InvariantCulture),
            .SecureTy = AdminShellData.StringValue(reader("secure_ty")),
            .UserId = Convert.ToInt32(reader("user_id"), CultureInfo.InvariantCulture),
            .UserTy = AdminShellData.StringValue(reader("user_ty")),
            .ModifyBy = Convert.ToInt32(reader("modify_by"), CultureInfo.InvariantCulture),
            .ModifyDt = AdminShellData.NullableDate(reader("modify_dt")),
            .CreateBy = Convert.ToInt32(reader("create_by"), CultureInfo.InvariantCulture),
            .CreateDt = AdminShellData.NullableDate(reader("create_dt")),
            .UpdateNo = Convert.ToInt32(reader("update_no"), CultureInfo.InvariantCulture),
            .Inactive = FlagIsTrue(reader("inactive"))
        }
    End Function

    Private Shared Sub PopulateGrantLabels(connection As OdbcConnection, grant As AccessManagerGrant)
        If String.Equals(grant.SecureTy, AccessManagerConstants.SecureTypeSection, StringComparison.OrdinalIgnoreCase) Then
            grant.SecureLabel = ReadSectionLabel(connection, grant.SecureId)
        ElseIf String.Equals(grant.SecureTy, AccessManagerConstants.SecureTypeScript, StringComparison.OrdinalIgnoreCase) Then
            grant.SecureLabel = ReadScriptLabel(connection, grant.SecureId)
        End If

        Dim principal = GetPrincipalOnConnection(connection, grant.UserTy, grant.UserId)
        If principal IsNot Nothing Then
            grant.PrincipalLabel = principal.DisplayName
        End If
    End Sub

    Private Shared Function GetPrincipalOnConnection(connection As OdbcConnection, principalTy As String, principalId As Integer) As AccessManagerPrincipal
        If String.Equals(principalTy, AccessManagerConstants.PrincipalTypeUser, StringComparison.OrdinalIgnoreCase) Then
            Return ReadUserPrincipal(connection, principalId)
        End If
        If String.Equals(principalTy, AccessManagerConstants.PrincipalTypeGroup, StringComparison.OrdinalIgnoreCase) Then
            Return ReadGroupPrincipal(connection, principalId)
        End If
        Return Nothing
    End Function

    Private Shared Function ReadSectionLabel(connection As OdbcConnection, sectionId As Integer) As String
        Using command As New OdbcCommand("select section from section where section_id = ?", connection)
            command.Parameters.Add("@section_id", OdbcType.Int).Value = sectionId
            Dim value = command.ExecuteScalar()
            Return AdminShellData.StringValue(value)
        End Using
    End Function

    Private Shared Function ReadScriptLabel(connection As OdbcConnection, scriptId As Integer) As String
        Using command As New OdbcCommand("select title, script_name from script where script_id = ?", connection)
            command.Parameters.Add("@script_id", OdbcType.Int).Value = scriptId
            Using reader = command.ExecuteReader()
                If Not reader.Read() Then
                    Return String.Empty
                End If
                Dim title = AdminShellData.StringValue(reader("title"))
                Dim scriptName = AdminShellData.StringValue(reader("script_name"))
                If title.Length = 0 Then
                    Return scriptName
                End If
                Return title & " (" & scriptName & ")"
            End Using
        End Using
    End Function

    Private Shared Sub AppendUserPrincipals(
        connection As OdbcConnection,
        results As IList(Of AccessManagerPrincipal),
        query As String,
        includeInactive As Boolean,
        limit As Integer)

        Dim inactiveClause = BuildInactiveClause("m.inactive", includeInactive)
        Dim sql =
            "select m.member_id,m.user_name,m.first_name,m.last_name,m.inactive from member_login m " &
            "where (m.user_name like ? or m.first_name like ? or m.last_name like ?)" & inactiveClause &
            " order by m.user_name limit ?"

        Dim likeQuery = "%" & query & "%"
        Using command As New OdbcCommand(sql, connection)
            command.Parameters.Add("@user_name", OdbcType.VarChar, 255).Value = likeQuery
            command.Parameters.Add("@first_name", OdbcType.VarChar, 255).Value = likeQuery
            command.Parameters.Add("@last_name", OdbcType.VarChar, 255).Value = likeQuery
            command.Parameters.Add("@limit", OdbcType.Int).Value = limit
            Using reader = command.ExecuteReader()
                While reader.Read()
                    results.Add(ReadUserPrincipal(reader))
                End While
            End Using
        End Using
    End Sub

    Private Shared Sub AppendGroupPrincipals(
        connection As OdbcConnection,
        results As IList(Of AccessManagerPrincipal),
        query As String,
        includeInactive As Boolean,
        limit As Integer)

        Dim inactiveClause = BuildInactiveClause("g.inactive", includeInactive)
        Dim sql =
            "select g.group_id,g.group,g.inactive from `group` g " &
            "where g.group like ?" & inactiveClause & " order by g.group limit ?"

        Using command As New OdbcCommand(sql, connection)
            command.Parameters.Add("@group", OdbcType.VarChar, 255).Value = "%" & query & "%"
            command.Parameters.Add("@limit", OdbcType.Int).Value = limit
            Using reader = command.ExecuteReader()
                While reader.Read()
                    results.Add(ReadGroupPrincipal(reader))
                End While
            End Using
        End Using
    End Sub

    Private Shared Function ReadUserPrincipal(connection As OdbcConnection, memberId As Integer) As AccessManagerPrincipal
        Const sql As String =
            "select m.member_id,m.user_name,m.first_name,m.last_name,m.inactive from member_login m where m.member_id = ?"
        Using command As New OdbcCommand(sql, connection)
            command.Parameters.Add("@member_id", OdbcType.Int).Value = memberId
            Using reader = command.ExecuteReader()
                If Not reader.Read() Then
                    Return Nothing
                End If
                Return ReadUserPrincipal(reader)
            End Using
        End Using
    End Function

    Private Shared Function ReadGroupPrincipal(connection As OdbcConnection, groupId As Integer) As AccessManagerPrincipal
        Const sql As String = "select g.group_id,g.group,g.inactive from `group` g where g.group_id = ?"
        Using command As New OdbcCommand(sql, connection)
            command.Parameters.Add("@group_id", OdbcType.Int).Value = groupId
            Using reader = command.ExecuteReader()
                If Not reader.Read() Then
                    Return Nothing
                End If
                Return ReadGroupPrincipal(reader)
            End Using
        End Using
    End Function

    Private Shared Function ReadUserPrincipal(reader As OdbcDataReader) As AccessManagerPrincipal
        Dim firstName = AdminShellData.StringValue(reader("first_name"))
        Dim lastName = AdminShellData.StringValue(reader("last_name"))
        Dim userName = AdminShellData.StringValue(reader("user_name"))
        Dim displayName = (lastName & ", " & firstName).Trim(", ".ToCharArray())
        If displayName.Length = 0 Then
            displayName = userName
        Else
            displayName &= " (" & userName & ")"
        End If

        Return New AccessManagerPrincipal With {
            .PrincipalTy = AccessManagerConstants.PrincipalTypeUser,
            .PrincipalId = Convert.ToInt32(reader("member_id"), CultureInfo.InvariantCulture),
            .DisplayName = displayName,
            .UserName = userName,
            .Inactive = FlagIsTrue(reader("inactive"))
        }
    End Function

    Private Shared Function ReadGroupPrincipal(reader As OdbcDataReader) As AccessManagerPrincipal
        Return New AccessManagerPrincipal With {
            .PrincipalTy = AccessManagerConstants.PrincipalTypeGroup,
            .PrincipalId = Convert.ToInt32(reader("group_id"), CultureInfo.InvariantCulture),
            .DisplayName = AdminShellData.StringValue(reader("group")),
            .UserName = String.Empty,
            .Inactive = FlagIsTrue(reader("inactive"))
        }
    End Function

    Private Shared Function CountRows(connection As OdbcConnection, sql As String, id As Integer) As Integer
        Using command As New OdbcCommand(sql, connection)
            command.Parameters.Add("@id", OdbcType.Int).Value = id
            Return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture)
        End Using
    End Function

    Private Sub LoadDirectScriptGrants(connection As OdbcConnection, result As AccessManagerEffectiveAccess)
        Const sql As String =
            "select a.access_id,a.secure_ty,a.secure_id,a.user_ty,a.user_id,a.inactive " &
            "from access a where a.secure_ty = 'SCRI' and a.secure_id = ? and a.permission_cd = 'G' " &
            "and a.user_ty = ? and a.user_id = ?"

        Using command As New OdbcCommand(sql, connection)
            command.Parameters.Add("@script_id", OdbcType.Int).Value = result.ScriptId
            command.Parameters.Add("@user_ty", OdbcType.VarChar, 4).Value = result.PrincipalTy
            command.Parameters.Add("@user_id", OdbcType.Int).Value = result.PrincipalId
            Using reader = command.ExecuteReader()
                While reader.Read()
                    result.DirectScriptGrants.Add(ReadEffectiveGrant(reader, "direct-script"))
                End While
            End Using
        End Using
    End Sub

    Private Sub LoadInheritedSectionGrants(connection As OdbcConnection, result As AccessManagerEffectiveAccess)
        Const sql As String =
            "select a.access_id,a.secure_ty,a.secure_id,a.user_ty,a.user_id,a.inactive,s.section_id,s.section " &
            "from section_script ss " &
            "join section s on s.section_id = ss.section_id " &
            "join access a on a.secure_ty = 'SECT' and a.secure_id = s.section_id " &
            "where ss.script_id = ? and a.permission_cd = 'G' and a.user_ty = ? and a.user_id = ? " &
            "order by s.section"

        Using command As New OdbcCommand(sql, connection)
            command.Parameters.Add("@script_id", OdbcType.Int).Value = result.ScriptId
            command.Parameters.Add("@user_ty", OdbcType.VarChar, 4).Value = result.PrincipalTy
            command.Parameters.Add("@user_id", OdbcType.Int).Value = result.PrincipalId
            Using reader = command.ExecuteReader()
                While reader.Read()
                    Dim grant = ReadEffectiveGrant(reader, "inherited-section")
                    grant.SectionId = Convert.ToInt32(reader("section_id"), CultureInfo.InvariantCulture)
                    grant.SectionName = AdminShellData.StringValue(reader("section"))
                    result.InheritedSectionGrants.Add(grant)
                End While
            End Using
        End Using
    End Sub

    Private Shared Function ReadEffectiveGrant(reader As OdbcDataReader, source As String) As AccessManagerEffectiveGrant
        Return New AccessManagerEffectiveGrant With {
            .AccessId = Convert.ToInt32(reader("access_id"), CultureInfo.InvariantCulture),
            .Source = source,
            .SecureTy = AdminShellData.StringValue(reader("secure_ty")),
            .SecureId = Convert.ToInt32(reader("secure_id"), CultureInfo.InvariantCulture),
            .PrincipalTy = AdminShellData.StringValue(reader("user_ty")),
            .PrincipalId = Convert.ToInt32(reader("user_id"), CultureInfo.InvariantCulture),
            .Inactive = FlagIsTrue(reader("inactive"))
        }
    End Function

    Private Shared Function HasActiveGrant(grants As IList(Of AccessManagerEffectiveGrant)) As Boolean
        Dim grantIndex As Integer
        For grantIndex = 0 To grants.Count - 1
            If Not grants(grantIndex).Inactive Then
                Return True
            End If
        Next
        Return False
    End Function

    Private Shared Function ListPositionedSectionIds(connection As OdbcConnection, transaction As OdbcTransaction) As List(Of Integer)
        Const sql As String = "select section_id from section where inactive = 'N' and position is not null and position > 0 order by position"
        Dim ids As New List(Of Integer)()
        Using command As New OdbcCommand(sql, connection, transaction)
            Using reader = command.ExecuteReader()
                While reader.Read()
                    ids.Add(Convert.ToInt32(reader("section_id"), CultureInfo.InvariantCulture))
                End While
            End Using
        End Using
        Return ids
    End Function

    Private Shared Function ListPositionedSectionScriptIds(connection As OdbcConnection, transaction As OdbcTransaction, sectionId As Integer) As List(Of Integer)
        Const sql As String = "select script_id from section_script where section_id = ? order by position"
        Dim ids As New List(Of Integer)()
        Using command As New OdbcCommand(sql, connection, transaction)
            command.Parameters.Add("@section_id", OdbcType.Int).Value = sectionId
            Using reader = command.ExecuteReader()
                While reader.Read()
                    ids.Add(Convert.ToInt32(reader("script_id"), CultureInfo.InvariantCulture))
                End While
            End Using
        End Using
        Return ids
    End Function

    Private Shared Sub ClearSectionPosition(connection As OdbcConnection, transaction As OdbcTransaction, sectionId As Integer, actingMemberId As Integer)
        ExecuteNonQuery(connection, transaction,
            "update section set position = null, modify_by = ?, modify_dt = now() where section_id = ?",
            New OdbcParameter("@modify_by", OdbcType.Int) With {.Value = actingMemberId},
            New OdbcParameter("@section_id", OdbcType.Int) With {.Value = sectionId})
    End Sub

    Private Shared Sub RenumberSectionPositions(
        connection As OdbcConnection,
        transaction As OdbcTransaction,
        orderedIds As IList(Of Integer),
        actingMemberId As Integer)

        Dim position As Integer = 1
        Dim idIndex As Integer
        For idIndex = 0 To orderedIds.Count - 1
            ExecuteNonQuery(connection, transaction,
                "update section set position = ?, modify_by = ?, modify_dt = now() where section_id = ?",
                New OdbcParameter("@position", OdbcType.Int) With {.Value = position},
                New OdbcParameter("@modify_by", OdbcType.Int) With {.Value = actingMemberId},
                New OdbcParameter("@section_id", OdbcType.Int) With {.Value = orderedIds(idIndex)})
            position += 1
        Next
    End Sub

    Private Shared Sub RenumberSectionScriptPositions(
        connection As OdbcConnection,
        transaction As OdbcTransaction,
        sectionId As Integer,
        orderedIds As IList(Of Integer),
        actingMemberId As Integer)

        Dim position As Integer = 1
        Dim idIndex As Integer
        For idIndex = 0 To orderedIds.Count - 1
            ExecuteNonQuery(connection, transaction,
                "update section_script set position = ?, modify_by = ?, modify_dt = now() where section_id = ? and script_id = ?",
                New OdbcParameter("@position", OdbcType.Int) With {.Value = position},
                New OdbcParameter("@modify_by", OdbcType.Int) With {.Value = actingMemberId},
                New OdbcParameter("@section_id", OdbcType.Int) With {.Value = sectionId},
                New OdbcParameter("@script_id", OdbcType.Int) With {.Value = orderedIds(idIndex)})
            position += 1
        Next
    End Sub

    Private Shared Sub MoveIdInOrder(orderedIds As IList(Of Integer), itemId As Integer, newPosition As Integer)
        Dim currentIndex As Integer = -1
        Dim index As Integer
        For index = 0 To orderedIds.Count - 1
            If orderedIds(index) = itemId Then
                currentIndex = index
                Exit For
            End If
        Next

        If currentIndex < 0 Then
            Throw New AdminShellValidationException("Item was not found in the ordered list.")
        End If

        Dim movedId = orderedIds(currentIndex)
        orderedIds.RemoveAt(currentIndex)
        orderedIds.Insert(newPosition - 1, movedId)
    End Sub

    Private Shared Function GetMaxPosition(connection As OdbcConnection, transaction As OdbcTransaction, sql As String, ParamArray parameters() As Object) As Integer
        Using command As New OdbcCommand(sql, connection, transaction)
            Dim parameterIndex As Integer
            For parameterIndex = 0 To parameters.Length - 1
                command.Parameters.Add("@p" & parameterIndex.ToString(), OdbcType.Int).Value = parameters(parameterIndex)
            Next
            Dim value = command.ExecuteScalar()
            If value Is Nothing OrElse Convert.IsDBNull(value) Then
                Return 0
            End If
            Return Convert.ToInt32(value, CultureInfo.InvariantCulture)
        End Using
    End Function

    Private Shared Function GetLastInsertId(connection As OdbcConnection, transaction As OdbcTransaction) As Integer
        Using command As New OdbcCommand("select last_insert_id()", connection, transaction)
            Return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture)
        End Using
    End Function

    Private Shared Function SectionScriptExists(connection As OdbcConnection, transaction As OdbcTransaction, sectionId As Integer, scriptId As Integer) As Boolean
        Using command As New OdbcCommand("select count(*) from section_script where section_id = ? and script_id = ?", connection, transaction)
            command.Parameters.Add("@section_id", OdbcType.Int).Value = sectionId
            command.Parameters.Add("@script_id", OdbcType.Int).Value = scriptId
            Return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0
        End Using
    End Function

    Private Shared Sub EnsureExists(connection As OdbcConnection, transaction As OdbcTransaction, sql As String, id As Integer, label As String)
        Using command As New OdbcCommand(sql, connection, transaction)
            command.Parameters.Add("@id", OdbcType.Int).Value = id
            Dim value = command.ExecuteScalar()
            If value Is Nothing OrElse Convert.IsDBNull(value) Then
                Throw New AdminShellValidationException(label & " was not found.")
            End If
        End Using
    End Sub

    Private Function ExecuteNonQuery(sql As String, ParamArray parameters() As OdbcParameter) As Integer
        Using connection As New OdbcConnection(_connectionString)
            connection.Open()
            Return ExecuteNonQuery(connection, Nothing, sql, parameters)
        End Using
    End Function

    Private Shared Function ExecuteNonQuery(
        connection As OdbcConnection,
        transaction As OdbcTransaction,
        sql As String,
        ParamArray parameters() As OdbcParameter) As Integer

        Using command As New OdbcCommand(sql, connection, transaction)
            Dim parameterIndex As Integer
            For parameterIndex = 0 To parameters.Length - 1
                command.Parameters.Add(parameters(parameterIndex))
            Next
            Return command.ExecuteNonQuery()
        End Using
    End Function

    Private Shared Sub ThrowConcurrency(entityLabel As String)
        Throw New AdminShellConcurrencyException(entityLabel & " was changed by another user.")
    End Sub

    Private Shared Function FlagIsTrue(value As Object) As Boolean
        Dim flag = AdminShellData.StringValue(value)
        Return String.Equals(flag, "Y", StringComparison.OrdinalIgnoreCase) OrElse
            String.Equals(flag, "1", StringComparison.OrdinalIgnoreCase) OrElse
            String.Equals(flag, "true", StringComparison.OrdinalIgnoreCase)
    End Function
End Class
