Imports System.Net.Sockets
Imports System.Net.WebSockets
Imports System.Text
Imports System.Text.Json
Imports System.Text.Json.Serialization
Imports System.Threading
Namespace Boro_Comm

    Module Cliente
        Public Class WebSocketClient
            Implements IAsyncDisposable

            Private ReadOnly _url As Uri
            Private ReadOnly _clientId As String

            Private _socket As ClientWebSocket
            Private _cts As CancellationTokenSource
            Private _sendLock As SemaphoreSlim

            Private ReadOnly _jsonOptions As JsonSerializerOptions

            Public Event Connected()
            Public Event Disconnected()
            Public Event MessageReceived(message As ServerMessage)
            Public Event ErrorOccurred(exception As Exception)

            Public ReadOnly Property IsConnected As Boolean
                Get
                    Return _socket IsNot Nothing AndAlso _socket.State = WebSocketState.Open
                End Get
            End Property

            Public Sub New(url As String, clientId As String)
                _url = New Uri(url)
                _clientId = clientId

                _sendLock = New SemaphoreSlim(1, 1)

                _jsonOptions = New JsonSerializerOptions With {
                    .PropertyNameCaseInsensitive = True,
                    .DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                }
            End Sub

            ' ============================================================
            ' PUBLIC
            ' ============================================================

            Public Async Function StartAsync() As Task
                If _cts IsNot Nothing Then
                    Return
                End If
                _cts = New CancellationTokenSource()
                Await ConnectionLoopAsync(_cts.Token)
            End Function

            Public Sub NowStop()
                If _cts Is Nothing Then
                    Return
                End If
                _cts.Cancel()
            End Sub

            Public Async Function SendAsync(message As ClientMessage) As Task

                If Not IsConnected Then
                    Throw New InvalidOperationException(
                        "El WebSocket no está conectado."
                    )
                End If

                Dim json As String = JsonSerializer.Serialize(message, _jsonOptions)
                Dim bytes As Byte() = Encoding.UTF8.GetBytes(json)
                Await _sendLock.WaitAsync(_cts.Token)

                Try
                    Dim segment As New ArraySegment(Of Byte)(bytes)
                    Await _socket.SendAsync(
                        segment,
                        WebSocketMessageType.Text,
                        True,
                        _cts.Token
                    )
                Finally
                    _sendLock.Release()
                End Try
            End Function

            ' ============================================================
            ' CONNECTION LOOP
            ' ============================================================

            Private Async Function ConnectionLoopAsync(
                cancellationToken As CancellationToken
            ) As Task
                Dim retryDelay As Integer = 1000
                While Not cancellationToken.IsCancellationRequested
                    Try
                        Await ConnectAsync(cancellationToken)
                        retryDelay = 1000
                        Await ReceiveLoopAsync(cancellationToken)
                    Catch ex As OperationCanceledException
                        Exit While
                    Catch ex As Exception
                        RaiseEvent ErrorOccurred(ex)
                    Finally
                        'Await CloseSocketAsync()
                        RaiseEvent Disconnected()
                    End Try

                    If cancellationToken.IsCancellationRequested Then
                        Exit While
                    End If

                    Try
                        Await Task.Delay(
                            retryDelay,
                            cancellationToken
                        )
                    Catch ex As OperationCanceledException
                        Exit While
                    End Try
                    retryDelay = Math.Min(
                        retryDelay * 2,
                        30000
                    )
                End While
                Await CloseSocketAsync()
            End Function

            ' ============================================================
            ' CONNECT
            ' ============================================================

            Private Async Function ConnectAsync(cancellationToken As CancellationToken) As Task
                _socket = New ClientWebSocket()
                _socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(20)
                Await _socket.ConnectAsync(_url, cancellationToken)
                RaiseEvent Connected()
            End Function

            ' ============================================================
            ' RECEIVE LOOP
            ' ============================================================

            Private Async Function ReceiveLoopAsync(cancellationToken As CancellationToken) As Task
                While _socket IsNot Nothing AndAlso _socket.State = WebSocketState.Open AndAlso Not cancellationToken.IsCancellationRequested
                    Dim json As String = Await ReceiveMessageAsync(cancellationToken)
                    If json Is Nothing Then
                        Exit While
                    End If
                    Dim message As ServerMessage
                    Try
                        message = JsonSerializer.Deserialize(Of ServerMessage)(json, _jsonOptions)
                    Catch ex As JsonException
                        RaiseEvent ErrorOccurred(
                            New Exception(
                                "JSON inválido recibido del servidor: " &
                                json,
                                ex
                            )
                        )
                        Continue While
                    End Try
                    If message Is Nothing Then
                        Continue While
                    End If
                    RaiseEvent MessageReceived(message)
                End While
            End Function

            ' ============================================================
            ' RECEIVE SINGLE MESSAGE
            '
            ' IMPORTANTE:
            ' WebSocket puede fragmentar un mensaje.
            ' Por eso no asumimos que ReceiveAsync() == mensaje completo.
            ' ============================================================

            Private Async Function ReceiveMessageAsync(cancellationToken As CancellationToken) As Task(Of String)
                Using stream As New IO.MemoryStream()
                    Dim buffer(8191) As Byte
                    While True
                        Dim result As WebSocketReceiveResult
                        result = Await _socket.ReceiveAsync(New ArraySegment(Of Byte)(buffer), cancellationToken)
                        If result.MessageType =
                            WebSocketMessageType.Close Then
                            Return Nothing
                        End If
                        If result.MessageType =
                            WebSocketMessageType.Binary Then
                            Throw New InvalidOperationException(
                                "El servidor envió un mensaje binario. " &
                                "El protocolo espera JSON en texto."
                            )
                        End If
                        If result.Count > 0 Then
                            stream.Write(
                                buffer,
                                0,
                                result.Count
                            )
                        End If
                        If result.EndOfMessage Then
                            Exit While
                        End If
                    End While
                    Return Encoding.UTF8.GetString(
                        stream.ToArray()
                    )
                End Using
            End Function

            ' ============================================================
            ' CLOSE
            ' ============================================================

            Private Async Function CloseSocketAsync() As Task
                If _socket Is Nothing Then
                    Return
                End If
                Try
                    If _socket.State = WebSocketState.Open OrElse _socket.State = WebSocketState.CloseReceived Then
                        Using timeoutCts As New CancellationTokenSource(TimeSpan.FromSeconds(2))
                            Await _socket.CloseAsync(
                                WebSocketCloseStatus.NormalClosure,
                                "Client disconnecting",
                                timeoutCts.Token
                            )
                        End Using
                    End If
                Catch
                    ' La conexión probablemente ya estaba muerta.
                    ' No hacemos nada.
                Finally
                    _socket.Dispose()
                    _socket = Nothing
                End Try
            End Function

            ' ============================================================
            ' DISPOSE
            ' ============================================================

            Private Function DisposeAsync() As ValueTask Implements IAsyncDisposable.DisposeAsync
                NowStop()
                If _sendLock IsNot Nothing Then
                    _sendLock.Dispose()
                End If
                If _cts IsNot Nothing Then
                    _cts.Dispose()
                End If
                Return Nothing
            End Function
        End Class

        Public Class ClientMessage
            <JsonPropertyName("response")>
            Public Property Response As String
        End Class
        Public Class ServerMessage
            <JsonPropertyName("command")>
            Public Property Command As String
        End Class
    End Module

    Module Servidor
        Public Class TCPCliente
            Private client As TcpClient
            Private clientStream As NetworkStream
            Private thread As Thread
            Private isConnected As Boolean
            Private serverIp As String
            Private serverPort As Integer

            Public Event MessageReceived As EventHandler(Of String)
            Public Event ErrorOccurred(exception As Exception) 'TODO: implementar (el CLI puede reiniciarse)

            Public Sub New(Optional host As String = "127.0.0.1", Optional port As Integer = 13120)
                client = New TcpClient()
                serverIp = host
                serverPort = port
                isConnected = False
            End Sub

            ' Método para conectar al servidor
            Public Sub ConnectToServer()
                Try
                    client.Connect(serverIp, serverPort)
                    clientStream = client.GetStream()
                    isConnected = True
                    Console.WriteLine("Conectado al servidor.")
                    ' Iniciar hilo para leer los mensajes del servidor
                    thread = New Thread(AddressOf ReadMessages)
                    thread.Start()
                Catch ex As Exception
                    Console.WriteLine("Error conectando al servidor: " & ex.Message)
                End Try
            End Sub

            ' Método para desconectar del servidor
            Public Sub DisconnectFromServer()
                Try
                    isConnected = False
                    clientStream.Close()
                    client.Close()
                    Console.WriteLine("Desconectado del servidor.")
                Catch ex As Exception
                    Console.WriteLine("Error desconectando: " & ex.Message)
                End Try
            End Sub

            ' Método para enviar un mensaje al servidor
            Public Sub SendMesssage(message As String)
                If isConnected AndAlso clientStream IsNot Nothing Then
                    Try
                        Dim data As Byte() = Encoding.UTF8.GetBytes(message)
                        clientStream.Write(data, 0, data.Length)
                    Catch ex As Exception
                        Console.WriteLine("Error enviando mensaje: " & ex.Message)
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
                        Console.WriteLine("Error leyendo mensaje: " & ex.Message)
                        Exit While
                    End Try
                End While
            End Sub
        End Class
    End Module
End Namespace