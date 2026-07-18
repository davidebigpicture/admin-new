Imports System
Imports System.Collections.Generic

Module CodeAdminServiceTests
    Private _failures As Integer

    Function Main() As Integer
        TestCreateRejectsDuplicatePairs()
        TestGetWorkspaceDoesNotRequireMajorCode()
        TestProtectedValueCannotBeUpdated()
        TestDeleteRequiresSelection()

        If _failures = 0 Then
            Console.WriteLine("All CodeAdminService tests passed.")
            Return 0
        End If

        Console.Error.WriteLine(_failures.ToString() & " CodeAdminService test(s) failed.")
        Return 1
    End Function

    Private Sub TestCreateRejectsDuplicatePairs()
        Dim repository As New FakeCodeAdminRepository()
        repository.Classes.Add(New CodeAdminClass With {
            .CodeClass = "WINDOW_SHADE_CD",
            .CodeClassDesc = "Window Shades",
            .Edit = True
        })
        repository.ExistingPairs("WINDOW_SHADE_CD|BLUE") = True

        Dim service = CreateService(repository)
        AssertThrows(Of AccessManagerValidationException)(
            Sub() service.CreateValue(New CreateCodeValueCommand With {
                .CodeClass = "WINDOW_SHADE_CD",
                .CodeValue = "BLUE",
                .CodeValueDesc = "Blue"
            }),
            "duplicate class/value pairs are rejected")
    End Sub

    Private Sub TestGetWorkspaceDoesNotRequireMajorCode()
        Dim repository As New FakeCodeAdminRepository()
        repository.Classes.Add(New CodeAdminClass With {
            .CodeClass = "WINDOW_SHADE_CD",
            .CodeClassDesc = "Window Shades",
            .Edit = True
        })

        Dim service = CreateService(repository)
        Dim workspace = service.GetWorkspace()

        If workspace.Classes.Count <> 1 OrElse workspace.DefaultCodeClass <> "WINDOW_SHADE_CD" Then
            Fail("workspace loads without resolving major code")
        Else
            Pass("workspace loads without resolving major code")
        End If
    End Sub

    Private Sub TestProtectedValueCannotBeUpdated()
        Dim repository As New FakeCodeAdminRepository()
        repository.Values.Add(New CodeAdminValue With {
            .CodeValueId = 1,
            .CodeClass = "APPLICATION_DB",
            .CodeValue = "DEV_DOMAIN",
            .CodeValueDesc = "Dev Domain",
            .IsProtected = True
        })
        repository.Classes.Add(New CodeAdminClass With {
            .CodeClass = "APPLICATION_DB",
            .CodeClassDesc = "Application DB",
            .Edit = True
        })

        Dim service = CreateService(repository)
        AssertThrows(Of AccessManagerForbiddenException)(
            Sub() service.UpdateValue(New UpdateCodeValueCommand With {
                .CodeValueId = 1,
                .CodeClass = "APPLICATION_DB",
                .CodeValue = "DEV_DOMAIN",
                .CodeValueDesc = "Changed"
            }),
            "protected values cannot be updated")
    End Sub

    Private Sub TestDeleteRequiresSelection()
        Dim service = CreateService(New FakeCodeAdminRepository())
        AssertThrows(Of AccessManagerValidationException)(
            Sub() service.DeleteValues(New DeleteCodeValuesCommand With {
                .CodeValueIds = New List(Of Integer)()
            }),
            "delete requires at least one id")
    End Sub

    Private Function CreateService(repository As FakeCodeAdminRepository) As CodeAdminService
        Return New CodeAdminService(
            New PilotUser With {.MemberId = 42, .UserName = "tester"},
            repository,
            canOpenApp:=True)
    End Function

    Private Sub AssertThrows(Of TException As Exception)(action As Action, message As String)
        Try
            action()
            _failures += 1
            Console.Error.WriteLine("FAIL: " & message)
        Catch ex As TException
            If _failures = 0 Then
                Console.WriteLine("PASS: " & message)
            End If
        Catch ex As Exception
            _failures += 1
            Console.Error.WriteLine("FAIL: " & message & " (threw " & ex.GetType().Name & ")")
        End Try
    End Sub

    Private Sub Pass(message As String)
        Console.WriteLine("PASS: " & message)
    End Sub

    Private Sub Fail(message As String)
        _failures += 1
        Console.Error.WriteLine("FAIL: " & message)
    End Sub
End Module

Friend Class FakeCodeAdminRepository
    Implements ICodeAdminRepository

    Public Classes As New List(Of CodeAdminClass)()
    Public Values As New List(Of CodeAdminValue)()
    Public ExistingPairs As New Dictionary(Of String, Boolean)(StringComparer.OrdinalIgnoreCase)

    Public Function ResolveMajorCode() As String Implements ICodeAdminRepository.ResolveMajorCode
        Return "3900"
    End Function

    Public Function ListEditableClasses() As IList(Of CodeAdminClass) Implements ICodeAdminRepository.ListEditableClasses
        Return Classes
    End Function

    Public Function GetClass(codeClass As String) As CodeAdminClass Implements ICodeAdminRepository.GetClass
        Dim classIndex As Integer
        For classIndex = 0 To Classes.Count - 1
            If String.Equals(Classes(classIndex).CodeClass, codeClass, StringComparison.OrdinalIgnoreCase) Then
                Return Classes(classIndex)
            End If
        Next
        Return Nothing
    End Function

    Public Function ListValues(codeClass As String, search As String) As IList(Of CodeAdminValue) Implements ICodeAdminRepository.ListValues
        Return Values
    End Function

    Public Function GetValueById(codeValueId As Integer) As CodeAdminValue Implements ICodeAdminRepository.GetValueById
        Dim valueIndex As Integer
        For valueIndex = 0 To Values.Count - 1
            If Values(valueIndex).CodeValueId = codeValueId Then
                Return Values(valueIndex)
            End If
        Next
        Return Nothing
    End Function

    Public Function ValuePairExists(codeClass As String, codeValue As String, excludeId As Integer?) As Boolean Implements ICodeAdminRepository.ValuePairExists
        Return ExistingPairs.ContainsKey(codeClass & "|" & codeValue)
    End Function

    Public Function CreateValue(command As CreateCodeValueCommand, majorCode As String) As CodeAdminValue Implements ICodeAdminRepository.CreateValue
        Throw New NotImplementedException()
    End Function

    Public Function UpdateValue(command As UpdateCodeValueCommand) As CodeAdminValue Implements ICodeAdminRepository.UpdateValue
        Throw New NotImplementedException()
    End Function

    Public Function PatchValue(command As PatchCodeValueCommand) As CodeAdminValue Implements ICodeAdminRepository.PatchValue
        Throw New NotImplementedException()
    End Function

    Public Function DeleteValue(codeValueId As Integer) As CodeAdminDeleteResult Implements ICodeAdminRepository.DeleteValue
        Throw New NotImplementedException()
    End Function

    Public Sub ActivateValue(codeClass As String, codeValue As String, majorCode As String) Implements ICodeAdminRepository.ActivateValue
        Throw New NotImplementedException()
    End Sub

    Public Sub DeactivateValue(codeClass As String, codeValue As String, majorCode As String) Implements ICodeAdminRepository.DeactivateValue
        Throw New NotImplementedException()
    End Sub

    Public Sub SetPosition(codeClass As String, codeValue As String, newPosition As Integer) Implements ICodeAdminRepository.SetPosition
        Throw New NotImplementedException()
    End Sub
End Class
