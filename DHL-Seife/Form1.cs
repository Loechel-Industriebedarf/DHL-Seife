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
        private static string xmlordertype = "1"; //Parcel type (Germany only or international)
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
        private static string xmlcommunicationmail = ""; //Mail that gets used for postfilals
        private static string sqlinsertnewmemo = ""; //Insert String to insert memo to the database



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
                if (!String.IsNullOrEmpty(args[1])) { orderNumber = args[1]; xmlournumber = args[1]; parameterstart = true; logTextToFile("> " + args[1], true); }
            }
            catch(Exception ex)
            {
                orderNumber = "";
                xmlournumber = "";
                //logTextToFile("> The program was started manually.");
                logTextToFile("> Das Programm wurde manuell gestartet.");
                logTextToFile(ex.ToString(), true);
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

            //If more than 3 seconds passed, write the new time to the settings.
            if(diff.TotalSeconds > 3)
            {
                Properties.Settings.Default.lastRun = now;
                Properties.Settings.Default.Save();
            }
            //If less than 3 seconds passed, kill the program.
            else
            {
                //logTextToFile("> Less than 3 seconds passed! Double run!");
                logTextToFile("> Doppelte Ausführung! Bitte 3 Sekunden warten.");
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
            var insertnewmemotodb = doc.Descendants("insertnewmemotodb");
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
            foreach (var foo in insertnewmemotodb) { sqlinsertnewmemo = foo.Value; }
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

            string sql = "SELECT dbo.AUFTRAGSKOPF.FSROWID, dbo.AUFTRAGSKOPF.BELEGART, LFIRMA1, LFIRMA2, RFIRMA1, RFIRMA2, DCOMPANY3, ICOMPANY3, LSTRASSE, RSTRASSE, LPLZ, RPLZ, LORT, RORT, LLAND, RLAND, " +
                "dbo.AUFTRAGSKOPF.CODE1, dbo.AUFTRAGSKOPF.BELEGNR, NetWeightPerSalesUnit, MENGE_BESTELLT, dbo.AUFTRAGSPOS.STATUS, dbo.AUFTRAGSPOS.FARTIKELNR, dbo.AUFTRAGSPOS.ARTIKELNR, " +
                "GEWICHT " +
                "FROM dbo.AUFTRAGSKOPF, dbo.AUFTRAGSPOS " +
                "LEFT JOIN dbo.VERSANDGUT ON dbo.VERSANDGUT.BELEGNR = dbo.AUFTRAGSPOS.BELEGNR " +
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
                logTextToFile(ex.ToString(), true);
            }

            xmlweight = "0";
            String xmlweightTemp = "0";
            Boolean addPackaging = true; //If the weight wasn't added manually: add extra weight for packaging later

            while (dr.Read())
            {
                rowid = dr["FSROWID"].ToString();

                xmlordertype = dr["BELEGART"].ToString();

                if (String.IsNullOrEmpty(dr["LFIRMA1"].ToString())) { xmlrecipient = removeSpecialCharacters(dr["RFIRMA1"].ToString()); }
                else { xmlrecipient = removeSpecialCharacters(dr["LFIRMA1"].ToString()); }

                if (String.IsNullOrEmpty(dr["LFIRMA2"].ToString())) { xmlrecipient02 = removeSpecialCharacters(dr["RFIRMA2"].ToString()); }
                else { xmlrecipient02 = removeSpecialCharacters(dr["LFIRMA2"].ToString()); }

                if (String.IsNullOrEmpty(dr["DCOMPANY3"].ToString())) { xmlrecipient03 = removeSpecialCharacters(dr["ICOMPANY3"].ToString()); }
                else { xmlrecipient03 = removeSpecialCharacters(dr["DCOMPANY3"].ToString()); }

                if (String.IsNullOrEmpty(dr["LSTRASSE"].ToString())){
                    getStreetAndStreetnumber(dr, "RSTRASSE");
                }
                else {
                    getStreetAndStreetnumber(dr, "LSTRASSE");
                }

                if (String.IsNullOrEmpty(dr["LPLZ"].ToString())) { xmlplz = dr["RPLZ"].ToString().Trim(); }
                else { xmlplz = dr["LPLZ"].ToString().Trim(); }
                //Check, if zip code contains letters
                if (Regex.Matches(xmlplz, @"[a-zA-Z]").Count > 0) 
                {
                    //For zips like 5051DV
                    if (!xmlplz.ToLower().Contains(' '))
                    {
                        int i = 0;
                        //Check how many chars there are at the end of the zip
                        for (i = xmlplz.Length-1; i >= 0; i--)
                        {
                            if (!char.IsDigit(xmlplz[i]))
                            {
                                break;
                            }
                        }
                        //Substring starts at 1...; isDigit starts at 0
                        i--;

                        //DHL wants zips with numbers splitted: 5051DV -> 5051 DV
                        xmlplz = xmlplz.Substring(0, i) + " " + xmlplz.Substring(i);
                    }
                }

                if (String.IsNullOrEmpty(dr["LORT"].ToString())) { xmlcity = removeSpecialCharacters(dr["RORT"].ToString().Trim()); }
                else { xmlcity = removeSpecialCharacters(dr["LORT"].ToString().Trim()); }

                //Read delivery country; If it is emty, set it to "Deutschland"
                if (!String.IsNullOrEmpty(dr["LLAND"].ToString())) { xmlcountry = dr["LLAND"].ToString().Trim(); }
                else if(!String.IsNullOrEmpty(dr["RLAND"].ToString().Trim())) {
                    xmlcountry = dr["RLAND"].ToString().Trim();
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
                xmlcommunicationmail = dr["CODE1"].ToString().Trim();


                xmlournumber = dr["BELEGNR"].ToString();
                String netWeight = dr["NetWeightPerSalesUnit"].ToString();
                String orderAmount = dr["MENGE_BESTELLT"].ToString();

                try
                {
                    Console.WriteLine(dr["GEWICHT"].ToString());

                    if (dr["GEWICHT"].ToString() == null || dr["GEWICHT"].ToString() == "") {
                        if (dr["STATUS"].ToString() == "2")
                        {       
                            xmlweight = (Convert.ToDouble(xmlweight) + Convert.ToDouble(netWeight) * Convert.ToDouble(orderAmount)).ToString();
                        }
                        else
                        {
                            //If there are no positions with status 2, just take the weight of all positions
                            xmlweightTemp = (Convert.ToDouble(xmlweightTemp) + Convert.ToDouble(netWeight) * Convert.ToDouble(orderAmount)).ToString();
                        }
                    }
                    else
                    {
                        xmlweight = dr["GEWICHT"].ToString();
                        addPackaging = false;
                    }
                }
                catch(Exception ex)
                {
                    //logTextToFile("> Article weight for "+ dr["ARTIKELNR"] + " missing!");
                    logTextToFile("> Artikelgewicht für "+ dr["ARTIKELNR"] + " fehlt!");
                    logTextToFile(ex.ToString(), true);
                }
  
            }

            //If the weight is to small, set it to zero
            if (String.IsNullOrEmpty(xmlweight) || xmlweight == "0.001")
            {
                if (Convert.ToDouble(xmlweightTemp) > 0.001)
                {
                    xmlweight = xmlweightTemp;
                }
                else
                {
                    xmlweight = "0";
                }       
            }
            //If the weight is fine and extra weight for packaging should be added, add 300 grams for packaging
            else
            {
                if (addPackaging)
                {
                    xmlweight = (Convert.ToDouble(xmlweight) + 0.3).ToString();
                }    
            }
        }

        /// <summary>
        /// Figures out, if the input street contains a street number and returns the street name and number seperatly
        /// </summary>
        private static void getStreetAndStreetnumber(OdbcDataReader dr, string streetDef) {
            string streetDefinition = dr[streetDef].ToString().Trim();
            xmlstreetnumber = "";
            xmlstreet = "";
            int lastindex = streetDefinition.LastIndexOf(" ");
            int lastindexdot = streetDefinition.LastIndexOf(".");
            int indexlength = streetDefinition.Length;

            try
            {
                //If there is no number in the string, write eveything into the street and set the street number to 0
                if (!streetDefinition.Any(char.IsDigit))
                {
                    xmlstreet = streetDefinition;
                    xmlstreetnumber = "0";
                }
                //The user didn't put a space before the street number (Teststr.123)
                //AND user didn't put a dot at the end of the adress line (Teststreet 1. | for weird people that write their adress like that...)
                else if (lastindexdot > lastindex && streetDefinition.Length - lastindexdot != 1)
                {
                    xmlstreet = streetDefinition.Substring(0, lastindexdot + 1).ToString();
                    xmlstreetnumber = streetDefinition.Substring(lastindexdot + 1).ToString();
                }
                //If the last degit of the adress is not a number (Teststreet; Teststreet 123B; Teststreet 123 B)
                else if(!char.IsDigit(streetDefinition[streetDefinition.Length - 1]))
                {
                    //There are no spaces and no numbers at the street number (Teststreet)
                    if (lastindex == -1)
                    {
                        xmlstreet = removeSpecialCharacters(streetDefinition);
                        xmlstreetnumber = "0";
                    }
                    //Last char is a letter (Teststreet 123 B)
                    else if (streetDefinition[lastindex].Equals(' ') && char.IsLetter(streetDefinition[lastindex + 1]))
                    {
                        xmlstreet = streetDefinition.Substring(0, lastindex).ToString();
                        int lastindexnew = xmlstreet.LastIndexOf(" ");
                        xmlstreet = streetDefinition.Substring(0, lastindexnew + 1).ToString();
                        xmlstreetnumber = streetDefinition.Substring(lastindexnew + 1).ToString();
                    }
                    //Last char is a letter and no spaces between streetnumber and letter (Teststreet 123B)
                    else
                    {
                        xmlstreet = streetDefinition.Substring(0, lastindex + 1).ToString();
                        xmlstreetnumber = streetDefinition.Substring(lastindex + 1).ToString();
                    }       
                }
                //"Correct" street adress (Test street 123; Teststreet 123)
                //OR adress without spaces in the end (Teststreet123; Test street123)
                else
                {
                    int i = 1;
                    //The street number cannot contain more than 5 numbers
                    for (i = 1; i <= 5; i++)
                    {
                        //Fix for street numbers like 25-26
                        if (char.IsDigit(streetDefinition[streetDefinition.Length - i]) || streetDefinition[streetDefinition.Length - i].Equals('-'))
                        {
                            //Add the last digit to the end of the street number
                            xmlstreetnumber = streetDefinition[streetDefinition.Length - i].ToString() + xmlstreetnumber;
                        }
                        else
                        {
                            //If last number is actually a letter, just set the streetnumber to 0
                            if(i == 1)
                            {
                                xmlstreetnumber = "0";
                            }
                            //If there is no more number, break the loop
                            break;
                        }
                    }
                    
                    xmlstreet = streetDefinition.Substring(0, streetDefinition.Length - i + 1);
                }

                xmlstreet = xmlstreet.Trim();
                xmlstreet = removeSpecialCharacters(xmlstreet);
                if (String.IsNullOrEmpty(xmlstreetnumber))
                {
                    xmlstreetnumber = "0";
                }
            }
            catch(Exception ex)
            {
                xmlstreet = removeSpecialCharacters(streetDefinition);
                xmlstreetnumber = "0";
                logTextToFile(ex.ToString(), true);
            }

            //People don't like to write the word "street" completely
            // xmlstreet = xmlstreet.Replace("str.", "straße");
            // xmlstreet = xmlstreet.Replace("Str.", "Straße");
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
            string postFiliale = "";
            if (xmlstreet.ToLower().Contains("dhl-packstation"))
            {
                xmlstreet = xmlstreet.Replace("dhl-", "");
            }
            else if (xmlstreet.ToLower().Contains("dhl packstation"))
            {
                xmlstreet = xmlstreet.Replace("dhl ", "");
            }
            if (xmlstreet.ToLower().Contains("packstation"))
            {
                packstationStart = "<Packstation>" +
                    "<cis:postNumber>";
                packstationEnd = "</cis:postNumber>" +
                  "</Packstation>";
                if (!String.IsNullOrEmpty(xmlrecipient02))
                {
                    packstationNumber = Regex.Replace(xmlrecipient02, @"[^0-9]", "").Trim(); //For people who write additional words in the packstation number field; Only allows numbers
                }
                else
                {
                    packstationNumber = Regex.Replace(xmlrecipient03, @"[^0-9]", "").Trim(); //For people who write additional words in the packstation number field; Only allows numbers
                }
            }
            if (xmlstreet.ToLower().Contains("postfiliale"))
            {
                postFiliale = "<Communication>" +
                    "<cis:email>" + xmlcommunicationmail + "</cis:email>" +
                    "</Communication>" +
                    "<Postfiliale>" +
                    "<cis:postfilialNumber>" + xmlstreetnumber +
                    "</cis:postfilialNumber>" +
                  "</Postfiliale>";
            }
                xmlrecipient = xmlrecipient + " " + xmlrecipient02 + " " + xmlrecipient03; //Combines the recipients for unneccessary use of multiple fields


            //These values have a max length; Cut them, if they are too long
            //If recipient(01) is too long, write the rest of it to recipient02. If recipient02 is too long, write the rest to recipient03
            if (xmlrecipient.Length > 35) { xmlrecipient02 = xmlrecipient.Substring(35, xmlrecipient.Length- 35) + " " + xmlrecipient02; xmlrecipient = xmlrecipient.Substring(0, 35); }
            if (xmlrecipient02.Length > 35) { xmlrecipient03 = xmlrecipient02.Substring(35, xmlrecipient02.Length- 35) + " " + xmlrecipient03; xmlrecipient02 = xmlrecipient02.Substring(0, 35); }
            if (xmlrecipient03.Length > 35) { xmlrecipient03 = xmlrecipient03.Substring(0, 35); }
            if (xmlstreet.Length > 35) { xmlstreet = xmlstreet.Substring(0, 35); }
            if (xmlstreetnumber.Length > 5) { xmlstreetnumber = xmlstreetnumber.Substring(0, 5); }
            if (xmlplz.Length > 10) { xmlplz = xmlplz.Substring(0, 10); }
            if (xmlcity.Length > 35) { xmlcity = xmlcity.Substring(0, 35); }
            if (xmlcountry.Length > 30) { xmlcountry = xmlcountry.Substring(0, 30); }
            if (newxmlmail.Length > 70) { newxmlmail = newxmlmail.Substring(0, 70); }
            try
            {
                double weight = Convert.ToDouble(xmlweight.Replace(".",","));
                if (weight > 30) { xmlweight = "30"; }
                else if (weight <= 0.01) { xmlweight = "4";  }
                else if (weight < 0.1) { xmlweight = "0.1"; }
            }
            catch(Exception ex)
            {
                logTextToFile(ex.ToString(), true);
            }
            



            webRequest = CreateWebRequest();
            XmlDocument soapEnvelopeXml = new XmlDocument();
            try
            {
                string senderName = "";
                string senderStreetName = "";
                string senderStreetNumber = "";
                string senderZip = "";
                string senderCity =  "";
                string senderNumber =  "";
                if (xmlordertype.Equals("10"))
                {
                    senderName = "Mercateo Deutschland AG";
                    senderStreetName = "Museumsgasse";
                    senderStreetNumber = "4-5";
                    senderZip = "06366";
                    senderCity = "Köthen";
                    senderNumber = "+49 89 12 140 777";
                }
                else
                {
                    senderName = "Löchel Industriebedarf";
                    senderStreetName = "Hans-Hermann-Meyer-Strasse";
                    senderStreetNumber = "2";
                    senderZip = "27232";
                    senderCity = "Sulingen";
                    senderNumber = "+49 4271 5727";
                }


                //Starts at 0, the string variables on the bottom are groups of five
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
                     <cis:name1>{21}</cis:name1>
                  </Name>
                  <Address>
                     <cis:streetName>{22}</cis:streetName>
                     <cis:streetNumber>{23}</cis:streetNumber>
                     <cis:zip>{24}</cis:zip>
                     <cis:city>{25}</cis:city>    
                     <cis:Origin>
                        <cis:country>Deutschland</cis:country>
                     </cis:Origin>
                  </Address>
                  <Communication>
                  <cis:phone>{26}</cis:phone>
                  </Communication>
               </Shipper>
               <Receiver>
                  <cis:name1>{4}</cis:name1>
                  <cis:name2>{16}</cis:name2>
                  <cis:name3>{17}</cis:name3>
                    {18}{20}{19}{27}
                  <Address>
                     <cis:streetName>{5}</cis:streetName>
                     <cis:streetNumber>{6}</cis:streetNumber>
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
</soapenv:Envelope>", xmluser, xmlshippmentdate, xmlweight, xmlmail, xmlrecipient, xmlstreet, 
xmlstreetnumber, xmlplz, xmlcity, xmlcountry, xmlpass, 
xmlaccountnumber, xmlournumber, xmlparceltype, newxmlmailopen, newxmlmailclose, 
xmlrecipient02, xmlrecipient03, packstationStart, packstationEnd, packstationNumber, 
senderName, senderStreetName, senderStreetNumber, senderZip, senderCity, 
senderNumber, postFiliale);
                soapEnvelopeXml.LoadXml(xml);

                Console.WriteLine(xml);
            }
            catch(Exception ex)
            {
                //logTextToFile("> XML error!");
                logTextToFile("> XML Fehler!");
                logTextToFile(ex.ToString(), true);
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
        static int apiConnectTries = 0; //If the connection to the api fails, it should try again.
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
                            //logTextToFile("Critical adress-error!");
                            logTextToFile("> Kritischer Adressfehler!");
                            logTextToFile(soapResult, true);
                        }
                        else if (soapResult.Contains("Weak validation"))
                        {
                            //logTextToFile("You'll have to pay 'Leitcodenachentgelt' for this order!");
                            logTextToFile("> Leitcodenachentgelt muss für diesen Auftrag bezahlt werden!");
                            logTextToFile(soapResult, true);
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
                                labelName = "labels/" + DateTime.Now.ToString("ddMMyyyy-HHmmss") + "-" + xmlrecipient.Replace(" ", string.Empty).Replace("/", string.Empty).Replace("\\", string.Empty) + ".pdf";
                                Client.DownloadFile(labelUrl, @labelName);
                            }
                            catch (Exception ex)
                            {
                                logTextToFile(ex.ToString(), true);
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
            catch (WebException ex)
            {
                //Log the error message of the WebException
                using (WebResponse response = ex.Response)
                {
                    HttpWebResponse httpResponse = (HttpWebResponse)response;
                    Console.WriteLine("Error code: {0}", httpResponse.StatusCode);
                    using (Stream data = response.GetResponseStream())
                    using (var reader = new StreamReader(data))
                    {
                        string text = reader.ReadToEnd();
                        //logTextToFile("> Error while connecting to DHL-API!");
                        logTextToFile("> Fehler bei der Verbindung mit der DHL-API!");
                        logTextToFile(text, true);
                    }
                }
            }
            catch (Exception ex)
            {
                //logTextToFile("> Error while connecting to DHL-API!");
                logTextToFile("> Fehler bei der Verbindung mit der DHL-API!");
                logTextToFile(ex.ToString(), true);

                apiConnectTries++;
                //If there is an error while connecting to the api, try again 3 times
                if(apiConnectTries <= 3)
                {
                    System.Threading.Thread.Sleep(5000);
                    sendSoapRequest();
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
                    logTextToFile(ex.ToString(), true);
                }

                //logTextToFile("> " + labelName + " successfully printed!");
                logTextToFile("> " + labelName + " wurde erfolgreich gedruckt!");
            }
            catch (Exception ex)
            {
                logTextToFile(ex.ToString(), true);
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
                logTextToFile(ex.ToString(), true);
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
                logTextToFile(ex.ToString(), true);
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
            logTextToFile(log, false);
        }
        private static void logTextToFile(String log, Boolean nl)
        {
            //Write to database
            try
            {
                string sql = sqlinsertnewmemo + " + '" + log;
                if (nl)
                {
                    sql += "\r\n";
                }
                sql += "\r\n' WHERE BelegNr = '" + orderNumber + "'";
                OdbcConnection conn = new OdbcConnection(connectionString);
                conn.Open();
                OdbcCommand comm = new OdbcCommand(sql, conn);
                comm.ExecuteNonQuery();
            }
            catch (Exception ex)
            {

            }

            //Write to log file
            using (StreamWriter sw = File.AppendText(logfile))
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

        /// <summary>
        /// This function removes all special characters from a string.
        /// </summary>
        public static string removeSpecialCharacters(string str)
        {
            return Regex.Replace(str, @"[^a-zA-Z0-9äÄöÖüÜß\/\-_.]+", " ", RegexOptions.Compiled);
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
