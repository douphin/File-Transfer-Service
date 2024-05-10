using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System;
using RevisedFileTransferService;
using System.Text.Json;


// This is used to test different functionality of the file transfer program before compiling and deploying

// More work needs done here to adequately test everything

namespace UnitTestsFileTransferService
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestPathCreation()
        {
            string pre_Path = @"\\datasrv\test_path\'yyyy_MM_dd'\";
            string post_Path = @"\\datasrv\test_path\" + DateTime.Now.ToString("yyyy_MM_dd") + @"\";

            string madePath = TransferObject.MakePath(pre_Path, DateTime.Now);

            Assert.AreEqual(post_Path, madePath);

        }

        [TestMethod]
        public void TestReadWriteJSON()
        {
            const string transferlist_filename = @"TestTransferList.json";
            const string status_filename = @"TestTransferStatus.json";

            string JSONstring = File.ReadAllText(transferlist_filename);
            TransferSerializer? ServiceData = JsonSerializer.Deserialize<TransferSerializer>(JSONstring);

        }
    }
}