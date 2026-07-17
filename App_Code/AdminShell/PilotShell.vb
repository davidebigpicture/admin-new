Imports System
Imports System.Text
Imports System.Web

Public NotInheritable Class PilotShell
    Private Sub New()
    End Sub

    Public Shared Function RenderHeader(user As PilotUser, pageTitle As String, currentPath As String) As String
        Dim encodedUser = HttpUtility.HtmlEncode(user.UserName)
        Dim encodedTitle = HttpUtility.HtmlEncode(If(pageTitle, String.Empty))
        Dim encodedBanner = HttpUtility.HtmlEncode(PilotConfig.BannerTitle)
        Dim encodedType = HttpUtility.HtmlEncode(PilotConfig.BannerType)
        Dim logoutUrl = HttpUtility.HtmlAttributeEncode(PilotConfig.LogoutUrl)
        Dim stylesUrl = HttpUtility.HtmlAttributeEncode(PilotConfig.StylesheetUrl)
        Dim routes = PilotConfig.GetRoutes()
        Dim defaultRoute = PilotConfig.DefaultRoute
        Dim activePath = If(String.IsNullOrWhiteSpace(currentPath), defaultRoute, currentPath.Trim())

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
        html.AppendLine("<script>")
        html.AppendLine("var elapsedTime=0,timeOutInterval=3600;")
        html.AppendLine("function startTimer(){if(elapsedTime<timeOutInterval){elapsedTime++;var e=document.getElementById('timeremaining');if(e){var r=timeOutInterval-elapsedTime,m=Math.floor(r/60),s=r%60;e.textContent='Session time: '+String(Math.floor(m/60)).padStart(2,'0')+':'+String(m%60).padStart(2,'0')+':'+String(s).padStart(2,'0');}window.setTimeout(startTimer,1000);}else{window.location='" & logoutUrl & "';}}")
        html.AppendLine("function logout(){window.location='" & logoutUrl & "';}")
        html.AppendLine("function manageCodesO(){alert('Code administration is not part of the pilot yet.');}")
        html.AppendLine("function manageCodes(){alert('Code administration is not part of the pilot yet.');}")
        html.AppendLine("</script>")
        html.AppendLine("</head>")
        html.AppendLine("<body class=""bodyVer5 is-dev"" style=""margin:0"" onload=""startTimer()"">")
        html.AppendLine("<div class=""row banner"">")
        html.AppendLine("<div class=""col-xs-4 col-sm-2""><div class=""pageheaderwhite"">BIG PICTURE</div></div>")
        html.AppendLine("<div class=""col-xs-4 col-sm-3 hidden-xs""><div class=""pageheaderwhitesmall"">" & encodedType & "</div></div>")
        html.AppendLine("<div class=""col-xs-8 col-sm-7""><div class=""pageheaderwhite"">" & encodedBanner & "</div></div>")
        html.AppendLine("</div>")
        html.AppendLine("<div class=""row"" id=""rowRibbonSession"">")
        html.AppendLine("<div class=""col-sm-8 col-xs-8""><span class=""welcometext""><i class=""fa fa-lock""></i> " & encodedUser & " : <a href=""javascript:logout();"">Sign Out</a> | <span id=""timeremaining""></span></span></div>")
        html.AppendLine("<div class=""col-sm-4 col-xs-4 text-right""><span class=""label label-warning"">Pilot</span></div>")
        html.AppendLine("</div>")
        html.AppendLine("<div class=""row"" id=""menuAndContent"">")
        html.AppendLine("<div class=""col-sm-2 col-xl-1"" id=""col-left"">")
        html.AppendLine("<table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" id=""menuModule"">")
        html.AppendLine("<tr><td class=""module"" data-active=""true""><a class=""navheading"" href=""" &
            HttpUtility.HtmlAttributeEncode(defaultRoute) & """>Configuration</a></td></tr>")
        html.AppendLine("</table>")
        html.AppendLine("</div>")
        html.AppendLine("<div class=""col-sm-10 col-xl-11"" id=""col-right"">")
        html.AppendLine("<div id=""sectionHeader""><span class=""pageheader"">Configuration</span></div>")
        html.AppendLine("<table cellpadding=""0"" cellspacing=""0"" border=""0"" id=""sectionMenu""><tr>")

        For Each route As PilotRouteMapping In routes
            Dim isActive = String.Equals(activePath, route.PilotPath, StringComparison.OrdinalIgnoreCase)
            Dim tabClass = If(isActive, "tabon", "taboff")
            Dim textClass = If(isActive, "tabtexton", "tabtextoff")
            html.AppendLine(
                "<td align=""center"" class=""" & tabClass & """><a class=""" & textClass & """ href=""" &
                HttpUtility.HtmlAttributeEncode(route.PilotPath) & """>" &
                HttpUtility.HtmlEncode(route.NavLabel) & "</a></td>")
        Next

        html.AppendLine("</tr></table>")
        html.AppendLine("<div class=""mainbody"">")
        Return html.ToString()
    End Function

    Public Shared Function RenderFooter() As String
        Dim currentYear = DateTime.UtcNow.Year.ToString(System.Globalization.CultureInfo.InvariantCulture)
        Return "</div></div></div>" &
            "<div class=""footerstyle"">&copy; Copyright 2002-" & currentYear & " Big Picture Software</div>" &
            "<script src=""https://maxcdn.bootstrapcdn.com/bootstrap/3.4.1/js/bootstrap.min.js""></script>" &
            "</body></html>"
    End Function
End Class
