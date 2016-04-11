using System;
using PX.Data;

namespace PX.SM.BoxStorageProvider
{
    [Serializable]
    public class BoxFolderCache : IBqlTable
    {
        public abstract class folderID : IBqlField { }
        [PXDBString(256, IsKey = true)]
        public virtual string FolderID { get; set; }

        public abstract class parentFolderID : IBqlField { }
        [PXDBString(256)]
        public virtual string ParentFolderID { get; set; }

        public abstract class screenID : IBqlField { }
        [PXDBString(8, IsUnicode = false)]
        public virtual string ScreenID { get; set; }
      
        public abstract class lastModifiedDateTime : IBqlField { }
        [PXDBDateAndTime(PreserveTime = true, UseSmallDateTime = false, UseTimeZone = false)]
        public virtual DateTime? LastModifiedDateTime { get; set; }

        public abstract class refNoteID : IBqlField { }
        [PXDBGuid(false)]
        public virtual Guid? RefNoteID { get; set; }
    }
}
