Imports Microsoft.Win32
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
    Function BoroHearInterop(Optional ByVal content As String = Nothing) As Boolean
        Try
            Dim regKey As RegistryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Borocito\\boro-get\\boro-hear", True)
            If regKey Is Nothing Then
                Return False
            Else
                Try
                    If content <> Nothing Then
                        AddToLog("BoroHearInterop", content, False)
                        Process.Start(regKey.GetValue("boro-hear"), content)
                    End If
                    Return True
                Catch
                    Return False
                End Try
            End If
        Catch ex As Exception
            Console.WriteLine("[BoroHearInterop@Init]Error: " & ex.Message)
            Return False
        End Try
    End Function
End Module
Module GlobalUses
    Public parameters As String
    Public DIRCommons As String = "C:\Users\" & Environment.UserName & "\AppData\Local\Microsoft\Borocito"
    Public DIRHome As String = DIRCommons & "\boro-get\" & My.Application.Info.AssemblyName
End Module
Module StartUp
    Sub Init()
        AddToLog("Init", My.Application.Info.AssemblyName & " " & My.Application.Info.Version.ToString & " (" & Application.ProductVersion & ")" & " has started! " & DateTime.Now.ToString("hh:mm:ss tt dd/MM/yyyy"), True)
        Try
            CommonActions()
            RegisterInstance()
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
    Sub RegisterInstance()
        Try
            Dim llaveReg As String = "SOFTWARE\\Borocito\\boro-get\\" & My.Application.Info.AssemblyName
            Dim registerKey As RegistryKey = Registry.CurrentUser.OpenSubKey(llaveReg, True)
            If registerKey IsNot Nothing Then
                registerKey.SetValue("Version", My.Application.Info.Version.ToString & " (" & Application.ProductVersion & ")")
            End If
        Catch ex As Exception
            AddToLog("RegisterInstance@StartUp", "Error: " & ex.Message, True)
        End Try
    End Sub
    Sub ReadParameters(ByVal parametros As String)
        Try
            If parametros <> Nothing Then
                Dim parameter As String = parametros
                Dim args() As String = parameter.Split(" ")
                Dim strI As Integer = -1
                Dim strContent As String = Nothing
                If parametros.Contains("'") Then
                    strI = parametros.IndexOf("'")
                    strContent = parametros.Substring(strI + 1, parametros.IndexOf("'", strI + 1) - strI - 1)
                End If

                AddToLog("ReadParameters", parametros, False)

                If args(0).ToLower = "/selecthk" Then
                    BoroHearInterop(vbCrLf & Main.SelectHKey(strContent))

                ElseIf args(0).ToLower = "/selectkey" Then
                    BoroHearInterop(vbCrLf & Main.SelectKey(strContent))

                ElseIf args(0).ToLower = "/getvalue" Then
                    BoroHearInterop(vbCrLf & Main.GetValue(strContent))

                ElseIf args(0).ToLower = "/setvalue" Then
                    'el valueKind DEBE estar antes que el valor
                    BoroHearInterop(vbCrLf & Main.SetValue(args(1), args(2), strContent))


                ElseIf args(0).ToLower = "/createsubkey" Then
                    BoroHearInterop(vbCrLf & Main.CreateSubKey(strContent))

                ElseIf args(0).ToLower = "/deletevalue" Then
                    BoroHearInterop(vbCrLf & Main.DeleteValue(strContent))

                ElseIf args(0).ToLower = "/deletesubkeytree" Then
                    BoroHearInterop(vbCrLf & Main.DeleteSubKeyTree(strContent))

                ElseIf args(0).ToLower = "/deletesubkey" Then
                    BoroHearInterop(vbCrLf & Main.DeleteSubKey(strContent))


                ElseIf args(0).ToLower = "/getvaluenames()" Then
                    BoroHearInterop(vbCrLf & Main.GetValueNames())

                ElseIf args(0).ToLower = "/getsubkeynames()" Then
                    BoroHearInterop(vbCrLf & Main.GetSubKeyNames())

                ElseIf args(0).ToLower = "/getvaluekind" Then
                    BoroHearInterop(vbCrLf & Main.GetValueKind(strContent))

                ElseIf args(0).ToLower = "/exit" Or args(0).ToLower = "/stop" Or args(0).ToLower = "/close" Then
                    End

                End If

            End If
        Catch ex As Exception
            AddToLog("ReadParameters@Init", "Error: " & ex.Message, True)
        End Try
    End Sub
End Module
