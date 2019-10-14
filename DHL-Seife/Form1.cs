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
        private static SettingsReader sett = new SettingsReader();
        private static LogWriter log = new LogWriter(sett);
        private static SQLHelper sqlh = new SQLHelper(sett, log);
        private static XMLHelper xmlh = new XMLHelper(sett, log, sqlh);
        private static SOAPHelper soaph = new SOAPHelper(sett, log, sqlh, xmlh);



        public Form1()
        {
            //Our users tend to run the program twice, per "accident"...
            checkDoubleRun();

            //The order number can be transmitted via command line parameter
            string[] args = Environment.GetCommandLineArgs();

            //Program was started via command line parameters
            try
            {
                if (!String.IsNullOrEmpty(args[1])) {
                    sett.orderNumber = args[1];
                    log.writeLog("> " + args[1], true);
                }
            }
            //Program gui was started
            catch(Exception ex)
            {
                //log.writeLog("> The program was started manually.");
                log.writeLog("> Das Programm wurde manuell gestartet.");
                log.writeLog(ex.ToString());
                log.writeLog(ex.Message.ToString(), true);
            }
            

            InitializeComponent();


            //If the program was started via a parameter, skip the whole gui thing
            if (!String.IsNullOrEmpty(sett.orderNumber))
            {
                sqlh.doSQLMagic();
                xmlh.doXMLMagic();
                soaph.sendSoapRequest();

                Application.Exit();
                Environment.Exit(1);
            }
            else
            {
                printManualShippingLabel.Visible = true;
            }
            writeToGui();
        }


        /// <summary>
        /// Checks, how much seconds passed since the last run. If it's less than 10, don't run the program
        /// </summary>
        private void checkDoubleRun()
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
                log.writeLog("> Doppelte Ausführung! Bitte 3 Sekunden warten.");
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
        private void writeToGui()
        {
            sett.orderNumber = sqlh.xmlournumber;
            textBoxOrdernumber.Text = sqlh.xmlournumber;
            textBoxRecepient.Text = sqlh.xmlrecipient;
            textBoxStreet.Text = sqlh.xmlstreet;
            textBoxStreetNumber.Text = sqlh.xmlstreetnumber;
            textBoxPLZ.Text = sqlh.xmlplz;
            textBoxCity.Text = sqlh.xmlcity;
            textBoxCountry.Text = sqlh.xmlcountry;
            textBoxWeight.Text = sqlh.xmlweight;
            textBoxMail.Text = sqlh.xmlmail;
        }



        /// <summary>
        /// Primary button to create a shipping label.
        /// If no order number was transmitted (via parameter), the button acts as "get data from Enventa"-button.
        /// </summary>
        private void printShippingLabel_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(sqlh.xmlournumber))
            {
                sqlh.doSQLMagic();
                writeToGui();
                printManualShippingLabel.Visible = false;
            }
            else
            {
                xmlh.doXMLMagic();
                soaph.sendSoapRequest();
                Application.Exit();
            }
        }


        /// <summary>
        /// This button only appears, if no data from Enventa was read. It starts the label-printing.
        /// </summary>
        private void printManualShippingLabel_Click(object sender, EventArgs e)
        {
            xmlh.doXMLMagic();
            soaph.sendSoapRequest();
            Application.Exit();
        }

        /// <summary>
        /// Disable reading stuff from enventa database, when no order number is given.
        /// </summary>
        private void textBoxOrdernumber_TextChanged(object sender, EventArgs e)
        {
            sett.orderNumber = textBoxOrdernumber.Text;
            if (String.IsNullOrEmpty(sett.orderNumber)) { printShippingLabel.Enabled = false; } else { printShippingLabel.Enabled = true; }
        }

        private void textBoxRecepient_TextChanged(object sender, EventArgs e)
        {
            sqlh.xmlrecipient = textBoxRecepient.Text;
        }

        private void textBoxStreet_TextChanged(object sender, EventArgs e)
        {
            sqlh.xmlstreet = textBoxStreet.Text;
        }

        private void textBoxStreetNumber_TextChanged(object sender, EventArgs e)
        {
            sqlh.xmlstreetnumber = textBoxStreetNumber.Text;
        }

        private void textBoxPLZ_TextChanged(object sender, EventArgs e)
        {
            sqlh.xmlplz = textBoxPLZ.Text;
        }

        private void textBoxCity_TextChanged(object sender, EventArgs e)
        {
            sqlh.xmlcity = textBoxCity.Text;
        }

        private void textBoxCountry_TextChanged(object sender, EventArgs e)
        {
            sqlh.xmlcountry = textBoxCountry.Text;
        }

        private void textBoxWeight_TextChanged(object sender, EventArgs e)
        {
            sqlh.xmlweight = textBoxWeight.Text;
        }

        private void textBoxMail_TextChanged(object sender, EventArgs e)
        {
            sqlh.xmlmail = textBoxMail.Text;
        }

        private void Main_Load(object sender, EventArgs e)
        {

        }

        private void label9_Click(object sender, EventArgs e)
        {

        }

        
    }
}
