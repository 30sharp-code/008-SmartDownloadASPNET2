<%@ Page Language="VB" %>
<%@ Import Namespace="System.Data" %>
<%@ Import Namespace="System.IO" %>
<script language="VB" runat="server">
    '------------------------------------------------------------------------+
    Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs)
        Const ChunkSize As Long = 10000
        Dim dlDir As String = "downloadfiles/"
        Dim strFileName As String = Request.QueryString("FileName")
        
        If (strFileName <> "") Then
            If (Regex.IsMatch(strFileName, "[/]") Or Regex.IsMatch(strFileName, "[\\]") Or _
                InStr(strFileName, "..") Or _
                Regex.IsMatch(strFileName, "^[^\\\./:\*\?\&quot;&lt;&gt;\|]{1}[^\\/:\*\?\&quot;&lt;&gt;\|]{0,254}$ ")) Then
                    
                ' NOTE: The only way we should get here is if the user is fiddling with the query string
                ' TODO: Add instrumentation to detect hacking attempts. 
            
            Else
                Dim buffer(ChunkSize) As Byte       ' Buffer to read input file.
                Dim length As Integer               ' Number of Bytes returned by read.
                Dim dataToRead As Long              ' Total bytes to read.
                Dim path As String = Server.MapPath(dlDir + Request.QueryString("FileName"))
                Dim file As System.IO.FileInfo = New System.IO.FileInfo(path)
                Dim filename As String = System.IO.Path.GetFileName(path)

                ' Open the file.
                Using iStream = New System.IO.FileStream(path, System.IO.FileMode.Open, _
                                                       IO.FileAccess.Read, IO.FileShare.Read)
                    dataToRead = iStream.Length
                    Response.ContentType = "application/octet-stream"
                    Response.AddHeader("Content-Disposition", "attachment; filename=" & filename)

                    ' Read and send the file 1 chuck at a time
                    While dataToRead > 0
                        If Response.IsClientConnected Then
                            length = iStream.Read(buffer, 0, ChunkSize)
                            Response.OutputStream.Write(buffer, 0, length)
                            Response.Flush()
                            dataToRead = dataToRead - length
                        Else
                            'prevent infinite loop if user disconnects
                            dataToRead = -1
                        End If
                    End While
                End Using
            End If

        End If
        BindFileDataToGrid("Name")
    End Sub

    '------------------------------------------------------------------------+
	Sub SortFileList(sender as Object, e as DataGridSortCommandEventArgs)
		BindFileDataToGrid(e.SortExpression)
	End Sub

    '------------------------------------------------------------------------+
	Sub BindFileDataToGrid(strSortField As String)
        Dim strPath As String = "downloadfiles/"

		Dim myDirInfo    As DirectoryInfo
		Dim arrFileInfo  As Array
		Dim myFileInfo   As FileInfo

		Dim filesTable   As New DataTable
		Dim myDataRow    As DataRow
		Dim myDataView   As DataView

        ' Add the clumns to the gris
		filesTable.Columns.Add("Name", Type.GetType("System.String"))
		filesTable.Columns.Add("Length", Type.GetType("System.Int32"))
		filesTable.Columns.Add("LastWriteTime", Type.GetType("System.DateTime"))
		filesTable.Columns.Add("Extension", Type.GetType("System.String"))

        ' Get Directory & File Info
		myDirInfo = New DirectoryInfo(Server.MapPath(strPath))
        arrFileInfo = myDirInfo.GetFiles()

        ' Iterate the FileInfo objects and extract te data
		For Each myFileInfo In arrFileInfo
			myDataRow = filesTable.NewRow()
			myDataRow("Name")          = myFileInfo.Name
			myDataRow("Length")        = myFileInfo.Length
			myDataRow("LastWriteTime") = myFileInfo.LastWriteTime
			myDataRow("Extension")     = myFileInfo.Extension
			
			filesTable.Rows.Add(myDataRow)
		Next myFileInfo

        ' Create a new DataView.
		myDataView = filesTable.DefaultView
		myDataView.Sort = strSortField

		' Set DataGrid's data source and data bind.
		dgFileList.DataSource = myDataView
		dgFileList.DataBind()
	End Sub

</script>

<html>
<head>
   <title>Build Dynamic File Links</title>
</head>
<body>

<form runat="server">
<asp:DataGrid id="dgFileList" runat="server"
	BorderColor           = "blue"
	CellSpacing           = 0
	CellPadding           = 4
    HeaderStyle-BackColor = "#0051E5"
    HeaderStyle-ForeColor = "#FFFFFF"
    HeaderStyle-Font-Bold = "True"
    ItemStyle-BackColor   = "silver"
    AutoGenerateColumns   = "False"
	AllowSorting          = "True"
	OnSortCommand         = "SortFileList">

	<Columns>
		<asp:HyperLinkColumn DataNavigateUrlField="Name" DataNavigateUrlFormatString="ChunkedFileFetch.aspx?FileName={0}" DataTextField="Name" HeaderText="Name:" SortExpression="Name" />
		<asp:BoundColumn DataField="Length" HeaderText="File Size in bytes:" ItemStyle-HorizontalAlign="Right" SortExpression="Length" />
		<asp:BoundColumn DataField="LastWriteTime" HeaderText="Date Created:" SortExpression="LastWriteTime" />
		<asp:BoundColumn DataField="Extension" HeaderText="Type:" SortExpression="Extension" />
	</Columns>
</asp:DataGrid>
</form>
<hr />
</body>
</html>
