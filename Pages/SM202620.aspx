<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormDetail.master" AutoEventWireup="true"
    CodeFile="SM202620.aspx.cs" Inherits="Pages_SM_SM202620" Title="Box Screen Configuration"
    ValidateRequest="false" %>
<%@ MasterType VirtualPath="~/MasterPages/FormDetail.master" %>

<asp:Content ID="dsContent" ContentPlaceHolderID="phDS" runat="server">
    <px:PXDataSource ID="ds" runat="server" Visible="True" PrimaryView="Screens" TypeName="PX.SM.BoxStorageProvider.ScreenConfiguration" SuspendUnloading="False">
        <DataTrees>
			<px:PXTreeDataMember TreeView="SiteMap" TreeKeys="NodeID" />
		</DataTrees>
    </px:PXDataSource>
</asp:Content>
<asp:Content ID="formContent" ContentPlaceHolderID="phF" runat="Server">
    <px:PXFormView ID="screensForm" runat="server" DataSourceID="ds" DataMember="Screens" Width="100%">
        <Template>
            <px:PXTreeSelector ID="screenIDSelector" runat="server" DataField="ScreenID" Width="200px" PopulateOnDemand="True" ShowRootNode="False" 
                TreeDataSourceID="ds" TreeDataMember="SiteMap" MinDropWidth="415">
				<DataBindings>
					<px:PXTreeItemBinding DataMember="SiteMap" TextField="Title" ValueField="ScreenID" ToolTipField="TitleWithPath" ImageUrlField="Icon" />
				</DataBindings>
				<AutoCallBack Command="Cancel" Target="ds"></AutoCallBack>
			</px:PXTreeSelector>
        </Template>
    </px:PXFormView>
</asp:Content>
<asp:Content ID="gridContent" ContentPlaceHolderID="phG" runat="Server">
    <px:PXGrid ID="fieldsGrid" runat="server" DataSourceID="ds" Style="z-index: 100" AdjustPageSize="Auto" AllowSearch="True" SkinID="Details" MatrixMode="true" Caption="Grouping Fields" CaptionVisible="true" >
        <Levels>
            <px:PXGridLevel DataMember="Fields">
                <Mode InitNewRow="True" />
                <RowTemplate>
                    <px:PXDropDown ID="fieldDdlID" runat="server" DataField="FieldName" />
                </RowTemplate>
                <Columns>
                    <px:PXGridColumn AllowNull="False" DataField="FieldName" TextAlign="Center" Width="150px" />
                </Columns>
            </px:PXGridLevel>
        </Levels>
        <AutoSize Enabled="True" MinHeight="150" />
    </px:PXGrid>
</asp:Content>
