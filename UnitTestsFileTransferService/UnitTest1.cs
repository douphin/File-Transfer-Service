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
        public void TestReadJSON()
        {
            const string transferlist_filename = @"..\..\..\TestTransferList.json";
            const string status_filename = @"..\..\..\TestTransferStatus.json";

            string transferListString = File.ReadAllText(transferlist_filename);
            TransferSerializer? ServiceData = JsonSerializer.Deserialize<TransferSerializer>(transferListString);

            Assert.IsNotNull(ServiceData);
            Assert.AreEqual("192.168.0.1", ServiceData.transferObjects[0].srcIPaddress);
            Assert.AreEqual("192.168.0.2", ServiceData.transferObjects[0].destIPaddress);

            string transferStatusString = File.ReadAllText(status_filename);
            StatusSerializer? StatusData = JsonSerializer.Deserialize<StatusSerializer>(transferStatusString);

            Assert.IsNotNull(StatusData);
            Assert.AreEqual("Untested", StatusData.statusObjects[0].FTPconnectionstatus);
            Assert.AreEqual("Good", StatusData.statusObjects[0].Copystatus);
        }

        [TestMethod]
        public void TestWriteJSON()
        {
            const string transferlist_filename = @"..\..\..\TestTransferList.json";

            string testTime = DateTime.Now.ToString("f");

            string transferListString = File.ReadAllText(transferlist_filename);
            TransferSerializer? ServiceData = JsonSerializer.Deserialize<TransferSerializer>(transferListString);

            ServiceData.LastUpdate = testTime;
            ServiceData.transferObjects[0].transferName = $"Test-Transfer {testTime}";

            transferListString = JsonSerializer.Serialize(ServiceData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(transferlist_filename, transferListString);

            // Check to see that the dates we wrote show up

            string checkTransferList = File.ReadAllText(transferlist_filename);
            TransferSerializer? CheckServiceData = JsonSerializer.Deserialize<TransferSerializer>(checkTransferList);

            Assert.IsNotNull(CheckServiceData);
            Assert.AreEqual(testTime, CheckServiceData.LastUpdate);
            Assert.AreEqual($"Test-Transfer {testTime}", CheckServiceData.transferObjects[0].transferName); 
        }
    }
}