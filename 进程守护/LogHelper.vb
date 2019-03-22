Imports System
Imports System.IO
Imports System.Threading
Imports System.Threading.Thread

Public Class LogHelper
    Private Shared rootPath As String = System.IO.Directory.GetCurrentDirectory & "/Logs/"
    Private Shared lc As Object = New Object()

    Public Shared Sub CheckDir()
        Dim dir As DirectoryInfo = New DirectoryInfo(rootPath)
        If Not dir.Exists Then
            dir.Create()
        End If
    End Sub

    Public Shared Sub Log(ByVal content As String, Optional tagName As String = "default")
        Dim th As New Thread(Sub()
                                 SyncLock lc
                                     Try
                                         CheckDir()
                                         Dim now As DateTime = DateTime.Now
                                         Dim str As String = now.ToString("[HH:mm:ss] ") & "<" & tagName & "> " & content & vbCrLf
                                         Dim filePath As String = rootPath & now.ToString("yyyy_MM_dd_") & "watchDog.txt"
                                         File.AppendAllText(filePath, str)
                                     Catch e As Exception
                                     End Try
                                 End SyncLock
                             End Sub)
        th.Start()
    End Sub
End Class

