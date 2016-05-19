using PX.Common;

namespace PX.SM.BoxStorageProvider
{
    [PXLocalizable]
    public static class Messages
    {
        public const string MiscellaneousFilesFolderName = "Miscellaneous Files";
        public const string ActivitiesFolderName = "Activities";
        public const string SynchronizationError = "One or more folders could not be synchronized.";
        public const string BrowseBoxFiles = "Browse Box Files...";
        public const string RootFolderNotSetup = "The Box.com root folder name is not setup in the file provider settings.";
        public const string RootFolderNotFound = "Failed to locate root folder {0} in this user's home folder.";
        public const string FileNotFoundInBoxFileCache = "No file corresponding to blob handler value {0} was found in the Box file cache.";
        public const string FileNotFoundInBox = "File {0} appears to have been deleted from Box.";
        public const string BoxFolderNotFound = "Failed to locate corresponding Box folder.";
        public const string BoxFileNotFound = "Failed to locate corresponding Box file.";
        public const string PrimaryGraphForNoteIDNotFound = "Failed to locate primary graph for Note ID {0}.";
        public const string PrimaryGraphForScreenIDNotFound = "Failed to locate primary graph for Screen ID {0}.";
        public const string PrimaryViewForGraphUndefined = "Primary view for graph {0} is undefined.";
        public const string FailedToRetrieveViewInfo = "Failed to retrieve view info for view {0} of graph {1}.";
        public const string FailedToGetCacheForView = "Failed to get cache for view {0} of graph {1}.";
        public const string EntityRowForNoteIDNotFound = "Failed to locate entity row for Note ID {0}";
        public const string SiteMapNodeForGraphNotFound = "Failed to locate site map entry for graph {0}.";
        public const string ScreenMainFolderDoesNotExist = "The main folder for screen {0} doesn't exist. Please run the synchronization process and try again.";
        public const string ErrorExtractingKeyValuesFromFolderName = "The number of key values received ({0}) doesn't match the number of expected keys ({1}) for view {2}.";
        public const string NoMatchingRecordFound = "No matching record found in screen {0} for keys {1}.";
        public const string ErrorAddingFileSaveFileFailed = "Error adding file {0}. SaveFile function returned false.";
        public const string ErrorAddingFileUIDNull = "Error adding file {0}. UID is null.";
        public const string GetFileWithNoDataReturnedUIDNull = "GetFileWithNoData for file {0} returned a record with a null UID.";
        public const string UploadFileRevisionMissing = "UploadFileRevision record for file {0} is missing.";
        public const string UploadFileRevisionMissingBlobHandler = "UploadFileRevision record for file {0} has no blob handler GUID.";
        public const string BoxUserNotFoundOrTokensExpired = "Failed to retrieve user. Please verify that you are authenticated with Box.";
        public const string BoxServiceError = "Box service returned the following error : ";
        public const string BoxFolderNotFoundTryAgain = "Folder ID {0} not found. Please try again.";
        public const string BoxFolderNotFoundRunSynchAgain = "Folder for screen {0} not found. Please try again.";
        public const string MiscFolderNotFoundRunSynchAgain = "Miscellaneous Files Folder not found. Please run folder synchronization and try again.";
        public const string BoxUserProfile = "Box User Profile";
        public const string SubLevelConfigurationInvalid = "The sublevel folder name can't be generated. Please verify this screen configuration.";
        public const string UndefinedGrouping = "Undefined Grouping";

        public const string Expired = "Expired";
        public const string Configured = "Configured";
        public const string NotConfigured = "Not configured";
    }
}
