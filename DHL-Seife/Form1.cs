using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Odbc;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using Microsoft.VisualBasic;
using System.Threading;
using System.Diagnostics;
using RawPrint;
using System.Xml.Linq;

namespace DHL_Seife
{
    public partial class Form1 : Form
    {
        private static HttpWebRequest request;
        private static string orderNumber = "";
        private static string xmluser = ""; //DHL api username / dhl business username
        private static string xmlpass = ""; //DHL api password  / dhl business password
        private static string xmlaccountnumber = ""; //DHL customer id / dhl business id
        private static string xmlournumber = orderNumber;
        private static string xmlshippmentdate = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd"); //YYYY-MM-DD
        private static string xmlweight = "1"; //In kg
        private static string xmlmail = ""; //recipient mail
        private static string xmlrecipient = ""; //recipient name
        private static string xmlstreet = ""; //recipient street
        private static string xmlstreetnumber = ""; //recipient streetnumber
        private static string xmlplz = ""; //recipient plz
        private static string xmlcity = ""; //recipient city
        private static string xmlcountry = "Deutschland"; //recipient country
        private static string xmlparceltype = "V01PAK"; //Parcel type (Germany only or international)
        private static string rowid = ""; //Row ID for insert
        private static string rowidshipmentnumber = "{BEF38EDC-2DBF-11E8-949A-000C29018628}"; //Row ID for insert
        private static string connectionString; //Connection String for Database
        private static string logfile = "log.log"; //Log file
        private static string printerName = ""; //Name of the printer to print on later
        private static string dhlsoapconnection = ""; //Connection string for the soap request
        private static string api_user = ""; //Username to connect to the api
        private static string api_password = ""; //Password to connect to the api

        

        public Form1()
        {
            //Reads settings from xml file
            readSettingsFromXML();

            //The order number can be transmitted via command line parameter
            string[] args = Environment.GetCommandLineArgs();
            Boolean parameterstart = false;
            try
            {
                if (!String.IsNullOrEmpty(args[1])) { orderNumber = args[1]; xmlournumber = args[1]; parameterstart = true; }
            }
            catch(Exception ex)
            {
                orderNumber = "";
                xmlournumber = "";
                logTextToFile(ex.ToString());
            }
            

            InitializeComponent();

            try
            {
                connectionString = System.IO.File.ReadAllText(@"var/dbconnection.txt");
            }
            catch (Exception ex)
            {
                logTextToFile(ex.ToString());
            }
            
            if (!String.IsNullOrEmpty(xmlournumber))
            {
                doSQLMagic(printShippingLabel);
                printShippingLabel.Enabled = true;
                //If the program was started via a parameter, skip the whole gui thing
                if (parameterstart)
                {
                    doXMLMagic();
                    sendSoapRequest();
                    Application.Exit();
                    Environment.Exit(1);
                }
            }
            else
            {
                printManualShippingLabel.Visible = true;
            }
            writeToGui();
        }



        /// <summary>
        /// Reads the settings from the file
        /// </summary>
        private void readSettingsFromXML()
        {
            XDocument doc = XDocument.Load("var/settings.xml");
            var dbconnection = doc.Descendants("dbconnection");
            var printer = doc.Descendants("printer");
            var dhlsoap = doc.Descendants("dhlsoap");
            var api_username = doc.Descendants("api_username");
            var api_pass = doc.Descendants("api_password");
            var dhl_id = doc.Descendants("dhl_id");
            var dhl_pass = doc.Descendants("dhl_password");
            var dhl_username = doc.Descendants("dhl_username");
            foreach (var foo in dbconnection) { connectionString = foo.Value; }
            foreach (var foo in printer) { printerName = foo.Value; }
            foreach (var foo in dhlsoap) { dhlsoapconnection = foo.Value; }
            foreach (var foo in api_username) { api_user = foo.Value; }
            foreach (var foo in api_pass) { api_password = foo.Value; }
            foreach (var foo in dhl_id) { xmlaccountnumber = foo.Value; }
            foreach (var foo in dhl_pass) { xmlpass = foo.Value; }
            foreach (var foo in dhl_username) { xmluser = foo.Value; }
        }



