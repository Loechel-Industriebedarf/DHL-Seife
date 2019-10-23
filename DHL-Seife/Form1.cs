using System;
using System.Data.Odbc;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using Spire.Pdf;
using System.Collections;
using DHL_Seife.util;
using DHL_Seife.prog;

namespace DHL_Seife
{
	public partial class Form1 : Form
	{
		private static SettingsReader Sett = new SettingsReader();
		private static LogWriter Log = new LogWriter(Sett);
		private static SQLHelper SqlH = new SQLHelper(Sett, Log);
		private static XMLHelper XmlH = new XMLHelper(Sett, Log, SqlH);
		private static SOAPHelper SoapH = new SOAPHelper(Sett, Log, SqlH, XmlH);


		/// <summary>
		/// Main method that gets started, when the program is... Started.
		/// First it checks, if the user ran the program more than 3 seconds ago. We don't want that here.
		/// Then it checks, if it was run manually or via command line parameters.
		/// If it was run manually, display the gui.
		/// If it wasn't run manually, take the order number from the args parameters, read the data from sql and send a xml request to dhl.
		/// </summary>
		public Form1()
		{
			//Our users tend to run the program twice, per "accident"...
			CheckDoubleRun();

			//The order number can be transmitted via command line parameter
			string[] args = Environment.GetCommandLineArgs();

			//The program is able to send dhl and dpd orders via command line parameters
			String orderType = "DHL";

			//Program was started via command line parameters
			try
			{
				if (!String.IsNullOrEmpty(args[1]))
				{
					Sett.OrderNumber = args[1];
					Log.writeLog("> " + args[1], true);
				}
				if (!String.IsNullOrEmpty(args[2]))
				{
					orderType = args[2];
					Log.writeLog("> " + orderType, false);
				}
			}
			//Program gui was started
			catch (Exception ex)
			{
				//log.writeLog("> The program was started manually.");
				Log.writeLog("> Das Programm wurde manuell gestartet.");
				Log.writeLog(ex.ToString(), true);
			}


			InitializeComponent();


			//If the program was started via a parameter, skip the whole gui thing
			if (!String.IsNullOrEmpty(Sett.OrderNumber))
			{
				switch (orderType)
				{
					case "DHL":
						SqlH.DoSQLMagic();
						XmlH.DoDHLXMLMagic();
						SoapH.SendDHLSoapRequest();
						break;

					case "DPD":
						SqlH.DoSQLMagic();
						SoapH.DPDAuth();
						XmlH.DoDPDXMLMagic();
						SoapH.SendDPDSoapRequest();
						break;

					default:
						SqlH.DoSQLMagic();
						XmlH.DoDHLXMLMagic();
						SoapH.SendDHLSoapRequest();
						break;
				}

				Application.Exit();
				Environment.Exit(1);
			}
			else
			{
				printManualShippingLabel.Visible = true;
			}
			WriteToGui();
		}


		/// <summary>
		/// Checks, how much seconds passed since the last run. If it's less than 3, don't run the program.
		/// Our users like to start the program multiple times "per accident". So this is a quick and stupid fix to not pay for multiple labels.
		/// </summary>
		private void CheckDoubleRun()
		{
			DateTime now = DateTime.Now;
			DateTime lastrun = Properties.Settings.Default.lastRun;
			TimeSpan diff = (now - lastrun);

			//If more than 3 seconds passed, write the new time to the settings.
			if (diff.TotalSeconds > 3)
			{
				Properties.Settings.Default.lastRun = now;
				Properties.Settings.Default.Save();
			}
			//If less than 3 seconds passed, kill the program.
			else
			{
				//logTextToFile("> Less than 3 seconds passed! Double run!");
				Log.writeLog("> Doppelte Ausführung! Bitte 3 Sekunden warten.");
				Application.Exit();
				Environment.Exit(1);
			}


		}




















