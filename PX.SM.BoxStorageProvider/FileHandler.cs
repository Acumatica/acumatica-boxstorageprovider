using PX.Common;
using PX.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Compilation;

namespace PX.SM.BoxStorageProvider
{
    public class FileHandler : PXGraph<FileHandler>
    {
        public const string MiscellaneousFolderScreenId = "00000000";
        public const string ActivityMaintScreenId = "CR306010";

        public PXSelect<BoxFolderCache, Where<BoxFolderCache.screenID, Equal<Required<BoxFolderCache.screenID>>>> FoldersByScreen;

        public PXSelect<BoxFolderCache, Where<BoxFolderCache.folderID, Equal<Required<BoxFolderCache.folderID>>>> FoldersByFolderID;

        public PXSelect<BoxFolderCache, Where<BoxFolderCache.parentFolderID, Equal<Required<BoxFolderCache.parentFolderID>>>> FoldersByParentFolderID;

        public PXSelect<BoxFolderCache, Where<BoxFolderCache.refNoteID, Equal<Required<BoxFolderCache.refNoteID>>>> FoldersByNote;

        public PXSelect<BoxFileCache, Where<BoxFileCache.blobHandler, Equal<Required<BoxFileCache.blobHandler>>>> FilesByBlobHandler;

        public PXSelect<BoxScreenGroupingFields,
            Where<BoxScreenGroupingFields.screenID, Equal<Required<BoxScreenGroupingFields.screenID>>>> FieldsGroupingByScreenID;

        // Views needed to synchronize and manage file list
        public PXSelectJoin<BoxFileCache,
            InnerJoin<UploadFileRevisionNoData, On<UploadFileRevisionNoData.blobHandler, Equal<BoxFileCache.blobHandler>>,
            InnerJoin<UploadFile, On<UploadFile.fileID, Equal<UploadFileRevisionNoData.fileID>,
                And<UploadFile.lastRevisionID, Equal<UploadFileRevisionNoData.fileRevisionID>>>,
            InnerJoin<NoteDoc, On<NoteDoc.fileID, Equal<UploadFile.fileID>>>>>, Where<NoteDoc.noteID, Equal<Required<NoteDoc.noteID>>>> FilesByNoteID;

        public PXSelectJoin<BoxFileCache,
            InnerJoin<UploadFileRevisionNoData, On<UploadFileRevisionNoData.blobHandler, Equal<BoxFileCache.blobHandler>>,
            LeftJoin<NoteDoc, On<NoteDoc.fileID, Equal<UploadFileRevisionNoData.fileID>>>>, Where<NoteDoc.noteID, IsNull, And<BoxFileCache.blobHandler, Equal<Required<BoxFileCache.blobHandler>>>>> OrphanFiles;

        public PXSelect<BoxFolderSublevelCache,
            Where<BoxFolderSublevelCache.screenID, Equal<Required<BoxFolderSublevelCache.screenID>>,
                And<BoxFolderSublevelCache.grouping, Equal<Required<BoxFolderSublevelCache.grouping>>>>> SubLevelByScreenAndGrouping;

        public PXSelect<UploadFile> UploadFiles;
        public PXSelect<UploadFileRevision> UploadFileRevisions;
        public PXSelect<NoteDoc> NoteDocs;


        public string GetOrCreateBoxFolderForNoteID(Guid refNoteID)
        {
            var tokenHandler = PXGraph.CreateInstance<UserTokenHandler>();
            var bfc = (BoxFolderCache)FoldersByNote.Select(refNoteID);
            string folderID = bfc?.FolderID;
            if (bfc == null)
            {
                // Folder doesn't exist in cache; retrieve it from Box or create it if it doesn't exist.
                folderID = CreateBoxFolder(refNoteID, tokenHandler).ID;
                Actions.PressSave();
            }

            return folderID;
        }

        public byte[] DownloadFileFromBox(Guid blobHandler)
        {
            var tokenHandler = PXGraph.CreateInstance<UserTokenHandler>();
            BoxFileCache bfc = GetFileInfoFromCache(blobHandler);
            try
            {
                return BoxUtils.DownloadFile(tokenHandler, bfc.FileID).Result;
            }
            catch (AggregateException ae)
            {
                ScreenUtils.HandleAggregateException(ae, HttpStatusCode.NotFound, (exception) =>
                {
                    throw new PXException(Messages.BoxFileNotFound, exception);
                });

                return new byte[0];
            }
        }

        public void DeleteFileFromBox(Guid blobHandler)
        {
            var tokenHandler = PXGraph.CreateInstance<UserTokenHandler>();
            BoxFileCache bfc = GetFileInfoFromCache(blobHandler);

            try
            {
                BoxUtils.DeleteFile(tokenHandler, bfc.FileID).Wait();
            }
            catch (AggregateException ae)
            {
                ScreenUtils.HandleAggregateException(ae, HttpStatusCode.NotFound, (exception) =>
                {
                    throw new PXException(Messages.FileNotFoundInBox, bfc.FileID, exception);
                });
            }
        }

