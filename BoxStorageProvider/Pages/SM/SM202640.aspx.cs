using System;
using PX.Web.UI;
using PX.Data;
using PX.SM.BoxStorageProvider;
using PX.Common;

public partial class Pages_SM_SM202640 : PXPage
{
    protected void Page_Load(object sender, EventArgs e)
    {
        var graph = PXGraph.CreateInstance<FileHandler>();
        try
        {
            BoxFolderCache relatedFolder = graph.FoldersByFolderID.Select(Request.QueryString["itemID"]);
            new EntityHelper(graph).NavigateToRow(relatedFolder.RefNoteID, PXRedirectHelper.WindowMode.Same);
        }
        catch (PXRedirectRequiredException ex)
        {
            string url = PXDataSource.getMainForm(ex.Graph.GetType());
            if (url != null)
            {
                ex.Graph.Unload();
                PXContext.Session.RedirectGraphType[PXUrl.ToAbsoluteUrl(url)] = ex.Graph.GetType();
                Response.Redirect(this.ResolveUrl(url));
            }
        }
    }
}