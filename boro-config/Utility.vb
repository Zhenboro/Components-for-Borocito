Imports Microsoft.Win32
Module GlobalUses
    Public parameters As String
    Public DIRCommons As String = "C:\Users\" & Environment.UserName & "\AppData\Local\Microsoft\Borocito"
    Public DIRHome As String = DIRCommons & "\boro-get\" & My.Application.Info.AssemblyName
    Public HttpOwnerServer As String
End Module
Module Utility
    Public tlmContent As String
    Function AddToLog(ByVal from As String, ByVal content As String, Optional ByVal flag As Boolean = False, Optional ByVal cmdColor As ConsoleColor = ConsoleColor.White) As String
        Try
            Dim OverWrite As Boolean = False
            If My.Computer.FileSystem.FileExists(DIRHome & "\" & My.Application.Info.AssemblyName & ".log") Then
                OverWrite = True
            End If
            Dim finalContent As String = Nothing
            If flag = True Then
                finalContent = " [!!!]"
            End If
            Dim Message As String = DateTime.Now.ToString("hh:mm:ss tt dd/MM/yyyy") & finalContent & " [" & from & "] " & content
            tlmContent = tlmContent & Message & vbCrLf
            If ConsoleMode > 1 Then
                myConsola.AppendToConsole("[" & from & "]" & finalContent & " " & content, cmdColor)
            Else
                If cmdColor <> ConsoleColor.White Then
                    Console.ForegroundColor = cmdColor
                    Console.WriteLine("[" & from & "]" & finalContent & " " & content)
                    Console.ForegroundColor = ConsoleColor.White
                Else
                    Console.WriteLine("[" & from & "]" & finalContent & " " & content)
                End If
            End If
            Try
                My.Computer.FileSystem.WriteAllText(DIRHome & "\" & My.Application.Info.AssemblyName & ".log", vbCrLf & Message, OverWrite)
            Catch
            End Try
            Return finalContent & "[" & from & "]" & content
        Catch ex As Exception
            Console.WriteLine("[AddToLog@Utility]Error: " & ex.Message)
            Return "[AddToLog@Utility]Error: " & ex.Message
        End Try
    End Function
    Function ReadBorocitoLog() As String
        Try
            Dim lastLog As String = Nothing
            While True
                If lastLog <> IO.File.ReadAllLines(DIRCommons & "\Borocito.log")(IO.File.ReadAllLines(DIRCommons & "\Borocito.log").Length - 1) Then
                    AddToLog("BorocitoCLI", IO.File.ReadAllLines(DIRCommons & "\Borocito.log")(IO.File.ReadAllLines(DIRCommons & "\Borocito.log").Length - 1), False, ConsoleColor.Yellow)
                    lastLog = IO.File.ReadAllLines(DIRCommons & "\Borocito.log")(IO.File.ReadAllLines(DIRCommons & "\Borocito.log").Length - 1)
                End If
                Threading.Thread.Sleep(500)
            End While
            Return "Borocito Log Reader stopped"
        Catch ex As Exception
            Return AddToLog("ReadBorocitoLog@Memory", "Error: " & ex.Message, True)
        End Try
    End Function
End Module
Module Boro_Hear
    Function BoroHearSendContent(Optional ByVal content As String = Nothing) As Boolean
        Try
            Dim regKey As RegistryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Borocito\\boro-get\\boro-hear", True)
            If regKey Is Nothing Then
                Return False
            Else
                Try
                    If content <> Nothing Then
                        AddToLog("BoroHearSendContent", content, False)
                        Process.Start(regKey.GetValue("boro-hear"), content)
                    End If
                    Return True
                Catch
                    Return False
                End Try
            End If
        Catch ex As Exception
            Console.WriteLine("[BoroHearSendContent@Init]Error: " & ex.Message)
            Return False
        End Try
    End Function
    Function BoroHearSendFile(Optional ByVal filePath As String = Nothing) As Boolean
        Try
            Dim regKey As RegistryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Borocito\\boro-get\\boro-hear", True)
            If regKey Is Nothing Then
                Return False
            Else
                Try
                    If filePath <> Nothing Then
                        AddToLog("BoroHearSendFile", filePath, False)
                        Process.Start(regKey.GetValue("boro-hear"), "/filesend " & filePath)
                    End If
                    Return True
                Catch
                    Return False
                End Try
            End If
        Catch ex As Exception
            Console.WriteLine("[BoroHearSendFile@Init]Error: " & ex.Message)
            Return False
        End Try
    End Function
End Module
Module Memory
    Public regKey As RegistryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Borocito", True)
    Public OwnerServer As String
    Public UID As String
    Public ConsoleMode As SByte = 0
    Function LoadRegedit() As String
        Try

            OwnerServer = regKey.GetValue("OwnerServer")
            UID = regKey.GetValue("UID")
            HttpOwnerServer = "http://" & OwnerServer
            ConsoleMode = regKey.GetValue("ConsoleMode")
            Return AddToLog("LoadRegedit@Memory", "Data loaded!", False)
        Catch ex As Exception
            Return AddToLog("LoadRegedit@Memory", "Error: " & ex.Message, True)
        End Try
    End Function
    Function SaveRegedit() As String
        Try
            regKey.SetValue("ConsoleMode", ConsoleMode, RegistryValueKind.String)
            regKey.SetValue("OwnerServer", OwnerServer, RegistryValueKind.String)
            regKey.SetValue("UID", UID, RegistryValueKind.String)
            LoadRegedit()
            Return AddToLog("SaveRegedit@Memory", "Data saved!", False)
        Catch ex As Exception
            Return AddToLog("SaveRegedit@Memory", "Error: " & ex.Message, True)
        End Try
    End Function
    Sub RegisterInstance()
        Try
            Dim llaveReg As String = "SOFTWARE\\Borocito\\boro-get\\" & My.Application.Info.AssemblyName
            Dim registerKey As RegistryKey = Registry.CurrentUser.OpenSubKey(llaveReg, True)
            If registerKey IsNot Nothing Then
                registerKey.SetValue("Version", My.Application.Info.Version.ToString & " (" & My.Application.Info.Version.ToString & ")")
            End If
        Catch ex As Exception
            AddToLog("RegisterInstance@Memory", "Error: " & ex.Message, True)
        End Try
    End Sub
End Module
Module StartUp
    Sub Init()
        AddToLog("Init", My.Application.Info.AssemblyName & " " & My.Application.Info.Version.ToString & " (" & My.Application.Info.Version.ToString & ")" & " has started! " & DateTime.Now.ToString("hh:mm:ss tt dd/MM/yyyy"), True, ConsoleColor.Green)
        Try
            CommonActions()
            'Cargamos los datos del registro de Windows
            LoadRegedit()
            SaveRegedit()
            RegisterInstance()
            'InicializeBorocitoLogReader() 'Satura la consola
        Catch ex As Exception
            AddToLog("Init@StartUp", "Error: " & ex.Message, True)
        End Try
    End Sub
    Sub CommonActions()
        Try
            If Not My.Computer.FileSystem.DirectoryExists(DIRCommons) Then
                My.Computer.FileSystem.CreateDirectory(DIRCommons)
            End If
            If Not My.Computer.FileSystem.DirectoryExists(DIRHome) Then
                My.Computer.FileSystem.CreateDirectory(DIRHome)
            End If
        Catch ex As Exception
            AddToLog("CommonActions@StartUp", "Error: " & ex.Message, True)
        End Try
    End Sub
    Function ReadParameters(ByVal parametros() As String) As String
        Try
            If parametros.Length > 0 Then
                For Each parameter As String In parametros

                    If parameter.ToLower.StartsWith("--consolemode") Then
                        HandleConsoleMode(parameter)
                    ElseIf parameter = "//" Then

                    Else
                    End If

                Next
            End If
            Return Nothing
        Catch ex As Exception
            Return AddToLog("ReadParameters@StartUp", "Error: " & ex.Message, True)
        End Try
    End Function
    Sub InicializeBorocitoLogReader()
        Try
            Dim threadBorocitoLogReader As Threading.Thread = New Threading.Thread(AddressOf ReadBorocitoLog)
            threadBorocitoLogReader.Start()
        Catch ex As Exception
            AddToLog("InicializeBorocitoLogReader@StartUp", "Error: " & ex.Message, True)
        End Try
    End Sub
End Module