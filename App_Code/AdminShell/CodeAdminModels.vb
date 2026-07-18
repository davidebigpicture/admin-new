Imports System
Imports System.Collections.Generic

Public NotInheritable Class CodeAdminConstants
    Private Sub New()
    End Sub

    Public Const PilotRoute As String = "managed/code-admin/index.aspx"
    Public Const CanonicalRoute As String = "cgi-bin/codeadminO.pl"
    Public Const InactiveYes As String = "Y"
    Public Const InactiveNo As String = "N"
    Public Const ProtectedValueDevDomain As String = "DEV_DOMAIN"
    Public Const ProtectedClassGroupType As String = "GROUP_TY_CD"
    Public Const ProtectedClassApplicationDb As String = "APPLICATION_DB"
    Public Const DefaultPageSize As Integer = 200
    Public Const MaxPageSize As Integer = 500
    Public Const MaxSearchLength As Integer = 100
    Public Const MaxCodeValueLength As Integer = 50
    Public Const MaxDescriptionLength As Integer = 1000
    Public Const MaxLongDescriptionLength As Integer = 4000
End Class

Public Class CodeAdminClass
    Public Property CodeClass As String
    Public Property CodeClassDesc As String
    Public Property Edit As Boolean
End Class

Public Class CodeAdminValue
    Public Property CodeValueId As Integer
    Public Property CodeClass As String
    Public Property CodeValue As String
    Public Property CodeValueDesc As String
    Public Property CodeValueLongDesc As String
    Public Property Inactive As Boolean
    Public Property MajorCode As String
    Public Property MinorCode As String
    Public Property OrderBy As Integer?
    Public Property FormDisplay As String
    Public Property IsProtected As Boolean
End Class

Public Class CodeAdminValuePage
    Public Property Items As IList(Of CodeAdminValue)
    Public Property TotalCount As Integer
    Public Property Start As Integer
    Public Property PageSize As Integer
    Public Property CanDelete As Boolean
End Class

Public Class CodeAdminWorkspace
    Public Property Classes As IList(Of CodeAdminClass)
    Public Property DefaultCodeClass As String
    Public Property MajorCode As String
    Public Property ShowClassCodes As Boolean
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
End Class

Public Class UpdateCodeValueCommand
    Public Property CodeValueId As Integer
    Public Property CodeClass As String
    Public Property CodeValue As String
    Public Property CodeValueDesc As String
    Public Property CodeValueLongDesc As String
    Public Property MinorCode As String
End Class

Public Class CodeValueLifecycleCommand
    Public Property CodeClass As String
    Public Property CodeValue As String
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
