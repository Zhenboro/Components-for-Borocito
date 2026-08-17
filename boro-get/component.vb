Imports System.IO.Compression
Imports Microsoft.Win32
Namespace Boro_Get

    Module GlobalVariables
        Public execFilePath As String
        Public installFolder As String
        Public asyncDownloaderZipURI As Uri
        Public isAlreadyInstalled As Boolean

        Public DIRPacket As String = DIRCommons & "\" & PacketName
        Public DIRPacketRepo As String = DIRPacket & "\" & PacketName & ".inf"
        Public DIRPacketFile As String = DIRPacket & "\" & PacketName & ".zip"

        Public RepositoryFileURL As String
        Public RepositoryFilePath As String = DIRCommons & "\Repositories.ini"

        Public isUninstall As Boolean = False
        Public PacketName As String = Nothing
        Public PacketRunParameters As String = Nothing
        Public MustRunAtEnd As Boolean = True

        Sub GetRepositoryFile()
            Try
                Dim regKey As RegistryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Borocito\\boro-get", True)
                If regKey Is Nothing Then
                    End
                Else
                    RepositoryFileURL = regKey.GetValue("Repository")
                End If
            Catch ex As Exception
                AddToLog("GetRepositoryFile@Global", "Error: " & ex.Message, True)
                End
            End Try
        End Sub
    End Module

    Module PacketAdministrator
        Public WithEvents asyncDownloaderZip As New Net.WebClient
        Sub SearchInRepoList(ByVal packetName As String)
            Try
                If My.Computer.FileSystem.FileExists(RepositoryFilePath) Then
                    My.Computer.FileSystem.DeleteFile(RepositoryFilePath)
                End If
                'Descargar repo list
                My.Computer.Network.DownloadFile(RepositoryFileURL, RepositoryFilePath)
                'Verificar la existencia del paquete en el repo list
                Dim repoInfoToDownload As String = GetIniValue("REPOSITORIES", packetName, RepositoryFilePath)
                If repoInfoToDownload <> Nothing Then
                    'de existir, llamar a DownloadFromRepoList
                    DownloadFromRepoInfo(repoInfoToDownload)
                Else
                    End
                End If
            Catch ex As Exception
                AddToLog("SearchInRepoList@PacketAdministrator", "Error: " & ex.Message, True)
                End
            End Try
        End Sub
        Sub DownloadFromRepoInfo(ByVal repoFileLink As String)
            Try
                If My.Computer.FileSystem.FileExists(DIRPacketRepo) Then
                    My.Computer.FileSystem.DeleteFile(DIRPacketRepo)
                End If
                'Descargar repo info
                My.Computer.Network.DownloadFile(repoFileLink, DIRPacketRepo)
                'verificar si existe un link
                Dim packageZipURL As String = GetIniValue("INSTALLER", "Binaries", DIRPacketRepo)
                If packageZipURL <> Nothing Then
                    DownloadZipPackage(packageZipURL)
                Else
                    End
                End If
            Catch ex As Exception
                AddToLog("DownloadFromRepoInfo@PacketAdministrator", "Error: " & ex.Message, True)
                End
            End Try
        End Sub
        Sub DownloadZipPackage(ByVal zipFileURL As String)
            Try
                If My.Computer.FileSystem.FileExists(DIRPacketFile) Then
                    My.Computer.FileSystem.DeleteFile(DIRPacketFile)
                End If
                'Descargar packet zip
                asyncDownloaderZipURI = New Uri(zipFileURL)
                asyncDownloaderZip.DownloadFileAsync(asyncDownloaderZipURI, DIRPacketFile)
            Catch ex As Exception
                AddToLog("DownloadZipPackage@PacketAdministrator", "Error: " & ex.Message, True)
                End
            End Try
        End Sub
        Private Sub DownloadInstallPackage_DownloadFileCompleted(ByVal sender As Object, ByVal e As System.ComponentModel.AsyncCompletedEventArgs) Handles asyncDownloaderZip.DownloadFileCompleted
            FinishInstall()
        End Sub
        Sub FinishInstall()
            Try
                AddToLog("FinishInstall@PacketAdministrator", "Finishing...", False)
                SetInstallVars()
                RegisterInstall()
                ZipFile.ExtractToDirectory(DIRPacketFile, installFolder)
                If MustRunAtEnd Then
                    ComponentInstanceManager(execFilePath)
                End If
                Dim contenido As String = Nothing
                contenido = vbCrLf & "[BORO-GET: PACKET.ADMIN]" &
                        vbCrLf & PacketName & " has been installed!" &
                        vbCrLf & "Run: " & MustRunAtEnd &
                        vbCrLf & "Parameters: " & PacketRunParameters &
                        vbCrLf & "[BORO-GET: PACKET.ADMIN]" & vbCrLf
                BoroHearInterop(contenido)
                End
            Catch ex As Exception
                AddToLog("FinishInstall@PacketAdministrator", "Error: " & ex.Message, True)
                End
            End Try
        End Sub
        Sub SetInstallVars()
            Try
                AddToLog("SetInstallVars@PacketAdministrator", "Seting install variables...", False)
                installFolder = DIRCommons & "\" & PacketName
                If My.Computer.FileSystem.DirectoryExists(installFolder) Then
                    My.Computer.FileSystem.CreateDirectory(installFolder)
                End If
                execFilePath = installFolder & "\" & GetIniValue("ASSEMBLY", "Executable", DIRPacketRepo)
            Catch ex As Exception
                AddToLog("SetInstallVars@PacketAdministrator", "Error: " & ex.Message, True)
            End Try
        End Sub
        Sub RegisterInstall()
            Try
                AddToLog("RegisterInstall@PacketAdministrator", "Checking registry...", False)
                Dim llaveReg As String = "SOFTWARE\\Borocito\\boro-get\\" & PacketName
                Dim regKey As RegistryKey = Registry.CurrentUser.OpenSubKey(llaveReg, True)
                If regKey Is Nothing Then
                    AddToLog("RegisterInstall@PacketAdministrator", "Registering...", False)
                    Registry.CurrentUser.CreateSubKey(llaveReg, True)
                    regKey = Registry.CurrentUser.OpenSubKey(llaveReg, True)
                Else
                    AddToLog("RegisterInstall@PacketAdministrator", "Already registered!", False)
                End If
                regKey.SetValue(PacketName, execFilePath)
                regKey.SetValue("Author", GetIniValue("GENERAL", "Author", DIRPacketRepo))
                regKey.SetValue("From", GetIniValue("GENERAL", "From", DIRPacketRepo))
                regKey.SetValue("Name", GetIniValue("ASSEMBLY", "Name", DIRPacketRepo))
                regKey.SetValue("Executable", GetIniValue("ASSEMBLY", "Executable", DIRPacketRepo))
                regKey.SetValue("Version", GetIniValue("ASSEMBLY", "Version", DIRPacketRepo))
                regKey.SetValue("Web", GetIniValue("ASSEMBLY", "Web", DIRPacketRepo))
                regKey.SetValue("Binaries", GetIniValue("INSTALLER", "Binaries", DIRPacketRepo))
            Catch ex As Exception
                AddToLog("RegisterInstall@PacketAdministrator", "Error: " & ex.Message, True)
            End Try
        End Sub
        Sub Uninstall()
            Try
                AddToLog("Uninstall@PacketAdministrator", "Uninstalling...", False)
                Dim packageRegKey As RegistryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Borocito\\boro-get\\" & PacketName, True)
                Dim executableFile As String = packageRegKey.GetValue(PacketName)
                Dim installDirectory As String = DIRCommons & "\" & PacketName
                Try
                    Dim proc = Process.GetProcessesByName(PacketName)
                    For i As Integer = 0 To proc.Count - 1
                        proc(i).Kill()
                    Next i
                Catch ex As Exception
                    AddToLog("Uninstall(CheckRunning)@PacketAdministrator", "Error: " & ex.Message, True)
                End Try
                If My.Computer.FileSystem.FileExists(executableFile) Then
                    My.Computer.FileSystem.DeleteFile(executableFile)
                End If
                If My.Computer.FileSystem.DirectoryExists(installDirectory) Then
                    My.Computer.FileSystem.DeleteDirectory(installDirectory, FileIO.DeleteDirectoryOption.DeleteAllContents)
                End If
                Dim llaveReg As String = "SOFTWARE\\Borocito\\boro-get"
                Dim regKey As RegistryKey = Registry.CurrentUser.OpenSubKey(llaveReg, True)
                If regKey IsNot Nothing Then
                    regKey.DeleteSubKeyTree(PacketName)
                End If
                Dim contenido As String = Nothing
                contenido = vbCrLf & "[BORO-GET: UNINSTALL.START]" &
                        vbCrLf & PacketName & " has been uninstalled!" &
                        vbCrLf & "[BORO-GET: UNINSTALL.END]" & vbCrLf
                BoroHearInterop(contenido)
            Catch ex As Exception
                AddToLog("Uninstall@PacketAdministrator", "Error: " & ex.Message, True)
            End Try
        End Sub
        Sub ComponentInstanceManager(ByVal componentPath As String)
            Try
                AddToLog("ComponentInstanceManager@PacketAdministrator", "Running it...", False)
                Process.Start(componentPath, PacketRunParameters)
            Catch ex As Exception
                AddToLog("ComponentInstanceManager@PacketAdministrator", "Error: " & ex.Message, True)
            End Try
        End Sub
    End Module

    Module PacketManager
        Sub StopComponent()
            Try
                Dim proc = Process.GetProcessesByName(PacketName)
                If proc.Count = 0 Then
                    BoroHearInterop(PacketName & " has no instances")
                Else
                    For i As Integer = 0 To proc.Count - 1
                        proc(i).Kill()
                    Next i
                    BoroHearInterop(PacketName & " has been closed!")
                End If
            Catch ex As Exception
                AddToLog("StopComponent@PacketManager", "Error: " & ex.Message, True)
            End Try
        End Sub
        Function IsComponentRunning(ByVal name As String) As Boolean
            Try
                Dim p() As Process = Process.GetProcessesByName(name)
                If p.Count > 0 Then
                    Return True
                Else
                    Return False
                End If
            Catch ex As Exception
                AddToLog("IsComponentRunning@PacketManager", "Error: " & ex.Message, True)
                Return False
            End Try
        End Function
        Function ComponentInfo(ByVal name As String) As String()
            Try
                Dim regKey As RegistryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Borocito\\boro-get\\" & name, True)
                Dim contenido As String() = Nothing
                If regKey Is Nothing Then
                    Return {"No data to read"}
                Else
                    contenido = {
                        regKey.GetValue("Author"),
                        regKey.GetValue("Name"),
                        regKey.GetValue("Version"),
                        regKey.GetValue(name),
                        regKey.GetValue("Executable"),
                        regKey.GetValue("From"),
                        regKey.GetValue("Web")
                    }
                End If
                Return contenido
            Catch ex As Exception
                AddToLog("ComponentInfo@PacketManager", "Error: " & ex.Message, True)
                Return Nothing
            End Try
        End Function

        Sub PacketInfo(ByVal packetName As String)
            Try
                Dim contenido As String = Nothing
                Dim PacketInfo As String() = ComponentInfo(packetName)
                If PacketInfo.Length > 1 Then
                    contenido = vbCrLf & "[BORO-GET: STATUS.START]" &
                        vbCrLf & "Author: " & PacketInfo(0) &
                        vbCrLf & "Name: " & PacketInfo(1) &
                        vbCrLf & "Version: " & PacketInfo(2) &
                        vbCrLf & packetName & ": " & PacketInfo(3) &
                        vbCrLf & "Binaries: " & PacketInfo(4) &
                        vbCrLf & "Web: " & PacketInfo(6) &
                        vbCrLf & "isRunning: " & IsComponentRunning(packetName) &
                        vbCrLf & "[BORO-GET: STATUS.END]" & vbCrLf
                Else
                    contenido = vbCrLf & "[BORO-GET: STATUS.START]" & "No data to read about '" & packetName & "'" & vbCrLf & "[BORO-GET: STATUS.END]" & vbCrLf
                End If
                BoroHearInterop(contenido)
            Catch ex As Exception
                AddToLog("PacketInfo@PacketManager", "Error: " & ex.Message, True)
            End Try
        End Sub
        Sub IndexEveryInstalledComponent()
            Try
                Dim contenido As String = Nothing
                Dim Componentes As String = Nothing
                Dim regKey As RegistryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Borocito\\boro-get", True)

                For Each key As String In regKey.GetSubKeyNames
                    Dim PacketInfo As String() = ComponentInfo(key)
                    Componentes &= key & " " & IsComponentRunning(key) & ", " & PacketInfo(2) & vbCrLf
                Next

                contenido = vbCrLf & "[BORO-GET: INSTALLED.START]" &
                vbCrLf & Componentes &
                "[BORO-GET: INSTALLED.END]" & vbCrLf

                BoroHearInterop(contenido)
            Catch ex As Exception
                AddToLog("IndexEveryInstalledComponent@PacketManager", "Error: " & ex.Message, True)
            End Try
        End Sub
    End Module
End Namespace