        public Guid SaveFileToBoxAndUpdateFileCache(byte[] data, PXBlobStorageContext saveContext)
        {
            BoxUtils.FileFolderInfo boxFile = null;
            Guid blobHandlerGuid = Guid.NewGuid();

            var tokenHandler = PXGraph.CreateInstance<UserTokenHandler>();

            if (saveContext == null || saveContext.FileInfo == null || !saveContext.NoteID.HasValue)
            {
                var fileName = string.Empty;
                if (saveContext?.FileInfo?.Name == null)
                {
                    fileName = blobHandlerGuid.ToString();
                }
                else
                {
                    fileName = BoxUtils.CleanFileOrFolderName(saveContext.FileInfo.Name);
                    fileName = $"{Path.GetFileNameWithoutExtension(fileName)} ({blobHandlerGuid.ToString()}){Path.GetExtension(fileName)}";
                }

                //We don't know on which screen this file belongs. We'll have to save it in miscellaneous files folder.
                BoxUtils.FileFolderInfo boxFolder = GetMiscellaneousFolder();
                boxFile = BoxUtils.UploadFile(tokenHandler, boxFolder.ID, fileName, data).Result;
            }
            else
            {
                CheckForMissingNoteRecord(saveContext);
                var fileName = BoxUtils.CleanFileOrFolderName(Path.GetFileName(saveContext.FileInfo.Name));
                string boxFolderID = GetOrCreateBoxFolderForNoteID(saveContext.NoteID.Value);
                try
                {
                    boxFile = BoxUtils.UploadFile(tokenHandler, boxFolderID, fileName, data).Result;
                }
                catch (AggregateException ae)
                {
                    ScreenUtils.HandleAggregateException(ae, HttpStatusCode.NotFound, exception =>
                    {
                        using (new PXConnectionScope())
                        {
                            BoxFolderCache folderCacheToDelete = FoldersByFolderID.Cache.CreateInstance() as BoxFolderCache;
                            folderCacheToDelete.FolderID = boxFolderID;
                            var deletedFolderInfo = FoldersByFolderID.Delete(folderCacheToDelete);
                            Actions.PressSave();
                        }

                        ScreenUtils.TraceAndThrowException(Messages.BoxFolderNotFoundRunSynchAgain, boxFolderID);
                    });
                }

                if (!string.IsNullOrEmpty(saveContext.FileInfo.Comment))
                {
                    BoxUtils.SetFileDescription(tokenHandler, boxFile.ID, saveContext.FileInfo.Comment).Wait();
                }
            }

            var bfc = (BoxFileCache)FilesByBlobHandler.Cache.CreateInstance();
            bfc.BlobHandler = blobHandlerGuid;
            bfc.FileID = boxFile.ID;
            bfc.ParentFolderID = boxFile.ParentFolderID;
            bfc = FilesByBlobHandler.Insert(bfc);
            Actions.PressSave();

            return blobHandlerGuid;
        }

        private void CheckForMissingNoteRecord(PXBlobStorageContext saveContext)
        {
            EntityHelper entityHelper = new EntityHelper(this);
            Note note = entityHelper.SelectNote(saveContext.NoteID);
            if (note == null)
            {
                PXNoteAttribute.InsertNoteRecord(saveContext.Graph.Views[saveContext.ViewName].Cache, saveContext.NoteID.Value);
                this.Caches[typeof(Note)].ClearQueryCache();
            }
        }

        public BoxUtils.FileFolderInfo GetBoxFileInfoForFileID(Guid fileID)
        {
            Guid blobHandler = GetBlobHandlerForFileID(fileID);
            BoxFileCache bfc = GetFileInfoFromCache(blobHandler);
            var tokenHandler = PXGraph.CreateInstance<UserTokenHandler>();

            BoxUtils.FileFolderInfo fileInfo = BoxUtils.GetFileInfo(tokenHandler, bfc.FileID).Result;
            if (fileInfo == null)
            {
                throw new PXException(Messages.FileNotFoundInBox, bfc.FileID);
            }

            return fileInfo;
        }

        public BoxFileCache GetFileInfoFromCache(Guid blobHandler)
        {
            var file = (BoxFileCache)FilesByBlobHandler.Select(blobHandler);
            if (file == null)
            {
                throw new PXException(Messages.FileNotFoundInBoxFileCache, blobHandler);
            }

            return file;
        }

        public BoxUtils.FileFolderInfo GetMiscellaneousFolder()
        {
            var bfc = (BoxFolderCache)FoldersByScreen.Select(MiscellaneousFolderScreenId);
            if (bfc == null)
            {
                throw new PXException(Messages.MiscFolderNotFoundRunSynchAgain);
            }
            else
            {
                try
                {
                    // Folder was found in BoxFolderCache, retrieve it by ID
                    var tokenHandler = PXGraph.CreateInstance<UserTokenHandler>();
                    BoxUtils.FileFolderInfo folderInfo = BoxUtils.GetFolderInfo(tokenHandler, bfc.FolderID).Result;
                    return folderInfo;
                }
                catch (AggregateException ae)
                {
                    ScreenUtils.HandleAggregateException(ae, HttpStatusCode.NotFound, (exception) =>
                    {
                        using (new PXConnectionScope())
                        {
                            // Delete entry from BoxFolderCache so that it gets created again.
                            FoldersByScreen.Delete(bfc);
                            Actions.PressSave();
                        }

                        throw new PXException(Messages.MiscFolderNotFoundRunSynchAgain, bfc.FolderID, exception);
                    });

                    return null;
                }
            }
        }

