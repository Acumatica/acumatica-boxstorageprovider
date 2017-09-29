using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using PX.Data;
using PX.SM.BoxStorageProvider;
using PX.Common;

public partial class Frames_BoxAuth : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!String.IsNullOrEmpty(Request.QueryString["userID"]) && !String.IsNullOrEmpty(Request.QueryString["code"]))
        {
            ProcessAuthorizationCode(Request.QueryString["userID"], Request.QueryString["code"]);
        }
        else
        {
            Response.Write("An unknown error occurent during authentication. Full QueryString: " + Request.QueryString);
            Response.End();
        }
    }

    private void ProcessAuthorizationCode(string userID, string code)
    {
        var graph = PXGraph.CreateInstance<UserProfile>();
        var tokenHandler = PXGraph.CreateInstance<UserTokenHandler>();
        BoxUtils.CompleteAuthorization(tokenHandler, code).Wait();
    }
}