		/*
         * 
         * 
         * GUI
         * stuff
         * comes
         * here
         * now!
         * 
         * 
         * */


		/// <summary>
		/// Inserts the different variables into the gui.
		/// </summary>
		private void WriteToGui()
		{
			Sett.OrderNumber = SqlH.XmlOurNumber;
			textBoxOrdernumber.Text = SqlH.XmlOurNumber;
			textBoxRecepient.Text = SqlH.XmlRecipient;
			textBoxStreet.Text = SqlH.XmlStreet;
			textBoxStreetNumber.Text = SqlH.XmlStreetnumber;
			textBoxPLZ.Text = SqlH.XmlPlz;
			textBoxCity.Text = SqlH.XmlCity;
			textBoxCountry.Text = SqlH.XmlCountry;
			textBoxWeight.Text = SqlH.XmlWeight;
			textBoxMail.Text = SqlH.XmlMail;
		}



		/// <summary>
		/// Primary button to create a shipping label.
		/// If no order number was transmitted (via parameter), the button acts as "get data from Enventa"-button.
		/// </summary>
		/// <param name="e">Reacts, when the PrintShippingLabel button was pressed.</param>
		private void PrintShippingLabel_Click(object sender, EventArgs e)
		{
			if (String.IsNullOrEmpty(SqlH.XmlOurNumber))
			{
				SqlH.DoSQLMagic();
				WriteToGui();
				printManualShippingLabel.Visible = false;
			}
			else
			{
				XmlH.DoDHLXMLMagic();
				SoapH.SendDHLSoapRequest();
				Application.Exit();
			}
		}


		/// <summary>
		/// This button only appears, if no data from Enventa was read. It starts the label-printing.
		/// </summary>
		/// <param name="e">Reacts, when the PrintShippingLabel button was pressed.</param>
		private void PrintManualShippingLabel_Click(object sender, EventArgs e)
		{
			XmlH.DoDHLXMLMagic();
			SoapH.SendDHLSoapRequest();
			Application.Exit();
		}

		/// <summary>
		/// Disable reading stuff from enventa database, when no order number is given.
		/// </summary>
		/// <param name="e">Reacts, when the text box with our order number was changed.</param>
		private void TextBoxOrdernumber_TextChanged(object sender, EventArgs e)
		{
			Sett.OrderNumber = textBoxOrdernumber.Text;
			if (String.IsNullOrEmpty(Sett.OrderNumber)) { printShippingLabel.Enabled = false; } else { printShippingLabel.Enabled = true; }
		}

		private void TextBoxRecepient_TextChanged(object sender, EventArgs e)
		{
			SqlH.XmlRecipient = textBoxRecepient.Text;
		}

		private void TextBoxStreet_TextChanged(object sender, EventArgs e)
		{
			SqlH.XmlStreet = textBoxStreet.Text;
		}

		private void TextBoxStreetNumber_TextChanged(object sender, EventArgs e)
		{
			SqlH.XmlStreetnumber = textBoxStreetNumber.Text;
		}

		private void TextBoxPLZ_TextChanged(object sender, EventArgs e)
		{
			SqlH.XmlPlz = textBoxPLZ.Text;
		}

		private void TextBoxCity_TextChanged(object sender, EventArgs e)
		{
			SqlH.XmlCity = textBoxCity.Text;
		}

		private void TextBoxCountry_TextChanged(object sender, EventArgs e)
		{
			SqlH.XmlCountry = textBoxCountry.Text;
		}

		private void TextBoxWeight_TextChanged(object sender, EventArgs e)
		{
			SqlH.XmlWeight = textBoxWeight.Text;
		}

		private void TextBoxMail_TextChanged(object sender, EventArgs e)
		{
			SqlH.XmlMail = textBoxMail.Text;
		}

		private void Main_Load(object sender, EventArgs e)
		{

		}

		private void Label9_Click(object sender, EventArgs e)
		{

		}


	}
}