        public void SynchronizeScreen(Screen screen, BoxUtils.FileFolderInfo rootFolder, bool forceSync)
        {
            string folderName = string.Format("{0} ({1})", (object)BoxUtils.CleanFileOrFolderName(screen.Name), (object)screen.ScreenID);

            BoxFolderCache screenFolderInfo = FoldersByScreen.Select(screen.ScreenID);
            BoxUtils.FileFolderInfo folderInfo = null;
            var tokenHandler = PXGraph.CreateInstance<UserTokenHandler>();

            if (screenFolderInfo != null)
            {
                try
                {
                    folderInfo = BoxUtils.GetFolderInfo(tokenHandler, screenFolderInfo.FolderID).Result;
                }
                catch (AggregateException ae)
                {
                    ScreenUtils.HandleAggregateException(ae, HttpStatusCode.NotFound, (exception) =>
                    {
                        // Folder no longer exist on Box - it may have been deleted on purpose by the user. Remove it from cache so it is recreated on the next run.
                        screenFolderInfo = FoldersByScreen.Delete(screenFolderInfo);
                        Actions.PressSave();
                        throw new PXException(Messages.BoxFolderNotFoundRunSynchAgain, screenFolderInfo.ScreenID, exception);
                    });
                }
            }

            if (folderInfo == null)
            {
                // Folder wasn't found, try finding it by name in the root folder.
                folderInfo = BoxUtils.FindFolder(tokenHandler, rootFolder.ID, folderName).Result;
            }

            if (folderInfo == null)
            {
                // Folder doesn't exist at all - create it
                folderInfo = BoxUtils.CreateFolder(tokenHandler, folderName, rootFolder.ID).Result;
            }

            if (screenFolderInfo == null)
            {
                screenFolderInfo = (BoxFolderCache)FoldersByScreen.Cache.CreateInstance();
                screenFolderInfo.FolderID = folderInfo.ID;
                screenFolderInfo.ParentFolderID = folderInfo.ParentFolderID;
                screenFolderInfo.ScreenID = screen.ScreenID;
                screenFolderInfo.LastModifiedDateTime = null; //To force initial sync
                screenFolderInfo = FoldersByScreen.Insert(screenFolderInfo);
                Actions.PressSave();
            }

            // We don't synchronize the miscellaneous files folder, since we can't easily identify the corresponding NoteID from folder
            if (screen.ScreenID != FileHandler.MiscellaneousFolderScreenId && (forceSync || screenFolderInfo.LastModifiedDateTime != folderInfo.ModifiedAt))
            {
                SynchronizeFolderContentsWithScreen(screenFolderInfo.ScreenID, screenFolderInfo.FolderID, forceSync);
                screenFolderInfo.LastModifiedDateTime = folderInfo.ModifiedAt;
                FoldersByScreen.Update(screenFolderInfo);
                Actions.PressSave();
            }
        }

        public void UpdateFolderDescriptions(Screen screen)
        {
            var tokenHandler = PXGraph.CreateInstance<UserTokenHandler>();
            EntityHelper entityHelper = new EntityHelper(this);

            BoxFolderCache screenFolderInfo = FoldersByScreen.Select(screen.ScreenID);
            foreach (BoxFolderCache boxFolderCache in FoldersByParentFolderID.Select(screenFolderInfo.FolderID))
            {
                object row = entityHelper.GetEntityRow(boxFolderCache.RefNoteID, true);
                if (row != null)
                {
                    var description = GetFolderDescriptionForEntityRow(row);
                    try
                    {
                        BoxUtils.UpdateFolderDescription(tokenHandler, boxFolderCache.FolderID, description).Wait();
                    }
                    catch (AggregateException ae)
                    {
                        ScreenUtils.HandleAggregateException(ae, HttpStatusCode.NotFound, (exception) =>
                        {

                            // Folder no longer exist on Box - it may have been deleted on purpose by the user
                            screenFolderInfo = FoldersByScreen.Delete(boxFolderCache);

                        });
                    }
                }
            }

            Actions.PressSave();
        }

