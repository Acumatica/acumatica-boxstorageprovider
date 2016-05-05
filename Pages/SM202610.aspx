<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormView.master" AutoEventWireup="true" 
    CodeFile="SM202610.aspx.cs" Inherits="Pages_SM_SM202610" Title="Box User Profile"
    ValidateRequest="false"  %>

<%@ MasterType VirtualPath="~/MasterPages/FormView.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
    <px:PXDataSource ID="ds" runat="server" Visible="True" PrimaryView="User" TypeName="PX.SM.BoxStorageProvider.UserProfile" PageLoadBehavior="GoFirstRecord" SuspendUnloading="False">
        <CallbackCommands>
            <px:PXDSCallbackCommand Name="Cancel" Visible="False" />
            <px:PXDSCallbackCommand CommitChanges="True" Name="Login" StartNewGroup="True" />
            <px:PXDSCallbackCommand CommitChanges="True" Name="CompleteAuthentication" Visible="False" />
        </CallbackCommands>
    </px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" runat="Server">
   <px:PXFormView ID="form" runat="server" DataSourceID="ds" Style="z-index: 100" Caption="User Settings" Width="100%" DataMember="User">
		<Template>
            <px:PXLayoutRule runat="server" StartColumn="True" ControlSize="M" LabelsWidth="M" />
            <%--The Acumatica UserID is needed for authentication dance, read from the code-behind--%>
			<px:PXTextEdit ID="edUserID" runat="server" DataField="UserId" style="position:absolute;visibility:hidden" SuppressLabel="True"/>
            <px:PXTextEdit ID="edUserStatus" runat="server" DataField="UserStatus"/>
			<px:PXTextEdit ID="edBoxUserID" runat="server" DataField="BoxUserId"/>
			<px:PXTextEdit ID="edBoxEmail" runat="server" DataField="BoxEmailAddress"/>
			<px:PXDateTimeEdit ID="edRefreshTokenDate" runat="server" DataField="RefreshTokenDate" Size="M"/>
		</Template>
        <AutoSize Container="Window" Enabled="True" MinHeight="200" />
    </px:PXFormView>
    <px:PXSmartPanel ID="pnlAuth" runat="server" style="height:600px;width:1020px" AutoCallBack-Enabled="true" AutoCallBack-Target="frmAuth" AutoCallBack-Command="Refresh"
        ClientEvents-AfterHide="PanelAuth_Hide" CaptionVisible="true" DesignView="Content" LoadOnDemand="true" Key="AuthInfo" AutoReload="true" Caption="Login to Box">
            <iframe id="fraAuth" runat="server" height="99%" width="99%" scrolling="no"></iframe>
    </px:PXSmartPanel>
        <script language="javascript" type="text/javascript">
            function PanelAuth_Hide() {
                var ds = px_all[dsID];
                if (ds != null) ds.executeCallback("CompleteAuthentication");
            }
        </script>
</asp:Content>
