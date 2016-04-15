using System;
using PX.Data;

namespace PX.SM.BoxStorageProvider
{
    [Serializable]
    public class BoxFileCache : IBqlTable
    {
        public abstract class blobHandler : IBqlField { }
        [PXDBGuid(false, IsKey = true)]
        [PXUIField(DisplayName = "Blob Handler")]
        public virtual Guid? BlobHandler { get; set; }

        public abstract class fileID : IBqlField { }
        [PXDBString(256)]
        [PXUIField(DisplayName = "Box File ID")]
        public virtual string FileID { get; set; }

        public abstract class parentFolderID : IBqlField { }
        [PXDBString(256)]
        [PXUIField(DisplayName = "Box Parent Folder ID")]
        public virtual string ParentFolderID { get; set; }
    }
}
