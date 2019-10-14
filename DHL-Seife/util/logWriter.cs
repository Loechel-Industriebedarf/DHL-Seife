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
        public String orderNumber = "";

        private SettingsReader sett;
        private Boolean firstrun = true; //If it's not the first run, don't insert additional line breaks



        public LogWriter(SettingsReader settingsBuffer)
        {
            sett = settingsBuffer;
        }





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
            //Write to database
            try
            {
                log = log.Replace("'", "´"); //Replace fixes sql errors, if the log contains '
                string sql = sett.sqlinsertnewmemo + " + '" + log;
                if (nl)
                {
                    sql += "\r\n";
                }
                sql += "\r\n' WHERE BelegNr = '" + orderNumber + "'";
                OdbcConnection conn = new OdbcConnection(sett.connectionString);
                conn.Open();
                OdbcCommand comm = new OdbcCommand(sql, conn);
                comm.ExecuteNonQuery();
                if (termin)
                {
                    sql = sett.sqlinsertnewtermin;
                    sql = sql.Replace("%ordernumber%", orderNumber).Replace("%log%", log).Replace("%time%", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"));
                    Console.WriteLine(sql);
                    comm = new OdbcCommand(sql, conn);
                    comm.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            //Write to log file
            using (StreamWriter sw = File.AppendText(sett.logfile))
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

    }
}
