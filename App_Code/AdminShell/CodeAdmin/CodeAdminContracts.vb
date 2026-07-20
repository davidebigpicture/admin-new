Imports System
Imports System.Collections.Generic

<Serializable>
Public Class CodeAdminClass
    Public Property CodeClass As String
    Public Property CodeClassDesc As String
    Public Property Edit As Boolean
End Class

<Serializable>
Public Class CodeAdminValue
    Public Property CodeValueId As Integer
    Public Property CodeClass As String
    Public Property CodeValue As String
    Public Property CodeValueDesc As String
    Public Property CodeValueLongDesc As String
    Public Property Status As String
    Public Property Inactive As Boolean
    Public Property MajorCode As String
    Public Property MinorCode As String
    Public Property OrderBy As Integer?
    Public Property FormDisplay As String
    Public Property OptionValue1 As String
    Public Property OptionValue2 As String
    Public Property OptionValue3 As String
    Public Property OptionValue4 As String
    Public Property OptionValue5 As String
    Public Property OptionValue6 As String
    Public Property OptionValue7 As String
    Public Property OptionValue8 As String
    Public Property OptionValue9 As String
    Public Property OptionValue10 As String
    Public Property OptionValue11 As String
    Public Property OptionValue12 As String
    Public Property OptionValue13 As String
    Public Property OptionValue14 As String
    Public Property OptionValue15 As String
    Public Property OptionValue16 As String
    Public Property OptionValue17 As String
    Public Property IsProtected As Boolean
    Public Property FieldMetadata As CodeAdminDetailMetadata
End Class

<Serializable>
Public Class CodeAdminValuePage
    Public Property Items As IList(Of CodeAdminValue)
    Public Property TotalCount As Integer
    Public Property Start As Integer
    Public Property PageSize As Integer
    Public Property CanDelete As Boolean
End Class

<Serializable>
Public Class CodeAdminWorkspace
    Public Property Classes As IList(Of CodeAdminClass)
    Public Property DefaultCodeClass As String
    Public Property MajorCode As String
    Public Property ShowClassCodes As Boolean
    Public Property FieldMetadata As IDictionary(Of String, CodeAdminDetailMetadata)
End Class

<Serializable>
Public Class CodeAdminFieldOption
    Public Property Value As String
    Public Property Label As String
End Class

<Serializable>
Public Class CodeAdminFieldMetadata
    Public Property Key As String
    Public Property Label As String
    Public Property ControlType As String
    Public Property LookupSource As String
    Public Property Required As Boolean
    Public Property Options As IList(Of CodeAdminFieldOption)
    Public Property Section As String
    Public Property Order As Integer
End Class

<Serializable>
Public Class CodeAdminDetailMetadata
    Public Property Fields As IList(Of CodeAdminFieldMetadata)
End Class

Public Class CodeAdminDeleteResult
    Public Property Deleted As Boolean
    Public Property SkippedInUse As Boolean
    Public Property Message As String
End Class

