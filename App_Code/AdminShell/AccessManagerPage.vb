Public Class AccessManagerPage
    Inherits ManagedToolPage

    Protected Overrides ReadOnly Property ToolTitle As String
        Get
            Return "Access Manager"
        End Get
    End Property

    Protected Overrides ReadOnly Property ToolSubtitle As String
        Get
            Return "Unified sections, scripts, and grants"
        End Get
    End Property

    Protected Overrides ReadOnly Property AccessDeniedMessage As String
        Get
            Return "You do not have permission to use Access Manager."
        End Get
    End Property

    Protected Overrides Function CanOpenTool(user As PilotUser) As Boolean
        Return PilotJsonApi.CanUseAccessManager(user)
    End Function
End Class