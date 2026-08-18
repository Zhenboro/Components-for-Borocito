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
            Private readThread As Thread

            Private serverIp As String
            Private serverPort As Integer

            Private isComm As Boolean = False
            Private isRunning As Boolean = False

            Private reconnectDelay As Integer = 5000 ' 5 segundos

            ' Lock para proteger el acceso al socket
            Private ReadOnly connectionLock As New Object()

            Public Event MessageReceived As EventHandler(Of String)
            Public Event ErrorOccurred As EventHandler(Of Exception)
            Public Event Connected As EventHandler
            Public Event Disconnected As EventHandler

            Public Sub New(Optional host As String = "127.0.0.1",
                   Optional port As Integer = 13120,
                   Optional reconnectDelayMs As Integer = 5000)

                serverIp = host
                serverPort = port
                reconnectDelay = reconnectDelayMs
            End Sub

            ''' <summary>
            ''' Inicia el cliente y comienza a intentar conectarse.
            ''' Si el servidor no está disponible, seguirá intentando.
            ''' </summary>
            Public Sub ConnectToServer()
                If isRunning Then
                    Return
                End If
                isRunning = True
                readThread = New Thread(AddressOf ConnectionLoop)
                readThread.IsBackground = True
                readThread.Start()
            End Sub

            ''' <summary>
            ''' Bucle principal de conexión/reconexión.
            ''' </summary>
            Private Sub ConnectionLoop()
                While isRunning
                    Try
                        If Not IsConnected Then
                            Console.WriteLine($"Intentando conectar a {serverIp}:{serverPort}...")
                            If TryConnect() Then
                                AddToLog("ConnectionLoop@TCPCliente", "Conectado al servidor!", True)
                                RaiseEvent Connected(Me, EventArgs.Empty)
                                ' Leer mensajes.
                                ' Esta llamada bloquea hasta que se pierda la conexión.
                                ReadMessages()
                            End If
                        End If
                    Catch ex As Exception
                        RaiseError(ex)
                    Finally
                        If isRunning Then
                            DisconnectInternal()
                            AddToLog("ConnectionLoop@TCPCliente", "Conexion perdida, reintentando en " & reconnectDelay & " ms...", True)
                            Thread.Sleep(reconnectDelay)
                        End If
                    End Try
                End While
            End Sub

            ''' <summary>
            ''' Intenta establecer una conexión TCP.
            ''' </summary>
            Private Function TryConnect() As Boolean
                SyncLock connectionLock
                    If Not isRunning Then
                        Return False
                    End If

                    Try
                        ' Crear un TcpClient NUEVO en cada intento.
                        client = New TcpClient()
                        ' Timeout para la conexión inicial.
                        Dim result = client.BeginConnect(serverIp, serverPort, Nothing, Nothing)

                        If Not result.AsyncWaitHandle.WaitOne(5000) Then
                            client.Close()
                            client = Nothing
                            Return False
                        End If
                        client.EndConnect(result)
                        clientStream = client.GetStream()
                        isComm = True
                        Return True
                    Catch ex As Exception
                        isComm = False
                        Try
                            If client IsNot Nothing Then
                                client.Close()
                            End If
                        Catch
                        End Try
                        client = Nothing
                        clientStream = Nothing
                        AddToLog("TryConnect@TCPCliente", "No se logro conectar con el servidor")
                        Return False
                    End Try
                End SyncLock

            End Function

            ''' <summary>
            ''' Desconecta voluntariamente y detiene el proceso de reconexión.
            ''' </summary>
            Public Sub DisconnectFromServer()
                isRunning = False
                DisconnectInternal()
                AddToLog("DisconnectFromServer@TCPCliente", "Desconectado del servidor")
            End Sub

            ''' <summary>
            ''' Cierra la conexión actual.
            ''' No modifica isRunning.
            ''' </summary>
            Private Sub DisconnectInternal()
                Dim wasConnected As Boolean = False
                SyncLock connectionLock
                    wasConnected = IsConnected
                    isComm = False

                    Try
                        If clientStream IsNot Nothing Then
                            clientStream.Close()
                        End If
                    Catch
                    End Try
                    Try
                        If client IsNot Nothing Then
                            client.Close()
                        End If
                    Catch
                    End Try

                    clientStream = Nothing
                    client = Nothing
                End SyncLock

                If wasConnected Then
                    RaiseEvent Disconnected(Me, EventArgs.Empty)
                End If
            End Sub

            ''' <summary>
            ''' Envía un mensaje al servidor.
            ''' </summary>
            Public Sub SendMesssage(message As String)
                If String.IsNullOrEmpty(message) Then
                    Return
                End If
                Try
                    Dim stream As NetworkStream = Nothing

                    SyncLock connectionLock
                        If Not IsConnected OrElse clientStream Is Nothing Then
                            Console.WriteLine("No hay conexión con el servidor.")
                            Return
                        End If
                        stream = clientStream
                    End SyncLock

                    Dim data As Byte() = Encoding.UTF8.GetBytes(message)

                    stream.Write(data, 0, data.Length)
                    stream.Flush()
                Catch ex As Exception
                    AddToLog("SendMesssage@TCPCliente", "No se logro enviar mensaje al servidor")
                    ' Marcamos la conexión como caída.
                    DisconnectInternal()
                End Try

            End Sub

            ''' <summary>
            ''' Lee continuamente los mensajes del servidor.
            ''' </summary>
            Private Sub ReadMessages()
                Dim buffer(4095) As Byte
                While isRunning AndAlso IsConnected
                    Try
                        Dim stream As NetworkStream = Nothing

                        SyncLock connectionLock
                            If Not IsConnected OrElse clientStream Is Nothing Then
                                Exit While
                            End If
                            stream = clientStream
                        End SyncLock

                        Dim bytesRead As Integer = stream.Read(buffer, 0, buffer.Length)
                        ' Read() = 0 significa que el servidor cerró la conexión.
                        If bytesRead = 0 Then
                            AddToLog("ReadMessages@TCPCliente", "El servidor se ha cerrado!", True)
                            Exit While
                        End If

                        Dim message As String = Encoding.UTF8.GetString(buffer, 0, bytesRead)
                        RaiseEvent MessageReceived(Me, message)
                    Catch ex As Exception
                        If isRunning Then
                            Console.WriteLine("Error leyendo del servidor: " & ex.Message)
                            RaiseError(ex)
                        End If
                        Exit While
                    End Try
                End While
            End Sub

            ''' <summary>
            ''' Lanza el evento de error sin permitir que una excepción
            ''' del consumidor destruya el hilo de conexión.
            ''' </summary>
            Private Sub RaiseError(ex As Exception)
                Try
                    RaiseEvent ErrorOccurred(Me, ex)
                Catch
                    ' No permitir que un error en el handler
                    ' mate el hilo de conexión.
                End Try
            End Sub

            ''' <summary>
            ''' Indica si actualmente existe una conexión.
            ''' </summary>
            Public ReadOnly Property IsConnected As Boolean
                Get
                    Return isComm
                End Get
            End Property

        End Class
    End Module

End Namespace
