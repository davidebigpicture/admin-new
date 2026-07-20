Imports System
Imports System.Text
Imports System.Web

Public NotInheritable Class PilotShell
    Private Const ShellAssetVersion As String = "0719ads1"
    Public Const BuildMarker As String = "pilot-shell-unified"

    Private Sub New()
    End Sub

    Public Shared Function RenderHeader(user As PilotUser, pageTitle As String, currentPath As String) As String
        Dim encodedUser = HttpUtility.HtmlEncode(user.UserName)
        Dim encodedTitle = HttpUtility.HtmlEncode(If(pageTitle, String.Empty))
        Dim encodedBanner = HttpUtility.HtmlEncode(PilotConfig.BannerTitle)
        Dim encodedType = HttpUtility.HtmlEncode(PilotConfig.BannerType)
        Dim logoutUrl = HttpUtility.HtmlAttributeEncode(PilotConfig.LogoutUrl)
        Dim stylesUrl = HttpUtility.HtmlAttributeEncode(PilotConfig.StylesheetUrl)
        Dim shellCssUrl = HttpUtility.HtmlAttributeEncode(
            PilotConfig.CombinePilot("managed/shared/shell.css") & "?v=" & ShellAssetVersion)
        Dim defaultRoute = PilotConfig.DefaultRoute
        Dim activePath = If(String.IsNullOrWhiteSpace(currentPath), defaultRoute, currentPath.Trim())
        Dim managedBase = PilotConfig.CombinePilot("managed") & "/"
        Dim encodedCurrentPath = HttpUtility.JavaScriptStringEncode(activePath)

        Dim html As New StringBuilder()
        html.AppendLine("<!DOCTYPE HTML>")
        html.AppendLine("<html lang=""en-US"">")
        html.AppendLine("<head>")
        html.AppendLine("<meta charset=""utf-8"">")
        html.AppendLine("<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">")
        html.AppendLine("<meta http-equiv=""Cache-Control"" content=""no-cache, no-store"">")
        html.AppendLine("<title>" & encodedTitle & " - " & encodedBanner & "</title>")
        html.AppendLine("<script src=""https://code.jquery.com/jquery-3.7.1.min.js""></script>")
        html.AppendLine("<script src=""https://code.jquery.com/ui/1.14.0/jquery-ui.min.js""></script>")
        html.AppendLine("<link rel=""stylesheet"" href=""https://code.jquery.com/ui/1.14.0/themes/base/jquery-ui.css"">")
        html.AppendLine("<link rel=""stylesheet"" href=""https://maxcdn.bootstrapcdn.com/bootstrap/3.4.1/css/bootstrap.min.css"">")
        html.AppendLine("<link rel=""stylesheet"" href=""https://maxcdn.bootstrapcdn.com/font-awesome/4.7.0/css/font-awesome.min.css"">")
        html.AppendLine("<link rel=""stylesheet"" href=""" & stylesUrl & """>")
        html.AppendLine("<link rel=""stylesheet"" href=""" & shellCssUrl & """>")
        html.AppendLine("<!-- " & BuildMarker & ":" & ShellAssetVersion & " -->")
        html.AppendLine("<script>")
        html.AppendLine("var elapsedTime=0,timeOutInterval=3600;")
        html.AppendLine("function startTimer(){if(elapsedTime<timeOutInterval){elapsedTime++;var e=document.getElementById('timeremaining');if(e){var r=timeOutInterval-elapsedTime,m=Math.floor(r/60),s=r%60;e.textContent='Session time: '+String(Math.floor(m/60)).padStart(2,'0')+':'+String(m%60).padStart(2,'0')+':'+String(s).padStart(2,'0');}window.setTimeout(startTimer,1000);}else{window.location='" & logoutUrl & "';}}")
        html.AppendLine("window.PilotManagedBase=""" & HttpUtility.JavaScriptStringEncode(managedBase) & """;")
        html.AppendLine("window.PilotCurrentPath=""" & encodedCurrentPath & """;")
        html.AppendLine("</script>")
        html.AppendLine("</head>")
        html.AppendLine("<body class=""bodyVer5 is-dev pilot-classic"" onload=""startTimer()"">")
        html.AppendLine("<header class=""shell-header"">")
        html.AppendLine("<div class=""shell-header__inner"">")
        html.AppendLine("<a class=""shell-logo"" href=""https://www.ebigpicture.com/"" aria-label=""Big Picture Software""><img src=""https://www.ebigpicture.com/img/logo.png"" alt=""Big Picture Software"" width=""234"" height=""148""></a>")
        html.AppendLine("<div class=""shell-brand"">")
        html.AppendLine("<h1>" & encodedTitle & "</h1>")
        html.AppendLine("<p class=""shell-brand__client"">" & encodedBanner & "</p>")
        html.AppendLine("</div>")
        html.AppendLine("<p class=""shell-user"">")
        html.AppendLine("Signed in as <strong>" & encodedUser & "</strong>")
        html.AppendLine("<button type=""button"" class=""shell-logout"" id=""logoutButton"" aria-label=""Sign out"" title=""Sign out""><i class=""fa fa-sign-out"" aria-hidden=""true""></i></button>")
        html.AppendLine("<span class=""shell-session-time"" id=""timeremaining""></span>")
        html.AppendLine("</p>")
        html.AppendLine("<button type=""button"" class=""admin-menu-mobile-toggle"" id=""adminMenuMobileToggle"" aria-controls=""adminMenu"" aria-expanded=""false"" aria-label=""Show admin menu"" title=""Show admin menu""><i class=""fa fa-bars"" aria-hidden=""true""></i></button>")
        html.AppendLine("</div>")
        html.AppendLine("</header>")
        html.AppendLine("<div class=""admin-layout admin-layout--classic"" id=""adminLayout"">")
        html.AppendLine("<aside class=""admin-menu"" id=""adminMenu"" aria-label=""Primary navigation""></aside>")
        html.AppendLine("<main class=""shell-main shell-main--classic"">")
        html.AppendLine("<div class=""mainbody"">")
        Return html.ToString()
    End Function

    Public Shared Function RenderFooter(currentPath As String) As String
        Dim currentYear = DateTime.UtcNow.Year.ToString(System.Globalization.CultureInfo.InvariantCulture)
        Dim managedBase = PilotConfig.CombinePilot("managed") & "/"
        Dim shellJsUrl = HttpUtility.HtmlAttributeEncode(
            PilotConfig.CombinePilot("managed/shared/shell.js") & "?v=" & ShellAssetVersion)
        Dim apiClientUrl = HttpUtility.HtmlAttributeEncode(PilotConfig.CombinePilot("managed/shared/api-client.js"))
        Dim sessionUrl = HttpUtility.HtmlAttributeEncode(PilotConfig.CombinePilot("managed/shared/session.js"))
        Dim activePath = HttpUtility.JavaScriptStringEncode(
            If(String.IsNullOrWhiteSpace(currentPath), PilotConfig.DefaultRoute, currentPath.Trim()))

        Dim html As New StringBuilder()
        html.AppendLine("</div>")
        html.AppendLine("</main>")
        html.AppendLine("</div>")
        html.AppendLine("<footer class=""shell-footer"">")
        html.AppendLine("<div class=""shell-footer__inner""><span>Admin Shell</span></div>")
        html.AppendLine("</footer>")
        html.AppendLine("<script src=""https://maxcdn.bootstrapcdn.com/bootstrap/3.4.1/js/bootstrap.min.js""></script>")
        html.AppendLine("<script src=""" & apiClientUrl & """></script>")
        html.AppendLine("<script src=""" & sessionUrl & """></script>")
        html.AppendLine("<script src=""" & shellJsUrl & """></script>")
        html.AppendLine("<script>")
        html.AppendLine("(function () {")
        html.AppendLine("    if (!window.PilotApiClient || !window.PilotSession || !window.ManagedShell) { return; }")
        html.AppendLine("    window.PilotApiClient.setApiBase(window.PilotManagedBase || """");")
        html.AppendLine("    window.PilotSession.configure({ sessionUrl: ""api/session.ashx"" });")
        html.AppendLine("    window.ManagedShell.bindLogout(document.getElementById(""logoutButton""));")
        html.AppendLine("    var currentPath = window.PilotCurrentPath || """ & activePath & """;")
        html.AppendLine("    window.PilotSession.load().then(function (session) {")
        html.AppendLine("        window.ManagedShell.renderSectionMenu(")
        html.AppendLine("            document.getElementById(""adminMenu""),")
        html.AppendLine("            session.menuSections || [],")
        html.AppendLine("            currentPath")
        html.AppendLine("        );")
        html.AppendLine("    }).catch(function () {});")
        html.AppendLine("}());")
        html.AppendLine("</script>")
        html.AppendLine("<div class=""footerstyle"">&copy; Copyright 2002-" & currentYear & " Big Picture Software</div>")
        html.AppendLine("</body>")
        html.AppendLine("</html>")
        Return html.ToString()
    End Function
End Class
