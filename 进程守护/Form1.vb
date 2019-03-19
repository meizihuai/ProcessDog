Imports System
Imports System.IO
Imports System.Text
Imports System.Threading
Imports System.Threading.Thread
Imports System.Diagnostics
Imports Newtonsoft
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports System.Net

Public Class Form1
    Dim defaultHttpUrl As String = "http://127.0.0.1:3210/watchdog/?func=Test"
    Dim voltageHttp As String = "http://10.253.12.107:5001/api/values/GetGateWayStatusInfo"
    Dim xmlPath As String = "ProgressDogConfig.txt"
    Dim myConf As conf
    Dim dogOpen As Boolean = False
    Dim isVoltageUrlRight As Boolean = False
    Dim voltageUrl As String = ""
    Structure conf
        Dim appName As String
        Dim autoStart As Boolean
        Dim waitCount As Integer
        Dim httpUrl As String
        Dim watchHttp As Boolean
        Dim voltageShutDown As Boolean
        Dim voltageHttpIp As String
        Dim errRestartWindows As Boolean
        Dim showAppWindow As Boolean
        Sub New(ByVal _appName As String, ByVal _autoStart As Boolean, ByVal _httpUrl As String, ByVal _watchHttp As Boolean, _voltageShutDown As Boolean)
            appName = _appName
            autoStart = _autoStart
            waitCount = 8
            httpUrl = _httpUrl
            watchHttp = _watchHttp
            errRestartWindows = True
            showAppWindow = True
            voltageShutDown = _voltageShutDown
            ' voltageHttp = _voltageHttp
        End Sub
    End Structure

    Private Sub Form1_FormClosed(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosedEventArgs) Handles Me.FormClosed
        End
    End Sub
    Private Sub Form1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        Me.Text = "进程守护V1.7"
        Me.MaximizeBox = False
        Control.CheckForIllegalCrossThreadCalls = False
        Me.FormBorderStyle = Windows.Forms.FormBorderStyle.FixedSingle
        ini()
        If myConf.autoStart Then
            StartDog()
        End If
        Dim th As New Thread(AddressOf ProgressDog)
        th.Start()
    End Sub
    Private Sub ini()
        iniConfig()
        If myConf.voltageShutDown Then
            Dim th As New Thread(AddressOf GetVoltageUrl)
            th.Start()
        Else
            lblVoltagIP.Text = "未开启..."
        End If
    End Sub
    Private Sub iniConfig()
        If File.Exists(xmlPath) = False Then
            DefaultConfig()
            lblAppName.Text = myConf.appName
            lblStatus.Text = "没有运行"
            CBVoltage.Checked = myConf.voltageShutDown
            Return
        End If
        Try
            Dim sr As New StreamReader(xmlPath, Encoding.Default)
            Dim str As String = sr.ReadToEnd
            sr.Close()
            myConf = JsonConvert.DeserializeObject(str, GetType(conf))
            If myConf.httpUrl = "" Then
                myConf.httpUrl = defaultHttpUrl
            End If
        Catch ex As Exception
            DefaultConfig()
        End Try
        lblAppName.Text = myConf.appName
        lblStatus.Text = "没有运行"
        CBVoltage.Checked = myConf.voltageShutDown

    End Sub
    Private Sub DefaultConfig()
        If File.Exists(xmlPath) Then File.Delete(xmlPath)
        myConf = New conf("TekDeviceControler.exe", True, defaultHttpUrl, True, True)
        SaveMyConfig()
    End Sub
    Private Sub SaveMyConfig()
        Dim json As String = JsonConvert.SerializeObject(myConf, Formatting.Indented)
        Dim sw As New StreamWriter(xmlPath, False, Encoding.Default)
        sw.Write(json)
        sw.Close()
    End Sub
    Private Sub GetVoltageUrl()
        lblVoltagIP.Text = "监测中..."
        lblVoltagIP.ForeColor = Color.Red

        Dim voltageHttpIp As String = myConf.voltageHttpIp
        If voltageHttpIp <> "" Then
            Dim ip As String = ""
            Try
                ip = IPAddress.Parse(voltageHttpIp).ToString()
            Catch ex As Exception

            End Try
            voltageHttpIp = ip
        End If
        If voltageHttpIp <> "" Then
            Dim url As String = "http://" & voltageHttpIp & ":5001/api/values/GetGateWayStatusInfo"
            If TestVoltageUrl(url) Then
                '找到电压接口了
                Me.isVoltageUrlRight = True
                Me.voltageUrl = url
                lblVoltagIP.Text = voltageHttpIp
                lblVoltagIP.ForeColor = Color.Blue
                GetVoltageLoop()
                Return
            End If
            Dim st() As String = voltageHttpIp.Split(".")
            Dim leftString = st(0) & "." & st(1) & "." & st(2) & "."
            Dim lastint As Integer = Val(st(3))
            For i = 1 To 255
                Dim newIp As String = leftString & i
                url = "http://" & newIp & ":5001/api/values/GetGateWayStatusInfo"
                lblVoltagIP.Text = "正在测试 " & newIp
                lblVoltagIP.ForeColor = Color.Red
                If TestVoltageUrl(url) Then
                    voltageHttpIp = newIp
                    '找到电压接口了
                    myConf.voltageHttpIp = voltageHttpIp
                    SaveMyConfig()
                    Me.isVoltageUrlRight = True
                    Me.voltageUrl = url
                    lblVoltagIP.Text = voltageHttpIp
                    lblVoltagIP.ForeColor = Color.Blue
                    GetVoltageLoop()
                    Return
                End If
            Next
        End If
        Dim hostName As String = Dns.GetHostName
        Dim ips As IPHostEntry = Dns.GetHostByName(hostName)
        For Each itm In ips.AddressList
            Dim ip As String = itm.ToString
            If ip <> "127.0.0.1" And ip <> "localhost" Then
                Dim st() As String = ip.Split(".")
                Dim leftString = st(0) & "." & st(1) & "." & st(2) & "."
                Dim lastint As Integer = Val(st(3))
                For i = 1 To 255
                    Dim newIp As String = leftString & i
                    Dim url As String = "http://" & newIp & ":5001/api/values/GetGateWayStatusInfo"
                    lblVoltagIP.Text = "正在测试 " & newIp
                    lblVoltagIP.ForeColor = Color.Red
                    If TestVoltageUrl(url) Then
                        voltageHttpIp = newIp
                        '找到电压接口了
                        myConf.voltageHttpIp = voltageHttpIp
                        SaveMyConfig()
                        Me.isVoltageUrlRight = True
                        Me.voltageUrl = url
                        lblVoltagIP.Text = voltageHttpIp
                        lblVoltagIP.ForeColor = Color.Blue
                        GetVoltageLoop()
                        Return
                    End If
                Next
            End If
        Next

        If Me.isVoltageUrlRight = False Then
            Me.isVoltageUrlRight = False
            Me.voltageUrl = ""
            lblVoltagIP.Text = "没有监测到电压数据接口服务"
            lblVoltagIP.ForeColor = Color.Red
        End If
    End Sub
    Private Function TestVoltageUrl(url As String) As Boolean
        Try
            Dim req As HttpWebRequest = WebRequest.Create(url)
            req.Accept = "*/*"
            req.UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 5.1; zh-CN; rv:1.9.2.13) Gecko/20101203 Firefox/3.6.13"
            req.CookieContainer = New CookieContainer
            req.KeepAlive = True
            req.ContentType = "application/x-www-form-urlencoded"
            req.Method = "GET"
            req.Timeout = 100
            req.ReadWriteTimeout = 100
            Dim rp As HttpWebResponse = req.GetResponse
            Dim str As String = New StreamReader(rp.GetResponseStream(), Encoding.UTF8).ReadToEnd
            Dim np As NormalResponse = JsonConvert.DeserializeObject(Of NormalResponse)(str)
            If IsNothing(np) Then Return False
            If np.result Then
                Return True
            End If
        Catch ex As Exception

        End Try
        Return False
    End Function
    Private Sub GetVoltageLoop()
        Dim waitCount As Integer = myConf.waitCount
        If waitCount = 0 Then waitCount = 8
        Dim url As String = voltageUrl
        Dim minVoltage As Double = 10.5
        Dim th As New Thread(Sub()
                                 While True
                                     Try
                                         Dim str As String = "正在获取..."
                                         lblVoltageValue.Text = str
                                         Dim d As Double = GetVoltage(url)
                                         If d = 0 Then
                                             str = "0 V [接口度数问题]"
                                             lblVoltageValue.ForeColor = Color.Red
                                         End If
                                         If d > 0 And d <= minVoltage Then
                                             str = d & " V [电压过低！]"
                                             lblVoltageValue.ForeColor = Color.Red
                                             ShutDownWindows()
                                         End If
                                         If d > minVoltage Then
                                             str = d & " V [电压正常]"
                                             lblVoltageValue.ForeColor = Color.Blue
                                         End If
                                         lblVoltageValue.Text = str & " " & Now.ToString("yyyy-MM-dd HH:mm:ss")
                                     Catch ex As Exception

                                     End Try
                                     Sleep(waitCount * 1000)
                                 End While
                             End Sub)
        th.Start()
    End Sub
    Private Function GetVoltage(url As String) As Double
        Try
            Dim req As HttpWebRequest = WebRequest.Create(url)
            req.Accept = "*/*"
            req.UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 5.1; zh-CN; rv:1.9.2.13) Gecko/20101203 Firefox/3.6.13"
            req.CookieContainer = New CookieContainer
            req.KeepAlive = True
            req.ContentType = "application/x-www-form-urlencoded"
            req.Method = "GET"
            req.Timeout = 3000
            req.ReadWriteTimeout = 3000
            Dim rp As HttpWebResponse = req.GetResponse
            Dim str As String = New StreamReader(rp.GetResponseStream(), Encoding.UTF8).ReadToEnd
            Dim np As NormalResponse = JsonConvert.DeserializeObject(Of NormalResponse)(str)
            If IsNothing(np) Then Return 0
            If np.result Then
                Dim info As WGStatusInfo = JsonConvert.DeserializeObject(Of WGStatusInfo)(np.data.ToString)
                If IsNothing(info) = False Then
                    Return info.voltage
                End If
            Else
                Return 0
            End If
        Catch ex As Exception

        End Try
        Return 0
    End Function
    Private Function TestHttpWeb() As Boolean
        Try
            Dim result As String = GetH(myConf.httpUrl, "")
            If result.ToLower() = "true" Then
                Return True
            Else
                Return False
            End If
        Catch ex As Exception
            Return False
        End Try
    End Function
    Private Sub ReStartWindows()
        Process.Start("shutdown.exe", "-r -t 0")
    End Sub
    Private Sub ShutDownWindows()
        Process.Start("shutdown.exe", "-s -t 0")
    End Sub
    Private Sub ProgressDog()
        Dim waitCount As Integer = myConf.waitCount
        If waitCount = 0 Then waitCount = 8
        While True
            Dim isFind As Boolean = False
            Dim ps() As Process = Process.GetProcesses()
            For Each itm In ps
                If itm.ProcessName = myConf.appName.Replace(".exe", "") Then
                    lblStatus.Text = "正常运行"
                    lblStatus.ForeColor = Color.Green
                    isFind = True
                    If myConf.watchHttp Then
                        lblStatus.Text = "监测HTTP服务……"
                        If TestHttpWeb() = False Then
                            Dim int As Integer = 0
                            Dim isClosed As Boolean = False
                            While int < 5
                                Try
                                    lblStatus.Text = "HTTP服务异常，第" & int + 1 & "次尝试关闭..."
                                    itm.Kill()
                                    isClosed = True
                                    isFind = False
                                    Exit While
                                Catch ex As Exception
                                    isClosed = False
                                End Try
                                int = int + 1
                            End While
                            lblStatus.Text = "HTTP服务异常！无法关闭程序！"
                            lblStatus.ForeColor = Color.Red
                            If Not isClosed Then
                                If myConf.errRestartWindows Then
                                    lblStatus.Text = "HTTP服务异常！无法关闭程序！即将重启Windows"
                                    Sleep(3000)
                                    ReStartWindows()
                                End If
                            End If
                        Else
                            lblStatus.Text = "正常运行"
                        End If
                    End If
                    Exit For
                End If
            Next
            If isFind = False Then
                lblStatus.Text = "没有运行"
                lblStatus.ForeColor = Color.Red
                If dogOpen Then
                    If File.Exists("updateing.upd") Then
                        lblStatus.Text = "主程序正在更新"
                    Else
                        If File.Exists(myConf.appName) Then
                            lblStatus.Text = "正在开启……"
                            ' RunEXE(myConf.appName)
                            lblStatus.ForeColor = Color.YellowGreen
                            If myConf.showAppWindow Then
                                Shell(myConf.appName, AppWinStyle.NormalFocus, False)
                            Else
                                Shell(myConf.appName, AppWinStyle.MinimizedNoFocus, False)
                            End If

                            lblStatus.Text = "已启动！"
                            lblStatus.ForeColor = Color.Green
                            ' lblStatus.Text = "正常运行"
                        Else
                            lblStatus.Text = myConf.appName & "文件不存在"
                            lblStatus.ForeColor = Color.Red
                        End If
                    End If

                End If
            End If
            For i = waitCount To 1 Step -1
                Label3.Text = i & " 秒"
                Sleep(1000)
            Next

        End While
    End Sub
    Public Function GetH(ByVal uri As String, ByVal msg As String) As String
        Dim num As Integer = 0
        While True
            Try
                Dim req As HttpWebRequest = WebRequest.Create(uri & msg)
                req.Accept = "*/*"
                req.UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 5.1; zh-CN; rv:1.9.2.13) Gecko/20101203 Firefox/3.6.13"
                req.CookieContainer = New CookieContainer
                req.KeepAlive = True
                req.ContentType = "application/x-www-form-urlencoded"
                req.Method = "GET"
                req.Timeout = 3000
                req.ReadWriteTimeout = 3000
                Dim b() As Byte = Encoding.Default.GetBytes(msg)
                Dim rp As HttpWebResponse = req.GetResponse
                Dim str As String = New StreamReader(rp.GetResponseStream(), Encoding.UTF8).ReadToEnd
                b = Encoding.Default.GetBytes(str)
                Return str
            Catch ex As Exception

            End Try
            num = num + 1
            If num = 2 Then Return ""
        End While
    End Function
    'Private Function RunEXE(ByVal exePath As String) As Boolean
    '    Dim p As New Process
    '    p.StartInfo.FileName = exePath
    '    'p.StartInfo.WorkingDirectory = Directory
    '    'p.StartInfo.Arguments = argument
    '    p.StartInfo.ErrorDialog = False               
    '    p.StartInfo.CreateNoWindow = True
    '    p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
    '    p.StartInfo.RedirectStandardOutput = True
    '    p.StartInfo.RedirectStandardInput = True
    '    p.StartInfo.RedirectStandardError = True
    '    '  p.EnableRaisingEvents = true                     
    '    p.Start()
    '    Return True
    'End Function
    Private Sub StartDog()
        Button1.Text = "停止守护"
        dogOpen = True
    End Sub
    Private Sub StopDog()
        Button1.Text = "开启守护"
        dogOpen = False
    End Sub
    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        If dogOpen Then
            StopDog()
        Else
            StartDog()
        End If
    End Sub

    Private Sub CBVoltage_CheckedChanged(sender As Object, e As EventArgs) Handles CBVoltage.CheckedChanged
        myConf.voltageShutDown = CBVoltage.Checked
        SaveMyConfig()
    End Sub
End Class
