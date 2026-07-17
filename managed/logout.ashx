<%@ WebHandler Language="VB" Class="PilotLogoutHandler" %>
Imports System.Web

Public Class PilotLogoutHandler
    Implements IHttpHandler

    Public Sub ProcessRequest(context As HttpContext) Implements IHttpHandler.ProcessRequest
        PilotAuth.SignOut(context)
        context.Response.Cache.SetCacheability(HttpCacheability.NoCache)
        context.Response.Cache.SetNoStore()
        context.Response.Redirect(PilotConfig.LoginUrl, False)
        context.ApplicationInstance.CompleteRequest()
    End Sub

    Public ReadOnly Property IsReusable As Boolean Implements IHttpHandler.IsReusable
        Get
            Return False
        End Get
    End Property
End Class
