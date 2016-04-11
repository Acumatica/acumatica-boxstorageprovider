<%@ Page Language="C#" MasterPageFile="~/MasterPages/ListView.master" AutoEventWireup="true" 
    CodeFile="SM202630.aspx.cs" Inherits="Pages_SM_SM202630" Title="Box Folder Synchronization"
    ValidateRequest="false"  %>

<%@ MasterType VirtualPath="~/MasterPages/ListView.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="server">
    <px:PXDataSource ID="ds" Width="100%" runat="server" Visible="True" PrimaryView="Screens" TypeName="PX.SM.BoxStorageProvider.FolderSynchronization">
		<CallbackCommands>            
            <px:PXDSCallbackCommand CommitChanges="true" Name="Process" StartNewGroup="true"/>
            <px:PXDSCallbackCommand CommitChanges="true" Name="ProcessAll"/>
		</CallbackCommands>
	</px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phL" runat="Server">
    <px:PXGrid ID="grid" runat="server" Height="400px" Width="100%" Style="z-index: 100" AllowPaging="true" AdjustPageSize="Auto" AllowSearch="true" DataSourceID="ds" SkinID="Inquire" Caption="Screens">
        <Levels>
            <px:PXGridLevel DataMember="Screens">
                <Columns>
                    <px:PXGridColumn AllowCheckAll="True" AllowNull="False" DataField="Selected" TextAlign="Center" Type="CheckBox" Width="60px" />
                    <px:PXGridColumn DataField="ScreenID" Width="100px" />
					<px:PXGridColumn DataField="Name" Width="300px" />
                </Columns>
            </px:PXGridLevel>
        </Levels>
		<AutoSize Container="Window" Enabled="True" MinHeight="200" />
		<Layout ShowRowStatus="False" />
    </px:PXGrid>
</asp:Content>