        public void RefreshRecordFileList(string screenID, string folderName, string folderID, Guid? refNoteID, bool isForcingSync)
        {
            var tokenHandler = PXGraph.CreateInstance<UserTokenHandler>();

            //Get list of files contained in the record folder. RecurseDepth=0 will retrieve all subfolders
            List<BoxUtils.FileFolderInfo> boxFileList = BoxUtils.GetFileList(tokenHandler, folderID, (int)BoxUtils.RecursiveDepth.Unlimited).Result;

            // Remove files from cache if they don't exist on Box server
            foreach (PXResult<BoxFileCache, UploadFileRevisionNoData, UploadFile, NoteDoc> result in FilesByNoteID.Select(refNoteID))
            {
                BoxUtils.FileFolderInfo boxFile = boxFileList.FirstOrDefault(f => f.ID == ((BoxFileCache)result).FileID);
                if (boxFile == null)
                {
                    //File was deleted
                    FilesByNoteID.Delete(result);
                    UploadFiles.Delete(result);
                    UploadFileRevisions.Delete(result);
                    NoteDocs.Delete(result);
                }
                else
                {
                    // File still exists, remove it from in-memory list 
                    // so we don't process it as a new file in the next loop
                    boxFileList.Remove(boxFile);
                }
            }

            // Remove any files/folders coming from activities stored beneath the current record, they've been processed above
            var filesFoundOnlyOnServer = boxFileList.Where(x => !x.Name.StartsWith(Messages.ActivitiesFolderName)).ToList();

            //Check for underlying activities records
            BoxFolderCache currentFolder = FoldersByFolderID.Select(folderID);
            if (currentFolder != null && boxFileList.Any(x => x.Name.StartsWith(Messages.ActivitiesFolderName)))
            {
                // If nullOrEmpty, Folder may have been created manually
                if (string.IsNullOrEmpty(currentFolder.ActivityFolderID))
                {
                    //Find actual folder ID and save in BoxFolderCache's ActivityFolderID field
                    BoxUtils.FileFolderInfo activityFolderinfo = BoxUtils.FindFolder(tokenHandler, folderID, Messages.ActivitiesFolderName).Result;
                    if (activityFolderinfo != null)
                    {
                        currentFolder.ActivityFolderID = activityFolderinfo?.ID;
                        FoldersByFolderID.Update(currentFolder);
                    }
                }

                if (currentFolder.ActivityFolderID != null)
                {
                    SynchronizeFolderContentsWithScreen(ActivityMaintScreenId, currentFolder.ActivityFolderID, isForcingSync);
                }
            }

            //Remaining files aren't found in cache but are in Box server. 
            if (filesFoundOnlyOnServer.Any())
            {
                if (refNoteID == null)
                {
                    // User may have created some folder manually with a name not matching to any record, or record
                    // may have been deleted in Acumatica. We can safely ignore it, but let's write to trace.
                    PXTrace.WriteWarning(string.Format("No record found for folder {0} (screen {1}, ID {2})", folderName, screenID, folderID));
                    return;
                }

                UploadFileMaintenance ufm = PXGraph.CreateInstance<UploadFileMaintenance>();
                ufm.IgnoreFileRestrictions = true;

                ufm.RowInserting.AddHandler<UploadFileRevision>(delegate (PXCache sender, PXRowInsertingEventArgs e)
                {
                    ((UploadFileRevision)e.Row).BlobHandler = new Guid?(Guid.NewGuid());
                });
                //Add files to the caches
                foreach (BoxUtils.FileFolderInfo boxFile in filesFoundOnlyOnServer)
                {
                    ufm.Clear();
                    string fileName = string.Format("{0}\\{1}", folderName, boxFile.Name.Replace(Path.GetInvalidPathChars(), ' '));
                    FileInfo fileInfo = ufm.GetFileWithNoData(fileName);
                    Guid? blobHandlerGuid;
                    if (fileInfo == null)
                    {
                        fileInfo = new FileInfo(fileName, null, new byte[0]);
                        //The SaveFile call will trigger a Load() on the BoxBlobStorageProvider which can be skipped
                        PXContext.SetSlot<bool>("BoxDisableLoad", true);
                        try
                        {
                            if (!ufm.SaveFile(fileInfo))
                            {
                                throw new PXException(Messages.ErrorAddingFileSaveFileFailed, fileName);
                            }
                        }
                        finally
                        {
                            PXContext.SetSlot<bool>("BoxDisableLoad", false);
                        }

                        if (!fileInfo.UID.HasValue)
                        {
                            throw new PXException(Messages.ErrorAddingFileUIDNull, fileName);
                        }

                        UploadFileMaintenance.SetAccessSource(fileInfo.UID.Value, null, screenID);
                        NoteDoc noteDoc = (NoteDoc)NoteDocs.Cache.CreateInstance();
                        noteDoc.NoteID = refNoteID;
                        noteDoc.FileID = fileInfo.UID;
                        NoteDocs.Insert(noteDoc);

                        blobHandlerGuid = ufm.Revisions.Current.BlobHandler;
                    }
                    else
                    {
                        //File already exists in the database, retrieve BlobHandler
                        if (!fileInfo.UID.HasValue)
                        {
                            throw new PXException(Messages.GetFileWithNoDataReturnedUIDNull, fileName);
                        }
                        blobHandlerGuid = GetBlobHandlerForFileID(fileInfo.UID.Value);
                        var orphan = (UploadFileRevisionNoData)(PXResult<BoxFileCache, UploadFileRevisionNoData>)OrphanFiles.Select(blobHandlerGuid);
                        //Reattach orphan to its record with FileID-NoteID
                        if (orphan != null && orphan.FileID != null)
                        {
                            var noteDoc = NoteDocs.Insert();
                            NoteDocs.SetValueExt<NoteDoc.noteID>(noteDoc, refNoteID);
                            NoteDocs.SetValueExt<NoteDoc.fileID>(noteDoc, orphan.FileID);
                            continue;
                        }
                    }

                    var bfc = (BoxFileCache)FilesByBlobHandler.Cache.CreateInstance();
                    bfc.BlobHandler = blobHandlerGuid;
                    bfc.FileID = boxFile.ID;
                    bfc.ParentFolderID = boxFile.ParentFolderID;
                    bfc = FilesByBlobHandler.Insert(bfc);
                }
            }
        }

