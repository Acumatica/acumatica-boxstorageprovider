using System;
using System.Collections;
using System.Collections.Generic;
using PX.Data;
using PX.Api;
using System.Linq;

namespace PX.SM.BoxStorageProvider
{
    public partial class FolderSynchronisationOptionsFilter : IBqlTable
    {
        #region IsForceUpdatingFolder

        public abstract class isForceUpdatingFolderDescription : IBqlField { }
        [PXBool]
        [PXUIField(DisplayName = "Force Update Folder Descriptions")]
        public virtual bool IsForceUpdatingFolderDescription { get; set; }

        #endregion

        #region IsForceRescaningFolder

        public abstract class isForceRescaningFolder : IBqlField { }
        [PXBool]
        [PXUIField(DisplayName = "Force Rescan Folder")]
        public virtual bool IsForceRescaningFolder { get; set; }

        #endregion
    }

    public class FolderSynchronization : PXGraph<FolderSynchronization>
    {

        public PXCancel<FolderSynchronisationOptionsFilter> Cancel;
        public PXFilter<FolderSynchronisationOptionsFilter> Filter;
        public PXFilteredProcessing<Screen, FolderSynchronisationOptionsFilter> Screens;

        private static void Process(List<Screen> list, FolderSynchronisationOptionsFilter filter)
        {
            FileHandler graph = PXGraph.CreateInstance<FileHandler>();
            var userTokenHandler = PXGraph.CreateInstance<UserTokenHandler>();

            bool failed = false;

            if (userTokenHandler.GetCurrentUser() == null || userTokenHandler.GetCurrentUser().AccessToken == null || userTokenHandler.GetCurrentUser().RefreshToken == null)
            {
                throw new Exception(Messages.BoxUserNotFoundOrTokensExpired);
            }

            var rootFolder = graph.GetRootFolder();

            for (int i = 0; i < list.Count; i++)
            {
                try
                {
                    graph.Clear();
                    graph.SynchronizeScreen(list[i], rootFolder, filter.IsForceRescaningFolder);
                    if (filter.IsForceUpdatingFolderDescription)
                    {
                        graph.UpdateFolderDescriptions(list[i]);
                    }

                    PXProcessing<Screen>.SetInfo(i, ActionsMessages.RecordProcessed);
                }
                catch (Exception e)
                {
                    failed = true;
                    PXProcessing<Screen>.SetError(i, e is PXOuterException ? e.Message + "\r\n" + string.Join("\r\n", ((PXOuterException)e).InnerMessages) : e.Message);
                }
            }

            if (failed)
            {
                throw new PXException(Messages.SynchronizationError);
            }
        }

        public virtual IEnumerable screens()
        {
            bool found = false;
            foreach (Screen item in Screens.Cache.Inserted)
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

        protected virtual void FolderSynchronisationOptionsFilter_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
        {
            var row = e.Row as FolderSynchronisationOptionsFilter;
            Screens.SetProcessDelegate(s => Process(s, row));
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
