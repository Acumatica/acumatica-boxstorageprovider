using System;
using System.Collections;
using System.Collections.Generic;
using PX.Data;
using PX.Api;

namespace PX.SM.BoxStorageProvider
{
    public class ScreenConfiguration : PXGraph<ScreenConfiguration>
    {
        public PXSave<Screen> Save;
        public PXCancel<Screen> Cancel;
        public PXSelect<Screen> Screens;

        public ScreenConfiguration()
        {
        }

        public virtual IEnumerable screens()
        {
            bool found = false;
            foreach (Screen item in this.Screens.Cache.Inserted)
            {
                found = true;
                yield return item;
            }
            if (found)
                yield break;
            
            foreach (Screen screen in GetAllScreensWithAttachmentsSupport(this))
            {
                yield return Screens.Insert(screen);
            }

            Screens.Cache.IsDirty = false;
        }

        public static IEnumerable GetAllScreensWithAttachmentsSupport(PXGraph graph)
        {
            foreach (SiteMap item in PXSelectGroupBy<SiteMap, Where<SiteMap.screenID, IsNotNull>, Aggregate<GroupBy<SiteMap.screenID>>>.Select(graph))
            {
                if (ScreenPrimaryViewSupportsAttachments(graph, item.ScreenID))
                {
                    yield return new Screen
                    {
                        ScreenID = item.ScreenID,
                        Name = item.Title
                    };
                }
            }

            yield return new Screen
            {
                ScreenID = FileHandler.MiscellaneousFolderScreenId,
                Name = Messages.MiscellaneousFilesFolderName,
            };
        }

        private static bool ScreenPrimaryViewSupportsAttachments(PXGraph graph, string screenID)
        {
            string graphType = PXPageIndexingService.GetGraphTypeByScreenID(screenID);
            if (string.IsNullOrEmpty(graphType)) return false;

            string primaryViewName = PXPageIndexingService.GetPrimaryView(graphType);
            if (string.IsNullOrEmpty(primaryViewName)) return false;

            PXViewInfo view = GraphHelper.GetGraphView(graphType, primaryViewName);
            if (view == null) return false;

            PXCache cache = graph.Caches[view.Cache.CacheType];
            if (cache == null) return false;

            foreach (System.Reflection.PropertyInfo prop in cache.GetItemType().GetProperties())
            {
                if (prop.IsDefined(typeof(PXNoteAttribute), true))
                {
                    return true;
                }
            }

            return false;
        }
    }

    [Serializable]
    public partial class Screen : IBqlTable
    {
        public abstract class selected : IBqlField { }
        [PXBool]
        [PXDefault(false)]
        [PXUIField(DisplayName = "Selected", Visibility = PXUIVisibility.Visible)]
        public bool? Selected { get; set; }

        public abstract class screenID : IBqlField { }
        [PXString(8, IsKey = true, IsUnicode = false)]
        [PXUIField(DisplayName = "Screen ID", Enabled = false)]
        public virtual string ScreenID { get; set; }

        public abstract class name : IBqlField { }
        [PXString(255)]
        [PXUIField(DisplayName = "Name", Enabled = false)]
        public virtual string Name { get; set; }
    }
}
