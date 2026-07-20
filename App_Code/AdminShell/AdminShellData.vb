Imports System
Imports System.Globalization

Public NotInheritable Class AdminShellData
    Private Sub New()
    End Sub

    Public Shared Function StringValue(value As Object) As String
        If value Is Nothing OrElse Convert.IsDBNull(value) Then
            Return String.Empty
        End If
        Return Convert.ToString(value, CultureInfo.InvariantCulture)
    End Function

    Public Shared Function NullableInt(value As Object) As Integer?
        If value Is Nothing OrElse Convert.IsDBNull(value) Then
            Return Nothing
        End If
        Return Convert.ToInt32(value, CultureInfo.InvariantCulture)
    End Function

    Public Shared Function NullableDate(value As Object) As DateTime?
        If value Is Nothing OrElse Convert.IsDBNull(value) Then
            Return Nothing
        End If
        Return Convert.ToDateTime(value, CultureInfo.InvariantCulture)
    End Function
End Class