        public string GetFolderDescriptionForEntityRow(object row)
        {
            PXCache cache = Caches[row.GetType()];
            if (cache == null) return "";

            foreach (PropertyInfo prop in cache.GetItemType().GetProperties())
            {
                if (prop.IsDefined(typeof(PXSearchableAttribute), true))
                {
                    var attribute = prop.GetCustomAttribute(typeof(PXSearchableAttribute)) as PXSearchableAttribute;
                    var rec = attribute.BuildRecordInfo(cache, row);
                    return new string($"{rec.Title}{Environment.NewLine}{rec.Line1}".Take(255).ToArray());
                }
            }

            return string.Empty;
        }

        public BoxUtils.FileFolderInfo GetRootFolder()
        {
            string rootFolderName = GetRootFolderName();
            if (string.IsNullOrEmpty(rootFolderName)) throw new PXException(Messages.RootFolderNotSetup);

            var tokenHandler = PXGraph.CreateInstance<UserTokenHandler>();
            BoxUtils.FileFolderInfo rootFolder = BoxUtils.FindFolder(tokenHandler, "0", rootFolderName).Result;
            if (rootFolder == null)
            {
                throw new PXException(Messages.RootFolderNotFound, rootFolderName);
            }

            return rootFolder;
        }


        private void SynchronizeFolderContentsWithScreen(string screenID, string folderID, bool isForcingSync)
        {
            // Retrieve top-level folder list
            var tokenHandler = PXGraph.CreateInstance<UserTokenHandler>();
            List<BoxUtils.FileFolderInfo> boxFolderList = new List<BoxUtils.FileFolderInfo>();
            if (FieldsGroupingByScreenID.Select(screenID).Any())
            {
                boxFolderList = BoxUtils.GetFolderList(tokenHandler, folderID, (int)BoxUtils.RecursiveDepth.FirstSubLevel).Result;
                boxFolderList = boxFolderList.Where(x => x.Name.Contains("\\")).ToList();
            }
            else
            {
                boxFolderList = BoxUtils.GetFolderList(tokenHandler, folderID, (int)BoxUtils.RecursiveDepth.NoDepth).Result;
            }

            foreach (BoxUtils.FileFolderInfo boxFolderInfo in boxFolderList)
            {
                BoxFolderCache bfc = FoldersByFolderID.Select(boxFolderInfo.ID);
                if (bfc == null)
                {
                    // We've never seen this folder; sync it
                    Guid? refNoteID;

                    if (screenID == ActivityMaintScreenId)
                    {
                        //Activity folders use custom naming defined in GetFolderNameForActivityRow() - just look for GUID inside it.
                        refNoteID = ExtractGuidFromString(boxFolderInfo.Name);
                    }
                    else
                    {
                        refNoteID = FindMatchingNoteIDForFolder(screenID, boxFolderInfo.Name);
                    }

                    if (refNoteID == null)
                    {
                        // User may have created some folder manually with a name not matching to any record, or record
                        // may have been deleted in Acumatica. We can safely ignore it, but let's write to trace.
                        PXTrace.WriteWarning(string.Format("No record found for folder {0} (screen {1}, ID {2})", boxFolderInfo.Name, screenID, boxFolderInfo.ID));
                        continue;
                    }

                    bfc = (BoxFolderCache)FoldersByNote.Select(refNoteID);
                    if (bfc != null)
                    {
                        // A folder existed before for this record; clear the previous entry for this refNoteID
                        FoldersByNote.Delete(bfc);
                    }

                    // Store folder in cache for future reference
                    bfc = (BoxFolderCache)FoldersByFolderID.Cache.CreateInstance();
                    bfc.FolderID = boxFolderInfo.ID;
                    bfc.ParentFolderID = boxFolderInfo.ParentFolderID;
                    bfc.RefNoteID = refNoteID;
                    bfc.LastModifiedDateTime = null; //To force initial sync
                    bfc = FoldersByFolderID.Insert(bfc);
                }

                if (isForcingSync || bfc.LastModifiedDateTime != boxFolderInfo.ModifiedAt)
                {
                    RefreshRecordFileList(screenID, boxFolderInfo.Name, boxFolderInfo.ID, bfc.RefNoteID, isForcingSync);
                    bfc.LastModifiedDateTime = boxFolderInfo.ModifiedAt;
                    FoldersByFolderID.Update(bfc);
                    PXContext.SetSlot<bool>("BoxDisableLoad", true);
                    try
                    {
                        Actions.PressSave();
                    }
                    finally
                    {
                        PXContext.SetSlot<bool>("BoxDisableLoad", false);
                    }
                }
            }
        }

