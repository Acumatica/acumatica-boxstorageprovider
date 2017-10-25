using System;
using System.Collections;
using System.Collections.Generic;
using PX.Data;
using PX.Api;
using System.Web.Compilation;
using System.Linq;
using System.Threading.Tasks;
using System.Net;

namespace PX.SM.BoxStorageProvider
{
    public class ScreenConfiguration : PXGraph<ScreenConfiguration, BoxScreenConfiguration>
    {
        public PXSelect<BoxScreenConfiguration> Screens;
        public PXSelect<BoxScreenGroupingFields, Where<BoxScreenGroupingFields.screenID, Equal<Current<BoxScreenConfiguration.screenID>>>> Fields;
        public PXSelect<BoxFolderCache, Where<BoxFolderCache.refNoteID, Equal<Required<BoxFolderCache.refNoteID>>>> FoldersByRefNoteID;
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

                //Construct ddl values and displayed values, specifying field name for duplicates
                var fieldsArray = PXFieldState.GetFields(graph, view.BqlSelect.GetTables(), true);
                var displayNames = fieldsArray.GroupBy(fa => fa.DisplayName).ToDictionary(k => k.Key, v => v.ToList());
                var labels = new List<string>();
                var values = new List<string>();
                foreach (var displayName in displayNames)
                {
                    if (displayName.Value.Count > 1)
                    {
                        foreach (var displayNameField in displayName.Value)
                        {
                            labels.Add($"{displayName.Key} ({displayNameField.Name})");
                            values.Add(displayNameField.Name);
                        }
                    }
                    else
                    {
                        labels.Add(displayName.Key);
                        values.Add(displayName.Value.FirstOrDefault()?.Name);
                    }

                }

                PXStringListAttribute.SetList<BoxScreenGroupingFields.fieldName>(Fields.Cache, null, values.ToArray(), labels.ToArray());
            }
        }

        public PXAction<BoxScreenConfiguration> MoveFolders;
        [PXButton(ConfirmationType = PXConfirmationType.Always, ConfirmationMessage = "Are you sure you want to rearrange your folders ?")]
        [PXUIField(DisplayName = "Move folders")]
        protected virtual void moveFolders()
        {
            var fileHandlerGraph = PXGraph.CreateInstance<FileHandler>();
            var tokenHandler = PXGraph.CreateInstance<UserTokenHandler>();

            PXLongOperation.StartOperation(this, () =>
            {
                EntityHelper entityHelper = new EntityHelper(this);
             
                var screenFolderCache = (BoxFolderCache)fileHandlerGraph.FoldersByScreen.Select(Screens.Current.ScreenID);

                var list = new List<BoxUtils.FileFolderInfo>();
                //For each subfolders of a screen found on box server
                try
                {
                    list = BoxUtils.GetFolderList(tokenHandler, screenFolderCache.FolderID, (int)BoxUtils.RecursiveDepth.FirstSubLevel).Result;

                }
                catch(AggregateException ae)
                {
                    ScreenUtils.HandleAggregateException(ae, HttpStatusCode.NotFound, (exception) => {
                        ScreenUtils.TraceAndThrowException(Messages.BoxFolderNotFoundRunSynchAgain, screenFolderCache.FolderID);
                    });
                }

                foreach (var folder in list)
                {
                    //If folder has a RefNoteID, it contains files and might need to be moved
                    BoxFolderCache bfc = (BoxFolderCache) fileHandlerGraph.FoldersByFolderID.Select(folder.ID);
                    if(bfc != null && bfc.RefNoteID.HasValue)
                    {
                        string presumedParentFolderID = screenFolderCache.FolderID;
                        if (fileHandlerGraph.FieldsGroupingByScreenID.Select(Screens.Current.ScreenID).Any())
                        {
                            presumedParentFolderID = fileHandlerGraph.GetOrCreateSublevelFolder(tokenHandler, Screens.Current.ScreenID, screenFolderCache.FolderID, bfc.RefNoteID.Value, false);
                        }

                        //if nested under the wrong folder
                        if (presumedParentFolderID != null && folder.ParentFolderID != presumedParentFolderID)
                        {
                            try
                            {
                                BoxUtils.MoveFolder(tokenHandler, folder.ID, presumedParentFolderID).Wait();
                                bfc.ParentFolderID = presumedParentFolderID;
                                fileHandlerGraph.FoldersByFolderID.Update(bfc);
                                fileHandlerGraph.Actions.PressSave();
                            }
                            catch (AggregateException ae)
                            {
                                ScreenUtils.HandleAggregateException(ae, HttpStatusCode.Conflict, (exception) => {
                                    //Skip this folder if a name conflict happens
                                });

                                continue;
                            }
                        }
                    }
                }
            });

        }
    }
}
