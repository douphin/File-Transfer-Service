using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevisedFileTransferService
{
    // This holds functions that are used to query databases
    internal class SQLfunctions
    {
        // Because DB1 requires a 32bit driver, a separate 32bit exe is needed to run the queries, so these functions just call
        //  another exe to do the work and then return the results

        // Used to query DB1 for sched-cons numbers for elog and mval files to be copied
        public static List<string> MVALELOG_SELECT(TransferObject tObj, string mvalORelog)
        {
            try
            {
                List<string> mvalIDtoCopy = new List<string>();

                ProcessStartInfo info = new ProcessStartInfo(@"C:\USR\SRC\CS\RevisedFileTransferService\32Bit_FileTransferSQLQueries.exe");
                info.Arguments = $"{mvalORelog} SELECT";
                info.UseShellExecute = false;

                Process compiler = Process.Start(info);

                compiler.WaitForExit();
     
                // The exe will write the needed sched-cons to a text file, which get read in here
                using (StreamReader sr = new StreamReader(@"C:\USR\SRC\CS\RevisedFileTransferService\SCHED-CONS_SELECT_" + mvalORelog.ToUpper() + ".txt"))
                {
                    while (sr.Peek() > -1)
                    {
                        mvalIDtoCopy.Add(sr.ReadLine());
                    }
                }

                // Removing the blank space at the end of the file
                mvalIDtoCopy.RemoveAt(mvalIDtoCopy.Count - 1);

                return mvalIDtoCopy;
            }
            catch (Exception ex)
            {
                tObj.LogMessage(ex.Message, TransferObject.WriteType.LineSeparation);
                return new List<string>();
            }
        }

        // Used to query DB1 to update copy times for copied files
        public static void MVALELOG_UPDATE(TransferObject tObj, List<string> list, string mvalORelog)
        {
            try
            {
                string args_Str = $"{mvalORelog} UPDATE";

                foreach(string arg in list)
                {
                    args_Str += " " + arg ;
                }

                ProcessStartInfo info = new ProcessStartInfo(@"C:\USR\SRC\CS\RevisedFileTransferService\32Bit_FileTransferSQLQueries.exe");
                info.Arguments = args_Str;
                info.UseShellExecute = false;

                Process compiler = Process.Start(info);

                compiler.WaitForExit();
            }
            catch (Exception ex)
            {
                tObj.LogMessage(ex.Message, TransferObject.WriteType.LineSeparation);
            }
        }

        // Used to query DB1 to update copy times and write an error message in the event of an errror
        public static void MVALELOG_COPYFAIL(TransferObject tObj, List<string> list, string mvalORelog)
        {
            try
            {
                tObj.LogMessage("COPYFAIL Started - ", TransferObject.WriteType.InLine);

                string args_Str = $"{mvalORelog} COPYFAIL";

                foreach (string arg in list)
                {
                    args_Str += " " + arg;
                }

                ProcessStartInfo info = new ProcessStartInfo(@"C:\USR\SRC\CS\RevisedFileTransferService\32Bit_FileTransferSQLQueries.exe");
                info.Arguments = args_Str;
                info.UseShellExecute = false;

                Process compiler = Process.Start(info);

                compiler.WaitForExit();

                tObj.LogMessage("COPYFAIL Finished", TransferObject.WriteType.NewLine);
            }
            catch(Exception ex)
            {
                tObj.LogMessage(ex.ToString(), TransferObject.WriteType.BreakLine);
            }
        }
    }
}
