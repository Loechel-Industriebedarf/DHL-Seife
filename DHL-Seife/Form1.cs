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

namespace DHL_Seife
{
    public partial class Form1 : Form
    {
        private static HttpWebRequest webRequest;
        private static string orderNumber = "";
        private static string xmluser = ""; //DHL api username / dhl business username
        private static string xmlpass = ""; //DHL api password  / dhl business password
        private static string xmlaccountnumber = ""; //DHL customer id / dhl business id
        private static string xmlaccountnumberint = ""; //DHL customer id / dhl business id international
        private static string xmlournumber = orderNumber;
        private static string xmlshippmentdate = DateTime.Now.ToString("yyyy-MM-dd"); //YYYY-MM-DD
        private static string xmlweight = "1"; //In kg
        private static string xmlmail = ""; //recipient mail
        private static string xmlrecipient = ""; //recipient name
        private static string xmlrecipient02 = ""; //recipient name (second line)
        private static string xmlrecipient03 = ""; //recipient name (third line)
        private static string xmlstreet = ""; //recipient street
        private static string xmlstreetnumber = ""; //recipient streetnumber
        private static string xmlplz = ""; //recipient plz
        private static string xmlcity = ""; //recipient city
        private static string xmlcountry = "Deutschland"; //recipient country
        private static string xmlparceltype = "V01PAK"; //Parcel type (Germany only or international)
        private static string rowid = ""; //Row ID for insert
        private static string rowidshipmentnumber = ""; //Row ID for insert
        private static string rowidcarrier = ""; //Row ID for insert
        private static string connectionString; //Connection String for Database
        private static string logfile = "log.log"; //Log file
        private static string printerName = ""; //Name of the printer to print on later
        private static string dhlsoapconnection = ""; //Connection string for the soap request
        private static string api_user = ""; //Username to connect to the api
        private static string api_password = ""; //Password to connect to the api
        private static Boolean firstrun = true; //For the logging method. If it's not the first run, don't insert additional line breaks
        private static string sqlshipmentnumber = ""; //Insert String to insert the shipment number to the database
        private static string sql_carrier_shipmentnumber = ""; //Insert String to insert the carrier number to the database



