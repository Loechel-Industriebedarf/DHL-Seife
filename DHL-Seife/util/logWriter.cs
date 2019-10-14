using DHL_Seife.util;
using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHL_Seife
{
    class LogWriter
    { 
        private SettingsReader sett;
        private Boolean firstrun = true; //If it's the first run, add an aditional line break to the text-log



        public LogWriter(SettingsReader settingsBuffer)
        {
            sett = settingsBuffer;
        }




        /// <summary>
        /// Handels the log writing
        /// </summary>
        /// <param name="log">The log text, that should be written to the file.</param>
        /// <param name="nl">If true: Insert a new line after the logtext.</param>
        /// <param name="termin">If true: Add a new termin ("Wiedervorlage") for the admin user.</param>
        public void writeLog(String log)
        {
            writeLog(log, false, false);
        }
        public void writeLog(String log, Boolean nl)
        {
            writeLog(log, nl, false);
        }
        public void writeLog(String log, Boolean nl, Boolean termin)
        {
            writeLogToDatabase(log, nl, termin);

            writeLogToFile(log, nl);
        }

        /// <summary>
        /// Writes the log to a file.
        /// </summary>
        /// <param name="log">The log text, that should be written to the file.</param>
        /// <param name="nl">If true: Insert a new line after the logtext. If false: Do nothing.</param>
        private void writeLogToFile(string log, bool nl)
        {
            try { 
                using (StreamWriter sw = File.AppendText(sett.Logfile))
                {
                    if (firstrun)
                    {
                        sw.WriteLine();
                        sw.WriteLine();
                        sw.WriteLine();
                        sw.WriteLine("> " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
                        firstrun = false;
                    }

                    sw.WriteLine(log);

                    if (nl)
                    {
                        sw.WriteLine();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// Writes the log to the orders sql.
        /// </summary>
        /// <param name="log">The log text, that should be written to the file.</param>
        /// <param name="nl">If true: Insert a new line after the logtext.</param>
        /// <param name="termin">If true: Add a new termin ("Wiedervorlage") for the admin user.</param>
        private void writeLogToDatabase(string log, bool nl, bool termin)
        {
            //Write to database
            try
            {
                log = log.Replace("'", "´"); //Replace fixes sql errors, if the log contains '
                string sql = sett.SqlInsertNewMemo + " + '" + log;
                if (nl)
                {
                    sql += "\r\n";
                }
                sql += "\r\n' WHERE BelegNr = '" + sett.OrderNumber + "'";
                OdbcConnection conn = new OdbcConnection(sett.ConnectionString);
                conn.Open();
                OdbcCommand comm = new OdbcCommand(sql, conn);
                comm.ExecuteNonQuery();
                if (termin)
                {
                    sql = sett.SqlInsertNewTermin;
                    sql = sql.Replace("%ordernumber%", sett.OrderNumber).Replace("%log%", log).Replace("%time%", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"));
                    Console.WriteLine(sql);
                    comm = new OdbcCommand(sql, conn);
                    comm.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
