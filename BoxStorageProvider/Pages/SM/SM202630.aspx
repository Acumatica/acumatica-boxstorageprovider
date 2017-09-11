<%@ Page Language="C#" MasterPageFile="~/MasterPages/ListView.master" AutoEventWireup="true"
    CodeFile="SM202630.aspx.cs" Inherits="Pages_SM_SM202630" Title="Box Folder Synchronization"
    ValidateRequest="false" %>

<%@ MasterType VirtualPath="~/MasterPages/ListView.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="server">
    <px:pxdatasource id="ds" width="100%" runat="server" visible="True" primaryview="Filter" typename="PX.SM.BoxStorageProvider.FolderSynchronization">
        <CallbackCommands>
            <px:PXDSCallbackCommand CommitChanges="true" Name="Process" StartNewGroup="true"></px:PXDSCallbackCommand>
            <px:PXDSCallbackCommand CommitChanges="true" Name="ProcessAll"></px:PXDSCallbackCommand>
        </CallbackCommands>
    </px:pxdatasource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phL" runat="Server">
    <px:pxformview runat="server" id="form" style="z-index: 100" width="100%" datamember="Filter">
        <Template>
            <px:PXLayoutRule runat="server" ID="ForceUpdateFolderDescriptionRow" StartRow="True" />       
	        <px:PXCheckBox runat="server" ID="ForceUpdateFolderDescriptionCheckbox" DataField="IsForceUpdatingFolderDescription" AlignLeft="True" />
            <px:PXLayoutRule runat="server" ID="ForceRescanOfFolderRow" StartRow="True" />        
	        <px:PXCheckBox runat="server" ID="ForceRescanOfFolderCheckbox" DataField="IsForceRescaningFolder" AlignLeft="True" />
        </Template>
    </px:pxformview>
    <px:pxgrid id="grid" runat="server" height="400px" width="100%" style="z-index: 100" allowpaging="true" adjustpagesize="Auto"
        allowsearch="true" datasourceid="ds" skinid="Inquire" caption="Screens">
        <Levels>
            <px:PXGridLevel DataMember="Screens">
                <Columns>
                    <px:PXGridColumn AllowCheckAll="True" AllowNull="False" DataField="Selected" TextAlign="Center" Type="CheckBox" Width="60px"></px:PXGridColumn>
                    <px:PXGridColumn DataField="ScreenID" Width="100px"></px:PXGridColumn>
                    <px:PXGridColumn DataField="Name" Width="300px"></px:PXGridColumn>
                </Columns>
            </px:PXGridLevel>
        </Levels>
        <AutoSize Container="Window" Enabled="True" MinHeight="200"></AutoSize>
        <Layout ShowRowStatus="False"></Layout>
    </px:pxgrid>
</asp:Content>
