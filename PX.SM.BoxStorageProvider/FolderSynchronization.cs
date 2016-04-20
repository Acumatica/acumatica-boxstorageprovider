using System;
using System.Collections;
using System.Collections.Generic;
using PX.Data;
using PX.Api;

namespace PX.SM.BoxStorageProvider
{
    public partial class FolderSynchronisationOptionsFilter : PX.Data.IBqlTable
    {
        #region IsForceUpdatingFolder

        public abstract class isForceUpdatingFolderDescription : PX.Data.IBqlField { }
        [PXBool]
        [PXUIField(DisplayName = "Force Update Folder Descriptions")]
        public virtual bool IsForceUpdatingFolderDescription { get; set; }

        #endregion

        #region IsForceRescaningFolder

        public abstract class isForceRescaningFolder : PX.Data.IBqlField { }
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
                    //TODO: New checkbox for force folder sync is needed
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
                    PXProcessing<Screen>.SetError(i, e is PXOuterException ? e.Message + "\r\n" + String.Join("\r\n", ((PXOuterException)e).InnerMessages) : e.Message);
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

            foreach (Screen screen in ScreenConfiguration.GetAllScreensWithAttachmentsSupport(this))
            {
                yield return Screens.Insert(screen);
            }

            Screens.Cache.IsDirty = false;
        }

        protected virtual void FolderSynchronisationOptionsFilter_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
        {
            var row = e.Row as FolderSynchronisationOptionsFilter;
            Screens.SetProcessDelegate(s => Process(s, row));
        }
    }
}
