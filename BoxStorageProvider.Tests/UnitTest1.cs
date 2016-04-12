using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BoxStorageProvider.Tests
{
    [TestClass]
    public class UnitTest1
    {
        const string AccessToken = "3srPJMhDl3e7XZmoBtb5nkFhCn7APaEY";
        const string RefreshToken = "aojVDCNmTAtdEYC3kaCCkyMfFELzdrGwTIhWFPq9APVSiQwNXYaF0vxuU4VPSsG0";

        //[TestMethod]
        //public void TestGetFileList()
        //{
        //    var files = PX.SM.BoxStorageProvider.BoxUtils.GetFileList(AccessToken, RefreshToken, "1088970822", 0).Result;
        //    //30 files; should test pagination
        //}

        //[TestMethod]
        //public void TestDownloadFile()
        //{
        //    var bytes = PX.SM.BoxStorageProvider.BoxUtils.DownloadFile(AccessToken, RefreshToken, "24491203713").Result;
        //    System.IO.File.WriteAllBytes("test.doc", bytes);
        //}

        //[TestMethod]
        //public void TestUploadFile()
        //{
        //    var file = PX.SM.BoxStorageProvider.BoxUtils.UploadFile(AccessToken, RefreshToken, "7327746693", "upload.doc", System.IO.File.ReadAllBytes("test.doc")).Result;
        //}
    }
}