        /// <summary>
        /// Inserts the different variables into the gui.
        /// </summary>
        private void writeToGui()
        {
            orderNumber = xmlournumber;
            textBoxOrdernumber.Text = xmlournumber;
            textBoxRecepient.Text = xmlrecipient;
            textBoxStreet.Text = xmlstreet;
            textBoxStreetNumber.Text = xmlstreetnumber;
            textBoxPLZ.Text = xmlplz;
            textBoxCity.Text = xmlcity;
            textBoxCountry.Text = xmlcountry;
            textBoxWeight.Text = xmlweight;
            textBoxMail.Text = xmlmail;
        }

        /// <summary>
        /// Connects to the sql server and reads the needed variables.
        /// </summary>
        private static void doSQLMagic(Button printShippingLabel)
        {
            printShippingLabel.Text = "Versandlabel drucken";

            string sql = "SELECT dbo.AUFTRAGSKOPF.FSROWID, LFIRMA1, RFIRMA1, LSTRASSE, RSTRASSE, LPLZ, RPLZ, LORT, RORT, LLAND, RLAND, " +
                "dbo.AUFTRAGSKOPF.CODE1, dbo.AUFTRAGSKOPF.BELEGNR, NetWeightPerSalesUnit, MENGE_BESTELLT " +
                "FROM dbo.AUFTRAGSKOPF, dbo.AUFTRAGSPOS " +
                "WHERE dbo.AUFTRAGSKOPF.BELEGNR = '" + xmlournumber + "' AND dbo.AUFTRAGSPOS.BELEGNR = '" + xmlournumber + "'";

            OdbcDataReader dr = null;
            try { 
                OdbcConnection conn = new OdbcConnection(connectionString);
                conn.Open();
                OdbcCommand comm = new OdbcCommand(sql, conn);
                dr = comm.ExecuteReader();
            }
            catch (Exception ex)
            {
                logTextToFile(ex.ToString());
            }

            xmlweight = "0";

            while (dr.Read())
            {
                rowid = dr["FSROWID"].ToString();

                if (String.IsNullOrEmpty(dr["LFIRMA1"].ToString())) { xmlrecipient = dr["RFIRMA1"].ToString(); }
                else { xmlrecipient = dr["LFIRMA1"].ToString(); }

                if (String.IsNullOrEmpty(dr["LSTRASSE"].ToString()))
                {
                    xmlstreet = dr["RSTRASSE"].ToString().Trim().Substring(0, dr["RSTRASSE"].ToString().LastIndexOf(" ") + 1).ToString();
                    xmlstreetnumber = dr["RSTRASSE"].ToString().Trim().Substring(dr["RSTRASSE"].ToString().LastIndexOf(" ") + 1).ToString();
                }
                else
                {
                    xmlstreet = dr["LSTRASSE"].ToString().Trim().Substring(0, dr["LSTRASSE"].ToString().LastIndexOf(" ") + 1).ToString();
                    xmlstreetnumber = dr["LSTRASSE"].ToString().Trim().Substring(dr["LSTRASSE"].ToString().LastIndexOf(" ") + 1).ToString();
                }

                if (String.IsNullOrEmpty(dr["LPLZ"].ToString())) { xmlplz = dr["RPLZ"].ToString().Trim(); }
                else { xmlplz = dr["LPLZ"].ToString().Trim(); }

                if (String.IsNullOrEmpty(dr["LORT"].ToString())) { xmlcity = dr["RORT"].ToString().Trim(); }
                else { xmlcity = dr["LORT"].ToString().Trim(); }

                if (!String.IsNullOrEmpty(dr["LLAND"].ToString())) { xmlcountry = dr["LLAND"].ToString().Trim(); }
                else if(!String.IsNullOrEmpty(dr["RLAND"].ToString().Trim())) {
                    xmlcountry = dr["LLAND"].ToString().Trim();
                }
                else { xmlcountry = "Deutschland"; }
                if (String.IsNullOrEmpty(xmlcountry))
                {
                    xmlcountry = "Deutschland";
                }

                if (dr["CODE1"].ToString().Contains('@')) {
                    xmlmail = dr["CODE1"].ToString().Trim();
                }
                

                xmlournumber = dr["BELEGNR"].ToString();
                String netWeight = dr["NetWeightPerSalesUnit"].ToString();
                String orderAmount = dr["MENGE_BESTELLT"].ToString();

                try
                {
                    xmlweight = (Convert.ToDouble(xmlweight) + Convert.ToDouble(netWeight) * Convert.ToDouble(orderAmount)).ToString();
                }
                catch(Exception ex)
                {
                    logTextToFile(ex.ToString());
                }
  
            }

            //Weight must be greater than 0
            if (String.IsNullOrEmpty(xmlweight) || xmlweight == "0")
            {
                xmlweight = "1";
            }
        }


