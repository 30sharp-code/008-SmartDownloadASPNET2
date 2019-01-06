Public Class FileInformation

  ' This class is used to retrieve file information
  ' for the download process, and to manage the 
  ' download state

#Region "Constructor"

  Public Sub New(ByVal sPath As String)
    m_objFile = New System.IO.FileInfo(sPath)
  End Sub

#End Region

  <Flags()> Enum DownloadState
    ' Clear: No download in progress, 
    ' the file can be manipulated
    fsClear = 1

    ' Locked: A dynamically created file must
    ' not be changed
    fsLocked = 2

    ' In Progress: File is locked, and download 
    ' is currently in progress
    fsDownloadInProgress = 6

    ' Broken: File is locked, download was in
    ' progress, but was cancelled 
    fsDownloadBroken = 10

    ' Finished: File is locked, download
    ' was completed
    fsDownloadFinished = 18
  End Enum

#Region "Private objects and variables"

  Private m_objFile As System.IO.FileInfo
  Private m_nState As DownloadState

#End Region

#Region "Public Properties 'inherited' from FileInfo"

  Public ReadOnly Property Exists() As Boolean
    Get
      ' ToDo - your code here (create the ZIP file dynamically)
      Return m_objFile.Exists
    End Get
  End Property

  Public ReadOnly Property FullName() As String
    Get
      Return m_objFile.FullName
    End Get
  End Property

  Public ReadOnly Property LastWriteTimeUTC() As Date
    Get
      Return m_objFile.LastWriteTimeUtc
    End Get
  End Property

  Public ReadOnly Property Length() As Long
    Get
      Return m_objFile.Length
    End Get
  End Property

#End Region

#Region "Public Properties"

  Public ReadOnly Property ContentType() As String
    ' MIME Type of the the file to download
    Get
      ' ToDo - your code here (Return the appropriate MIME type for your file)
      '
      ' This article shows a list of MIME types for IIS:
      ' (Appendix A: Default MIME Type Associations for IIS)
      ' http://www.microsoft.com/technet/prodtechnol/isa/2004/plan/mimetypes.mspx

      ' If you do not know the correct mime type for
      ' your document, please use "application/octet-stream".

      ' Returns ZIP MIME type
      Return "application/x-zip-compressed"
    End Get
  End Property

  Public ReadOnly Property EntityTag() As String
    ' The EntityTag used in the initial (200) response to, 
    ' and in resume-Requests from the client software... 
    Get
      ' ToDo - your code here (Create a unique string for your file)
      '
      ' Please note, that this unique code must keep
      ' the same as long as the file does not change. 
      ' If the file DOES change or is edited, however,
      ' the code MUST change.
      Return "MyExampleFileID"
    End Get
  End Property

  Public Property State() As DownloadState
    Get
      Return m_nState
    End Get
    Set(ByVal nState As DownloadState)
      m_nState = nState

      ' ToDo - optional
      ' At this point, you could delete the file automatically. 
      ' If the state is set to Finished, your might not need
      ' the file anymore:
      '
      ' If nState = DownloadState.fsDownloadFinished Then
      '   Clear()
      ' Else
      '   Save()
      ' End If

      Save()
    End Set
  End Property

#End Region

#Region "Public Methods"

  Public Sub Clear()
    ' Delete the source file and "clear" the file state...

    If State = DownloadState.fsDownloadBroken Or State = DownloadState.fsDownloadInProgress Then
      ' Do not allow deleting if the file download is in progress 

    Else
      m_objFile.Delete()
      State = DownloadState.fsClear

    End If

  End Sub

#End Region

#Region "Private Methods"

  Private Sub Save()

    ' ToDo - your code here (Save the state of this file's download to a database or XML file...)
    '
    ' Do not use the Session or Application or Cache to
    ' store this information, it must be independent from
    ' Application, Session or Cache states!
    '
    ' If you do not create files dynamically, 
    ' you do not need to save the state, of course.

  End Sub

#End Region

End Class
