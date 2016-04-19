using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Data;
using PX.Common;

[assembly: PX.SM.PXBlobStorageProvider(typeof(PX.SM.BoxStorageProvider.BoxBlobStorageProvider), "Box.com Storage")]
namespace PX.SM.BoxStorageProvider
{
    public class BoxBlobStorageProvider : IPXFileAttachmentProvider, IBlobStorageProvider
    {
        internal const string RootFolderParam = "RootFolder";
        internal string RootFolder;

        public void AddFile(Guid noteID)
        {
            // This function name is misleading -- this is invoked when the user clicks the "Browse Box Files" button. 
            FileHandler graph = PXGraph.CreateInstance<FileHandler>();
            BoxUtils.FileFolderInfo folder = graph.GetOrCreateBoxFolderForNoteID(noteID);
            throw new PXRedirectToUrlException("~/Pages/SM/SM202670.aspx?FolderID=" + folder.ID, "Box.com Folder");
        }

        public string GetEditUrl(Guid fileID)
        {
            FileHandler graph = PXGraph.CreateInstance<FileHandler>();
            BoxUtils.FileFolderInfo file = graph.GetBoxFileInfoForFileID(fileID);
            return string.Format("~/Pages/SM/SM202670.aspx?FolderID={0}&FileID={1}", file.ParentFolderID, (object)file.ID);
        }

        public string GetIdentity()
        {
            return "Box.com";
        }

        public string GetMenuTitle()
        {
            return Messages.BrowseBoxFiles;
        }

        public IEnumerable<BlobProviderSettings> GetSettings()
        {
            yield return new BlobProviderSettings()
            {
                Name = RootFolderParam,
                Value = this.RootFolder
            };
        }

        public void Init(IEnumerable<BlobProviderSettings> settings)
        {
            foreach (BlobProviderSettings providerSettings in settings)
            {
                if (providerSettings.Name == "RootFolder")
                    this.RootFolder = providerSettings.Value;
            }
        }

        public byte[] Load(Guid blobHandler)
        {
            //This slot is set by the FileHandler graph when synchronizing local files with the remote files
            //This is to avoid an unnecessary callback to Box.
            if (PXContext.GetSlot<bool>("BoxDisableLoad") == true)
            {
                return new byte[0];
            }
            else
            {
                FileHandler graph = PXGraph.CreateInstance<FileHandler>();
                return graph.DownloadFileFromBox(blobHandler);
            }
        }

        public void Remove(Guid blobHandler)
        {
            //This slot is set by the FileHandler graph when synchronizing local files with the remote files
            //This is to avoid an unnecessary callback to Box.
            if (!PXContext.GetSlot<bool>("BoxDisableLoad") == true)
            {
                FileHandler graph = PXGraph.CreateInstance<FileHandler>();
                graph.DeleteFileFromBox(blobHandler);
            }
        }

        public Guid Save(byte[] data, PXBlobStorageContext saveContext)
        {
            //TODO:
            //Store Activity files and wiki pictures under their parent record instead of Miscellaneous Files to maintain appropriate access rights (ex: Acumatica\Customers (AR303000)\ABARTENDE\Activities\1234\somefile.jpg
            //Do not include reference to PX.Objects.CR.EPActivity in this file, compare by string
            //if saveContext.PXGraph type == PX.Objects.CR.EPActivity
            //then save file under Acumatica\Screen\ScreenRecord\Activities\ActivityRecord\file.ext instead of miscellaneous
            FileHandler graph = PXGraph.CreateInstance<FileHandler>();
            Guid blobHandler = graph.SaveFileToBoxAndUpdateFileCache(data, saveContext);
            return blobHandler;
        }
    }
}