        private Guid? ExtractGuidFromString(string value)
        {
            var guidRegEx = new Regex(@"[0-9a-z]{8}\-[0-9a-z]{4}\-[0-9a-z]{4}\-[0-9a-z]{4}\-[0-9a-z]{12}", RegexOptions.IgnoreCase);
            var match = guidRegEx.Match(value);
            if (match.Success)
            {
                return Guid.Parse(match.Value);
            }
            else
            {
                return null;
            }
        }

        private Guid GetBlobHandlerForFileID(Guid fileID)
        {
            UploadFileRevisionNoData ufr = (UploadFileRevisionNoData)new PXSelect<UploadFileRevisionNoData,
                Where<UploadFileRevisionNoData.fileID, Equal<Required<UploadFileRevisionNoData.fileID>>>,
                OrderBy<Desc<UploadFileRevisionNoData.fileRevisionID>>>(this).Select(fileID);

            if (ufr == null) throw new PXException(Messages.UploadFileRevisionMissing, fileID);
            if (!ufr.BlobHandler.HasValue)
            {
                throw new PXException(Messages.UploadFileRevisionMissingBlobHandler, fileID);
            }

            return ufr.BlobHandler.Value;
        }

        private Guid? FindMatchingNoteIDForFolder(string screenID, string keyValues)
        {
            string graphType = PXPageIndexingService.GetGraphTypeByScreenID(screenID);
            if (string.IsNullOrEmpty(graphType))
            {
                throw new PXException(Messages.PrimaryGraphForScreenIDNotFound, screenID);
            }

            string primaryViewName = PXPageIndexingService.GetPrimaryView(graphType);
            if (string.IsNullOrEmpty(primaryViewName))
            {
                throw new PXException(Messages.PrimaryGraphForScreenIDNotFound, graphType);
            }

            var viewDescription = new Data.Description.PXViewDescription(primaryViewName);

            KeyValuePair<string, string>[] keyValuePairs;
            var graph = PXGraph.CreateInstance(PXBuildManager.GetType(graphType, true));
            try
            {
                keyValuePairs = GetKeyValuePairsFromKeyValues(graph, primaryViewName, keyValues);
            }
            catch (FolderNameKeyValuesMismatchException)
            {
                return null;
            }

            var view = graph.Views[primaryViewName];
            ScreenUtils.SelectCurrent(view, viewDescription, keyValuePairs);

            if (view.Cache.Current == null)
            {
                return null;
            }
            else
            {
                return PXNoteAttribute.GetNoteID(view.Cache, view.Cache.Current, EntityHelper.GetNoteField(view.Cache.Current.GetType()));
            }
        }

        private KeyValuePair<string, string>[] GetKeyValuePairsFromKeyValues(PXGraph graph, string viewName, string keyValues)
        {
            string[] keyNames = graph.GetKeyNames(viewName);
            string[] keyValuesArray;
            if (keyNames.Length == 1)
            {
                keyValuesArray = new string[1] { keyValues };
            }
            else
            {
                keyValuesArray = keyValues.Split(' ');
            }

            if (keyNames.Length != keyValuesArray.Length)
            {
                throw new FolderNameKeyValuesMismatchException(string.Format(Messages.ErrorExtractingKeyValuesFromFolderName, keyValuesArray.Length, keyNames.Length, viewName));
            }

            var pairs = new KeyValuePair<string, string>[keyNames.Length];

            for (int i = 0; i < keyNames.Length; i++)
            {
                pairs[i] = new KeyValuePair<string, string>(keyNames[i], keyValuesArray[i]);
            }

            return pairs;
        }

        private string GetRootFolderName()
        {
            BlobProviderSettings providerSettings = (BlobProviderSettings)PXSelect<BlobProviderSettings, Where<BlobProviderSettings.name, Equal<Required<BlobProviderSettings.name>>>>.Select(this, BoxBlobStorageProvider.RootFolderParam);
            if (providerSettings == null)
                return string.Empty;
            return providerSettings.Value;
        }

        private string GetSublevelName(Guid refNoteID)
        {
            EntityHelper entityHelper = new EntityHelper(this);
            object entityRow = entityHelper.GetEntityRow(new Guid?(refNoteID), true);
            Type primaryGraphType = entityHelper.GetPrimaryGraphType(entityRow, false);
            if (primaryGraphType == null || entityRow == null)
            {
                return null;
            }

            var screenID = PXSiteMap.Provider.FindSiteMapNode(primaryGraphType).ScreenID;
            PXCache entityCache = this.Caches[entityRow.GetType()];

            var folderNameBuilder = new StringBuilder();
            var fieldGroupings = FieldsGroupingByScreenID.Select(screenID).Select(x => (BoxScreenGroupingFields)x);
            if (!fieldGroupings.Any())
            {
                return null;
            }

            foreach (var field in fieldGroupings)
            {
                var value = entityCache.GetStateExt(entityRow, field.FieldName);
                folderNameBuilder.Append(ScreenUtils.UnwrapValue(value)).Append(' ');
            }

            var subLevelName = folderNameBuilder.ToString().Trim();
            return string.IsNullOrEmpty(subLevelName) ? Messages.UndefinedGrouping : subLevelName;
        }

