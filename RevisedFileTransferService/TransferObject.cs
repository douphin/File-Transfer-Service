using FluentFTP;
using FluentFTP.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace RevisedFileTransferService
{
    public class TransferObject
    {
        // The the public data members will are populated from the JSON
        public string transferName { get; set; } = "";

        public bool TransferActive { get; set; } = true;

        public string srcDNSname { get; set; } = "";

        public string destDNSname { get; set; } = "";

        public string srcIPaddress { get; set; } = "";

        public string destIPaddress { get; set; } = "";

        public double minutesTimer { get; set; }

        public bool logPrimary { get; set; }

        public bool logSecondary { get; set; }

        public string srcPath { get; set; } = "";

        public string destPath { get; set; } = "";

        public bool FTPbool { get; set; } = false;

        public string FTPusername { get; set; } = "";

        public string FTPpassword { get; set; } = "";

        public int lookBackDays { get; set; } = 0;

        public bool deleteAfterCopy { get; set; } = false;

        public int monthsUntilPurge { get; set; } = 24;

        public bool creationTimeAutoSort { get; set; } = false;


        // The private data members are only used in-program, and won't appear in a JSON
        private string ModsrcPath { get; set; } = "";

        // Destintation Path modified for the current date and time
        private string ModdestPath { get; set; } = "";

        private DateTime LastLookback { get; set; } = DateTime.Now.AddDays(-1);

        private System.Timers.Timer? timer;

        private readonly string LogPathPrimary = @"C:\USR\Logs\File Transfer Logs\Main Service Logs\Service_Log_";

        private string LogPathSecondary = @"C:\USR\Logs\File Transfer Logs\Auxiliary Logs\";

        private string logDate = DateTime.Now.ToString("MMM-dd-yyyy");

        private bool isSpecialFTP = false;

        private StatusObject statusObject = new StatusObject();


        

        // This will be called the first time a JSON is initialized whether that be on program start up or when the data JSON is changed
        public void InitializeObj()
        {
            try
            {
                LogPathSecondary += transferName + " Logs";

                // Don't initialized if the transfer is turned off
                if (!TransferActive)
                {
                    if (Directory.Exists(LogPathSecondary))
                    {
                        LogMessage("Object Inactive", WriteType.NewLine);
                    }

                    return;
                }

                if (!Directory.Exists(LogPathSecondary))
                {
                    Directory.CreateDirectory(LogPathSecondary);
                }

                // Check to make sure that all of the necessary fields are filled out
                if ( transferName == "" || srcIPaddress == "" || destIPaddress == "" || srcPath == "" || destPath == "" || ( FTPbool && ( FTPpassword == "" || FTPusername == "" ) ) )
                {    
                    logPrimary = true;
                    logSecondary = false;
                    LogMessage("Essential Field(s) have been left blank, transfer cannot continue until these fields are properly filled out ", WriteType.LineSeparation);
                    return;
                }

                // Make sure the status object has the a name for all of it's statuses
                statusObject.transferName = transferName;

                // Initializing the Timer by adding the duration, and the function to call
                double time = minutesTimer * 60 * 1000;
 
                timer = new System.Timers.Timer(time);
                timer.Elapsed += new ElapsedEventHandler(TransferFiles);

                // initialize the copy paths using the IP address as the computer name, i.e. take \copy\path\ and turn it into \\10.100.123.123\copy\path\
                destPath = $"\\\\{destIPaddress}" + destPath;

                if (!FTPbool)
                {
                    srcPath = $"\\\\{srcIPaddress}" + srcPath;
                }

                if (transferName == "IDtype1" || transferName == "IDtype2")
                {
                    isSpecialFTP = true;
                }

                LogMessage("Object Initialized", WriteType.BreakLine);
            }
            catch (Exception ex)
            {
                RecordUnhandledError(ex.Message);
            }
        }

        // Start the timers
        public void StartTimer()
        {
            try
            {
                timer.Start();
            }
            catch (Exception ex)
            {
                LogMessage(ex.Message, WriteType.LineSeparation);
            }
        }

        // Stop the timers
        public void StopTimer()
        {
            try
            {
                timer.Stop();
            }
            catch (Exception ex)
            {
                LogMessage(ex.Message, WriteType.LineSeparation);
            }
        }



        // This is the function that will get called on whenever the timer expires
        private void TransferFiles(Object source, ElapsedEventArgs args)
        {
            try
            {
                // Ping check both source and destingation
                if (!PingCheck())
                {
                    LogMessage("File Transfer aborted, Ping check incomplete", WriteType.LineSeparation);
                    return;
                }

                // The source doesn't get modified for certain copy jobs
                if (!isSpecialFTP) ModsrcPath = MakePath(srcPath, DateTime.Now);

                // This destination gets modified no matter what
                ModdestPath = MakePath(destPath, DateTime.Now);

                // Call the correct copy method
                if (FTPbool) { FTPFileMove(); }

                if (!FTPbool) { SharedFileMove(); }

                // Once a day, go back and copy any missed files
                // Because of how the FTP copy is currently built, it doesn't support lookback
                if (lookBackDays > 0 && !FTPbool && LastLookback.Day != DateTime.Now.Day)
                {
                    // Copy missed files for each lookback day, adjusting the path accordingly
                    for (int i = 1; i <= lookBackDays; i++)
                    {
                        int day = -1 * i;
                        ModsrcPath = MakePath(srcPath, DateTime.Now.AddDays(day));
                        ModdestPath = MakePath(destPath, DateTime.Now.AddDays(day));

                        SharedFileMove();



                        LastLookback = DateTime.Now;
                    }
                    PurgeOldFiles();
                    LogMessage("Purge Files Call", WriteType.LineSeparation);

                }
            }
            catch(Exception ex)
            {
                LogMessage($"Error: {ex.Message}", WriteType.BufferLine);
                LogMessage(ex.ToString(), WriteType.BreakLine);
            }
        }

        // Will update the copy path to include the provided date
        // It will take the path provide and pull out the 'mmDDyy' and replace it the provided current date in the provided format
        public static string MakePath( string completePath, DateTime dt)
        {
            string[] pathParts = completePath.Split('\'');
            completePath = "";

            int i = 1;

            foreach (string segment in pathParts)
            {
                if (i % 2 == 0)
                {
                    pathParts[i - 1] = dt.ToString(segment);
                }
                completePath += pathParts[i - 1];

                i++;
            }
            return completePath;
        }


        // Will copy files from a file share
        void SharedFileMove()
        {
            try
            {
                DirectoryInfo SourceDirInfo = new DirectoryInfo(ModsrcPath);
                DirectoryInfo DestinationDirInfo = new DirectoryInfo(ModdestPath);

                statusObject.srcPathstatus = "Error";
                statusObject.destPathstatus = "Error";

                if (SourceDirInfo.Exists) { statusObject.srcPathstatus = "Good"; }
                if (DestinationDirInfo.Exists) { statusObject.destPathstatus = "Good"; }

                // Created all relevant directories and subdirectories at the source if not created yet
                Directory.CreateDirectory(ModdestPath);

                // Copy each file into the new directory 
                foreach (FileInfo File in SourceDirInfo.GetFiles())
                {
                    if (!(new FileInfo(ModdestPath.ToString() + File.Name).Exists)) // Checking if the file exists at the source or not
                    {
                        DateTime CreationTime = File.CreationTime;
                        DateTime CurrentTime = DateTime.Now;


                        if (creationTimeAutoSort)
                        {
                            DestinationDirInfo = new DirectoryInfo(MakePath(destPath, CreationTime));
                            Directory.CreateDirectory(MakePath(destPath, CreationTime));
                        }

                        // Making sure that the file has been completely written before being copied
                        if (CurrentTime >= CreationTime.AddSeconds(1260))
                        {
                            LogMessage(DateTime.Now.ToString() + @"  -  Copying " + DestinationDirInfo.FullName + File.Name, WriteType.NewLine);
                            File.CopyTo(Path.Combine(DestinationDirInfo.FullName, File.Name), true); // Copying the data

                            if (deleteAfterCopy) File.Delete();
                        }
                    }
                }
                statusObject.Copystatus = "Good";
                LogMessage(" -----Copy Done-----", WriteType.BreakLine);
            }
            catch (Exception ex)
            {
                LogMessage($"Error: {ex.Message}", WriteType.BufferLine);
                LogMessage(ex.ToString(), WriteType.BreakLine);
                statusObject.Copystatus = "Error";
            }
        }


        // Will copy files from an FTP server
        void FTPFileMove()
        {
            try
            {
                using (var ftp = new FtpClient(srcIPaddress, FTPusername, FTPpassword))
                {
                    // Doing Status checks
                    statusObject.FTPconnectionstatus = "Error";
                    ftp.Connect();
                    statusObject.FTPconnectionstatus = "Good";

                    int i = 0;
                    List<string> toCopy = new List<string>();
                    List<string> toUpdate = new List<string>();

                    // Populating a list in the event the copy job is for specialFTP
                    if (isSpecialFTP) toCopy = SQLfunctions.DATA_SELECT(this, transferName.Substring(0, 4));

                    ftp.SetWorkingDirectory(srcPath);

                    // More status checks
                    DirectoryInfo DestinationDirInfo = new DirectoryInfo(ModdestPath);
                    statusObject.destPathstatus = "Error";
                    if (DestinationDirInfo.Exists) { statusObject.destPathstatus = "Good"; }

                    // Foreach file found on the ftp server
                    foreach (var item in ftp.GetListing(ftp.GetWorkingDirectory(), FtpListOption.Recursive))
                    {
                        string IDtype = ItemContains(item.Name, toCopy); // Gets the ID_NUM 

                        if ( item.Modified.Day == DateTime.Now.Day)
                        {
                            FtpStatus flag = ftp.DownloadFile(string.Concat(ModdestPath, item.FullName.AsSpan(11)), item.FullName, FtpLocalExists.Skip, FtpVerify.Retry);


                            // If the ftp fails
                            if (flag.IsFailure())
                            {
                                LogMessage($"Copy for {item.Name} Failed, Trying again", WriteType.BreakLine);
                                flag = ftp.DownloadFile(string.Concat(ModdestPath, item.FullName.AsSpan(11)), item.FullName, FtpLocalExists.Skip, FtpVerify.Retry);
                                if (flag.IsFailure())
                                {
                                    LogMessage($"Copy for {item.Name} Failed again, moving on", WriteType.BreakLine);
                                    continue;
                                }
                            }

                            string fileToDelete = item.FullName;

                            i++;

                            if (isSpecialFTP)
                            {
                                toUpdate.Add(IDtype);
                                fileToDelete = item.Name + ";*";
                            }

                            if (deleteAfterCopy) ftp.DeleteFile(fileToDelete);
                        }
                    }


                    if (i > 0) LogMessage($"Verified {i} items", WriteType.BufferLine);
                    else       LogMessage("-------    "        , WriteType.InLine);
                    
                    // Status checks
                    statusObject.Copystatus = "Good";
                    statusObject.srcPathstatus = "Good";
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error: {ex.Message}", WriteType.BreakLine);
                LogMessage(ex.ToString(), WriteType.BreakLine);
                statusObject.Copystatus = "Error";
            }
        }

        public void PurgeOldFiles()
        {
            DateTime PurgeCutOff = DateTime.Now.AddMonths(-1 * monthsUntilPurge);
            string purgePath = destPath.Split('\'')[0];

            DirectoryInfo PurgePath = new DirectoryInfo(purgePath);

            if (!PurgePath.Exists) return;

            foreach (DirectoryInfo dI in PurgePath.GetDirectories())
            {
                DateTime parsedDate = DateTime.ParseExact(dI.Name,destPath.Split('\'')[1], null);

                if (parsedDate < PurgeCutOff)
                {
                    dI.Delete(true);
                    LogMessage($"Deleting {dI.Name}", WriteType.NewLine);
                }
            }
        }


        // This will take the current file from the ftp server and see if it's on the copy list, returning 
        public static string ItemContains(string file_item, List<string> db_IDtype)
        {
            if (db_IDtype.Count == 0 )
            {
                return "-1";
            }
            foreach (string currItem in db_IDtype)
            {
                if(file_item.Contains(currItem))
                {
                    return currItem;
                }
            }
            
            return "-1";
        }

        // This will used to ping both the source and destination computers to make sure they are active before atempting a copy
        private bool PingCheck()
        {
            try
            {
                // Ping method pulled from https://learn.microsoft.com/en-us/dotnet/api/system.net.networkinformation.ping?view=net-7.0
                Ping pingSender = new Ping();
                PingOptions options = new PingOptions();

                // Use the default Ttl value which is 128,
                // but change the fragmentation behavior.
                options.DontFragment = true;

                // Create a buffer of 32 bytes of data to be transmitted.
                string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
                byte[] buffer = Encoding.ASCII.GetBytes(data);
                int timeout = 120;

                PingReply replySrc = pingSender.Send(srcIPaddress, timeout, buffer, options);
                PingReply replyDest = pingSender.Send(destIPaddress, timeout, buffer, options);

                bool SrcStatus = replySrc.Status == IPStatus.Success;
                bool DestStatus = replyDest.Status == IPStatus.Success;

                if (SrcStatus && DestStatus)
                {
                    //LogMessage($"Ping Succeeded for both {srcIPaddress} and {destIPaddress}", WriteType.NewLine);

                    statusObject.srcIPstatus = "Good";
                    statusObject.destIPstatus = "Good";
                    return true;
                }

                if (!SrcStatus)
                {
                    LogMessage($"Ping Failed for {srcIPaddress}", WriteType.BreakLine);
                    statusObject.srcIPstatus = "Error";
                }
                if (!DestStatus)
                {
                    LogMessage($"Ping Failed for {destIPaddress}", WriteType.BreakLine);
                    statusObject.destIPstatus = "Error";
                }

                return false;
            }
            catch (Exception ex)
            {
                LogMessage("Ping Check Error: " + srcIPaddress + " - " + ex.Message.ToString(), WriteType.BreakLine);
                return false;
            }
        }

        // Will pass the statuses back up to TransferSerializer
        public StatusObject ReturnStatus()
        {
            try
            {
                statusObject.transferName = transferName;
                return statusObject;
            }
            catch(Exception e) 
            {
                LogMessage (e.ToString(), WriteType.BreakLine);
                return null;
            }

        }

        // This is used to indicate how much or little blank space to include when writing a log message
        public enum WriteType
        {
            InLine,
            NewLine,
            BreakLine,
            BufferLine,
            LineSeparation
        }


        // This will initiate the process for writing a message to a txt file, the writetype indicates what to include when writing a message
        public void LogMessage(string msg, WriteType writeChoice)
        {
            // Update the date
            logDate = DateTime.Now.ToString("MMM-dd-yyyy");

            string auxFilename = LogPathSecondary + @"\" + transferName + "_Logs_" + logDate + ".txt";
            string mainFilename = LogPathPrimary + logDate + ".txt";

            try
            {
                if (logPrimary)
                {
                    statusObject.LogPrimarystatus = "Error";
                    WritetoFile(mainFilename, msg, writeChoice, 0);
                    statusObject.LogPrimarystatus = "Good";
                }
            }
            catch (Exception ex)
            {
                RecordUnhandledError(ex.Message);
            }

            try
            {
                if (logSecondary)
                {
                    statusObject.LogSecondarystatus = "Error";
                    WritetoFile(auxFilename, msg, writeChoice, 0);
                    statusObject.LogSecondarystatus = "Good";
                }
            }
            catch (Exception ex)
            {
                RecordUnhandledError(ex.Message);
            }
        }

        // Will be called by LogMessage(..., ...) or called rescursively and will actually write to a file
        private void WritetoFile(string filename, string msg, WriteType writeChoice, int baseCase)
        {
            if (baseCase > 5)
            {
                throw new Exception($"Unable to write to file {filename}");
            }

            if (!File.Exists(filename))
            {
                // Create a file to write to.   
                using (StreamWriter file = File.CreateText(filename))
                {
                    file.WriteLine("-------- Start of Log --------");
                    file.WriteLine();
                }
            }

            try
            {
                using (StreamWriter file = new StreamWriter(filename, true))
                {
                    switch (writeChoice)
                    {
                        case WriteType.InLine:
                            file.Write(DateTime.Now.ToString("t") + ": " + transferName + "_" + msg);
                            break;
                        case WriteType.NewLine:
                            file.WriteLine(DateTime.Now.ToString("t") + ": " + transferName + "_" + msg);
                            break;
                        case WriteType.BreakLine:
                            file.WriteLine(DateTime.Now.ToString("t") + ": " + transferName + "_" + msg);
                            file.WriteLine();
                            break;
                        case WriteType.BufferLine:
                            file.WriteLine();
                            file.WriteLine(DateTime.Now.ToString("t") + ": " + transferName + "_" + msg);
                            file.WriteLine();
                            break;
                        case WriteType.LineSeparation:
                            file.WriteLine();
                            file.WriteLine("----------------");
                            file.WriteLine(DateTime.Now.ToString("t") + ": " + transferName + "_" + msg);
                            file.WriteLine("----------------");
                            file.WriteLine();
                            break;
                    }
                }
            }
            // Sometimes an error is thrown when something is already writing to this specific file, so wait a random amount of time then retry up to 5 times
            catch
            {
                Task.Delay(new Random().Next(200)).Wait();
                WritetoFile(filename, msg, writeChoice, ++baseCase);
            }
        }

        // Will write any unhandled errors to a static txt file
        void RecordUnhandledError(string error)
        {
            try
            {
                string strvar = DateTime.Now.ToString("t") + "-" + error;
                string[] lines = { "\n", strvar };
                File.AppendAllLines(WindowsBackgroundService.UnhandledErrorPath, lines);
            }
            catch
            {
                // Will retry to write until it succeeds, may be an issue oneday, but not yet
                Task.Delay(new Random().Next(200)).Wait();
                RecordUnhandledError(error);
            }
        }

        

    }
}
