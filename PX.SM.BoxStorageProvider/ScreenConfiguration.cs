using System;
using System.Collections;
using System.Collections.Generic;
using PX.Data;
using PX.Api;

namespace PX.SM.BoxStorageProvider
{
    public class ScreenConfiguration : PXGraph<ScreenConfiguration, BoxScreenConfiguration>
    {
        public PXSelect<BoxScreenConfiguration> Screens;
        public PXSelect<BoxScreenGroupingFields, Where<BoxScreenGroupingFields.screenID, Equal<Current<BoxScreenConfiguration.screenID>>>> Fields;
        public PXSelectSiteMapTree<False, False, False, False, False> SiteMap;
    }
}