        private BoxUtils.FileFolderInfo CreateBoxFolder(Guid refNoteID, UserTokenHandler tokenHandler)
        {
            EntityHelper entityHelper = new EntityHelper(this);
            object entityRow = entityHelper.GetEntityRow(new Guid?(refNoteID), true);
            Type primaryGraphType = entityHelper.GetPrimaryGraphType(entityRow, false);
            if (primaryGraphType == null)
            {
                ScreenUtils.TraceAndThrowException(Messages.PrimaryGraphForNoteIDNotFound, refNoteID);
            }
            if (entityRow == null)
            {
                ScreenUtils.TraceAndThrowException(Messages.EntityRowForNoteIDNotFound, refNoteID);
            }
            
            object activityRefNoteID = entityRow.GetType().GetProperty("RefNoteID")?.GetValue(entityRow);
            if (activityRefNoteID != null && Type.GetType("PX.Objects.CR.CRActivity, PX.Objects").IsAssignableFrom(entityRow.GetType()))
            {
                return GetOrCreateActivityFolder(refNoteID, tokenHandler, entityRow, activityRefNoteID);
            }
            else
            {
                // Create folder from screenID, for example "Customers (AR303000)"
                PXSiteMapNode siteMapNode = PXSiteMap.Provider.FindSiteMapNode(primaryGraphType);
                if (siteMapNode == null)
                {
                    throw new PXException(Messages.SiteMapNodeForGraphNotFound, primaryGraphType.FullName);
                }

                var bfcParent = (BoxFolderCache)FoldersByScreen.Select(siteMapNode.ScreenID);
                if (bfcParent == null)
                {
                    throw new PXException(Messages.ScreenMainFolderDoesNotExist, siteMapNode.ScreenID);
                }

                string parentFolderID = string.Empty;
                if (FieldsGroupingByScreenID.Select(siteMapNode.ScreenID).Any())
                {
                    parentFolderID = GetOrCreateSublevelFolder(tokenHandler, siteMapNode.ScreenID, bfcParent.FolderID, refNoteID);
                }
                else
                {
                    parentFolderID = bfcParent.FolderID;
                }

                return GetOrCreateFolderForEntity(tokenHandler, parentFolderID, entityRow, refNoteID);
            }
        }

        public string GetOrCreateSublevelFolder(UserTokenHandler tokenHandler, string screenID, string parentFolderID, Guid refNoteID)
        {
            BoxFolderSublevelCache sublevelFolder;
            var subLevelGrouping = GetSublevelName(refNoteID);
            if (string.IsNullOrEmpty(subLevelGrouping))
            {
                ScreenUtils.TraceAndThrowException(Messages.SubLevelConfigurationInvalid);
            }

            sublevelFolder = (BoxFolderSublevelCache)SubLevelByScreenAndGrouping.Select(screenID, subLevelGrouping);
            if (sublevelFolder == null)
            {
                var subLevelFolderInfo = GetOrCreateFolder(tokenHandler, parentFolderID, null, null, subLevelGrouping);

                sublevelFolder = (BoxFolderSublevelCache)SubLevelByScreenAndGrouping.Cache.CreateInstance();
                sublevelFolder.FolderID = subLevelFolderInfo.ID;
                sublevelFolder.Grouping = subLevelGrouping;
                sublevelFolder.ScreenID = screenID;
                SubLevelByScreenAndGrouping.Insert(sublevelFolder);
            }
            else
            {
                try
                {
                    // Folder was found in BoxFolderCache, retrieve it by ID
                    var sublevelFolderInfo = BoxUtils.GetFolderInfo(tokenHandler, sublevelFolder.FolderID).Result;
                }
                catch (AggregateException ae)
                {
                    ScreenUtils.HandleAggregateException(ae, HttpStatusCode.NotFound, (exception) =>
                    {
                        using (new PXConnectionScope())
                        {
                            // Delete entry from BoxFolderCache so that it gets created again.
                            SubLevelByScreenAndGrouping.Delete(sublevelFolder);
                            Actions.PressSave();
                        }

                        throw new PXException(Messages.BoxFolderNotFoundTryAgain, sublevelFolder.FolderID, exception);
                    });
                }
            }

            return sublevelFolder.FolderID;
        }

