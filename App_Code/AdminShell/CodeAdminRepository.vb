Imports System
Imports System.Collections.Generic
Imports System.Configuration
Imports System.Data
Imports System.Data.OleDb
Imports System.Globalization

Public Class CodeAdminRepository
    Implements ICodeAdminRepository

    Private ReadOnly _connectionString As String

    Public Sub New()
        Dim configured = ConfigurationManager.ConnectionStrings("ConnectionString")
        If configured Is Nothing OrElse String.IsNullOrWhiteSpace(configured.ConnectionString) Then
            Throw New ConfigurationErrorsException("ConnectionString is not configured for Code Admin.")
        End If
        _connectionString = configured.ConnectionString
    End Sub

    Public Function ResolveMajorCode() As String Implements ICodeAdminRepository.ResolveMajorCode
        Dim configured = If(ConfigurationManager.AppSettings("CodeAdminMajorCode"), String.Empty).Trim()
        If Not String.IsNullOrWhiteSpace(configured) Then
            Return configured
        End If

        Const sql As String =
            "select code_value_desc from code_value " &
            "where code_class = 'APPLICATION_DB' and code_value = 'ORG_ID' and inactive = 'N'"

        Using connection As New OleDbConnection(_connectionString),
              command As New OleDbCommand(sql, connection)
            connection.Open()
            Dim majorCode = DbString(command.ExecuteScalar()).Trim()
            If Not String.IsNullOrWhiteSpace(majorCode) Then
                Return majorCode
            End If
        End Using

        Throw New AccessManagerServiceException("CodeAdminMajorCode is not configured.")
    End Function

    Public Function ListEditableClasses() As IList(Of CodeAdminClass) Implements ICodeAdminRepository.ListEditableClasses
        Dim sql = If(
            ClassEditColumnSupported(),
            "select code_class, code_class_desc, edit from code_class where edit = 'Y' order by code_class_desc",
            "select code_class, code_class_desc from code_class order by code_class_desc")

        Dim results As New List(Of CodeAdminClass)()
        Using connection As New OleDbConnection(_connectionString),
              command As New OleDbCommand(sql, connection)
            connection.Open()
            Using reader = command.ExecuteReader()
                While reader.Read()
                    results.Add(ReadClass(reader, ClassEditColumnSupported()))
                End While
            End Using
        End Using
        Return results
    End Function

    Public Function GetClass(codeClass As String) As CodeAdminClass Implements ICodeAdminRepository.GetClass
        Dim sql = If(
            ClassEditColumnSupported(),
            "select code_class, code_class_desc, edit from code_class where code_class = ?",
            "select code_class, code_class_desc from code_class where code_class = ?")

        Using connection As New OleDbConnection(_connectionString),
              command As New OleDbCommand(sql, connection)
            command.Parameters.Add("@code_class", OleDbType.VarChar, 50).Value = codeClass
            connection.Open()
            Using reader = command.ExecuteReader()
                If Not reader.Read() Then
                    Return Nothing
                End If
                Return ReadClass(reader, ClassEditColumnSupported())
            End Using
        End Using
    End Function

    Public Function ListValues(codeClass As String, search As String) As IList(Of CodeAdminValue) Implements ICodeAdminRepository.ListValues
        Dim sql =
            "select code_value_id, code_class, code_value, code_value_desc, code_value_long_desc, " &
            "inactive, major_code, minor_code, order_by, form_display " &
            "from code_value where code_class = ?"

        Dim parameters As New List(Of OleDbParameter)()
        parameters.Add(New OleDbParameter("@code_class", OleDbType.VarChar, 50) With {.Value = codeClass})

        If Not String.IsNullOrWhiteSpace(search) Then
            sql &= " and ((upper(code_value) like upper('%'||?||'%')) or (upper(code_value_desc) like upper('%'||?||'%')))"
            parameters.Add(New OleDbParameter("@search_1", OleDbType.VarChar, 100) With {.Value = search.Trim()})
            parameters.Add(New OleDbParameter("@search_2", OleDbType.VarChar, 100) With {.Value = search.Trim()})
        End If

        If String.Equals(codeClass, "PERMISSION_CD", StringComparison.OrdinalIgnoreCase) Then
            sql &= " and code_value not like '%876%'"
        End If

        sql &= " order by order_by, code_value_desc"

        Dim results As New List(Of CodeAdminValue)()
        Using connection As New OleDbConnection(_connectionString),
              command As New OleDbCommand(sql, connection)
            Dim parameterIndex As Integer
            For parameterIndex = 0 To parameters.Count - 1
                command.Parameters.Add(parameters(parameterIndex))
            Next
            connection.Open()
            Using reader = command.ExecuteReader()
                While reader.Read()
                    results.Add(ReadValue(reader))
                End While
            End Using
        End Using
        Return results
    End Function

    Public Function GetValueById(codeValueId As Integer) As CodeAdminValue Implements ICodeAdminRepository.GetValueById
        Const sql As String =
            "select code_value_id, code_class, code_value, code_value_desc, code_value_long_desc, " &
            "inactive, major_code, minor_code, order_by, form_display " &
            "from code_value where code_value_id = ?"

        Using connection As New OleDbConnection(_connectionString),
              command As New OleDbCommand(sql, connection)
            command.Parameters.Add("@code_value_id", OleDbType.Integer).Value = codeValueId
            connection.Open()
            Using reader = command.ExecuteReader()
                If Not reader.Read() Then
                    Return Nothing
                End If
                Return ReadValue(reader)
            End Using
        End Using
    End Function

    Public Function ValuePairExists(codeClass As String, codeValue As String, excludeId As Integer?) As Boolean Implements ICodeAdminRepository.ValuePairExists
        Dim sql = "select count(*) from code_value where code_class = ? and code_value = ?"
        If excludeId.HasValue Then
            sql &= " and code_value_id <> ?"
        End If

        Using connection As New OleDbConnection(_connectionString),
              command As New OleDbCommand(sql, connection)
            command.Parameters.Add("@code_class", OleDbType.VarChar, 50).Value = codeClass
            command.Parameters.Add("@code_value", OleDbType.VarChar, 50).Value = codeValue
            If excludeId.HasValue Then
                command.Parameters.Add("@exclude_id", OleDbType.Integer).Value = excludeId.Value
            End If
            connection.Open()
            Return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0
        End Using
    End Function

    Public Function CreateValue(command As CreateCodeValueCommand, majorCode As String) As CodeAdminValue Implements ICodeAdminRepository.CreateValue
        Const sql As String =
            "insert into code_value (" &
            "code_class, code_value, code_value_desc, code_value_long_desc, major_code, minor_code, inactive, form_display" &
            ") values (?, ?, ?, ?, ?, ?, 'N', '')"

        Using connection As New OleDbConnection(_connectionString),
              dbCommand As New OleDbCommand(sql, connection)
            dbCommand.Parameters.Add("@code_class", OleDbType.VarChar, 50).Value = command.CodeClass
            dbCommand.Parameters.Add("@code_value", OleDbType.VarChar, 50).Value = command.CodeValue
            dbCommand.Parameters.Add("@code_value_desc", OleDbType.VarChar, 1000).Value = command.CodeValueDesc
            dbCommand.Parameters.Add("@code_value_long_desc", OleDbType.VarChar, 4000).Value = If(command.CodeValueLongDesc, String.Empty)
            dbCommand.Parameters.Add("@major_code", OleDbType.VarChar, 50).Value = majorCode
            dbCommand.Parameters.Add("@minor_code", OleDbType.VarChar, 50).Value = If(command.MinorCode, String.Empty)
            connection.Open()
            dbCommand.ExecuteNonQuery()
        End Using

        Return GetValueByClassAndValue(command.CodeClass, command.CodeValue)
    End Function

    Public Function UpdateValue(command As UpdateCodeValueCommand) As CodeAdminValue Implements ICodeAdminRepository.UpdateValue
        Const sql As String =
            "update code_value set code_value_desc = ?, code_value_long_desc = ?, minor_code = ? " &
            "where code_value_id = ? and code_class = ? and code_value = ?"

        Using connection As New OleDbConnection(_connectionString),
              dbCommand As New OleDbCommand(sql, connection)
            dbCommand.Parameters.Add("@code_value_desc", OleDbType.VarChar, 1000).Value = command.CodeValueDesc
            dbCommand.Parameters.Add("@code_value_long_desc", OleDbType.VarChar, 4000).Value = If(command.CodeValueLongDesc, String.Empty)
            dbCommand.Parameters.Add("@minor_code", OleDbType.VarChar, 50).Value = If(command.MinorCode, String.Empty)
            dbCommand.Parameters.Add("@code_value_id", OleDbType.Integer).Value = command.CodeValueId
            dbCommand.Parameters.Add("@code_class", OleDbType.VarChar, 50).Value = command.CodeClass
            dbCommand.Parameters.Add("@code_value", OleDbType.VarChar, 50).Value = command.CodeValue
            connection.Open()
            Dim affected = dbCommand.ExecuteNonQuery()
            If affected = 0 Then
                Throw New AccessManagerValidationException("Code value was not found.")
            End If
        End Using

        Return GetValueById(command.CodeValueId)
    End Function

    Public Function PatchValue(command As PatchCodeValueCommand) As CodeAdminValue Implements ICodeAdminRepository.PatchValue
        Dim fieldName = command.FieldName.Trim().ToLowerInvariant()
        Dim sql As String

        Select Case fieldName
            Case "code_value_desc"
                sql = "update code_value set code_value_desc = ? where code_value_id = ?"
            Case "code_value_long_desc"
                sql = "update code_value set code_value_long_desc = ? where code_value_id = ?"
            Case "inactive"
                sql = "update code_value set inactive = ? where code_value_id = ?"
            Case "order_by"
                sql = "update code_value set order_by = ? where code_value_id = ?"
            Case Else
                Throw New AccessManagerValidationException("Field cannot be updated inline.")
        End Select

        Using connection As New OleDbConnection(_connectionString),
              dbCommand As New OleDbCommand(sql, connection)
            If fieldName = "order_by" Then
                dbCommand.Parameters.Add("@field_value", OleDbType.Integer).Value = Integer.Parse(command.FieldValue, CultureInfo.InvariantCulture)
            Else
                dbCommand.Parameters.Add("@field_value", OleDbType.VarChar, 4000).Value = command.FieldValue
            End If
            dbCommand.Parameters.Add("@code_value_id", OleDbType.Integer).Value = command.CodeValueId
            connection.Open()
            Dim affected = dbCommand.ExecuteNonQuery()
            If affected = 0 Then
                Throw New AccessManagerValidationException("Code value was not found.")
            End If
        End Using

        Return GetValueById(command.CodeValueId)
    End Function

    Public Function DeleteValue(codeValueId As Integer) As CodeAdminDeleteResult Implements ICodeAdminRepository.DeleteValue
        Dim existing = GetValueById(codeValueId)
        If existing Is Nothing Then
            Throw New AccessManagerValidationException("Code value was not found.")
        End If

        If CodeAdminValidation.ValueIsProtected(existing.CodeValue) Then
            Throw New AccessManagerForbiddenException("This code value cannot be deleted.")
        End If

        If Not CodeAdminValidation.ClassAllowsDelete(existing.CodeClass) Then
            Throw New AccessManagerForbiddenException("This code class cannot be deleted.")
        End If

        If IsValueInUse(existing) Then
            Return New CodeAdminDeleteResult With {
                .Deleted = False,
                .SkippedInUse = True,
                .Message = "Value '" & existing.CodeValueDesc & "' is being used in CRM and cannot be deleted."
            }
        End If

        Const sql As String = "delete from code_value where code_value_id = ?"
        Using connection As New OleDbConnection(_connectionString),
              command As New OleDbCommand(sql, connection)
            command.Parameters.Add("@code_value_id", OleDbType.Integer).Value = codeValueId
            connection.Open()
            command.ExecuteNonQuery()
        End Using

        Return New CodeAdminDeleteResult With {
            .Deleted = True,
            .SkippedInUse = False,
            .Message = String.Empty
        }
    End Function

    Public Sub ActivateValue(codeClass As String, codeValue As String, majorCode As String) Implements ICodeAdminRepository.ActivateValue
        SetPosition(codeClass, codeValue, GetNextActivePosition(codeClass))
        Const sql As String =
            "update code_value set inactive = 'N' " &
            "where code_class = ? and code_value = ? and major_code = ?"

        Using connection As New OleDbConnection(_connectionString),
              command As New OleDbCommand(sql, connection)
            command.Parameters.Add("@code_class", OleDbType.VarChar, 50).Value = codeClass
            command.Parameters.Add("@code_value", OleDbType.VarChar, 50).Value = codeValue
            command.Parameters.Add("@major_code", OleDbType.VarChar, 50).Value = majorCode
            connection.Open()
            Dim affected = command.ExecuteNonQuery()
            If affected = 0 Then
                Throw New AccessManagerValidationException("Code value was not found.")
            End If
        End Using
    End Sub

    Public Sub DeactivateValue(codeClass As String, codeValue As String, majorCode As String) Implements ICodeAdminRepository.DeactivateValue
        Const sql As String =
            "update code_value set inactive = 'Y' " &
            "where code_class = ? and code_value = ? and major_code = ?"

        Using connection As New OleDbConnection(_connectionString),
              command As New OleDbCommand(sql, connection)
            command.Parameters.Add("@code_class", OleDbType.VarChar, 50).Value = codeClass
            command.Parameters.Add("@code_value", OleDbType.VarChar, 50).Value = codeValue
            command.Parameters.Add("@major_code", OleDbType.VarChar, 50).Value = majorCode
            connection.Open()
            Dim affected = command.ExecuteNonQuery()
            If affected = 0 Then
                Throw New AccessManagerValidationException("Code value was not found.")
            End If
        End Using
    End Sub

    Public Sub SetPosition(codeClass As String, codeValue As String, newPosition As Integer) Implements ICodeAdminRepository.SetPosition
        Dim activeValues = ListActiveValuesForPosition(codeClass)
        If activeValues.Count = 0 Then
            Return
        End If

        Dim currentIndex = activeValues.FindIndex(Function(item) String.Equals(item, codeValue, StringComparison.OrdinalIgnoreCase))
        If currentIndex < 0 Then
            activeValues.Add(codeValue)
            currentIndex = activeValues.Count - 1
        End If

        activeValues.RemoveAt(currentIndex)
        If newPosition < 1 Then
            newPosition = 1
        End If
        If newPosition > activeValues.Count + 1 Then
            newPosition = activeValues.Count + 1
        End If
        activeValues.Insert(newPosition - 1, codeValue)
        UpdatePositions(codeClass, activeValues)
    End Sub

    Private Function GetValueByClassAndValue(codeClass As String, codeValue As String) As CodeAdminValue
        Const sql As String =
            "select code_value_id, code_class, code_value, code_value_desc, code_value_long_desc, " &
            "inactive, major_code, minor_code, order_by, form_display " &
            "from code_value where code_class = ? and code_value = ?"

        Using connection As New OleDbConnection(_connectionString),
              command As New OleDbCommand(sql, connection)
            command.Parameters.Add("@code_class", OleDbType.VarChar, 50).Value = codeClass
            command.Parameters.Add("@code_value", OleDbType.VarChar, 50).Value = codeValue
            connection.Open()
            Using reader = command.ExecuteReader()
                If Not reader.Read() Then
                    Return Nothing
                End If
                Return ReadValue(reader)
            End Using
        End Using
    End Function

    Private Function ListActiveValuesForPosition(codeClass As String) As List(Of String)
        Const sql As String =
            "select code_value from code_value where code_class = ? and inactive = 'N' order by order_by, code_value_desc"

        Dim results As New List(Of String)()
        Using connection As New OleDbConnection(_connectionString),
              command As New OleDbCommand(sql, connection)
            command.Parameters.Add("@code_class", OleDbType.VarChar, 50).Value = codeClass
            connection.Open()
            Using reader = command.ExecuteReader()
                While reader.Read()
                    results.Add(DbString(ReaderValue(reader, "code_value")))
                End While
            End Using
        End Using
        Return results
    End Function

    Private Function GetNextActivePosition(codeClass As String) As Integer
        Const sql As String =
            "select count(*) from code_value where code_class = ? and inactive = 'N' and order_by is not null"

        Using connection As New OleDbConnection(_connectionString),
              command As New OleDbCommand(sql, connection)
            command.Parameters.Add("@code_class", OleDbType.VarChar, 50).Value = codeClass
            connection.Open()
            Return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) + 1
        End Using
    End Function

    Private Sub UpdatePositions(codeClass As String, orderedValues As IList(Of String))
        Const sql As String =
            "update code_value set order_by = ? where code_class = ? and code_value = ?"

        Using connection As New OleDbConnection(_connectionString)
            connection.Open()
            Dim position As Integer
            For position = 0 To orderedValues.Count - 1
                Using command As New OleDbCommand(sql, connection)
                    command.Parameters.Add("@order_by", OleDbType.Integer).Value = position + 1
                    command.Parameters.Add("@code_class", OleDbType.VarChar, 50).Value = codeClass
                    command.Parameters.Add("@code_value", OleDbType.VarChar, 50).Value = orderedValues(position)
                    command.ExecuteNonQuery()
                End Using
            Next
        End Using
    End Sub

    Private Function IsValueInUse(existing As CodeAdminValue) As Boolean
        Dim columns = ListReferencingColumns(existing.CodeClass)
        If columns.Count = 0 Then
            Return False
        End If

        Dim columnIndex As Integer
        For columnIndex = 0 To columns.Count - 1
            Dim column = columns(columnIndex)
            Dim sql As String
            If String.Equals(column.ColumnType, "DETAIL", StringComparison.OrdinalIgnoreCase) Then
                sql =
                    "select count(*) from member_detail, org_column_detail " &
                    "where org_column_detail.column_id = member_detail.column_id " &
                    "and org_column_detail.column_desc = ? and member_detail.data = ?"
            Else
                Dim columnName = CodeAdminValidation.ValidateSqlIdentifier(column.ColumnDesc)
                sql = "select count(*) from membership where " & columnName & " = ?"
            End If

            Using connection As New OleDbConnection(_connectionString),
                  command As New OleDbCommand(sql, connection)
                If String.Equals(column.ColumnType, "DETAIL", StringComparison.OrdinalIgnoreCase) Then
                    command.Parameters.Add("@column_desc", OleDbType.VarChar, 100).Value = column.ColumnDesc
                End If
                command.Parameters.Add("@code_value", OleDbType.VarChar, 50).Value = existing.CodeValue
                connection.Open()
                Dim count = Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture)
                If count > 0 Then
                    Return True
                End If
            End Using
        Next

        Return False
    End Function

    Private Function ListReferencingColumns(codeClass As String) As IList(Of CodeAdminReferenceColumn)
        Const sql As String =
            "select 'MEMBER' as column_type, column_desc from membership_column_detail where code_class = ? " &
            "union select 'DETAIL' as column_type, column_desc from org_column_detail where code_class = ?"

        Dim results As New List(Of CodeAdminReferenceColumn)()
        Using connection As New OleDbConnection(_connectionString),
              command As New OleDbCommand(sql, connection)
            command.Parameters.Add("@code_class_1", OleDbType.VarChar, 50).Value = codeClass
            command.Parameters.Add("@code_class_2", OleDbType.VarChar, 50).Value = codeClass
            connection.Open()
            Using reader = command.ExecuteReader()
                While reader.Read()
                    results.Add(New CodeAdminReferenceColumn With {
                        .ColumnType = DbString(ReaderValue(reader, "column_type")),
                        .ColumnDesc = DbString(ReaderValue(reader, "column_desc"))
                    })
                End While
            End Using
        End Using
        Return results
    End Function

    Private _classEditColumnSupported As Boolean?

    Private Function ClassEditColumnSupported() As Boolean
        If Not _classEditColumnSupported.HasValue Then
            _classEditColumnSupported = DetectClassEditColumn()
        End If
        Return _classEditColumnSupported.Value
    End Function

    Private Function DetectClassEditColumn() As Boolean
        Const probeSql As String = "select edit from code_class where 1 = 0"
        Try
            Using connection As New OleDbConnection(_connectionString),
                  command As New OleDbCommand(probeSql, connection)
                connection.Open()
                Using reader = command.ExecuteReader()
                End Using
            End Using
            Return True
        Catch
            Return False
        End Try
    End Function

    Private Shared Function ReadClass(reader As IDataRecord, editColumnSupported As Boolean) As CodeAdminClass
        Dim isEditable = Not editColumnSupported OrElse
            String.Equals(DbString(ReaderValue(reader, "edit")), "Y", StringComparison.OrdinalIgnoreCase)
        Return New CodeAdminClass With {
            .CodeClass = DbString(ReaderValue(reader, "code_class")),
            .CodeClassDesc = DbString(ReaderValue(reader, "code_class_desc")),
            .Edit = isEditable
        }
    End Function

    Private Shared Function ReadValue(reader As IDataRecord) As CodeAdminValue
        Dim codeValue = DbString(ReaderValue(reader, "code_value"))
        Return New CodeAdminValue With {
            .CodeValueId = Convert.ToInt32(ReaderValue(reader, "code_value_id"), CultureInfo.InvariantCulture),
            .CodeClass = DbString(ReaderValue(reader, "code_class")),
            .CodeValue = codeValue,
            .CodeValueDesc = DbString(ReaderValue(reader, "code_value_desc")),
            .CodeValueLongDesc = DbString(ReaderValue(reader, "code_value_long_desc")),
            .Inactive = String.Equals(DbString(ReaderValue(reader, "inactive")), CodeAdminConstants.InactiveYes, StringComparison.OrdinalIgnoreCase),
            .MajorCode = DbString(ReaderValue(reader, "major_code")),
            .MinorCode = DbString(ReaderValue(reader, "minor_code")),
            .OrderBy = DbNullableInt(ReaderValue(reader, "order_by")),
            .FormDisplay = DbString(ReaderValue(reader, "form_display")),
            .IsProtected = CodeAdminValidation.ValueIsProtected(codeValue)
        }
    End Function

    Private Shared Function ReaderValue(reader As IDataRecord, columnName As String) As Object
        Dim columnIndex As Integer
        For columnIndex = 0 To reader.FieldCount - 1
            If String.Equals(reader.GetName(columnIndex), columnName, StringComparison.OrdinalIgnoreCase) Then
                Return reader.GetValue(columnIndex)
            End If
        Next
        Throw New InvalidOperationException("Column not found: " & columnName)
    End Function

    Private Shared Function DbString(value As Object) As String
        If value Is Nothing OrElse Convert.IsDBNull(value) Then
            Return String.Empty
        End If
        Return Convert.ToString(value, CultureInfo.InvariantCulture)
    End Function

    Private Shared Function DbNullableInt(value As Object) As Integer?
        If value Is Nothing OrElse Convert.IsDBNull(value) Then
            Return Nothing
        End If
        Return Convert.ToInt32(value, CultureInfo.InvariantCulture)
    End Function

    Private Class CodeAdminReferenceColumn
        Public Property ColumnType As String
        Public Property ColumnDesc As String
    End Class
End Class
