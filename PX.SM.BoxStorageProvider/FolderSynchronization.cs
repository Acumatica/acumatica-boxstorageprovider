using System;
using System.Collections;
using System.Collections.Generic;
using PX.Data;
using PX.Api;

namespace PX.SM.BoxStorageProvider
{
    public class FolderSynchronization : PXGraph<FolderSynchronization>
    {
        public PXCancel<Screen> Cancel;
        public PXProcessing<Screen> Screens;
        
        public FolderSynchronization()
        {
            Screens.SetProcessDelegate(s => Process(s));
        }

        private static void Process(List<Screen> list)
        {
            FileHandler graph = PXGraph.CreateInstance<FileHandler>();

            bool failed = false;
            
            if (graph.User.Current == null || !PXAccess.IsRoleEnabled(PXAccess.GetAdministratorRole()))
            {
                throw new Exception(Messages.AdminAccountNotSetup);
            }
            
            var rootFolder = graph.GetRootFolder();
            
            for (int i = 0; i < list.Count; i++)
            {
                try
                {
                    graph.SynchronizeScreen(list[i], rootFolder);
                    PXProcessing<Screen>.SetInfo(i, ActionsMessages.RecordProcessed);
                }
                catch (Exception e)
                {
                    failed = true;
                    PXProcessing<Screen>.SetError(i, e is PXOuterException ? e.Message + "\r\n" + String.Join("\r\n", ((PXOuterException)e).InnerMessages) : e.Message);
                }
            }

            if(failed)
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
    }
}