        private BoxUtils.FileFolderInfo GetOrCreateActivityFolder(Guid refNoteID, UserTokenHandler tokenHandler, object entityRow, object activityRefNoteID)
        {
            //Save an activity related file into the record's activity folder
            var activityRefNoteGuid = Guid.Parse(activityRefNoteID.ToString());

            //Get or create record folder
            string folderID = GetOrCreateBoxFolderForNoteID(activityRefNoteGuid);
            BoxFolderCache recordFolderCache = FoldersByFolderID.Select(folderID);

            //Get or create Activities folder 
            BoxUtils.FileFolderInfo activityFolderInfo = null;
            if (string.IsNullOrEmpty(recordFolderCache.ActivityFolderID))
            {
                // Create Activities folder and update cache for future reference.
                activityFolderInfo = GetOrCreateFolder(tokenHandler, recordFolderCache.FolderID, null, null, Messages.ActivitiesFolderName);
                recordFolderCache.ActivityFolderID = activityFolderInfo.ID;
                FoldersByFolderID.Update(recordFolderCache);
                Actions.PressSave();
            }
            else
            {
                try
                {
                    // Folder was found in BoxFolderCache, retrieve it by ID
                    activityFolderInfo = BoxUtils.GetFolderInfo(tokenHandler, recordFolderCache.ActivityFolderID).Result;
                }
                catch (AggregateException ae)
                {
                    ScreenUtils.HandleAggregateException(ae, HttpStatusCode.NotFound, (exception) =>
                    {
                        using (new PXConnectionScope())
                        {
                            // Delete entry from BoxFolderCache so that it gets created again.
                            recordFolderCache.ActivityFolderID = null;
                            FoldersByNote.Update(recordFolderCache);
                            Actions.PressSave();
                        }

                        throw new PXException(Messages.BoxFolderNotFoundTryAgain, recordFolderCache.FolderID, exception);
                    });
                }
            }

            //Get/Create activityRecord folder
            string folderName = GetFolderNameForActivityRow(entityRow);
            return GetOrCreateFolder(tokenHandler, activityFolderInfo.ID, entityRow, refNoteID, folderName);
        }

        private BoxUtils.FileFolderInfo GetOrCreateFolderForEntity(UserTokenHandler tokenHandler, string parentFolderID, object entityRow, Guid refNoteID)
        {
            // Try to find folder; if it doesn't exist, create it.
            string folderName = GetFolderNameForEntityRow(entityRow);
            return GetOrCreateFolder(tokenHandler, parentFolderID, entityRow, refNoteID, folderName);
        }

        private BoxUtils.FileFolderInfo GetOrCreateFolder(UserTokenHandler tokenHandler, string parentFolderID, object entityRow, Guid? refNoteID, string folderName)
        {
            try
            {
                BoxUtils.FileFolderInfo folderInfo = BoxUtils.FindFolder(tokenHandler, parentFolderID, folderName).Result;

                if (folderInfo == null)
                {
                    // Folder doesn't exist on Box, create it.
                    var description = entityRow != null ? GetFolderDescriptionForEntityRow(entityRow) : string.Empty;
                    folderInfo = BoxUtils.CreateFolder(tokenHandler, folderName, parentFolderID, description).Result;
                }

                if (!FoldersByFolderID.Select(folderInfo.ID).Any())
                {
                    // Store the folder info in our local cache for future reference
                    BoxFolderCache bfc = (BoxFolderCache)FoldersByFolderID.Cache.CreateInstance();
                    bfc.FolderID = folderInfo.ID;
                    bfc.ParentFolderID = folderInfo.ParentFolderID;
                    bfc.RefNoteID = refNoteID;
                    bfc.LastModifiedDateTime = null; // To force initial sync of Box file list with record file ilst
                    bfc = FoldersByFolderID.Insert(bfc);
                }

                return folderInfo;
            }
            catch (AggregateException ae)
            {
                ScreenUtils.HandleAggregateException(ae, HttpStatusCode.NotFound, (exception) =>
                {
                    using (new PXConnectionScope())
                    {
                        var bfc = FoldersByFolderID.Cache.CreateInstance() as BoxFolderCache;
                        bfc.FolderID = parentFolderID;
                        FoldersByFolderID.Delete(bfc);
                        Actions.PressSave();
                    }

                    throw (new PXException(string.Format(Messages.BoxFolderNotFoundTryAgain, parentFolderID), exception));
                });

                return null;
            }
        }

        private string GetFolderNameForEntityRow(object entityRow)
        {
            PXCache cache = Caches[entityRow.GetType()];
            string[] keyValues = new string[cache.BqlKeys.Count];
            for (int i = 0; i < cache.BqlKeys.Count; i++)
            {
                keyValues[i] = cache.GetValue(entityRow, cache.BqlKeys[i].Name).ToString();
            }

            return BoxUtils.CleanFileOrFolderName(string.Join(" ", keyValues));
        }

        private string GetFolderNameForActivityRow(object entityRow)
        {
            PXCache cache = Caches[entityRow.GetType()];
            string folderName = String.Format("{0:yyyy-MM-dd} - {1} (ID: {2})",
                cache.GetValue(entityRow, "StartDate"),
                cache.GetValue(entityRow, "Subject"),
                cache.GetValue(entityRow, "NoteID"));

            return BoxUtils.CleanFileOrFolderName(folderName);
        }
    }
}
