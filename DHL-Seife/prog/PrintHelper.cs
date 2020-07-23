using DHL_Seife.util;
using Spire.Pdf;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHL_Seife.prog
{
	class PrintHelper
	{
		static int printTries = 0; //If the document fails to print, try it again x times.


		/// <summary>
		/// This class sends the labels to the printer.
		/// </summary>
		/// <param name="settingsBuffer">An SettingsReader object, that contains all settings.</param>
		/// <param name="lw">An LogWriter object, to write logs, if exceptions occur.</param>
		/// <param name="labelName">File name of the label that should be printed.</param>
		public PrintHelper(SettingsReader settingsBuffer, LogWriter lw, string labelName)
		{
			PrintLabel(settingsBuffer, lw, labelName);
		}

		/// <summary>
		/// Prints the labels. Simple as that.
		/// Uses Spire.pdf to realise the printing.
		/// </summary>
		/// <param name="settingsBuffer">An SettingsReader object, that contains all settings.</param>
		/// <param name="lw">An LogWriter object, to write logs, if exceptions occur.</param>
		/// <param name="labelName">File name of the label that should be printed.</param>
		private static void PrintLabel(SettingsReader sett, LogWriter log, string labelName)
		{
			try
			{
				// Print the file
				string filepath = labelName;

				PdfDocument pdfdocument = new PdfDocument();
				pdfdocument.LoadFromFile(filepath);
                pdfdocument.PrintSettings.PrinterName = sett.PrinterName;
                pdfdocument.PrintSettings.Copies = 1;
                pdfdocument.Print();
				pdfdocument.Dispose();

                //logTextToFile("> " + labelName + " was successfully printed!");
                DateTimeOffset endTime = DateTimeOffset.Now;

                TimeSpan totalDuration = endTime.Subtract(sett.StartTime);
                TimeSpan labelDuration = sett.LabelTime.Subtract(sett.StartTime);
                TimeSpan printDuration = endTime.Subtract(sett.LabelTime);

                log.writeLog("> " + endTime.ToString("dd.MM.yyyy HH:mm:ss:fff") + " - " + labelName + " wurde erfolgreich gedruckt!", false);
                log.writeLog("> Der Labeldownload und -druck dauerten " + totalDuration.Seconds + "." + totalDuration.Milliseconds+ " Sekunden.", true);
                log.writeLogToCsv(
                    sett.OrderNumber + ";" +
                    sett.StartTime.ToString("dd.MM.yyyy HH:mm:ss:fff") + ";" +
                    labelDuration.Seconds + "," + labelDuration.Milliseconds + ";" +
                    sett.LabelTime.ToString("dd.MM.yyyy HH:mm:ss:fff") + ";" +
                    printDuration.Seconds + "," + printDuration.Milliseconds + ";" +
                    endTime.ToString("dd.MM.yyyy HH:mm:ss:fff") + ";" +
                    totalDuration.Seconds + "," + totalDuration.Milliseconds
                );
            }
			catch (Exception ex)
			{
				log.writeLog(ex.ToString().ToString(), true, false);

				if(printTries < 5)
				{
					printTries++;
					System.Threading.Thread.Sleep(5000); //Wait for five seconds
					PrintLabel(sett, log, labelName); //Try to print again
				}
				else{
					log.writeLog(ex.ToString().ToString(), true, true);
				}
			}
		}
	}
}
