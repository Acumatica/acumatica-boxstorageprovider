using Box.V2;
using Box.V2.Auth;
using Box.V2.Config;
using Box.V2.Models;
using PX.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PX.SM.BoxStorageProvider
{
    public static class BoxUtils
    {
        public const string ClientID = "qo3lun142vpmtzyjlgqq1vkzycqb1tjk";
        private const string ClientSecret = "o9cdwhUUipOztBK4r9wMRXkcZQvcQsZo";
        private const string RedirectUri = "https://acumatica.github.io/acumatica-boxstorageprovider/";

        public enum RecursiveDepth
        {
            Unlimited = 0,
            NoDepth = 1,
            FirstSubLevel = 2
        }

        //System will do paging if folder contains more than 1000 files
        private const int FolderItemsCollectionLimit = 1000;

        private const int Expiration = 3600;

        private static BoxClient GetNewBoxClient(UserTokenHandler tokenHandler)
        {
            var currentUser = tokenHandler.GetCurrentUser();
            if (currentUser == null || currentUser.AccessToken == null || currentUser.RefreshToken == null)
            {
                throw new PXException(Messages.BoxUserNotFoundOrTokensExpired);
            }

            var config = new BoxConfig(ClientID, ClientSecret, new Uri(RedirectUri));
            OAuthSession session = new OAuthSession(tokenHandler.GetCurrentUser().AccessToken, tokenHandler.GetCurrentUser().RefreshToken, Expiration, "bearer");

            var client = new BoxClient(config, session);
            client.Auth.SessionAuthenticated += tokenHandler.SessionAuthenticated;
            client.Auth.SessionInvalidated += tokenHandler.SessionInvalidated;

            return client;
        }

        public static string CleanFileOrFolderName(string value)
        {
            string text = Regex.Replace(value, @"[\\\/\""\:\<\>\|\*\?]", " ");
            Regex regex = new Regex("[ ]{2,}");
            text = regex.Replace(text, " ");
            return text.Trim();
        }

        public static async Task CompleteAuthorization(UserTokenHandler tokenHandler, string authCode)
        {
            var config = new BoxConfig(ClientID, ClientSecret, new Uri(RedirectUri));
            var client = new BoxClient(config);
            client.Auth.SessionAuthenticated += tokenHandler.SessionAuthenticated;
            client.Auth.SessionInvalidated += tokenHandler.SessionInvalidated;

            await client.Auth.AuthenticateAsync(authCode).ConfigureAwait(false);
        }


        public static async Task<Box.V2.Models.BoxUser> GetUserInfo(UserTokenHandler tokenHandler)
        {
            var client = GetNewBoxClient(tokenHandler);
            return await client.UsersManager.GetCurrentUserInformationAsync().ConfigureAwait(false);
        }

        public static async Task<FileFolderInfo> CreateFolder(UserTokenHandler tokenHandler, string name, string parentFolderID)
        {
            var client = GetNewBoxClient(tokenHandler);
            return await BoxUtils.CreateFolder(client, tokenHandler, name, parentFolderID).ConfigureAwait(false);
        }

        private static async Task<FileFolderInfo> CreateFolder(BoxClient client, UserTokenHandler tokenHandler, string name, string parentFolderID)
        {
            var folderRequest = new BoxFolderRequest { Name = name, Parent = new BoxRequestEntity { Id = parentFolderID } };
            Box.V2.Models.BoxFolder folder = await client.FoldersManager.CreateAsync(folderRequest, new List<string> { BoxItem.FieldName, BoxItem.FieldModifiedAt }).ConfigureAwait(false);
            return new FileFolderInfo(folder.Id, folder.Name, parentFolderID, folder.ModifiedAt);
        }

        public static async Task<FileFolderInfo> CreateFolder(UserTokenHandler tokenHandler, string name, string parentFolderID, string description)
        {
            var client = GetNewBoxClient(tokenHandler);
            FileFolderInfo folderInfo = await CreateFolder(client, tokenHandler, name, parentFolderID).ConfigureAwait(false);
            UpdateFolderDescription(client, tokenHandler, folderInfo.ID, description).Wait();

            return new FileFolderInfo
            {
                ID = folderInfo.ID,
                Name = folderInfo.Name,
                ParentFolderID = folderInfo.ParentFolderID,
                ModifiedAt = folderInfo.ModifiedAt
            };
        }

        public static async Task<FileFolderInfo> UpdateFolderDescription(UserTokenHandler tokenHandler, string folderID, string description)
        {
            var client = GetNewBoxClient(tokenHandler);
            return await UpdateFolderDescription(client, tokenHandler, folderID, description).ConfigureAwait(false);
        }

        private static async Task<FileFolderInfo> UpdateFolderDescription(BoxClient client, UserTokenHandler tokenHandler, string folderID, string description)
        {
            if (!string.IsNullOrWhiteSpace(description))
            {
                var folderRequest = new BoxFolderRequest { Id = folderID, Description = description };
                BoxFolder folder = await client.FoldersManager.UpdateInformationAsync(folderRequest, new List<string> { BoxItem.FieldName, BoxItem.FieldParent, BoxItem.FieldModifiedAt }).ConfigureAwait(false);

                return new FileFolderInfo
                {
                    ID = folderID,
                    Name = folder.Name,
                    ParentFolderID = folder.Parent.Id,
                    ModifiedAt = folder.ModifiedAt
                };
            }

            return null;
        }

        public static async Task<FileFolderInfo> GetFileInfo(UserTokenHandler tokenHandler, string fileID)
        {
            var client = GetNewBoxClient(tokenHandler);
            Box.V2.Models.BoxFile file = await client.FilesManager.GetInformationAsync(fileID, new List<string> { BoxItem.FieldName, BoxItem.FieldModifiedAt, BoxItem.FieldParent }).ConfigureAwait(false);
            return new FileFolderInfo
            {
                ID = file.Id,
                Name = file.Name,
                ParentFolderID = file.Parent == null ? "0" : file.Parent.Id,
                ModifiedAt = file.ModifiedAt
            };
        }

        public static async Task<FileFolderInfo> GetFolderInfo(UserTokenHandler tokenHandler, string folderID)
        {
            var client = GetNewBoxClient(tokenHandler);
            Box.V2.Models.BoxFolder folder = await client.FoldersManager.GetInformationAsync(folderID, new List<string> { BoxItem.FieldName, BoxItem.FieldModifiedAt, BoxItem.FieldParent }).ConfigureAwait(false);
            return new FileFolderInfo
            {
                ID = folder.Id,
                Name = folder.Name,
                ParentFolderID = folder.Parent == null ? "0" : folder.Parent.Id,
                ModifiedAt = folder.ModifiedAt
            };
        }

        public static async Task<FileFolderInfo> UploadFile(UserTokenHandler tokenHandler, string parentFolderID, string fileName, byte[] data)
        {
            var client = GetNewBoxClient(tokenHandler);
            var fileRequest = new Box.V2.Models.BoxFileRequest { Name = fileName, Parent = new BoxRequestEntity { Id = parentFolderID } };
            Box.V2.Models.BoxFile file = await client.FilesManager.UploadAsync(fileRequest, new MemoryStream(data)).ConfigureAwait(false);
            return new FileFolderInfo
            {
                ID = file.Id,
                Name = file.Name,
                ParentFolderID = parentFolderID
            };
        }

        public static async Task<byte[]> DownloadFile(UserTokenHandler tokenHandler, string fileID)
        {
            var client = GetNewBoxClient(tokenHandler);
            var memoryStream = new MemoryStream();
            using (Stream stream = await client.FilesManager.DownloadStreamAsync(fileID).ConfigureAwait(false))
            {
                int bytesRead;
                var buffer = new byte[8192];
                do
                {
                    bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                    await memoryStream.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
                } while (bytesRead > 0);
            }
            return memoryStream.ToArray();
        }

        public static async Task DeleteFile(UserTokenHandler tokenHandler, string fileID)
        {
            var client = GetNewBoxClient(tokenHandler);
            await client.FilesManager.DeleteAsync(fileID).ConfigureAwait(false);
        }

        public static async Task<List<FileFolderInfo>> GetFileList(UserTokenHandler tokenHandler, string folderID, int recurseDepth)
        {
            var client = GetNewBoxClient(tokenHandler);
            return await GetFileListInternal(client, folderID, 0, recurseDepth, (int)RecursiveDepth.NoDepth, string.Empty).ConfigureAwait(false);
        }

        public static async Task<List<FileFolderInfo>> GetFolderList(UserTokenHandler tokenHandler, string folderID, int recurseDepth)
        {
            var client = GetNewBoxClient(tokenHandler);
            return await GetFolderListInternal(client, folderID, 0, recurseDepth, (int)RecursiveDepth.NoDepth, string.Empty).ConfigureAwait(false);
        }

        public static async Task<FileFolderInfo> CopyFolder(UserTokenHandler tokenHandler, string copyFolderID, string parentFolderID)
        {
            var client = GetNewBoxClient(tokenHandler);

            var folderRequest = new BoxFolderRequest { Id = copyFolderID, Parent = new BoxRequestEntity() { Id = parentFolderID, Type = BoxType.folder } };
            BoxFolder folder = await client.FoldersManager.CopyAsync(folderRequest, new List<string> { BoxItem.FieldName, BoxItem.FieldModifiedAt, BoxItem.FieldParent }).ConfigureAwait(false);
            
            return new FileFolderInfo(folder.Id, folder.Name, folder.Parent.Id, folder.ModifiedAt);
        }

        public static async Task<FileFolderInfo> FindFolder(UserTokenHandler tokenHandler, string parentFolderID, string name)
        {
            var client = GetNewBoxClient(tokenHandler);

            return await FindFolderInternal(client, parentFolderID, 0, name).ConfigureAwait(false);
        }

        public static async Task<FileFolderInfo> MoveFolder(UserTokenHandler tokenHandler, string movingFolderID, string newParentFolderID)
        {
            var client = GetNewBoxClient(tokenHandler);

            var folderRequest = new BoxFolderRequest { Id = movingFolderID, Parent = new BoxRequestEntity() { Id = newParentFolderID, Type = BoxType.folder } };
            BoxFolder folder = await client.FoldersManager.UpdateInformationAsync(folderRequest, new List<string> { BoxItem.FieldName, BoxItem.FieldModifiedAt, BoxItem.FieldParent }).ConfigureAwait(false);

            return new FileFolderInfo(folder.Id, folder.Name, folder.Parent.Id, folder.ModifiedAt);
        }

        private static async Task<FileFolderInfo> FindFolderInternal(BoxClient client, string parentFolderID, int offset, string name)
        {
            var list = new List<FileFolderInfo>();
            var folderItems = await client.FoldersManager.GetFolderItemsAsync(parentFolderID, FolderItemsCollectionLimit, offset, new List<string> { BoxItem.FieldName, BoxItem.FieldModifiedAt }).ConfigureAwait(false);

            foreach (var item in folderItems.Entries)
            {
                if (item.Type == "folder" && item.Name == name)
                {
                    return new FileFolderInfo
                    {
                        ID = item.Id,
                        Name = item.Name,
                        ParentFolderID = parentFolderID,
                        ModifiedAt = item.ModifiedAt
                    };
                }
            }

            if (folderItems.Offset + folderItems.Limit < folderItems.TotalCount)
            {
                return await FindFolderInternal(client, parentFolderID, offset + folderItems.Limit, name).ConfigureAwait(false);
            }

            return null;
        }

        private static async Task<List<FileFolderInfo>> GetFileListInternal(BoxClient client, string folderID, int offset, int recurseDepth, int currentDepth, string levelName)
        {
            var list = new List<FileFolderInfo>();
            var folderItems = await client.FoldersManager.GetFolderItemsAsync(folderID, FolderItemsCollectionLimit, offset).ConfigureAwait(false);

            foreach (var item in folderItems.Entries)
            {
                if (item.Type == "folder" && (currentDepth < recurseDepth || recurseDepth == 0))
                {
                    list.AddRange(await GetFileListInternal(client, item.Id, 0, recurseDepth, currentDepth + 1, string.IsNullOrEmpty(levelName) ? item.Name : string.Format("{0}\\{1}", levelName, item.Name)).ConfigureAwait(false));
                }
                else
                {
                    list.Add(new FileFolderInfo
                    {
                        ID = item.Id,
                        ParentFolderID = folderID,
                        Name = string.IsNullOrEmpty(levelName) ? item.Name : string.Format("{0}\\{1}", levelName, item.Name)
                    });
                }
            }

            if (folderItems.Offset + folderItems.Limit < folderItems.TotalCount)
            {
                list.AddRange(await GetFileListInternal(client, folderID, offset + folderItems.Limit, recurseDepth, currentDepth, levelName).ConfigureAwait(false));
            }

            return list;
        }

        private static async Task<List<FileFolderInfo>> GetFolderListInternal(BoxClient client, string folderID, int offset, int recurseDepth, int currentDepth, string levelName)
        {
            var list = new List<FileFolderInfo>();
            var folderItems = await client.FoldersManager.GetFolderItemsAsync(folderID, FolderItemsCollectionLimit, offset, new List<string> { BoxItem.FieldName, BoxItem.FieldModifiedAt }).ConfigureAwait(false);

            foreach (var item in folderItems.Entries)
            {
                if (item.Type == "folder")
                {
                    list.Add(new FileFolderInfo
                    {
                        ID = item.Id,
                        ParentFolderID = folderID,
                        Name = string.IsNullOrEmpty(levelName) ? item.Name : string.Format("{0}\\{1}", levelName, item.Name),
                        ModifiedAt = item.ModifiedAt
                    });

                    if (currentDepth < recurseDepth || recurseDepth == 0)
                    {
                        list.AddRange(await GetFolderListInternal(client, item.Id, 0, recurseDepth, currentDepth + 1, string.IsNullOrEmpty(levelName) ? item.Name : string.Format("{0}\\{1}", levelName, item.Name)).ConfigureAwait(false));
                    }
                }
            }

            if (folderItems.Offset + folderItems.Limit < folderItems.TotalCount)
            {
                list.AddRange(await GetFolderListInternal(client, folderID, offset + folderItems.Limit, recurseDepth, currentDepth, levelName).ConfigureAwait(false));
            }

            return list;
        }

        public static async Task SetFileDescription(UserTokenHandler tokenHandler, string fileID, string description)
        {
            var client = GetNewBoxClient(tokenHandler);
            var fileRequest = new Box.V2.Models.BoxFileRequest { Id = fileID, Description = description };
            await client.FilesManager.UpdateInformationAsync(fileRequest).ConfigureAwait(false);
        }

        public class FileFolderInfo
        {
            public string ID;
            public string Name;
            public string ParentFolderID;
            public DateTime? ModifiedAt;

            public FileFolderInfo() { }

            public FileFolderInfo(string id, string name, string parentFolderID, DateTime? modifiedAt)
            {
                this.ID = id;
                this.Name = name;
                this.ParentFolderID = parentFolderID;
                this.ModifiedAt = modifiedAt;
            }
        }
    }
}
