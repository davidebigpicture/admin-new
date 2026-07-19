Imports System
Imports System.Collections.Generic
Imports System.Configuration
Imports System.Data
Imports System.Data.OleDb
Imports System.Globalization
Imports System.Linq

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
            "inactive, major_code, minor_code, order_by, form_display, " &
            "option_value_1, option_value_2, option_value_3, option_value_4, option_value_5, option_value_6, " &
            "option_value_7, option_value_8, option_value_9, option_value_10, option_value_11, option_value_12, " &
            "option_value_13, option_value_14, option_value_15, option_value_16, option_value_17 " &
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

    Public Function ListLookupCodeValues(codeClass As String, selectedValues As IList(Of String), excludeValue As String) As IList(Of CodeAdminFieldOption) Implements ICodeAdminRepository.ListLookupCodeValues
        Dim sql = "select code_value, code_value_desc from code_value where code_class = ? and (inactive = 'N'"
        Dim parameters As New List(Of OleDbParameter)()
        parameters.Add(New OleDbParameter("@code_class", OleDbType.VarChar, 50) With {.Value = codeClass})
        If selectedValues IsNot Nothing AndAlso selectedValues.Count > 0 Then
            sql &= " or code_value in (" & String.Join(", ", Enumerable.Repeat("?", selectedValues.Count).ToArray()) & ")"
            Dim selectedIndex As Integer
            For selectedIndex = 0 To selectedValues.Count - 1
                parameters.Add(New OleDbParameter("@selected_value", OleDbType.VarChar, 50) With {.Value = selectedValues(selectedIndex)})
            Next
        End If
        sql &= ")"
        If Not String.IsNullOrWhiteSpace(excludeValue) Then
            sql &= " and code_value <> ?"
            parameters.Add(New OleDbParameter("@excluded_value", OleDbType.VarChar, 50) With {.Value = excludeValue})
        End If
        sql &= " order by upper(code_value_desc), upper(code_value)"
        Return ReadLookupOptions(sql, parameters)
    End Function

    Public Function ListLookupCodeClasses(selectedValue As String) As IList(Of CodeAdminFieldOption) Implements ICodeAdminRepository.ListLookupCodeClasses
        Const sql As String = "select code_class, code_class_desc from code_class order by upper(code_class_desc), upper(code_class)"
        Return ReadLookupOptions(sql, Nothing)
    End Function

    Public Function ListOrgSubTypeColumns(orgSubTypeCode As String, includeFunctionFields As Boolean) As IList(Of CodeAdminFieldOption) Implements ICodeAdminRepository.ListOrgSubTypeColumns
        Const sql As String =
            "select membership_column_detail.column_desc, membership_column_detail.column_rpt_desc, membership_column_detail.data_type, membership_column_detail.form_field_type " &
            "from membership_column_detail, code_value where membership_column_detail.org_sub_ty_cd = code_value.code_value " &
            "and code_value.code_class = 'ORG_SUB_TY_CD' and membership_column_detail.inactive = 'N' and membership_column_detail.org_sub_ty_cd = ? " &
            "union select org_column_detail.column_desc, org_column_detail.column_rpt_desc, org_column_detail.data_type, org_column_detail.form_field_type " &
            "from org_column_detail, code_value where org_column_detail.org_sub_ty_cd = code_value.code_value " &
            "and code_value.code_class = 'ORG_SUB_TY_CD' and org_column_detail.inactive = 'N' and org_column_detail.org_sub_ty_cd = ? order by 2, 1"
        Dim options As New List(Of CodeAdminFieldOption)()
        Using connection As New OleDbConnection(_connectionString), command As New OleDbCommand(sql, connection)
            command.Parameters.Add("@org_sub_ty_cd_member", OleDbType.VarChar, 50).Value = orgSubTypeCode
            command.Parameters.Add("@org_sub_ty_cd_detail", OleDbType.VarChar, 50).Value = orgSubTypeCode
            connection.Open()
            Using reader = command.ExecuteReader()
                While reader.Read()
                    Dim columnName = DbString(ReaderValue(reader, "column_desc"))
                    Dim dataType = DbString(ReaderValue(reader, "data_type"))
                    Dim fieldType = DbString(ReaderValue(reader, "form_field_type"))
                    If (Not includeFunctionFields AndAlso (String.Equals(dataType, "FUNCTION", StringComparison.OrdinalIgnoreCase) OrElse String.Equals(fieldType, "FUNCTION", StringComparison.OrdinalIgnoreCase) OrElse String.Equals(columnName, "ORG_RELATE_ID", StringComparison.OrdinalIgnoreCase) OrElse String.Equals(columnName, "ORG_RELATE_ID_2", StringComparison.OrdinalIgnoreCase))) Then
                        Continue While
                    End If
                    options.Add(New CodeAdminFieldOption With {.Value = columnName, .Label = DbString(ReaderValue(reader, "column_rpt_desc"))})
                End While
            End Using
        End Using
        Return options
    End Function

    Public Function ListFacPrefEmailFields() As IList(Of CodeAdminFieldOption) Implements ICodeAdminRepository.ListFacPrefEmailFields
        Const sql As String =
            "select column_desc, column_rpt_desc from membership_column_detail where form_field_type like 'EMAIL%' and org_sub_ty_cd = 'EMP' " &
            "union select column_desc, column_rpt_desc from org_column_detail where form_field_type like 'EMAIL%' and org_sub_ty_cd = 'EMP' order by 2, 1"
        Return ReadLookupOptions(sql, Nothing)
    End Function

    Public Function ListProducts(organizationId As String) As IList(Of CodeAdminFieldOption) Implements ICodeAdminRepository.ListProducts
        Dim productTable = If(String.Equals(organizationId, "825", StringComparison.OrdinalIgnoreCase), "product", "batch_product")
        Dim sql = "select product_id, product_desc from " & productTable & " order by inactive, product_desc"
        Try
            Return ReadLookupOptions(sql, Nothing)
        Catch ex As OleDbException
            Throw New AccessManagerServiceException("Product lookup is unavailable for this organization.")
        End Try
    End Function

    Public Function GetValueById(codeValueId As Integer) As CodeAdminValue Implements ICodeAdminRepository.GetValueById
        Const sql As String =
            "select code_value_id, code_class, code_value, code_value_desc, code_value_long_desc, " &
            "inactive, major_code, minor_code, order_by, form_display, " &
            "option_value_1, option_value_2, option_value_3, option_value_4, option_value_5, option_value_6, " &
            "option_value_7, option_value_8, option_value_9, option_value_10, option_value_11, option_value_12, " &
            "option_value_13, option_value_14, option_value_15, option_value_16, option_value_17 " &
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
        Return CreateValueInternal(command, majorCode, False)
    End Function

    Public Function CreateLicenseObjTypeValue(command As CreateCodeValueCommand, majorCode As String) As CodeAdminValue Implements ICodeAdminRepository.CreateLicenseObjTypeValue
        Return CreateValueInternal(command, majorCode, True)
    End Function

    Private Function CreateValueInternal(command As CreateCodeValueCommand, majorCode As String, rebuildLicenseTables As Boolean) As CodeAdminValue
        Const sql As String =
            "insert into code_value (" &
            "code_class, code_value, code_value_desc, code_value_long_desc, major_code, minor_code, inactive, form_display, " &
            "option_value_1, option_value_2, option_value_3, option_value_4, option_value_5, option_value_6, " &
            "option_value_7, option_value_8, option_value_9, option_value_10, option_value_11, option_value_12, " &
            "option_value_13, option_value_14, option_value_15, option_value_16, option_value_17" &
            ") values (?, ?, ?, ?, ?, ?, 'N', ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)"

        Using connection As New OleDbConnection(_connectionString)
            connection.Open()
            Using transaction = connection.BeginTransaction(), dbCommand As New OleDbCommand(sql, connection, transaction)
            dbCommand.Parameters.Add("@code_class", OleDbType.VarChar, 50).Value = command.CodeClass
            dbCommand.Parameters.Add("@code_value", OleDbType.VarChar, 50).Value = command.CodeValue
            dbCommand.Parameters.Add("@code_value_desc", OleDbType.VarChar, 1000).Value = command.CodeValueDesc
            dbCommand.Parameters.Add("@code_value_long_desc", OleDbType.VarChar, 4000).Value = If(command.CodeValueLongDesc, String.Empty)
            dbCommand.Parameters.Add("@major_code", OleDbType.VarChar, 50).Value = majorCode
            dbCommand.Parameters.Add("@minor_code", OleDbType.VarChar, 50).Value = If(command.MinorCode, String.Empty)
            AddDetailParameters(dbCommand, command.FormDisplay, GetOptionValues(command))
            dbCommand.ExecuteNonQuery()
            If rebuildLicenseTables Then
                RebuildLicenseObjTypeTables(connection, transaction, majorCode)
            End If
            transaction.Commit()
            End Using
        End Using

        Return GetValueByClassAndValue(command.CodeClass, command.CodeValue)
    End Function

    Public Function UpdateValue(command As UpdateCodeValueCommand) As CodeAdminValue Implements ICodeAdminRepository.UpdateValue
        Return UpdateValueInternal(command, Nothing, False)
    End Function

    Public Function UpdateLicenseObjTypeValue(command As UpdateCodeValueCommand, majorCode As String) As CodeAdminValue Implements ICodeAdminRepository.UpdateLicenseObjTypeValue
        Return UpdateValueInternal(command, majorCode, True)
    End Function

    Private Function UpdateValueInternal(command As UpdateCodeValueCommand, majorCode As String, rebuildLicenseTables As Boolean) As CodeAdminValue
        Const sql As String =
            "update code_value set code_value_desc = ?, code_value_long_desc = ?, minor_code = ?, form_display = ?, " &
            "option_value_1 = ?, option_value_2 = ?, option_value_3 = ?, option_value_4 = ?, option_value_5 = ?, option_value_6 = ?, " &
            "option_value_7 = ?, option_value_8 = ?, option_value_9 = ?, option_value_10 = ?, option_value_11 = ?, option_value_12 = ?, " &
            "option_value_13 = ?, option_value_14 = ?, option_value_15 = ?, option_value_16 = ?, option_value_17 = ? " &
            "where code_value_id = ? and code_class = ? and code_value = ?"

        Using connection As New OleDbConnection(_connectionString)
            connection.Open()
            Using transaction = connection.BeginTransaction(), dbCommand As New OleDbCommand(sql, connection, transaction)
            dbCommand.Parameters.Add("@code_value_desc", OleDbType.VarChar, 1000).Value = command.CodeValueDesc
            dbCommand.Parameters.Add("@code_value_long_desc", OleDbType.VarChar, 4000).Value = If(command.CodeValueLongDesc, String.Empty)
            dbCommand.Parameters.Add("@minor_code", OleDbType.VarChar, 50).Value = If(command.MinorCode, String.Empty)
            AddDetailParameters(dbCommand, command.FormDisplay, GetOptionValues(command))
            dbCommand.Parameters.Add("@code_value_id", OleDbType.Integer).Value = command.CodeValueId
            dbCommand.Parameters.Add("@code_class", OleDbType.VarChar, 50).Value = command.CodeClass
            dbCommand.Parameters.Add("@code_value", OleDbType.VarChar, 50).Value = command.CodeValue
            Dim affected = dbCommand.ExecuteNonQuery()
            If affected = 0 Then
                Throw New AccessManagerValidationException("Code value was not found.")
            End If
            If rebuildLicenseTables Then
                RebuildLicenseObjTypeTables(connection, transaction, majorCode)
            End If
            transaction.Commit()
            End Using
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
        Return DeleteValueInternal(codeValueId, Nothing, False)
    End Function

    Public Function DeleteLicenseObjTypeValue(codeValueId As Integer, majorCode As String) As CodeAdminDeleteResult Implements ICodeAdminRepository.DeleteLicenseObjTypeValue
        Return DeleteValueInternal(codeValueId, majorCode, True)
    End Function

    Private Function DeleteValueInternal(codeValueId As Integer, majorCode As String, rebuildLicenseTables As Boolean) As CodeAdminDeleteResult
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
        Using connection As New OleDbConnection(_connectionString)
            connection.Open()
            Using transaction = connection.BeginTransaction(), command As New OleDbCommand(sql, connection, transaction)
                command.Parameters.Add("@code_value_id", OleDbType.Integer).Value = codeValueId
                Dim affected = command.ExecuteNonQuery()
                If affected = 0 Then
                    Throw New AccessManagerValidationException("Code value was not found.")
                End If
                If rebuildLicenseTables Then
                    RebuildLicenseObjTypeTables(connection, transaction, majorCode)
                End If
                transaction.Commit()
            End Using
        End Using

        Return New CodeAdminDeleteResult With {
            .Deleted = True,
            .SkippedInUse = False,
            .Message = String.Empty
        }
    End Function

    Public Sub ActivateValue(codeClass As String, codeValue As String, majorCode As String) Implements ICodeAdminRepository.ActivateValue
        Const sql As String =
            "update code_value set inactive = 'N' " &
            "where code_class = ? and code_value = ? and major_code = ?"

        Using connection As New OleDbConnection(_connectionString)
            connection.Open()
            Using transaction = connection.BeginTransaction(), command As New OleDbCommand(sql, connection, transaction)
                Dim activeValues = ListActiveValuesForPosition(codeClass, connection, transaction)
                command.Parameters.Add("@code_class", OleDbType.VarChar, 50).Value = codeClass
                command.Parameters.Add("@code_value", OleDbType.VarChar, 50).Value = codeValue
                command.Parameters.Add("@major_code", OleDbType.VarChar, 50).Value = majorCode
                Dim affected = command.ExecuteNonQuery()
                If affected = 0 Then
                    Throw New AccessManagerValidationException("Code value was not found.")
                End If
                If activeValues.FindIndex(Function(item) String.Equals(item, codeValue, StringComparison.OrdinalIgnoreCase)) < 0 Then
                    activeValues.Add(codeValue)
                End If
                UpdatePositions(codeClass, activeValues, connection, transaction)
                transaction.Commit()
            End Using
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
        Using connection As New OleDbConnection(_connectionString)
            connection.Open()
            Using transaction = connection.BeginTransaction()
                Dim activeValues = ListActiveValuesForPosition(codeClass, connection, transaction)
                If activeValues.Count = 0 Then
                    transaction.Commit()
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
                UpdatePositions(codeClass, activeValues, connection, transaction)
                transaction.Commit()
            End Using
        End Using
    End Sub

    Public Function GetValueByClassAndValue(codeClass As String, codeValue As String) As CodeAdminValue Implements ICodeAdminRepository.GetValueByClassAndValue
        Const sql As String =
            "select code_value_id, code_class, code_value, code_value_desc, code_value_long_desc, " &
            "inactive, major_code, minor_code, order_by, form_display, " &
            "option_value_1, option_value_2, option_value_3, option_value_4, option_value_5, option_value_6, " &
            "option_value_7, option_value_8, option_value_9, option_value_10, option_value_11, option_value_12, " &
            "option_value_13, option_value_14, option_value_15, option_value_16, option_value_17 " &
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

    Private Function ListActiveValuesForPosition(codeClass As String, connection As OleDbConnection, transaction As OleDbTransaction) As List(Of String)
        Const sql As String =
            "select code_value from code_value where code_class = ? and inactive = 'N' order by order_by, code_value_desc"

        Dim results As New List(Of String)()
        Using command As New OleDbCommand(sql, connection, transaction)
            command.Parameters.Add("@code_class", OleDbType.VarChar, 50).Value = codeClass
            Using reader = command.ExecuteReader()
                While reader.Read()
                    results.Add(DbString(ReaderValue(reader, "code_value")))
                End While
            End Using
        End Using
        Return results
    End Function

    Private Function ReadLookupOptions(sql As String, parameters As IList(Of OleDbParameter)) As IList(Of CodeAdminFieldOption)
        Dim results As New List(Of CodeAdminFieldOption)()
        Using connection As New OleDbConnection(_connectionString), command As New OleDbCommand(sql, connection)
            If parameters IsNot Nothing Then
                Dim parameterIndex As Integer
                For parameterIndex = 0 To parameters.Count - 1
                    command.Parameters.Add(parameters(parameterIndex))
                Next
            End If
            connection.Open()
            Using reader = command.ExecuteReader()
                While reader.Read()
                    results.Add(New CodeAdminFieldOption With {
                        .Value = DbString(reader.GetValue(0)),
                        .Label = DbString(reader.GetValue(1))
                    })
                End While
            End Using
        End Using
        Return results
    End Function

    Private Sub RebuildLicenseObjTypeTables(connection As OleDbConnection, transaction As OleDbTransaction, majorCode As String)
        Using deleteWindowShades As New OleDbCommand("delete from SDHLS_LIC_TYPE_WS", connection, transaction),
              deleteGroups As New OleDbCommand("delete from SDHLS_LIC_TYPE_GROUP", connection, transaction)
            deleteWindowShades.ExecuteNonQuery()
            deleteGroups.ExecuteNonQuery()
        End Using

        Dim values As New List(Of CodeAdminValue)()
        Const selectSql As String = "select code_value, option_value_1, option_value_2 from code_value where code_class = ? and major_code = ?"
        Using selectValues As New OleDbCommand(selectSql, connection, transaction)
            selectValues.Parameters.Add("@code_class", OleDbType.VarChar, 50).Value = "LicenseObjType"
            selectValues.Parameters.Add("@major_code", OleDbType.VarChar, 50).Value = majorCode
            Using reader = selectValues.ExecuteReader()
                While reader.Read()
                    values.Add(New CodeAdminValue With {
                        .CodeValue = DbString(reader.GetValue(0)),
                        .OptionValue1 = DbString(reader.GetValue(1)),
                        .OptionValue2 = DbString(reader.GetValue(2))
                    })
                End While
            End Using
        End Using
        Dim valueIndex As Integer
        For valueIndex = 0 To values.Count - 1
            InsertLicenseRelations(connection, transaction, "insert into SDHLS_LIC_TYPE_WS values (?, ?)", values(valueIndex).CodeValue, values(valueIndex).OptionValue1)
            InsertLicenseRelations(connection, transaction, "insert into SDHLS_LIC_TYPE_GROUP values (?, ?)", values(valueIndex).CodeValue, values(valueIndex).OptionValue2)
        Next
    End Sub

    Private Shared Sub InsertLicenseRelations(connection As OleDbConnection, transaction As OleDbTransaction, sql As String, codeValue As String, storedValues As String)
        Dim values = storedValues.Split(","c)
        Dim valueIndex As Integer
        For valueIndex = 0 To values.Length - 1
            Dim relatedCodeValue = values(valueIndex).Trim()
            If relatedCodeValue.Length = 0 Then
                Continue For
            End If
            Using insertValue As New OleDbCommand(sql, connection, transaction)
                insertValue.Parameters.Add("@code_value", OleDbType.VarChar, 50).Value = codeValue
                insertValue.Parameters.Add("@related_code_value", OleDbType.VarChar, 50).Value = relatedCodeValue
                insertValue.ExecuteNonQuery()
            End Using
        Next
    End Sub

    Private Sub UpdatePositions(codeClass As String, orderedValues As IList(Of String), connection As OleDbConnection, transaction As OleDbTransaction)
        Const sql As String =
            "update code_value set order_by = ? where code_class = ? and code_value = ?"

        Dim position As Integer
        For position = 0 To orderedValues.Count - 1
            Using command As New OleDbCommand(sql, connection, transaction)
                command.Parameters.Add("@order_by", OleDbType.Integer).Value = position + 1
                command.Parameters.Add("@code_class", OleDbType.VarChar, 50).Value = codeClass
                command.Parameters.Add("@code_value", OleDbType.VarChar, 50).Value = orderedValues(position)
                command.ExecuteNonQuery()
            End Using
        Next
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
            .OptionValue1 = DbString(ReaderValue(reader, "option_value_1")),
            .OptionValue2 = DbString(ReaderValue(reader, "option_value_2")),
            .OptionValue3 = DbString(ReaderValue(reader, "option_value_3")),
            .OptionValue4 = DbString(ReaderValue(reader, "option_value_4")),
            .OptionValue5 = DbString(ReaderValue(reader, "option_value_5")),
            .OptionValue6 = DbString(ReaderValue(reader, "option_value_6")),
            .OptionValue7 = DbString(ReaderValue(reader, "option_value_7")),
            .OptionValue8 = DbString(ReaderValue(reader, "option_value_8")),
            .OptionValue9 = DbString(ReaderValue(reader, "option_value_9")),
            .OptionValue10 = DbString(ReaderValue(reader, "option_value_10")),
            .OptionValue11 = DbString(ReaderValue(reader, "option_value_11")),
            .OptionValue12 = DbString(ReaderValue(reader, "option_value_12")),
            .OptionValue13 = DbString(ReaderValue(reader, "option_value_13")),
            .OptionValue14 = DbString(ReaderValue(reader, "option_value_14")),
            .OptionValue15 = DbString(ReaderValue(reader, "option_value_15")),
            .OptionValue16 = DbString(ReaderValue(reader, "option_value_16")),
            .OptionValue17 = DbString(ReaderValue(reader, "option_value_17")),
            .IsProtected = CodeAdminValidation.ValueIsProtected(codeValue)
        }
    End Function

    Private Shared Sub AddDetailParameters(command As OleDbCommand, formDisplay As String, optionValues As IList(Of String))
        command.Parameters.Add("@form_display", OleDbType.VarChar, CodeAdminConstants.MaxOptionalValueLength).Value = DbParameterValue(formDisplay)
        Dim optionIndex As Integer
        For optionIndex = 0 To optionValues.Count - 1
            command.Parameters.Add("@option_value_" & (optionIndex + 1).ToString(CultureInfo.InvariantCulture), OleDbType.VarChar, CodeAdminConstants.MaxOptionalValueLength).Value = DbParameterValue(optionValues(optionIndex))
        Next
    End Sub

    Private Shared Function GetOptionValues(command As CreateCodeValueCommand) As IList(Of String)
        Return New String() {command.OptionValue1, command.OptionValue2, command.OptionValue3, command.OptionValue4, command.OptionValue5, command.OptionValue6, command.OptionValue7, command.OptionValue8, command.OptionValue9, command.OptionValue10, command.OptionValue11, command.OptionValue12, command.OptionValue13, command.OptionValue14, command.OptionValue15, command.OptionValue16, command.OptionValue17}
    End Function

    Private Shared Function GetOptionValues(command As UpdateCodeValueCommand) As IList(Of String)
        Return New String() {command.OptionValue1, command.OptionValue2, command.OptionValue3, command.OptionValue4, command.OptionValue5, command.OptionValue6, command.OptionValue7, command.OptionValue8, command.OptionValue9, command.OptionValue10, command.OptionValue11, command.OptionValue12, command.OptionValue13, command.OptionValue14, command.OptionValue15, command.OptionValue16, command.OptionValue17}
    End Function

    Private Shared Function DbParameterValue(value As String) As Object
        Return If(value Is Nothing, CType(DBNull.Value, Object), value)
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