        /// <summary>
        /// Create a xml-string from the inputs the user made erlier.
        /// This xml will be sent as soap request to the dhl server.
        /// </summary>
        private static void doXMLMagic()
        {
            //E-Mail is not a needed thing for the dhl-xml
            String newxmlmail = "";
            if (!String.IsNullOrEmpty(xmlmail))
            {
                newxmlmail = "<recipientEmailAddress>" + xmlmail + "</recipientEmailAddress>";
            }

            //DHL wants decimal values with dots, not commas
            if (xmlweight.Contains(','))
            {
                xmlweight = xmlweight.Replace(",", ".");
            }

            if (!xmlcountry.ToLower().Equals("deutschland") && !xmlcountry.ToLower().Equals("de"))
            {
                xmlparceltype = "V53WPAK";  //international parcel
                xmlaccountnumber = "22222222225301"; //international account number
            }



            request = CreateWebRequest();
            XmlDocument soapEnvelopeXml = new XmlDocument();
            String xml = String.Format(@"<?xml version=""1.0"" encoding=""utf-8""?>
                <soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:cis=""http://dhl.de/webservice/cisbase"" xmlns:bus=""http://dhl.de/webservices/businesscustomershipping"">
   <soapenv:Header>
      <cis:Authentification>
         <cis:user>{0}</cis:user>
         <cis:signature>{10}</cis:signature>
      </cis:Authentification>
   </soapenv:Header>
   <soapenv:Body>
      <bus:CreateShipmentOrderRequest>
         <bus:Version>
            <majorRelease>2</majorRelease>
            <minorRelease>0</minorRelease>
         </bus:Version>
         <ShipmentOrder>
            <sequenceNumber>01</sequenceNumber>
            <Shipment>
               <ShipmentDetails>
                  <product>{13}</product>
                  <cis:accountNumber>{11}</cis:accountNumber>
                  <customerReference>{12}</customerReference>
                  <shipmentDate>{1}</shipmentDate>
                  <ShipmentItem>
                     <weightInKG>{2}</weightInKG>
                  </ShipmentItem>
                  <Notification>
                     {3}
                  </Notification>
               </ShipmentDetails>
               <Shipper>
                  <Name>
                     <cis:name1>Löchel Industriebedarf</cis:name1>
                  </Name>
                  <Address>
                     <cis:streetName>Hans-Hermann-Meyer-Strasse</cis:streetName>
                     <cis:streetNumber>2</cis:streetNumber>
                     <cis:addressAddition>?</cis:addressAddition>
                     <cis:zip>27232</cis:zip>
                     <cis:city>Sulingen</cis:city>    
                     <cis:Origin>
                        <cis:country>Deutschland</cis:country>
                     </cis:Origin>
                  </Address>
                  <Communication>
                  <cis:phone>+49 4271 5727</cis:phone>
                  </Communication>
               </Shipper>
               <Receiver>
                  <cis:name1>{4}</cis:name1>
                  <Address>
                     <cis:streetName>{5}</cis:streetName>
                     <cis:streetNumber>{6}</cis:streetNumber>
                     <cis:addressAddition>?</cis:addressAddition>
                     <cis:zip>{7}</cis:zip>
                     <cis:city>{8}</cis:city>
                     <cis:Origin>
                        <cis:country>{9}</cis:country>
                     </cis:Origin>
                  </Address>
                  <Communication>
                  </Communication>
               </Receiver>
            </Shipment>
         </ShipmentOrder>
      </bus:CreateShipmentOrderRequest>
   </soapenv:Body>
</soapenv:Envelope>", xmluser, xmlshippmentdate, xmlweight, newxmlmail, xmlrecipient, xmlstreet, xmlstreetnumber, xmlplz, xmlcity, xmlcountry, xmlpass, xmlaccountnumber, xmlournumber, xmlparceltype);
            soapEnvelopeXml.LoadXml(xml);

            using (Stream stream = request.GetRequestStream())
            {
                soapEnvelopeXml.Save(stream);
            }
        }




        /// <summary>
        /// Sends a soap request to the dhl-api and receives an answer in xml-format.
        /// Next, it reads the xml answer and opens the labelUrl in the default web-browser.
        /// </summary>
        private static void sendSoapRequest()
        {
            // Get a soap response
            using (WebResponse response = request.GetResponse())
            {
                using (StreamReader rd = new StreamReader(response.GetResponseStream()))
                {
                    string soapResult = rd.ReadToEnd();

                    //Check, if a hard validation error occurs. If yes: log it.
                    if(soapResult.Contains("Hard validation"))
                    {
                        logTextToFile(soapResult);
                    }

                    XmlDocument xmldoc = new XmlDocument();
                    xmldoc.LoadXml(soapResult);

                    XmlNodeList xnList = xmldoc.GetElementsByTagName("labelUrl");
                    foreach (XmlNode xn in xnList)
                    {
                        string labelUrl = xn.InnerText;
                        //Download label and save it to file
                        string labelName = "";
                        try
                        {
                            WebClient Client = new WebClient();
                            labelName = "labels/" + DateTime.Now.ToString("ddMMyyyy-HHmm") + "-" + xmlrecipient.Replace(" ", string.Empty) + ".pdf";
                            Client.DownloadFile(labelUrl, @labelName);
                        }
                        catch (Exception ex)
                        {
                            logTextToFile(ex.ToString());
                        }
                        //Print label
                        printLabel(labelName);
                    }

                    xnList = xmldoc.GetElementsByTagName("cis:shipmentNumber");
                    foreach (XmlNode xn in xnList)
                    {
                        string shipmentnumber = xn.InnerText;
                        writeShipmentNumber(shipmentnumber);
                    }
                }
            }
        }

        /// <summary>
        /// Prints the shipping label. The printers name is saved in a txt file.
        /// </summary>
        private static void printLabel(string labelName)
        {
            try
            {
                string filepath = labelName;

                //Filename to be shown in print-queue
                string filename = "label-" + DateTime.Now.ToString("yyyy-MM-dd-HHmmss") + ".pdf" ;
                

                // Create an instance of the Printer
                IPrinter printer = new Printer();

                // Print the file
                printer.PrintRawFile(printerName, filepath, filename);
            }
            catch (Exception ex)
            {
                logTextToFile(ex.ToString());
            }

            
        }




        /// <summary>
        /// Writes shipment-number to the database.
        /// </summary>
        private static void writeShipmentNumber(string shipmentnumber)
        {
            string sql = "INSERT INTO [LOE01].[dbo].[AdditionalFieldValue] (FSROWVERSION, DefRowID, TableRowID, ValueString) VALUES " +
                "('0', '" + rowidshipmentnumber + "', '" + rowid + "', '" + shipmentnumber + "')";
            try
            {
                OdbcConnection conn = new OdbcConnection(connectionString);
                conn.Open();
                OdbcCommand comm = new OdbcCommand(sql, conn);
                comm.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                logTextToFile(ex.ToString());
            }
            
        }


        /// <summary>
        /// Create a soap webrequest to to the dhl-api. Also adds basic http-authentication.
        /// </summary>
        public static HttpWebRequest CreateWebRequest()
        {
            String encoded = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(api_user + ":" + api_password));

            //SOAP webrequest
            HttpWebRequest webRequest = null;
            try
            {
                webRequest = (HttpWebRequest)WebRequest.Create(dhlsoapconnection);
                webRequest.Headers.Add("Authorization", "Basic " + encoded);
                webRequest.Headers.Add(@"SOAP:Action");
                webRequest.ContentType = "text/xml;charset=\"utf-8\"";
                webRequest.Accept = "text/xml";
                webRequest.Method = "POST";
            }
            catch (Exception ex)
            {
                logTextToFile(ex.ToString());
            }
            return webRequest;
        }



