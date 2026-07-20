Imports System.Web
Imports System.Web.UI
Imports System.Globalization

Public Class ManagedShellMaster
    Inherits MasterPage

    Public Const ShellAssetVersion As String = "0719u"
    Private Const ShellCssVersion As String = "0719z"

    Public Property ToolTitle As String
    Public Property ToolSubtitle As String

    Public ReadOnly Property EncodedToolTitle As String
        Get
            Return HttpUtility.HtmlEncode(If(ToolTitle, String.Empty))
        End Get
    End Property

    Public ReadOnly Property EncodedToolSubtitle As String
        Get
            Return HttpUtility.HtmlEncode(If(ToolSubtitle, String.Empty))
        End Get
    End Property

    Public ReadOnly Property EncodedClientTitle As String
        Get
            Return HttpUtility.HtmlEncode(PilotConfig.BannerTitle)
        End Get
    End Property

    Public ReadOnly Property EncodedSessionTimeoutSeconds As String
        Get
            Dim seconds = PilotConfig.SessionTimeoutMinutes * 60
            Return seconds.ToString(CultureInfo.InvariantCulture)
        End Get
    End Property

    Public ReadOnly Property EncodedStylesheetUrl As String
        Get
            Return EncodeUrl(PilotConfig.StylesheetUrl)
        End Get
    End Property

    Public ReadOnly Property EncodedShellCssUrl As String
        Get
            Return EncodeUrl(PilotConfig.CombinePilot("managed/shared/shell.css") & "?v=" & ShellCssVersion)
        End Get
    End Property

    Public ReadOnly Property EncodedApiClientUrl As String
        Get
            Return EncodeUrl(PilotConfig.CombinePilot("managed/shared/api-client.js"))
        End Get
    End Property

    Public ReadOnly Property EncodedSessionUrl As String
        Get
            Return EncodeUrl(PilotConfig.CombinePilot("managed/shared/session.js"))
        End Get
    End Property

    Public ReadOnly Property EncodedDialogsUrl As String
        Get
            Return EncodeUrl(PilotConfig.CombinePilot("managed/shared/dialogs.js"))
        End Get
    End Property

    Public ReadOnly Property EncodedShellScriptUrl As String
        Get
            Return EncodeUrl(PilotConfig.CombinePilot("managed/shared/shell.js") & "?v=" & ShellAssetVersion)
        End Get
    End Property

    Private Function EncodeUrl(url As String) As String
        Return HttpUtility.HtmlAttributeEncode(url)
    End Function
End Class