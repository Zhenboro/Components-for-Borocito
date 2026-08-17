Imports System.Text.RegularExpressions
Imports Microsoft.Win32
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
            AddToLog("SessionEvent@Init", "Error: " & ex.Message, True)
        End Try
    End Sub

    Dim currentHiveKey As String
    Dim currentHiveKeyReg As RegistryKey
    Dim currentKey As String

    Function SelectHKey(ByVal hiveKey As String) As String
        Try
            currentHiveKey = hiveKey
            Select Case hiveKey
                Case "ClassesRoot"
                    currentHiveKeyReg = Registry.ClassesRoot
                Case "CurrentConfig"
                    currentHiveKeyReg = Registry.CurrentConfig
                Case "CurrentUser"
                    currentHiveKeyReg = Registry.CurrentUser
                Case "LocalMachine"
                    currentHiveKeyReg = Registry.LocalMachine
                Case "PerformanceData"
                    currentHiveKeyReg = Registry.PerformanceData
                Case "Users"
                    currentHiveKeyReg = Registry.Users
                Case Else
                    currentHiveKeyReg = Registry.CurrentUser
            End Select
            Return "HiveKey selected " & currentHiveKey
        Catch ex As Exception
            Return AddToLog("SelectHKey@RegistryEditor", "Error: " & ex.Message, True)
        End Try
    End Function
    Function SelectKey(ByVal nameKey As String) As String
        Try
            currentKey = nameKey
            currentHiveKeyReg = Registry.CurrentUser.OpenSubKey(nameKey, True)
            Return "Key selected " & currentKey
        Catch ex As Exception
            Return AddToLog("SelectKey@RegistryEditor", "Error: " & ex.Message, True)
        End Try
    End Function

    Function GetValue(ByVal valueName As String) As String
        Try
            Return currentHiveKeyReg.GetValue(valueName)
        Catch ex As Exception
            Return AddToLog("GetValue@RegistryEditor", "Error: " & ex.Message, True)
        End Try
    End Function
    Function SetValue(ByVal valueName As String, ByVal valueKind As String, ByVal value As String) As String
        Try
            If valueKind = Nothing Or valueKind.ToLower = "null" Or Not Regex.IsMatch(valueKind, "^[0-9 ]+$") Then
                valueKind = 1
            End If
            currentHiveKeyReg.SetValue(valueName, value, valueKind)
            Return "Seted! Name " & valueName & " Value " & value
        Catch ex As Exception
            Return AddToLog("SetValue@RegistryEditor", "Error: " & ex.Message, True)
        End Try
    End Function
    Function DeleteValue(ByVal valueName As String) As String
        Try
            currentHiveKeyReg.DeleteValue(valueName)
            Return "Deleted! " & valueName
        Catch ex As Exception
            Return AddToLog("DeleteValue@RegistryEditor", "Error: " & ex.Message, True)
        End Try
    End Function

    Function CreateSubKey(ByVal subKey As String) As String
        Try
            currentHiveKeyReg.CreateSubKey(subKey, True)
            Return "Key created! " & subKey
        Catch ex As Exception
            Return AddToLog("CreateSubKey@RegistryEditor", "Error: " & ex.Message, True)
        End Try
    End Function
    Function DeleteSubKey(ByVal subKey As String) As String
        Try
            currentHiveKeyReg.DeleteSubKey(subKey)
            Return "Key deleted! " & subKey
        Catch ex As Exception
            Return AddToLog("DeleteSubKey@RegistryEditor", "Error: " & ex.Message, True)
        End Try
    End Function
    Function DeleteSubKeyTree(ByVal subKey As String) As String
        Try
            currentHiveKeyReg.DeleteSubKeyTree(subKey)
            Return "Key tree deleted! " & subKey
        Catch ex As Exception
            Return AddToLog("DeleteSubKeyTree@RegistryEditor", "Error: " & ex.Message, True)
        End Try
    End Function

    Function GetValueNames() As String
        Try
            Dim contenido As String = Nothing
            For Each item As String In currentHiveKeyReg.GetValueNames()
                contenido &= item & vbCrLf
            Next
            Return contenido
        Catch ex As Exception
            Return AddToLog("GetValueNames@RegistryEditor", "Error: " & ex.Message, True)
        End Try
    End Function
    Function GetSubKeyNames() As String
        Try
            Dim contenido As String = Nothing
            For Each item As String In currentHiveKeyReg.GetSubKeyNames()
                contenido &= item & vbCrLf
            Next
            Return contenido
        Catch ex As Exception
            Return AddToLog("GetSubKeyNames@RegistryEditor", "Error: " & ex.Message, True)
        End Try
    End Function
    Function GetValueKind(ByVal valueName As String) As String
        Try
            Return currentHiveKeyReg.GetValueKind(valueName)
        Catch ex As Exception
            Return AddToLog("GetValueKind@RegistryEditor", "Error: " & ex.Message, True)
        End Try
    End Function
End Class