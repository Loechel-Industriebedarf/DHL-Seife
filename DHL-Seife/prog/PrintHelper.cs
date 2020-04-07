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
				pdfdocument.PrinterName = sett.PrinterName;
				pdfdocument.PrintDocument.PrinterSettings.Copies = 1;
				pdfdocument.PrintDocument.Print();
				pdfdocument.Dispose();

				//logTextToFile("> " + labelName + " was successfully printed!");
				log.writeLog("> " + labelName + " wurde erfolgreich gedruckt!", true);

			}
			catch (Exception ex)
			{
				log.writeLog(ex.ToString().ToString(), true, true);

				if(printTries < 3)
				{
					printTries++;
					System.Threading.Thread.Sleep(3000); //Wait for three seconds
					PrintLabel(sett, log, labelName); //Try to print again
				}
			}
		}
	}
}
