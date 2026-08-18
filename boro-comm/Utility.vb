Imports Microsoft.Win32
Module GlobalUses
    Public parameters As String
    Public DIRCommons As String = "C:\Users\" & Environment.UserName & "\AppData\Local\Microsoft\Borocito"
    Public DIRHome As String = DIRCommons & "\boro-get\" & My.Application.Info.AssemblyName
    Public customWS As String = Nothing
End Module
Module Utility
    Public tlmContent As String
    Function AddToLog(ByVal from As String, ByVal content As String, Optional ByVal flag As Boolean = False) As String
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
            Dim myMessage As String = "[" & from & "]" & finalContent & " " & content
            Console.WriteLine(myMessage)
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
End Module
Module Memory
    Public regKey As RegistryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Borocito", True)
    Public UID As String
    Public OwnerServer As String
    Sub LoadRegedit()
        Try
            AddToLog("LoadRegedit@Memory", "Loading data...", False)
            OwnerServer = regKey.GetValue("OwnerServer").ToString().Replace("http://", "").Replace("https://", "").Replace("/Borocito", "").Replace("/api", "")
            UID = regKey.GetValue("UID").ToString()
            RegisterInstance()
        Catch ex As Exception
            AddToLog("LoadRegedit@Memory", "Error: " & ex.Message, True)
        End Try
    End Sub
    Sub RegisterInstance()
        Try
            Dim llaveReg As String = "SOFTWARE\\Borocito\\boro-get\\" & My.Application.Info.AssemblyName
            Dim registerKey As RegistryKey = Registry.CurrentUser.OpenSubKey(llaveReg, True)
            If registerKey Is Nothing Then
                Registry.CurrentUser.CreateSubKey(llaveReg)
                registerKey = Registry.CurrentUser.OpenSubKey(llaveReg, True)
            End If
            registerKey.SetValue(My.Application.Info.AssemblyName, System.Windows.Forms.Application.StartupPath & My.Application.Info.AssemblyName & ".exe")
            registerKey.SetValue("Executable", My.Application.Info.AssemblyName & ".exe")
            registerKey.SetValue("Name", My.Application.Info.AssemblyName)
            registerKey.SetValue("Version", My.Application.Info.Version.ToString & " (" & Application.ProductVersion & ")")
        Catch ex As Exception
            AddToLog("RegisterInstance@Memory", "Error: " & ex.Message, True)
        End Try
    End Sub
End Module
Module StartUp
    Sub Init()
        AddToLog("Init", My.Application.Info.AssemblyName & " " & My.Application.Info.Version.ToString & " (" & Application.ProductVersion & ")" & " has started! " & DateTime.Now.ToString("hh:mm:ss tt dd/MM/yyyy"), True)
        Try
            CommonActions()
            LoadRegedit()
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
    Sub ReadParameters(ByVal parametros As String)
        Try
            If parametros <> Nothing And parametros.Contains(" ") Then
                Dim args As String() = parametros.Split(" ")
                If args(0).ToLower = "ws" Then
                    Dim server = args(1).Trim() 'localhost
                    customWS = server
                ElseIf args(0).ToLower = "stop" Then
                    End
                End If
            End If
        Catch ex As Exception
            AddToLog("ReadParameters@StartUp", "Error: " & ex.Message, True)
        End Try
    End Sub
End Module