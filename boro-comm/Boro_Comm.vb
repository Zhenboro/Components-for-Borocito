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
                        If Not isConnected Then
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
                        If Not isConnected OrElse clientStream Is Nothing Then
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
                While isRunning AndAlso isConnected
                    Try
                        Dim stream As NetworkStream = Nothing

                        SyncLock connectionLock
                            If Not isConnected OrElse clientStream Is Nothing Then
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