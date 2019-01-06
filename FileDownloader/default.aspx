<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="default.aspx.vb" Inherits="Download._default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>Resumable Downloads</title>
</head>
<body>
<center>
    <form id="form1" runat="server">
    <div>
        Resumable File Downloads<br />
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
    </div>
    </form>
</center>
</body>
</html>
