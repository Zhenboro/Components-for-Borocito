Imports System.Net.Sockets
Imports System.Threading
Imports System.Text
Namespace Boro_Comm

    Module Cliente

        Dim tcpClient As TCPCliente
        Sub ConnectToServer()
            tcpClient = New TCPCliente()
            ' Conectar al servidor
            tcpClient.ConnectToServer()
        End Sub
        Function SendMesssage(message As String) As String
            Try
                'TODO: permitir reconectarse al servidor para enviar.
                If tcpClient IsNot Nothing AndAlso message IsNot Nothing Then
                    tcpClient.SendMesssage("¡#" & message)
                    AddToLog("TCPCliente", "Broadcast message: " & message)
                End If
                Return message
            Catch ex As Exception
                Return ex.Message
            End Try
        End Function

        Public Class TCPCliente
            Private client As TcpClient
            Private clientStream As NetworkStream
            Private thread As Thread
            Private isConnected As Boolean
            Private serverIp As String = "127.0.0.1"
            Private serverPort As Integer = 13120

            Public Event MessageReceived As EventHandler(Of String)

            Public Sub New()
                client = New TcpClient()
                isConnected = False
            End Sub

            ' Método para conectar al servidor
            Public Sub ConnectToServer()
                Try
                    client.Connect(serverIp, serverPort)
                    clientStream = client.GetStream()
                    isConnected = True
                    ' Iniciar hilo para leer los mensajes del servidor
                    thread = New Thread(AddressOf ReadMessages)
                    thread.Start()
                    AddToLog("ConnectToServer@TCPCliente", "Conectado al servidor!")
                Catch ex As Exception
                    AddToLog("ConnectToServer@TCPCliente", "Error: " & ex.Message)
                End Try
            End Sub

            ' Método para desconectar del servidor
            Public Sub DisconnectFromServer()
                Try
                    isConnected = False
                    clientStream.Close()
                    client.Close()
                    AddToLog("DisconnectFromServer@TCPCliente", "Desconectado del servidor.")
                Catch ex As Exception
                    AddToLog("DisconnectFromServer@TCPCliente", "Error: " & ex.Message)
                End Try
            End Sub

            ' Método para enviar un mensaje al servidor
            Public Sub SendMesssage(message As String)
                If isConnected AndAlso clientStream IsNot Nothing Then
                    Try
                        Dim data As Byte() = Encoding.UTF8.GetBytes(message)
                        clientStream.Write(data, 0, data.Length)
                    Catch ex As Exception
                        AddToLog("SendMesssage@TCPCliente", "Error: " & ex.Message)
                    End Try
                End If
            End Sub

            ' Método para leer los mensajes del servidor
            Private Sub ReadMessages()
                Dim buffer(1024) As Byte
                While isConnected
                    Try
                        If clientStream.DataAvailable Then
                            Dim bytesRead As Integer = clientStream.Read(buffer, 0, buffer.Length)
                            If bytesRead > 0 Then
                                Dim message As String = Encoding.UTF8.GetString(buffer, 0, bytesRead)
                                RaiseEvent MessageReceived(Me, message)
                            End If
                        End If
                        Thread.Sleep(100)
                    Catch ex As Exception
                        AddToLog("ReadMessages@TCPCliente", "Error: " & ex.Message)
                        Exit While
                    End Try
                End While
            End Sub
        End Class
    End Module

End Namespace
