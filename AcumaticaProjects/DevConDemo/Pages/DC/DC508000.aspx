<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormDetail.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="DC508000.aspx.cs" Inherits="Page_DC508000" Title="Untitled Page" %>
<%@ MasterType VirtualPath="~/MasterPages/FormDetail.master" %>

<asp:Content ID="cont1" ContentPlaceHolderID="phDS" Runat="Server">
    <px:PXDataSource ID="ds" runat="server" Visible="True" Width="100%" 
        TypeName="PX.Objects.DevConDemo.DCInventoryInquiry" PrimaryView="Filter" BorderStyle="NotSet" >
		<CallbackCommands>
		</CallbackCommands>
	</px:PXDataSource>
</asp:Content>

<asp:Content ID="cont2" ContentPlaceHolderID="phF" Runat="Server">
    <px:PXFormView ID="filter" runat="server" DataSourceID="ds" Width="100%" DataMember="Filter" LinkPage="" DefaultControlID="edReplenishmentSource" TabIndex="900">
        <Activity HighlightColor="" SelectedColor="" Width="" Height=""></Activity>
     	<Template>
            <px:PXLayoutRule ID="Col1" runat="server" LabelsWidth="S" ControlSize="M"/>
	        <px:PXDropDown ID="edReplenishmentSource" runat="server" DataField="ReplenishmentSource" />
	        <px:PXSegmentMask CommitChanges="True" ID="edItemClassCD" runat="server" DataField="ItemClassID" />
        </Template>
	</px:PXFormView>
</asp:Content>
<asp:Content ID="cont3" ContentPlaceHolderID="phG" Runat="Server">
    <px:PXGrid ID="grid" runat="server"  DataSourceID="ds" Width="100%" Height="150px" SkinID="Inquire" TabIndex="2700" SyncPosition="True">
		<Levels>
            <px:PXGridLevel DataMember="InventoryItems">
				<Columns>
					<px:PXGridColumn AllowCheckAll="True" AllowNull="False" DataField="Selected" TextAlign="Center" Type="CheckBox" Width="26px" />
					<px:PXGridColumn DataField="InventoryCD" Width="140px" LinkCommand="ViewItem" />
					<px:PXGridColumn DataField="Descr" Width="200px"/>
					<px:PXGridColumn DataField="ItemClassID" Width="140px" LinkCommand="ViewClass" />
					<px:PXGridColumn DataField="INItemClass__Descr" Width="140px"/>
					<px:PXGridColumn AllowNull="False" DataField="ItemStatus" RenderEditorText="True" Width="100px"/>
				</Columns>
				<RowTemplate>
					<px:PXSelector ID="edInventoryID" runat="server" DataField="InventoryCD" AllowEdit="True" />
					<px:PXTextEdit ID="edDescription" runat="server" DataField="Descr" />
					<px:PXSegmentMask ID="edItemClassID" runat="server" DataField="ItemClassID" />
					<px:PXTextEdit ID="edDescription2" runat="server" DataField="INItemClass__Descr" />
					<px:PXDropDown ID="edItemStatus" runat="server" DataField="ItemStatus" />
                    <px:PXDropDown ID="edUsrRepSource" runat="server" DataField="UsrRepSource" />
                    <px:PXCheckBox ID="chkNegQty" runat="server" DataField="NegQty" />
                    <px:PXDropDown ID="edItemType" runat="server" DataField="ItemType" CommitChanges="true" />
                    <px:PXDropDown ID="edValMethod" runat="server" AllowNull="False" DataField="ValMethod" CommitChanges="True" SelectedIndex="1" />
                    <px:PXSelector ID="edTaxCategoryID" runat="server" DataField="TaxCategoryID" AllowEdit="True" AutoRefresh="True" />
                    <px:PXDropDown ID="edTaxCalcMode" runat="server" DataField="TaxCalcMode" />
                    <px:PXSelector ID="edPostClassID" runat="server" DataField="PostClassID" AllowEdit="True" />
                    <px:PXSelector ID="edLotSerClassID" runat="server" DataField="LotSerClassID" AllowEdit="True" />
                    <px:PXSelector ID="edPriceClassID" runat="server" DataField="PriceClassID" AllowEdit="True" />
				</RowTemplate>
            </px:PXGridLevel>
		</Levels>
		<AutoSize Container="Window" Enabled="True" MinHeight="200"/>
	</px:PXGrid>
</asp:Content>

