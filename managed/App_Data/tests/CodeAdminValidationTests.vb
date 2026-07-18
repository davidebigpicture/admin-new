Imports System
Imports System.Collections.Generic

Module CodeAdminValidationTests
    Private _failures As Integer

    Function Main() As Integer
        TestCodeValueValidation()
        TestPatchFieldValidation()
        TestProtectedValuesAndClasses()
        TestSqlIdentifierValidation()

        If _failures = 0 Then
            Console.WriteLine("All CodeAdminValidation tests passed.")
            Return 0
        End If

        Console.Error.WriteLine(_failures.ToString() & " CodeAdminValidation test(s) failed.")
        Return 1
    End Function

    Private Sub TestCodeValueValidation()
        AssertThrows(Of AccessManagerValidationException)(
            Sub() CodeAdminValidation.ValidateCodeValue("bad value"),
            "spaces are rejected in code values")
        AssertNoThrow(
            Sub() CodeAdminValidation.ValidateCodeValue("GOOD_VALUE-1"),
            "valid code values are accepted")
    End Sub

    Private Sub TestPatchFieldValidation()
        AssertThrows(Of AccessManagerValidationException)(
            Sub() CodeAdminValidation.ValidatePatchField("code_class", "GROUP_TY_CD"),
            "non-patchable fields are rejected")
        AssertNoThrow(
            Sub() CodeAdminValidation.ValidatePatchField("code_value_desc", "Updated label"),
            "description patches are accepted")
    End Sub

    Private Sub TestProtectedValuesAndClasses()
        AssertTrue(CodeAdminValidation.ValueIsProtected("DEV_DOMAIN"), "DEV_DOMAIN is protected")
        AssertFalse(CodeAdminValidation.ClassAllowsDelete("GROUP_TY_CD"), "GROUP_TY_CD cannot be deleted")
        AssertTrue(CodeAdminValidation.ClassAllowsDelete("WINDOW_SHADE_CD"), "ordinary classes can be deleted")
    End Sub

    Private Sub TestSqlIdentifierValidation()
        AssertThrows(Of AccessManagerServiceException)(
            Sub() CodeAdminValidation.ValidateSqlIdentifier("bad-column"),
            "hyphenated column names are rejected")
        AssertNoThrow(
            Sub() CodeAdminValidation.ValidateSqlIdentifier("VALID_COLUMN_1"),
            "valid column identifiers are accepted")
    End Sub

    Private Sub AssertTrue(condition As Boolean, message As String)
        If Not condition Then
            _failures += 1
            Console.Error.WriteLine("FAIL: " & message)
        End If
    End Sub

    Private Sub AssertFalse(condition As Boolean, message As String)
        AssertTrue(Not condition, message)
    End Sub

    Private Sub AssertNoThrow(action As Action, message As String)
        Try
            action()
            AssertTrue(True, message)
        Catch ex As Exception
            _failures += 1
            Console.Error.WriteLine("FAIL: " & message & " (" & ex.Message & ")")
        End Try
    End Sub

    Private Sub AssertThrows(Of TException As Exception)(action As Action, message As String)
        Try
            action()
            _failures += 1
            Console.Error.WriteLine("FAIL: " & message)
        Catch ex As TException
            AssertTrue(True, message)
        Catch ex As Exception
            _failures += 1
            Console.Error.WriteLine("FAIL: " & message & " (threw " & ex.GetType().Name & ")")
        End Try
    End Sub
End Module
