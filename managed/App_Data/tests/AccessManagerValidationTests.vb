Imports System
Imports System.Collections.Generic

Module AccessManagerValidationTests
    Private _failures As Integer

    Function Main() As Integer
        TestSectionNameValidation()
        TestScriptNameValidation()
        TestGrantValidation()
        TestHardDeleteConfirmation()

        If _failures = 0 Then
            Console.WriteLine("All AccessManagerValidation tests passed.")
            Return 0
        End If

        Console.Error.WriteLine(_failures.ToString() & " AccessManagerValidation test(s) failed.")
        Return 1
    End Function

    Private Sub TestSectionNameValidation()
        AssertThrows(Of AccessManagerValidationException)(
            Sub() AccessManagerValidation.ValidateSectionName(""),
            "blank section names are rejected")
        AssertThrows(Of AccessManagerValidationException)(
            Sub() AccessManagerValidation.ValidateSectionName("Bad|Name"),
            "pipe characters are rejected in section names")
        AssertNoThrow(
            Sub() AccessManagerValidation.ValidateSectionName("Logs"),
            "simple section names are accepted")
    End Sub

    Private Sub TestScriptNameValidation()
        AssertThrows(Of AccessManagerValidationException)(
            Sub() AccessManagerValidation.ValidateScriptName("relative/path.asp"),
            "script paths must be absolute")
        AssertThrows(Of AccessManagerValidationException)(
            Sub() AccessManagerValidation.ValidateScriptName("/admin/../views.asp"),
            "parent segments are rejected in script paths")
        AssertNoThrow(
            Sub() AccessManagerValidation.ValidateScriptName("/admin/admin/views.asp"),
            "absolute script paths are accepted")
    End Sub

    Private Sub TestGrantValidation()
        AssertThrows(Of AccessManagerValidationException)(
            Sub() AccessManagerValidation.ValidateGrantTarget("BAD", 1),
            "invalid secure types are rejected")
        AssertThrows(Of AccessManagerValidationException)(
            Sub() AccessManagerValidation.ValidateGrantPrincipal("USER", 0),
            "zero principal ids are rejected")
        AssertNoThrow(
            Sub() AccessManagerValidation.ValidateGrantTarget(AccessManagerConstants.SecureTypeSection, 10),
            "section secure targets are accepted")
    End Sub

    Private Sub TestHardDeleteConfirmation()
        AssertThrows(Of AccessManagerValidationException)(
            Sub() AccessManagerValidation.ValidateHardDeleteConfirmed(False),
            "hard delete requires confirmation")
        AssertNoThrow(
            Sub() AccessManagerValidation.ValidateHardDeleteConfirmed(True),
            "confirmed hard delete is accepted")
    End Sub

    Private Sub AssertTrue(condition As Boolean, message As String)
        If Not condition Then
            _failures += 1
            Console.Error.WriteLine("FAIL: " & message)
        End If
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
