using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevisedFileTransferService
{
    // FileTransferStatus.json will imitate this class
    public class StatusSerializer
    {
        public string LastUpdated { get; set; }

        // List of objects defined by StatusObject.cs
        public List<StatusObject>? statusObjects {  get; set; }
    }
}
