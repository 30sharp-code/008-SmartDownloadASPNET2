Imports System.IO
Imports System.Data

Partial Public Class _default
    Inherits System.Web.UI.Page

    Dim dgFilesList As DataGrid

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        BindFileDataToGrid("Name")
    End Sub

    Sub SorFileList(ByVal sender As Object, ByVal e As DataGridSortCommandEventArgs)
        BindFileDataToGrid(e.SortExpression)
    End Sub

    Sub BindFileDataToGrid(ByVal strSortField As String)
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
            myDataRow("Name") = myFileInfo.Name
            myDataRow("Length") = myFileInfo.Length
            myDataRow("LastWriteTime") = myFileInfo.LastWriteTime
            myDataRow("Extension") = myFileInfo.Extension

            filesTable.Rows.Add(myDataRow)
        Next myFileInfo

        ' Create a new DataView and sort it based on the sort field.
        myDataView = filesTable.DefaultView
        myDataView.Sort = strSortField

        ' Set DataGrid's data source and data bind.

        dgFileList.DataSource = myDataView
        dgFileList.DataBind()
    End Sub

End Class