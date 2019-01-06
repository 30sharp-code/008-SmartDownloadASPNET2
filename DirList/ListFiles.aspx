<%@ Page Language="VB" %>
<%@ Import Namespace="System.Data" %>
<%@ Import Namespace="System.IO" %>
<script language="VB" runat="server">
    '--------------------------------------------------------------------------+
	Sub Page_Load(sender as Object, e as EventArgs)
		BindFileDataToGrid("Name")
    End Sub
    
    '--------------------------------------------------------------------------+
	Sub SorFileList(sender as Object, e as DataGridSortCommandEventArgs)
		BindFileDataToGrid(e.SortExpression)
    End Sub
    
    '--------------------------------------------------------------------------+
	Sub BindFileDataToGrid(strSortField As String)
        Dim strPath As String = "./downloadfiles/"

        Dim myDirInfo As DirectoryInfo
        Dim arrFileInfo As Array
        Dim myFileInfo As FileInfo
        Dim filesTable As New DataTable
        Dim myDataRow As DataRow
        Dim myDataView As DataView

        filesTable.Columns.Add("Name", Type.GetType("System.String"))
		filesTable.Columns.Add("Length", Type.GetType("System.Int32"))
		filesTable.Columns.Add("LastWriteTime", Type.GetType("System.DateTime"))
		filesTable.Columns.Add("Extension", Type.GetType("System.String"))

		' Get Directory Info
		myDirInfo = New DirectoryInfo(Server.MapPath(strPath))
		' Get File Info
		arrFileInfo = myDirInfo.GetFiles()

        ' Iterate FileInfo objects to populate the DataTable. 
		For Each myFileInfo In arrFileInfo
			myDataRow = filesTable.NewRow()
			myDataRow("Name")          = myFileInfo.Name
			myDataRow("Length")        = myFileInfo.Length
			myDataRow("LastWriteTime") = myFileInfo.LastWriteTime
			myDataRow("Extension")     = myFileInfo.Extension
			
			filesTable.Rows.Add(myDataRow)
		Next myFileInfo

        ' Create a new DataView and sort it based on the sort field.
		myDataView = filesTable.DefaultView
		myDataView.Sort = strSortField

		' Set DataGrid's data source and data bind.
		dgFileList.DataSource = myDataView
		dgFileList.DataBind()
	End Sub

</script>

<html>
<head>
<title>Building File Links at Runtime.</title>
</head>
<body>
<form runat="server">
<br />
<asp:DataGrid id="dgFileList" runat="server"
	BorderColor           = "blue"
	CellSpacing           = 0
	CellPadding           = 6
    HeaderStyle-BackColor = "blue"
    HeaderStyle-ForeColor = "#FFFFFF"
    HeaderStyle-Font-Bold = "True"
    ItemStyle-BackColor   = "silver"
    AutoGenerateColumns   = "False"
	AllowSorting          = "True"
	OnSortCommand         = "SorFileList">

	<Columns>
		<asp:HyperLinkColumn DataNavigateUrlField="Name" DataNavigateUrlFormatString="downloadfiles/{0}" DataTextField="Name" HeaderText="Name:" SortExpression="Name" />
		<asp:BoundColumn DataField="Length" HeaderText="Size in bytes:" ItemStyle-HorizontalAlign="Right" SortExpression="Length" />
		<asp:BoundColumn DataField="LastWriteTime" HeaderText="Date Created:" SortExpression="LastWriteTime" />
		<asp:BoundColumn DataField="Extension" HeaderText="Type:" SortExpression="Extension" />
	</Columns>
</asp:DataGrid>
</form>
<hr />
</body>
</html>
