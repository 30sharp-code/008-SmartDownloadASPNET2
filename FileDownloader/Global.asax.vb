Imports System.Web
Imports System.Web.SessionState

Public Class [Global]
  Inherits System.Web.HttpApplication

  Private objComponents As System.ComponentModel.IContainer

  Public Sub New()
    MyBase.New()
    InitializeComponent()
  End Sub

  <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
    objComponents = New System.ComponentModel.Container
  End Sub

  Sub Application_Start(ByVal sender As Object, ByVal e As EventArgs)
    ' 
  End Sub

  Sub Session_Start(ByVal sender As Object, ByVal e As EventArgs)
    ' 
  End Sub

  Sub Application_BeginRequest(ByVal sender As Object, ByVal e As EventArgs)
    ' 
  End Sub

  Sub Application_AuthenticateRequest(ByVal sender As Object, ByVal e As EventArgs)
    ' 
  End Sub

  Sub Application_Error(ByVal sender As Object, ByVal e As EventArgs)
    ' 
  End Sub

  Sub Session_End(ByVal sender As Object, ByVal e As EventArgs)
    ' 
  End Sub

  Sub Application_End(ByVal sender As Object, ByVal e As EventArgs)
    ' 
  End Sub

End Class
