using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevisedFileTransferService
{
    public class StatusObject
    {
        public string transferName { get; set; } = "Unknown";

        public string srcIPstatus { get; set; } = "Untested";

        public string destIPstatus { get; set; } = "Untested";

        public string srcPathstatus { get; set; } = "Untested";

        public string destPathstatus { get; set; } = "Untested";

        public string FTPconnectionstatus { get; set; } = "Untested";

        public string Copystatus { get; set; } = "Untested";

        public string LogPrimarystatus { get; set; } = "Untested";

        public string LogSecondarystatus { get; set; } = "Untested";

        public StatusObject()
        {
            Copystatus = "Untested";
            srcIPstatus = "Untested";
            destIPstatus = "Untested";
            srcPathstatus = "Untested";
            destPathstatus = "Untested";
            LogPrimarystatus = "Untested";
            LogSecondarystatus = "Untested";
            FTPconnectionstatus = "Untested";
        }
    }
}