Public MustInherit Class CodeAdminValueCommand
    Public Property CodeClass As String
    Public Property CodeValue As String
    Public Property CodeValueDesc As String
    Public Property CodeValueLongDesc As String
    Public Property MinorCode As String
    Public Property FormDisplay As String
    Public Property OptionValue1 As String
    Public Property OptionValue2 As String
    Public Property OptionValue3 As String
    Public Property OptionValue4 As String
    Public Property OptionValue5 As String
    Public Property OptionValue6 As String
    Public Property OptionValue7 As String
    Public Property OptionValue8 As String
    Public Property OptionValue9 As String
    Public Property OptionValue10 As String
    Public Property OptionValue11 As String
    Public Property OptionValue12 As String
    Public Property OptionValue13 As String
    Public Property OptionValue14 As String
    Public Property OptionValue15 As String
    Public Property OptionValue16 As String
    Public Property OptionValue17 As String

    Friend Function GetOptionValues() As IList(Of String)
        Return New String() {OptionValue1, OptionValue2, OptionValue3, OptionValue4, OptionValue5, OptionValue6, OptionValue7, OptionValue8, OptionValue9, OptionValue10, OptionValue11, OptionValue12, OptionValue13, OptionValue14, OptionValue15, OptionValue16, OptionValue17}
    End Function

    Friend Sub SetOptionValue(optionIndex As Integer, value As String)
        Select Case optionIndex
            Case 1 : OptionValue1 = value
            Case 2 : OptionValue2 = value
            Case 3 : OptionValue3 = value
            Case 4 : OptionValue4 = value
            Case 5 : OptionValue5 = value
            Case 6 : OptionValue6 = value
            Case 7 : OptionValue7 = value
            Case 8 : OptionValue8 = value
            Case 9 : OptionValue9 = value
            Case 10 : OptionValue10 = value
            Case 11 : OptionValue11 = value
            Case 12 : OptionValue12 = value
            Case 13 : OptionValue13 = value
            Case 14 : OptionValue14 = value
            Case 15 : OptionValue15 = value
            Case 16 : OptionValue16 = value
            Case 17 : OptionValue17 = value
        End Select
    End Sub
End Class

Public Class CreateCodeValueCommand
    Inherits CodeAdminValueCommand
End Class

Public Class UpdateCodeValueCommand
    Inherits CodeAdminValueCommand

    Public Property CodeValueId As Integer
End Class

Public Class CodeValueLifecycleCommand
    Public Property CodeClass As String
    Public Property CodeValue As String
    Public Property Status As String
End Class

Public Class CodeValuePositionCommand
    Public Property CodeClass As String
    Public Property CodeValue As String
    Public Property NewPosition As Integer
End Class

Public Class DeleteCodeValuesCommand
    Public Property CodeValueIds As IList(Of Integer)
End Class

Public Class PatchCodeValueCommand
    Public Property CodeValueId As Integer
    Public Property FieldName As String
    Public Property FieldValue As String
End Class

Public Class CodeAdminMutationContext
    Public Property MajorCode As String
    Public Property RebuildLicenseObjTypeTables As Boolean
End Class

Public Interface ICodeAdminRepository
    Function ResolveMajorCode() As String
    Function ListEditableClasses() As IList(Of CodeAdminClass)
    Function GetClass(codeClass As String) As CodeAdminClass
    Function ListValues(codeClass As String, search As String) As IList(Of CodeAdminValue)
    Function ListLookupCodeValues(codeClass As String, selectedValues As IList(Of String), excludeValue As String) As IList(Of CodeAdminFieldOption)
    Function ListLookupCodeClasses(selectedValue As String) As IList(Of CodeAdminFieldOption)
    Function ListOrgSubTypeColumns(orgSubTypeCode As String, includeFunctionFields As Boolean) As IList(Of CodeAdminFieldOption)
    Function ListFacPrefEmailFields() As IList(Of CodeAdminFieldOption)
    Function ListProducts(organizationId As String) As IList(Of CodeAdminFieldOption)
    Function GetValueById(codeValueId As Integer) As CodeAdminValue
    Function GetValueByClassAndValue(codeClass As String, codeValue As String) As CodeAdminValue
    Function ValuePairExists(codeClass As String, codeValue As String, excludeId As Integer?) As Boolean
    Function CreateValue(command As CreateCodeValueCommand, context As CodeAdminMutationContext) As CodeAdminValue
    Function UpdateValue(command As UpdateCodeValueCommand, context As CodeAdminMutationContext) As CodeAdminValue
    Function PatchValue(command As PatchCodeValueCommand) As CodeAdminValue
    Function DeleteValue(codeValueId As Integer, context As CodeAdminMutationContext) As CodeAdminDeleteResult
    Sub SetStatus(codeClass As String, codeValue As String, status As String)
    Sub SetPosition(codeClass As String, codeValue As String, newPosition As Integer)
End Interface
