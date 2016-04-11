using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using PX.Data;
using PX.SM.BoxStorageProvider;
using PX.Common;

public partial class Pages_SM_SM202670 : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        string folderID = Request.QueryString["FolderID"];
        string fileID = Request.QueryString["FileID"];

        if (!String.IsNullOrEmpty(folderID) && !String.IsNullOrEmpty(fileID))
        {
            fraContent.Attributes["src"] = "https://box.com/embed_widget/000000000000/files/0/f/" + folderID + "/1/f_" + fileID;
        }
        else if (!String.IsNullOrEmpty(folderID))
        {
            fraContent.Attributes["src"] = "https://box.com/embed_widget/000000000000/files/0/f/" + folderID;
        }
        else
        {
            Response.Write("Error. Missing FolderID/FileID Parameters.");
            return;
        }
    }
}