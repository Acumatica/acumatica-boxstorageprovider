using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using PX.Data;
using PX.SM.BoxStorageProvider;
using PX.Common;

public partial class Pages_SM_SM202615 : System.Web.UI.Page
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
        BoxUserTokens currentUser = PXCache<BoxUserTokens>.CreateCopy(graph.User.Select());
        if (currentUser.UserID != Guid.Parse(userID)) throw new PXException("UserID mismatch.");

        var session = BoxUtils.CompleteAuthorization(code).Result;
        currentUser.AccessToken = session.AccessToken;
        currentUser.RefreshToken = session.RefreshToken;
        currentUser.RefreshTokenDate = PXTimeZoneInfo.Now;
        graph.User.Update(currentUser);
        graph.Actions.PressSave();
    }
}