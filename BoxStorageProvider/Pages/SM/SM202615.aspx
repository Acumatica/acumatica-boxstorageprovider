<%@ Page Language="C#" AutoEventWireup="true" CodeFile="SM202615.aspx.cs" Inherits="Pages_SM_SM202615" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Box.com Authentication</title>
</head>
<body>
    <div>
        You may now close this window.
    </div>
    <script>
        var frMain = window.opener.px.searchFrame(window.opener.top, "main")
        var ds = frMain.px_all[frMain.dsID]; //dsID defined in SM202610
        ds.executeCallback("CompleteAuthentication");
        window.close();
    </script>
</body>
</html>
