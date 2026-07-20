Imports System

Public Class AdminShellServiceException
    Inherits Exception

    Public Sub New(message As String)
        MyBase.New(message)
    End Sub
End Class

Public Class AdminShellForbiddenException
    Inherits AdminShellServiceException

    Public Sub New(message As String)
        MyBase.New(message)
    End Sub
End Class

Public Class AdminShellValidationException
    Inherits AdminShellServiceException

    Public Sub New(message As String)
        MyBase.New(message)
    End Sub
End Class

Public Class AdminShellConcurrencyException
    Inherits AdminShellServiceException

    Public Sub New(message As String)
        MyBase.New(message)
    End Sub
End Class