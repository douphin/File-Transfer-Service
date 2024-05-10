using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace RevisedFileTransferService
{
    // The FileTransferList.json immitates this class, excluding any functions or private data members
    // The public data members of this class will appear in the JSON in the order they appear here
    public class TransferSerializer
    {
        public bool UpdateService { get; set; }

        public string? LastUpdate { get; set; }

        // List of objects defined by TransferObject.cs
        public List<TransferObject>? transferObjects { get; set; }

        // List of objects defined by StatusObject.cs
        private List<StatusObject>? statusObjects { get; set; }

        // This functions returns a list of status objects, which hold the status of various parts of the copy process, see TransferObject.cs
        public List<StatusObject>? ReturnStatusList()
        {
            statusObjects = new List<StatusObject>();

            foreach (TransferObject TransObj in transferObjects)
            {
                try
                {
                    statusObjects.Add(TransObj.ReturnStatus());
                }
                catch(Exception ex)
                {
                    using (StreamWriter file = new StreamWriter(@"C:\USR\Logs\File Transfer Logs\UnhandledErrors.txt", true))
                    {
                        file.WriteLine();
                        file.WriteLine(DateTime.Now.ToString("t") + "_" + ex.ToString());
                        file.WriteLine();
                    }
                }
            }
            return statusObjects;
        }
        
        public void StopTimers()
        {
            foreach (TransferObject transObj in transferObjects)
            {
                if (transObj == null)
                {
                    continue;
                }

                transObj.StopTimer();
            }
        }

        public void StartTimers()
        {
            foreach(TransferObject transObj in transferObjects)
            {
                if(transObj == null) 
                {
                    continue;
                }

                //Task.Delay(TimeSpan.FromSeconds(1.5));

                transObj.InitializeObj();
                transObj.StartTimer();
            }
        }
    }
    
}
