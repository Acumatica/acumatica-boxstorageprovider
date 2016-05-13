INSERT INTO BoxFileCache (CompanyID, BlobHandler, FileID, ParentFolderID) 
SELECT CompanyID, BlobHandler, BoxFileID, ParentFolderID FROM BXFileInfo

INSERT INTO BoxFolderCache (CompanyID, FolderID, LastModifiedDateTime, RefNoteID, ParentFolderID, ScreenID)  
(
	SELECT CompanyID, FolderID, LastModifiedDateTime, AssociatedNoteID, ParentFolderID, NULL AS ScreenID FROM BXFolderTimestamp
	union
	SELECT CompanyID, FolderID, LastModifiedDateTime, NULL, NULL, ScreenID FROM BXSiteMap
)

INSERT INTO BoxUserTokens (CompanyID, UserID, AccessToken, BoxUserID, BoxemailAddress, SiteAdministrator, RefreshToken, RefreshTokenDate) 
SELECT CompanyID, UserID, AuthToken, BoxUserID, BoxemailAddress, SiteAdministrator, RefreshToken, RefreshTokenDate FROM BXUserProfile