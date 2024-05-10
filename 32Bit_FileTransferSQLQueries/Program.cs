using System.Data;
using System.Data.Odbc;

// This will get compiled into an exe for the File Transfer Service to run in order to query DB1

namespace FileTransferSQLQueries_32Bit
{
    internal class Program()
    {
        public static void Main(string[] args)
        {
            try
            {
                // The first arg should be "mval" or "elog" and the second should be "Select", "UPDATE", or "COPYFAIL"
                if (args.Length == 0)
                {
                    using (StreamWriter sw = new StreamWriter(@"C:\USR\Logs\File Transfer Logs\UnhandledErrors.txt", true))
                    {
                        sw.WriteLine("Bad args");
                    }
                }

                // If select, Run the query and write the results to a text file
                else if (string.Equals(args[1], "SELECT", StringComparison.OrdinalIgnoreCase))
                {
                    using (StreamWriter sw = new StreamWriter(@"C:\USR\SRC\CS\RevisedFileTransferService\SCHED-CONS_SELECT_" + args[0].ToUpper() + ".txt", false))
                    {
                        foreach (string item in MVALELOG_SELECT(args[0]))
                        {
                            sw.WriteLine($"{item}");
                        }
                        sw.WriteLine();
                    }
                }

                // If update, following the first two args, will be all of the sched-cons to write for,
                //   take those and write dump times for their copy
                else if (string.Equals(args[1], "UPDATE", StringComparison.OrdinalIgnoreCase))
                {
                    List<string> list = new(args);

                    list.RemoveAt(0);
                    list.RemoveAt(0);

                    MVALELOG_UPDATE(list, args[0]);
                }

                // If copyfail, do the same as update, just write error messages to the dump_error column
                else if (string.Equals(args[1], "COPYFAIL", StringComparison.OrdinalIgnoreCase))
                {
                    List<string> list = new(args);

                    list.RemoveAt(0);
                    list.RemoveAt(0);

                    MVALELOG_COPYFAIL(list, args[0]);
                }
            }
            catch (Exception ex)
            {
                using (StreamWriter sw = new StreamWriter(@"C:\USR\Logs\File Transfer Logs\UnhandledErrors.txt", true))
                {
                    sw.WriteLine(ex.ToString());
                }

            }
        }



        // Select sched-cons from DB1 for files to copy
        public static List<string> MVALELOG_SELECT(string mvalORelog)
        {
            string str = $"SELECT * FROM X_ACHVD_PCS WHERE ARCHIVE_TIME > DATE '{DateTime.Now.AddDays(-2).ToString("yyyy-MM-dd")}' AND ARCHIVE_TIME_{mvalORelog.ToUpper()}_DUMP = DATE '4000-01-01' ORDER BY ARCHIVE_TIME ASC;";

            OdbcConnection DB1 = new(){    ConnectionString = "DSN=DB1;UID=user;PWD=pass" };

            DataSet dataSet = new();

            DB1.Open();
            OdbcDataAdapter odbcDataAdapter = new(str, DB1);

            odbcDataAdapter.Fill(dataSet, "DB1_DATA");
            DB1.Close();

            List<string> mvalIDtoCopy = [];

            foreach (DataRow dr in dataSet.Tables[0].Rows)
            {
                mvalIDtoCopy.Add(dr["MASTER_NUM"].ToString().Trim() + "-" + dr["MASTER_DASH"].ToString().Trim());
            }

            return mvalIDtoCopy;

        }

        // Update sched-cons on DB1 for files copied
        public static void MVALELOG_UPDATE(List<string> list, string mvalORelog)
        {

            string str = $"UPDATE X_ACHVD_PCS SET ARCHIVE_TIME_{mvalORelog.ToUpper()}_DUMP = TO_DATE( '{DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt")}', 'yyyy-mm-dd HH:MI:SS AM') WHERE ";
            

            foreach (string item in list)
            {
                string[] splitSchedCons = item.Split('-');
                str += $" ( MASTER_NUM = {splitSchedCons[0]} AND MASTER_DASH = {splitSchedCons[1]} ) OR ";
            }

            str = string.Concat(str.AsSpan(0, str.Length - 4), " ;");

            OdbcConnection DB1 = new(){    ConnectionString = "DSN=DB1;UID=user;PWD=pass"};

            DB1.Open();

            OdbcCommand DB1command = new(str, DB1);
            DB1command.ExecuteNonQuery();

            DB1.Close();

        }

        // Write error statements for the sched-cons that failed to copy
        public static void MVALELOG_COPYFAIL(List<string> list, string mvalORelog)
        {

            string str = $"UPDATE X_ACHVD_PCS SET ARCHIVE_TIME_{mvalORelog.ToUpper()}_DUMP_ERROR = 'Could Not Copy, Files Not Present on DB1' WHERE ";

            foreach (string item in list)
            {
                string[] splitSchedCons = item.Split('-');
                str += $" ( MASTER_NUM = {splitSchedCons[0]} AND MASTER_DASH = {splitSchedCons[1]} ) OR ";
            }

            str = string.Concat(str.AsSpan(0, str.Length - 4), " ;");

            OdbcConnection DB1 = new(){    ConnectionString = "DSN=DB1;UID=user;PWD=pass" };

            DB1.Open();

            OdbcCommand DB1command = new(str, DB1);
            DB1command.ExecuteNonQuery();

            DB1.Close();

            MVALELOG_UPDATE(list, mvalORelog);
        }
    }
}