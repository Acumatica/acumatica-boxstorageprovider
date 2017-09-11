using System;
using System.Web;
using PX.Data;
using PX.SM.BoxStorageProvider;
using PX.Web.Customization;
using PX.Web.UI;
using System.Web.UI.HtmlControls;
using System.Text;

[Customization.CstDesignMode(Disabled = true)]
public partial class Pages_SM_SM202610 : PXPage
{
    protected void Page_Load(object sender, EventArgs e)
    {
        UserProfile graph = ds.DataGraph as UserProfile;
        if (graph != null && graph.User.Current != null)
        {
            string state = "acumaticaUrl=" + HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority) + ResolveUrl("~/") + "Pages/SM/SM202615.aspx" +
                "&userID=" + graph.User.Current.UserID.ToString();

            string authUrl = "https://www.box.com/api/oauth2/authorize?response_type=code" +
                "&client_id=" + BoxUtils.ClientID + 
                "&state=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(state));

            var edContent = fraAuth as HtmlControl;
            edContent.Attributes.Add("src", authUrl);
        }

        if (!this.Page.IsCallback)
        {
            this.Page.ClientScript.RegisterClientScriptBlock(GetType(), "dsID", "var dsID=\"" + this.ds.ClientID + "\";", true);
        }
    }
}