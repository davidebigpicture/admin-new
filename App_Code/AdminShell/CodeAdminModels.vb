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

Public Class CreateCodeValueCommand
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
End Class

Public Class UpdateCodeValueCommand
    Public Property CodeValueId As Integer
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
