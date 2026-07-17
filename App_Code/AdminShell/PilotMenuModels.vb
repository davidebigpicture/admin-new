Imports System.Collections.Generic

Public Class PilotMenuSection
    Public Property SectionId As Integer
    Public Property Title As String
    Public Property Items As IList(Of PilotMenuItem)
End Class

Public Class PilotMenuItem
    Public Property ScriptId As Integer
    Public Property Title As String
    Public Property Path As String
End Class