        /// <summary>
        /// Primary button to create a shipping label.
        /// If no order number was transmitted (via parameter), the button acts as "get data from Enventa"-button.
        /// </summary>
        private void printShippingLabel_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(orderNumber))
            {
                doSQLMagic(printShippingLabel);
                writeToGui();
                printManualShippingLabel.Visible = false;
            }
            else
            {
                doXMLMagic();
                sendSoapRequest();
                Application.Exit();
            }
        }

        /// <summary>
        /// This button only appears, if no data from Enventa was read. It starts the label-printing.
        /// </summary>
        private void printManualShippingLabel_Click(object sender, EventArgs e)
        {
            doXMLMagic();
            sendSoapRequest();
            Application.Exit();
        }

        private void textBoxOrdernumber_TextChanged(object sender, EventArgs e)
        {
            xmlournumber = textBoxOrdernumber.Text;
            if (String.IsNullOrEmpty(xmlournumber)) { printShippingLabel.Enabled = false; } else { printShippingLabel.Enabled = true; }
        }

        private void textBoxRecepient_TextChanged(object sender, EventArgs e)
        {
            xmlrecipient = textBoxRecepient.Text;
        }

        private void textBoxStreet_TextChanged(object sender, EventArgs e)
        {
            xmlstreet = textBoxStreet.Text;
        }

        private void textBoxStreetNumber_TextChanged(object sender, EventArgs e)
        {
            xmlstreetnumber = textBoxStreetNumber.Text;
        }

        private void textBoxPLZ_TextChanged(object sender, EventArgs e)
        {
            xmlplz = textBoxPLZ.Text;
        }

        private void textBoxCity_TextChanged(object sender, EventArgs e)
        {
            xmlcity = textBoxCity.Text;
        }

        private void textBoxCountry_TextChanged(object sender, EventArgs e)
        {
            xmlcountry = textBoxCountry.Text;
        }

        private void textBoxWeight_TextChanged(object sender, EventArgs e)
        {
            xmlweight = textBoxWeight.Text;
        }

        private void textBoxMail_TextChanged(object sender, EventArgs e)
        {
            xmlmail = textBoxMail.Text;
        }

        private void Main_Load(object sender, EventArgs e)
        {

        }

        private void label9_Click(object sender, EventArgs e)
        {

        }


        private static void logTextToFile(String log)
        {
            using (StreamWriter sw = File.AppendText(logfile))
            {
                sw.WriteLine();
                sw.WriteLine(DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss"));
                sw.WriteLine(log);
            }
        }
    }
}
