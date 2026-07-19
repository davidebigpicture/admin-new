Public NotInheritable Class CodeAdminConstants
    Private Sub New()
    End Sub

    Public Const PilotRoute As String = "managed/code-admin/index.aspx"
    Public Const CanonicalRoute As String = "cgi-bin/codeadminO.pl"
    Public Const StatusActive As String = "N"
    Public Const StatusInactive As String = "Y"
    Public Const StatusArchived As String = "A"
    Public Const InactiveYes As String = StatusInactive
    Public Const InactiveNo As String = StatusActive
    Public Const ProtectedValueDevDomain As String = "DEV_DOMAIN"
    Public Const ProtectedClassGroupType As String = "GROUP_TY_CD"
    Public Const ProtectedClassApplicationDb As String = "APPLICATION_DB"
    Public Const DefaultPageSize As Integer = 200
    Public Const MaxPageSize As Integer = 500
    Public Const MaxSearchLength As Integer = 100
    Public Const MaxCodeValueLength As Integer = 50
    Public Const MaxDescriptionLength As Integer = 1000
    Public Const MaxLongDescriptionLength As Integer = 4000
    Public Const MaxOptionalValueLength As Integer = 1000

    Public Shared Function IsLifecycleStatus(value As String) As Boolean
        Return String.Equals(value, StatusActive, StringComparison.OrdinalIgnoreCase) OrElse
            String.Equals(value, StatusInactive, StringComparison.OrdinalIgnoreCase) OrElse
            String.Equals(value, StatusArchived, StringComparison.OrdinalIgnoreCase)
    End Function
End Class