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
                Value = RootFolder
            };
        }

        public void Init(IEnumerable<BlobProviderSettings> settings)
        {
            foreach (BlobProviderSettings providerSettings in settings)
            {
                if (providerSettings.Name == "RootFolder")
                    RootFolder = providerSettings.Value;
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
            FileHandler graph = PXGraph.CreateInstance<FileHandler>();
            Guid blobHandler = graph.SaveFileToBoxAndUpdateFileCache(data, saveContext);
            return blobHandler;
        }
    }
}
