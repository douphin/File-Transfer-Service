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

        // Used to query DB1 for ID_NUM numbers for IDtype files to be copied
        public static List<string> DATA_SELECT(TransferObject tObj, string IDtype)
        {
            try
            {
                List<string> IDtoCopy = new List<string>();

                ProcessStartInfo info = new ProcessStartInfo(@"C:\USR\SRC\CS\RevisedFileTransferService\32Bit_FileTransferSQLQueries.exe");
                info.Arguments = $"{IDtype} SELECT";
                info.UseShellExecute = false;

                Process compiler = Process.Start(info);

                compiler.WaitForExit();
     
                // The exe will write the needed ID_NUM to a text file, which get read in here
                using (StreamReader sr = new StreamReader(@"C:\USR\SRC\CS\RevisedFileTransferService\ID_NUM_SELECT_" + IDtype.ToUpper() + ".txt"))
                {
                    while (sr.Peek() > -1)
                    {
                        IDtoCopy.Add(sr.ReadLine());
                    }
                }

                // Removing the blank space at the end of the file
                IDtoCopy.RemoveAt(IDtoCopy.Count - 1);

                return IDtoCopy;
            }
            catch (Exception ex)
            {
                tObj.LogMessage(ex.Message, TransferObject.WriteType.LineSeparation);
                return new List<string>();
            }
        }

        // Used to query DB1 to update copy times for copied files
        public static void DATA_UPDATE(TransferObject tObj, List<string> list, string IDtype)
        {
            try
            {
                string args_Str = $"{IDtype} UPDATE";

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
        public static void DATA_COPYFAIL(TransferObject tObj, List<string> list, string IDtype)
        {
            try
            {
                tObj.LogMessage("COPYFAIL Started - ", TransferObject.WriteType.InLine);

                string args_Str = $"{IDtype} COPYFAIL";

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