        public Form1()
        {
            //Our users tend to run the program twice, per "accident"...
            checkDoubleRun();

            //Reads settings from xml file
            readSettingsFromXML();

            //The order number can be transmitted via command line parameter
            string[] args = Environment.GetCommandLineArgs();
            Boolean parameterstart = false;
            try
            {
                if (!String.IsNullOrEmpty(args[1])) { orderNumber = args[1]; xmlournumber = args[1]; parameterstart = true; logTextToFile("> " + args[1]); }
            }
            catch(Exception ex)
            {
                orderNumber = "";
                xmlournumber = "";
                logTextToFile("> The program was started manually.");
                logTextToFile(ex.ToString());
            }
            

            InitializeComponent();

            
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
        /// Checks, how much seconds passed since the last run. If it's less than 10, don't run the program
        /// </summary>
        private void checkDoubleRun()
        {
            DateTime now = DateTime.Now;
            DateTime lastrun = Properties.Settings.Default.lastRun;
            TimeSpan diff = (now - lastrun);

            //If more than 10 seconds passed, write the new time to the settings.
            if(diff.TotalSeconds > 10)
            {
                Properties.Settings.Default.lastRun = now;
                Properties.Settings.Default.Save();
            }
            //If less than 10 seconds passed, kill the program.
            else
            {
                logTextToFile("> Less than 10 seconds passed! Double run!");
                Application.Exit();
                Environment.Exit(1);
            }

            
        }



        /// <summary>
        /// Reads the settings from the file
        /// </summary>
        private void readSettingsFromXML()
        {
            XDocument doc = XDocument.Load("var/settings.xml");
            var dbconnection = doc.Descendants("dbconnection");
            var dbrowidshipment = doc.Descendants("rowidshipment");
            var dbrowidcarrier = doc.Descendants("rowidcarrier");
            var printer = doc.Descendants("printer");
            var dhlsoap = doc.Descendants("dhlsoap");
            var api_username = doc.Descendants("api_username");
            var api_pass = doc.Descendants("api_password");
            var dhl_id = doc.Descendants("dhl_id");
            var dhl_id_int = doc.Descendants("dhl_id_int");
            var dhl_pass = doc.Descendants("dhl_password");
            var dhl_username = doc.Descendants("dhl_username");
            var insertshipmenttodb = doc.Descendants("insertshipmenttodb");
            var insertcarriertodb = doc.Descendants("insertcarriertodb");
            foreach (var foo in dbconnection) { connectionString = foo.Value; }
            foreach (var foo in dbrowidshipment) { rowidshipmentnumber = foo.Value; }
            foreach (var foo in dbrowidcarrier) { rowidcarrier = foo.Value; }
            foreach (var foo in printer) { printerName = foo.Value; }
            foreach (var foo in dhlsoap) { dhlsoapconnection = foo.Value; }
            foreach (var foo in api_username) { api_user = foo.Value; }
            foreach (var foo in api_pass) { api_password = foo.Value; }
            foreach (var foo in dhl_id) { xmlaccountnumber = foo.Value; }
            foreach (var foo in dhl_id_int) { xmlaccountnumberint = foo.Value; }
            foreach (var foo in dhl_pass) { xmlpass = foo.Value; }
            foreach (var foo in dhl_username) { xmluser = foo.Value; }
            foreach (var foo in insertshipmenttodb) { sqlshipmentnumber = foo.Value; }
            foreach (var foo in insertcarriertodb) { sql_carrier_shipmentnumber = foo.Value; }
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

            string sql = "SELECT dbo.AUFTRAGSKOPF.FSROWID, LFIRMA1, LFIRMA2, RFIRMA1, RFIRMA2, DCOMPANY3, ICOMPANY3, LSTRASSE, RSTRASSE, LPLZ, RPLZ, LORT, RORT, LLAND, RLAND, " +
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

                if (String.IsNullOrEmpty(dr["LFIRMA1"].ToString())) { xmlrecipient = removeSpecialCharacters(dr["RFIRMA1"].ToString()); }
                else { xmlrecipient = removeSpecialCharacters(dr["LFIRMA1"].ToString()); }

                if (String.IsNullOrEmpty(dr["LFIRMA2"].ToString())) { xmlrecipient02 = removeSpecialCharacters(dr["RFIRMA2"].ToString()); }
                else { xmlrecipient02 = removeSpecialCharacters(dr["LFIRMA2"].ToString()); }

                if (String.IsNullOrEmpty(dr["DCOMPANY3"].ToString())) { xmlrecipient03 = removeSpecialCharacters(dr["ICOMPANY3"].ToString()); }
                else { xmlrecipient03 = removeSpecialCharacters(dr["DCOMPANY3"].ToString()); }

                if (String.IsNullOrEmpty(dr["LSTRASSE"].ToString()))
                {
                    getStreetAndStreetnumber(dr, "RSTRASSE");
                }
                else
                {
                    getStreetAndStreetnumber(dr, "LSTRASSE");
                }

                if (String.IsNullOrEmpty(dr["LPLZ"].ToString())) { xmlplz = dr["RPLZ"].ToString().Trim(); }
                else { xmlplz = dr["LPLZ"].ToString().Trim(); }

                if (String.IsNullOrEmpty(dr["LORT"].ToString())) { xmlcity = removeSpecialCharacters(dr["RORT"].ToString().Trim()); }
                else { xmlcity = removeSpecialCharacters(dr["LORT"].ToString().Trim()); }

                //Read delivery country; If it is emty, set it to "Deutschland"
                if (!String.IsNullOrEmpty(dr["LLAND"].ToString())) { xmlcountry = dr["LLAND"].ToString().Trim(); }
                else if(!String.IsNullOrEmpty(dr["RLAND"].ToString().Trim())) {
                    xmlcountry = dr["LLAND"].ToString().Trim();
                }
                else { xmlcountry = "Deutschland"; }
                if (String.IsNullOrEmpty(xmlcountry))
                {
                    xmlcountry = "Deutschland";
                }

                //If the "CODE1" field contains an @, it is an e-mail adress.
                //If the "CODE1" field contains an amazon adress, ignore it; Amazon blocks DHL mails
                if (dr["CODE1"].ToString().Contains('@') && !dr["CODE1"].ToString().Contains("amazon")) {
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
                    logTextToFile("> Article weight or stock unit missing!");
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
        /// Figures out, if the input street contains a street number and returns the street name and number seperatly
        /// </summary>
        private static void getStreetAndStreetnumber(OdbcDataReader dr, string streetDef)
        {
            int lastindex = dr[streetDef].ToString().LastIndexOf(" ");
            int lastindexdot = dr[streetDef].ToString().LastIndexOf(".");
            int indexlength = dr[streetDef].ToString().Length;
            if (lastindex > indexlength) { lastindex = indexlength - 1; } //Fixes a problem, if last letter is a space
            //If there is no number in the string, write eveything into the street and set the street number to 0
            if (!dr[streetDef].ToString().Any(char.IsDigit))
            {
                xmlstreet = removeSpecialCharacters(dr[streetDef].ToString().Trim());
                xmlstreetnumber = "0";
            }
            //The user didn't put a space before the street number
            else if (lastindexdot > lastindex)
            {
                xmlstreet = removeSpecialCharacters(dr[streetDef].ToString().Trim().Substring(0, lastindexdot + 1).ToString());
                xmlstreetnumber = removeSpecialCharacters(dr[streetDef].ToString().Trim().Substring(lastindexdot + 1).ToString());
            }
            //"Correct" street adress
            else
            {
                xmlstreet = removeSpecialCharacters(dr[streetDef].ToString().Trim().Substring(0, lastindex + 1).ToString());
                xmlstreetnumber = removeSpecialCharacters(dr[streetDef].ToString().Trim().Substring(lastindex + 1).ToString());
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
            String newxmlmailopen = "";
            String newxmlmailclose = "";
            if (!String.IsNullOrEmpty(xmlmail) && xmlmail.Contains("@") && !xmlmail.Contains("amazon"))
            {
                newxmlmailopen = "<recipientEmailAddress>";
                newxmlmailclose = "</recipientEmailAddress>";
                newxmlmail = "<recipientEmailAddress>" + xmlmail + "</recipientEmailAddress>";
            }

            //DHL wants decimal values with dots, not commas
            if (xmlweight.Contains(','))
            {
                xmlweight = xmlweight.Replace(",", ".");
            }

            //If the country is not Germany, send an international parcel
            if (!xmlcountry.ToLower().Equals("deutschland") && !xmlcountry.ToLower().Equals("de"))
            {
                xmlparceltype = "V53WPAK";  //international parcel
                xmlaccountnumber = xmlaccountnumberint; //international account number
            }

            //If the street name contains "Packstation", we deliver to a packing station
            string packstationStart = "";
            string packstationEnd = "";
            string packstationNumber = "";
            if (xmlstreet.Contains("Packstation"))
            {
                packstationStart = "<Packstation>" +
                    "<cis:postNumber>";
                packstationEnd = "</cis:postNumber>" +
                  "</Packstation>";
                packstationNumber = xmlrecipient02;
            }


            //These values have a max length; Cut them, if they are too long
            //If recipient(01) is too long, write the rest of it to recipient02. If recipient02 is too long, write the rest to recipient03
            if (xmlrecipient.Length > 35) { xmlrecipient02 = xmlrecipient.Substring(35, xmlrecipient.Length- 35) + " " + xmlrecipient02; xmlrecipient = xmlrecipient.Substring(0, 35); }
            if (xmlrecipient02.Length > 35) { xmlrecipient03 = xmlrecipient02.Substring(35, xmlrecipient02.Length- 35) + " " + xmlrecipient03; xmlrecipient02 = xmlrecipient02.Substring(0, 35); }
            if (xmlrecipient03.Length > 35) { xmlrecipient03 = xmlrecipient.Substring(0, 35); }
            if (xmlstreet.Length > 35) { xmlstreet = xmlstreet.Substring(0, 35); }
            if (xmlstreetnumber.Length > 5) { xmlstreetnumber = xmlstreetnumber.Substring(0, 5); }
            if (xmlplz.Length > 10) { xmlplz = xmlplz.Substring(0, 10); }
            if (xmlcity.Length > 35) { xmlcity = xmlcity.Substring(0, 35); }
            if (xmlcountry.Length > 30) { xmlcountry = xmlcountry.Substring(0, 30); }
            if (newxmlmail.Length > 70) { newxmlmail = newxmlmail.Substring(0, 70); }



            webRequest = CreateWebRequest();
            XmlDocument soapEnvelopeXml = new XmlDocument();
            try
            {
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
                     {14}{3}{15}
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
                  <cis:name2>{16}</cis:name2>
                  <cis:name3>{17}</cis:name3>
                    {18}{20}{19}
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
</soapenv:Envelope>", xmluser, xmlshippmentdate, xmlweight, xmlmail, xmlrecipient, xmlstreet, xmlstreetnumber,
    xmlplz, xmlcity, xmlcountry, xmlpass, xmlaccountnumber, xmlournumber, xmlparceltype, newxmlmailopen, newxmlmailclose, xmlrecipient02, xmlrecipient03, 
    packstationStart, packstationEnd, packstationNumber);
                soapEnvelopeXml.LoadXml(xml);

            }
            catch(Exception ex)
            {
                logTextToFile(ex.ToString());
            }

            try
            {
                using (Stream stream = webRequest.GetRequestStream())
                {
                    soapEnvelopeXml.Save(stream);
                }
            }
            catch(Exception ex)
            {
                logTextToFile(ex.ToString());
            }
            
        }




        /// <summary>
        /// Sends a soap request to the dhl-api and receives an answer in xml-format.
        /// Next, it reads the xml answer and opens the labelUrl in the default web-browser.
        /// </summary>
        private static void sendSoapRequest()
        {
            try
            {
                // Get a soap response
                using (WebResponse response = webRequest.GetResponse())
                {
                    using (StreamReader rd = new StreamReader(response.GetResponseStream()))
                    {
                        string soapResult = rd.ReadToEnd();

                        //Check, if a hard validation error occurs. If yes: log it.
                        if (soapResult.Contains("Hard validation"))
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
                                labelName = "labels/" + DateTime.Now.ToString("ddMMyyyy-HHmmss") + "-" + xmlrecipient.Replace(" ", string.Empty) + ".pdf";
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
            catch(Exception ex)
            {
                logTextToFile(ex.ToString());
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

                // Print the file
                try
                {
                    PdfDocument pdfdocument = new PdfDocument();
                    pdfdocument.LoadFromFile(filepath);
                    pdfdocument.PrinterName = printerName;
                    pdfdocument.PrintDocument.PrinterSettings.Copies = 1;
                    pdfdocument.PrintDocument.Print();
                    pdfdocument.Dispose();

                }
                catch(Exception ex)
                {
                    logTextToFile(ex.ToString());
                }

                logTextToFile("> " + labelName + " successfully printed!");
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
            string sql = sqlshipmentnumber;
            sql = sql.Replace("%rowidshipmentnumber%", rowidshipmentnumber);
            sql = sql.Replace("%rowid%", rowid);
            sql = sql.Replace("%shipmentnumber%", shipmentnumber);
            string sql_carrier = sql_carrier_shipmentnumber;
            sql_carrier = sql_carrier.Replace("%rowidcarrier%", rowidcarrier);
            sql_carrier = sql_carrier.Replace("%rowid%", rowid);

            try
            {
                OdbcConnection conn = new OdbcConnection(connectionString);
                conn.Open();
                OdbcCommand comm = new OdbcCommand(sql, conn);
                OdbcCommand comm_carrier = new OdbcCommand(sql_carrier, conn);
                comm.ExecuteNonQuery();
                comm_carrier.ExecuteNonQuery();
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
            String encoded = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("iso-8859-1").GetBytes(api_user + ":" + api_password));

            //SOAP webrequest
            HttpWebRequest webRequest = null;
            try
            {
                webRequest = (HttpWebRequest)WebRequest.Create(@dhlsoapconnection);
                webRequest.Headers.Add("Authorization", "Basic " + encoded);
                webRequest.Headers.Add("SOAPAction: urn:createShipmentOrder");
                webRequest.ContentType = "text/xml;charset=\"utf-8\"";
                webRequest.Accept = "text/xml";
                webRequest.Method = "POST";
                webRequest.KeepAlive = true;                
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

        /// <summary>
        /// Logs given text to file. If it's the first log of the programs run, add a empty line and the current date.
        /// </summary>
        private static void logTextToFile(String log)
        {
            using (StreamWriter sw = File.AppendText(logfile))
            {
                if (firstrun)
                {
                    sw.WriteLine();
                    sw.WriteLine(DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
                    firstrun = false;
                }
                sw.WriteLine(log);
            }
        }

        /// <summary>
        /// This function removes all special characters from a string.
        /// </summary>
        public static string removeSpecialCharacters(string str)
        {
            return Regex.Replace(str, "[^a-zA-Z0-9äÄöÖüÜß_.]+", " ", RegexOptions.Compiled);
        }

        /// <summary>
        /// Disable reading stuff from enventa database, when no order number is given.
        /// </summary>
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

        
    }
}
