Imports System
Imports System.Web
Imports System.Web.UI

Public MustInherit Class ManagedToolPage
    Inherits Page

    Protected MustOverride ReadOnly Property ToolTitle As String
    Protected MustOverride ReadOnly Property ToolSubtitle As String
    Protected MustOverride ReadOnly Property AccessDeniedMessage As String

    Protected MustOverride Function CanOpenTool(user As PilotUser) As Boolean

    Protected Overrides Sub OnLoad(e As EventArgs)
        If Not PilotConfig.IsEnabledForHost(Request.Url.Host) Then
            Response.StatusCode = 404
            Response.TrySkipIisCustomErrors = True
            Response.End()
            Return
        End If

        Dim user As PilotUser = Nothing
        If Not PilotAuth.TryGetCurrentUser(Context, user) Then
            Dim returnUrl = HttpUtility.UrlEncode(Request.Url.PathAndQuery)
            Response.Redirect(PilotConfig.LoginUrl & "?returnUrl=" & returnUrl, False)
            Context.ApplicationInstance.CompleteRequest()
            Return
        End If

        If Not CanOpenTool(user) Then
            Response.StatusCode = 403
            Response.TrySkipIisCustomErrors = True
            Response.ContentType = "text/html"
            Response.Write("<p>" & HttpUtility.HtmlEncode(AccessDeniedMessage) & "</p>")
            Response.End()
            Return
        End If

        Dim shell = TryCast(Master, ManagedShellMaster)
        If shell IsNot Nothing Then
            shell.ToolTitle = ToolTitle
            shell.ToolSubtitle = ToolSubtitle
        End If

        MyBase.OnLoad(e)
    End Sub
End Class