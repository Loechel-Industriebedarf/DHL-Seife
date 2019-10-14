using DHL_Seife.util;
using Spire.Pdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHL_Seife.prog
{
    class PrintHelper
    {

        /// <summary>
        /// Prints the shipping label. The printers name is saved in a txt file.
        /// </summary>
        public PrintHelper(SettingsReader sett, LogWriter log, string labelName)
        {
            PrintLabel(sett, log, labelName);
        }

        private static void PrintLabel(SettingsReader sett, LogWriter log, string labelName)
        {
            try
            {
                string filepath = labelName;

                // Print the file
                try
                {
                    PdfDocument pdfdocument = new PdfDocument();
                    pdfdocument.LoadFromFile(filepath);
                    pdfdocument.PrinterName = sett.PrinterName;
                    pdfdocument.PrintDocument.PrinterSettings.Copies = 1;
                    pdfdocument.PrintDocument.Print();
                    pdfdocument.Dispose();

                }
                catch (Exception ex)
                {
                    log.writeLog(ex.ToString());
                    log.writeLog(ex.Message.ToString(), true);
                }

                //logTextToFile("> " + labelName + " successfully printed!");
                log.writeLog("> " + labelName + " wurde erfolgreich gedruckt!\r\n", true);
            }
            catch (Exception ex)
            {
                log.writeLog(ex.ToString());
                log.writeLog(ex.Message.ToString(), true);
            }
        }
    }
}
