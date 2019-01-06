Public Class ZIPHandler
  Implements IHttpHandler

    ' MULTIPART_BOUNDARY deliniate "chunks.
    '  It should be as unique possible so as not to look like data
    Private Const MULTIPART_BOUNDARY As String = "<a1b2c3d4e5f6g7h8i9j0>"
    Private Const MULTIPART_CONTENTTYPE As String = "multipart/byteranges; boundary=" & MULTIPART_BOUNDARY
    Private Const HTTP_HEADER_ACCEPT_RANGES As String = "Accept-Ranges"
    Private Const HTTP_HEADER_ACCEPT_RANGES_BYTES As String = "bytes"
    Private Const HTTP_HEADER_CONTENT_TYPE As String = "Content-Type"
    Private Const HTTP_HEADER_CONTENT_RANGE As String = "Content-Range"
    Private Const HTTP_HEADER_CONTENT_LENGTH As String = "Content-Length"
    Private Const HTTP_HEADER_ENTITY_TAG As String = "ETag"
    Private Const HTTP_HEADER_LAST_MODIFIED As String = "Last-Modified"
    Private Const HTTP_HEADER_RANGE As String = "Range"
    Private Const HTTP_HEADER_IF_RANGE As String = "If-Range"
    Private Const HTTP_HEADER_IF_MATCH As String = "If-Match"
    Private Const HTTP_HEADER_IF_NONE_MATCH As String = "If-None-Match"
    Private Const HTTP_HEADER_IF_MODIFIED_SINCE As String = "If-Modified-Since"
    Private Const HTTP_HEADER_IF_UNMODIFIED_SINCE As String = "If-Unmodified-Since"
    Private Const HTTP_HEADER_UNLESS_MODIFIED_SINCE As String = "Unless-Modified-Since"
    Private Const HTTP_METHOD_GET As String = "GET"
    Private Const HTTP_METHOD_HEAD As String = "HEAD"

    Public ReadOnly Property IsReusable() As Boolean Implements System.Web.IHttpHandler.IsReusable
        Get
            Return True ' Allow ASP.NET to reuse instances of this class.
        End Get
    End Property

    '-------------------------------------------------------------------------
    Public Sub ProcessRequest(ByVal objContext As System.Web.HttpContext) _
               Implements System.Web.IHttpHandler.ProcessRequest

        Dim objRequest As HttpRequest = objContext.Request
        Dim objResponse As HttpResponse = objContext.Response
        Dim objFile As Download.FileInformation ' Custom File information object...
        Dim objStream As System.IO.FileStream
        Dim arliRequestedRangesBegin() As Long ' Start of Chunk
        Dim arliRequestedRangesend() As Long ' End of Chunk
        Dim iResponseContentLength As Int32
        Dim iBytesToRead As Int32
        Dim iBufferSize As Int32 = 25000
        Dim iLengthOfReadChunk As Int32
        Dim iLoop As Int32
        Dim bWasDownloadInterupted As Boolean ' Was download Interupted
        Dim bIsChunkRequest As Boolean
        Dim bMultipart As Boolean
        Dim byBufer(iBufferSize) As Byte

        ' Get the path to the file that was requested. 
        ' ONLY FILE TYPES WITH EXTENSIONS MAPPED IN WEB.CONFIG WILL CAL THIS CODE
        Dim strFile As String = objContext.Server.MapPath(objRequest.FilePath)
        objFile = New Download.FileInformation(strFile)
        objResponse.Clear()
        Dim strMethod As String = objRequest.HttpMethod.ToString
        ' We could support more request types
        If Not objRequest.HttpMethod.Equals(HTTP_METHOD_GET) And _
           Not objRequest.HttpMethod.Equals(HTTP_METHOD_HEAD) Then
            objResponse.StatusCode = 501  ' HTTP Request TYpe Not implemented

        ElseIf Not objFile.Exists Then
            objResponse.StatusCode = 404  ' File Not found

        ElseIf objFile.Length > Int32.MaxValue Then
            objResponse.StatusCode = 413  ' Request For too many bytes 

        ElseIf Not ParseRequestHeaderRange(objRequest, arliRequestedRangesBegin, arliRequestedRangesend, _
                                           objFile.Length, bIsChunkRequest) Then
            objResponse.StatusCode = 400  ' Bad Request

        ElseIf Not CheckIfModifiedSince(objRequest, objFile) Then
            objResponse.StatusCode = 304  ' Not Modified

        ElseIf Not CheckIfUnmodifiedSince(objRequest, objFile) Then
            objResponse.StatusCode = 412  ' Modified Pre-condition failed

        ElseIf Not CheckIfMatch(objRequest, objFile) Then
            objResponse.StatusCode = 412  ' Entitiy Precondition failed

        ElseIf Not CheckIfNoneMatch(objRequest, objResponse, objFile) Then
            ' Nothing to download.

        Else
            ' Everything is good so far. 
            If bIsChunkRequest AndAlso CheckIfRange(objRequest, objFile) Then ' Valid Chunk Request.
                bMultipart = CBool(arliRequestedRangesBegin.GetUpperBound(0) > 0)
                ' Loop through chunks to calculate the total length
                For iLoop = arliRequestedRangesBegin.GetLowerBound(0) To arliRequestedRangesBegin.GetUpperBound(0)
                    iResponseContentLength += Convert.ToInt32(arliRequestedRangesend(iLoop) - arliRequestedRangesBegin(iLoop)) + 1
                    If bMultipart Then
                        ' Calc length of the headers
                        iResponseContentLength += MULTIPART_BOUNDARY.Length
                        iResponseContentLength += objFile.ContentType.Length
                        iResponseContentLength += arliRequestedRangesBegin(iLoop).ToString.Length
                        iResponseContentLength += arliRequestedRangesend(iLoop).ToString.Length
                        iResponseContentLength += objFile.Length.ToString.Length
                        iResponseContentLength += 49 ' add length needed for multipart header
                    End If
                Next iLoop

                If bMultipart Then
                    ' Calc length of last intermediate header 
                    iResponseContentLength += MULTIPART_BOUNDARY.Length
                    iResponseContentLength += 8 ' length of dash and line break 
                Else
                    objResponse.AppendHeader(HTTP_HEADER_CONTENT_RANGE, "bytes " & _
                                             arliRequestedRangesBegin(0).ToString & "-" & _
                                             arliRequestedRangesend(0).ToString & "/" & _
                                             objFile.Length.ToString)
                End If
                objResponse.StatusCode = 206 ' Partial Response

            Else
                ' Not a Chuck  request or Entity doesn't match
                ' Start a new download of the entire file
                iResponseContentLength = Convert.ToInt32(objFile.Length)
                objResponse.StatusCode = 200 ' Status OK
            End If

            objResponse.AppendHeader(HTTP_HEADER_CONTENT_LENGTH, iResponseContentLength.ToString)
            objResponse.AppendHeader(HTTP_HEADER_LAST_MODIFIED, objFile.LastWriteTimeUTC.ToString("r"))
            objResponse.AppendHeader(HTTP_HEADER_ACCEPT_RANGES, HTTP_HEADER_ACCEPT_RANGES_BYTES)
            objResponse.AppendHeader(HTTP_HEADER_ENTITY_TAG, """" & objFile.EntityTag & """") ' Entity Tag muss be Quote Enclosed

            If bMultipart Then
                ' File real MIME type gets pushed into the 
                ' response object later
                objResponse.ContentType = MULTIPART_CONTENTTYPE
            Else
                objResponse.ContentType = objFile.ContentType
            End If

            If objRequest.HttpMethod.Equals(HTTP_METHOD_HEAD) Then
                ' Only the HEAD was requested
            Else
                objResponse.Flush()
                ' We're Downloading !!
                objFile.State = FileInformation.DownloadState.fsDownloadInProgress
                objStream = New System.IO.FileStream(objFile.FullName, IO.FileMode.Open, _
                                                                       IO.FileAccess.Read, _
                                                                       IO.FileShare.Read)

                ' Process all the requested chubks.
                For iLoop = arliRequestedRangesBegin.GetLowerBound(0) To arliRequestedRangesBegin.GetUpperBound(0)

                    objStream.Seek(arliRequestedRangesBegin(iLoop), IO.SeekOrigin.Begin)
                    iBytesToRead = Convert.ToInt32(arliRequestedRangesend(iLoop) - arliRequestedRangesBegin(iLoop)) + 1
                    If bMultipart Then ' Send Headers
                        objResponse.Output.WriteLine("--" & MULTIPART_BOUNDARY) ' Indicate the part boundry
                        objResponse.Output.WriteLine(HTTP_HEADER_CONTENT_TYPE & ": " & objFile.ContentType)
                        objResponse.Output.WriteLine(HTTP_HEADER_CONTENT_RANGE & ": bytes " & _
                                                     arliRequestedRangesBegin(iLoop).ToString & "-" & _
                                                     arliRequestedRangesend(iLoop).ToString & "/" & _
                                                     objFile.Length.ToString)
                        objResponse.Output.WriteLine()
                    End If

                    ' Send the Data.
                    While iBytesToRead > 0

                        If objResponse.IsClientConnected Then
                            iLengthOfReadChunk = objStream.Read(byBufer, 0, Math.Min(byBufer.Length, iBytesToRead))
                            objResponse.OutputStream.Write(byBufer, 0, iLengthOfReadChunk)
                            objResponse.Flush()
                            ReDim byBufer(iBufferSize)
                            iBytesToRead -= iLengthOfReadChunk
                        Else
                            ' DOWNLOAD INTERUPTED
                            iBytesToRead = -1
                            bWasDownloadInterupted = True
                        End If
                    End While

                    If bMultipart Then
                        objResponse.Output.WriteLine() ' Mark the end of the part
                    End If

                    If bWasDownloadInterupted Then
                        Exit For
                    End If

                Next iLoop

                ' Doanload finished or cancelled... 
                If bWasDownloadInterupted Then ' Download broken...
                    objFile.State = FileInformation.DownloadState.fsDownloadBroken
                Else
                    If bMultipart Then
                        objResponse.Output.WriteLine("--" & MULTIPART_BOUNDARY & "--")
                        objResponse.Output.WriteLine()
                    End If

                    ' Download Complete
                    objFile.State = FileInformation.DownloadState.fsDownloadFinished
                End If
                objStream.Close()
            End If
        End If
        objResponse.End()
    End Sub

    Private Function CheckIfRange(ByVal objRequest As HttpRequest, ByVal objFile As Download.FileInformation) As Boolean
        Dim sRequestHeaderIfRange As String

        ' Get Requests If-Range Header value
        sRequestHeaderIfRange = RetrieveHeader(objRequest, HTTP_HEADER_IF_RANGE, objFile.EntityTag)

        ' If the requested file entity matches the current
        ' entity id, return True - If Not return f=False.
        Return sRequestHeaderIfRange.Equals(objFile.EntityTag)
    End Function

    Private Function CheckIfMatch(ByVal objRequest As HttpRequest, ByVal objFile As Download.FileInformation) As Boolean
        Dim sRequestHeaderIfMatch As String
        Dim sEntityIDs() As String
        Dim bReturn As Boolean

        ' Get Request If-Match Header, * is there was none
        sRequestHeaderIfMatch = RetrieveHeader(objRequest, HTTP_HEADER_IF_MATCH, "*")
        If sRequestHeaderIfMatch.Equals("*") Then
            bReturn = True ' No Match
        Else
            sEntityIDs = sRequestHeaderIfMatch.Replace("bytes=", "").Split(",".ToCharArray)
            ' Iterate entity IDs to see if there's a match to the current file ID 
            For iLoop As Int32 = sEntityIDs.GetLowerBound(0) To sEntityIDs.GetUpperBound(0)
                If sEntityIDs(iLoop).Trim.Equals(objFile.EntityTag) Then
                    bReturn = True
                End If
            Next iLoop
        End If
        Return bReturn
    End Function

    Private Function CheckIfNoneMatch(ByVal objRequest As HttpRequest, ByVal objResponse As HttpResponse, ByVal objFile As Download.FileInformation) As Boolean
        Dim sRequestHeaderIfNoneMatch As String
        Dim sEntityIDs() As String
        Dim bReturn As Boolean = True
        Dim sReturn As String = ""
        ' Get Request If-None-Match Header value
        sRequestHeaderIfNoneMatch = RetrieveHeader(objRequest, HTTP_HEADER_IF_NONE_MATCH, String.Empty)
        If sRequestHeaderIfNoneMatch.Equals(String.Empty) Then
            bReturn = True ' Perform request normally

        ElseIf sRequestHeaderIfNoneMatch.Equals("*") Then
            objResponse.StatusCode = 412  ' logically invalid request
            bReturn = False

        Else
            sEntityIDs = sRequestHeaderIfNoneMatch.Replace("bytes=", "").Split(",".ToCharArray)
            ' EntIDs were sent - Look for a match to the current one
            For iLoop As Int32 = sEntityIDs.GetLowerBound(0) To sEntityIDs.GetUpperBound(0)
                If sEntityIDs(iLoop).Trim.Equals(objFile.EntityTag) Then
                    sReturn = sEntityIDs(iLoop)
                    bReturn = False
                End If
            Next iLoop

            If Not bReturn Then
                ' One of the requested entities matches the current file's tag,
                objResponse.AppendHeader("ETag", sReturn)
                objResponse.StatusCode = 304  ' Not Modified

            End If
        End If
        Return bReturn
    End Function

    Private Function CheckIfModifiedSince(ByVal objRequest As HttpRequest, ByVal objFile As Download.FileInformation) As Boolean
        Dim sDate As String
        Dim dDate As Date
        Dim bReturn As Boolean

        ' Retrieve If-Modified-Since Header
        sDate = RetrieveHeader(objRequest, HTTP_HEADER_IF_MODIFIED_SINCE, String.Empty)
        If sDate.Equals(String.Empty) Then 
            bReturn = True ' If no date was sent - assume we need to re-download the wole file
        Else
            Try
                dDate = DateTime.Parse(sDate)
                ' True if the file was actually modified 
                bReturn = objFile.LastWriteTimeUTC >= DateTime.Parse(sDate)

            Catch ex As Exception
                bReturn = False

            End Try
        End If

        Return bReturn
    End Function

    Private Function CheckIfUnmodifiedSince(ByVal objRequest As HttpRequest, ByVal objFile As Download.FileInformation) As Boolean
        Dim sDate As String
        Dim dDate As Date
        Dim bReturn As Boolean

        ' Retrieve If-Unmodified-Since Header 
        sDate = RetrieveHeader(objRequest, HTTP_HEADER_IF_UNMODIFIED_SINCE, String.Empty)
        If sDate.Equals(String.Empty) Then 
            sDate = RetrieveHeader(objRequest, HTTP_HEADER_UNLESS_MODIFIED_SINCE, String.Empty)
        End If

        If sDate.Equals(String.Empty) Then
            bReturn = True
        Else
            Try
                dDate = DateTime.Parse(sDate)
                ' True if the file was not modified
                bReturn = objFile.LastWriteTimeUTC < DateTime.Parse(sDate)

            Catch ex As Exception
                bReturn = False

            End Try
        End If

        Return bReturn
    End Function

    Private Function ParseRequestHeaderRange(ByVal objRequest As HttpRequest, ByRef lBegin() As Long, ByRef lEnd() As Long, ByVal lMax As Long, ByRef bRangeRequest As Boolean) As Boolean
        Dim bValidRanges As Boolean
        Dim sSource As String
        Dim iLoop As Int32
        Dim sRanges() As String

        ' Retrieve Range Header from Request
        sSource = RetrieveHeader(objRequest, HTTP_HEADER_RANGE, String.Empty)
        If sSource.Equals(String.Empty) Then ' No Range was requested
            ReDim lBegin(0)
            ReDim lEnd(0)
            lBegin(0) = 0
            lEnd(0) = lMax - 1
            bValidRanges = True
            bRangeRequest = False

        Else

            bValidRanges = True
            bRangeRequest = True

            ' Remove "bytes=" string, and split the rest 
            sRanges = sSource.Replace("bytes=", "").Split(",".ToCharArray)

            ReDim lBegin(sRanges.GetUpperBound(0))
            ReDim lEnd(sRanges.GetUpperBound(0))

            ' Check each Range request
            For iLoop = sRanges.GetLowerBound(0) To sRanges.GetUpperBound(0)
                ' sRange(0) is the begin value - sRange(1) is the end value.
                Dim sRange() As String = sRanges(iLoop).Split("-".ToCharArray)
                If sRange(1).Equals(String.Empty) Then ' No end was specified
                    lEnd(iLoop) = lMax - 1
                Else
                    lEnd(iLoop) = Long.Parse(sRange(1))
                End If
                If sRange(0).Equals(String.Empty) Then
                    ' Calculate the beginning and end.
                    lBegin(iLoop) = lMax - 1 - lEnd(iLoop)
                    lEnd(iLoop) = lMax - 1
                Else
                    lBegin(iLoop) = Long.Parse(sRange(0))
                End If
                ' Begin and end must not exceed the total file size
                If (lBegin(iLoop) > (lMax - 1)) Or (lEnd(iLoop) > (lMax - 1)) Then
                    bValidRanges = False
                End If
                ' Begin and end cannot be < 0
                If (lBegin(iLoop) < 0) Or (lEnd(iLoop) < 0) Then
                    bValidRanges = False
                End If
                ' End must be larger or equal to begin value
                If lEnd(iLoop) < lBegin(iLoop) Then
                    bValidRanges = False
                End If

            Next iLoop
        End If

        Return bValidRanges
    End Function

    Private Function RetrieveHeader(ByVal objRequest As HttpRequest, ByVal sHeader As String, ByVal sDefault As String) As String
        Dim sReturn As String = objRequest.Headers.Item(sHeader)

        If (sReturn Is Nothing) OrElse sReturn.Equals(String.Empty) Then ' No Header Found
            Return sDefault

        Else
            Return sReturn.Replace("""", "") ' Clean quotes from header before return
        End If

    End Function

End Class

