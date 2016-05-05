using System;
using System.Collections;
using System.Collections.Generic;
using PX.Data;
using PX.Api;
using System.Web.Compilation;
using System.Linq;


namespace PX.SM.BoxStorageProvider
{
    public class ScreenConfiguration : PXGraph<ScreenConfiguration, BoxScreenConfiguration>
    {
        public PXSelect<BoxScreenConfiguration> Screens;
        public PXSelect<BoxScreenGroupingFields, Where<BoxScreenGroupingFields.screenID, Equal<Current<BoxScreenConfiguration.screenID>>>> Fields;
        public PXSelectSiteMapTree<False, False, False, False, False> SiteMap;

        protected virtual void BoxScreenConfiguration_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
        {
            var screenConfig = e.Row as BoxScreenConfiguration;
            if (!string.IsNullOrEmpty(screenConfig.ScreenID))
            {
                string graphTypeName = PXPageIndexingService.GetGraphTypeByScreenID(screenConfig.ScreenID);
                if (string.IsNullOrEmpty(graphTypeName))
                {
                    throw new PXException(Messages.PrimaryGraphForScreenIDNotFound, screenConfig.ScreenID);
                }
                Type graphType = PXBuildManager.GetType(graphTypeName, true);
                var graph = PXGraph.CreateInstance(graphType);

                string primaryViewName = PXPageIndexingService.GetPrimaryView(graphTypeName);
                PXView view = graph.Views[primaryViewName];

                var fields = PXFieldState.GetFields(graph, view.BqlSelect.GetTables(), true);
                var labels = fields.Select(x => x.DisplayName).ToArray();
                var values = fields.Select(x => x.Name).ToArray();
                PXStringListAttribute.SetList<BoxScreenGroupingFields.fieldName>(Fields.Cache, null, values, labels);
            }
        }
    }
}
