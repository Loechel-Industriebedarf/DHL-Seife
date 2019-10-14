using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DHL_Seife.util
{
    public class SettingsReader
    {
        public string ConnectionString { get; set; } //Connection String for Database
        public string RowIdShipmentnumber { get; set; } //Row ID for insert
        public string RowIdCarrier { get; set; } //Row ID for insert
        public string PrinterName { get; set; } //Name of the printer to print on later
        public string DHLSoapConnection { get; set; } //Connection string for the soap request
        public string ApiUser { get; set; }//Username to connect to the api
        public string ApiPassword { get; set; } //Password to connect to the api
        public string XmlAccountnumber { get; set; } //DHL customer id / dhl business id
        public string XmlAccountnumberInt { get; set; } //DHL customer id / dhl business id international
        public string XmlPass { get; set; } //DHL api password  / dhl business password
        public string XmlUser { get; set; } //DHL api username / dhl business username
        public string SqlShipmentnumber { get; set; } //Insert String to insert the shipment number to the database
        public string SqlCarrierShipmentnumber { get; set; } //Insert String to insert the carrier number to the database
        public string SqlInsertNewMemo { get; set; } //Insert String to insert memo to the database
        public string SqlInsertNewTermin { get; set; } //Insert String to insert termin to the database
        public string Logfile { get; set; } //Log file

        public string OrderNumber{ get; set; } //The number of the order



        public SettingsReader()
        {
            ReadSettings();
        }




        /// <summary>
        /// Reads all settings from xml file and saves them to variables.
        /// </summary>
        private void ReadSettings()
        {
            XDocument doc = XDocument.Load("var/settings.xml");
            Logfile = "log.log"; //TODO: Put in settings.xml

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
            var insertnewtermin = doc.Descendants("insertnewtermin");
            foreach (var foo in dbconnection) { ConnectionString = foo.Value; }
            foreach (var foo in dbrowidshipment) { RowIdShipmentnumber = foo.Value; }
            foreach (var foo in dbrowidcarrier) { RowIdCarrier = foo.Value; }
            foreach (var foo in printer) { PrinterName = foo.Value; }
            foreach (var foo in dhlsoap) { DHLSoapConnection = foo.Value; }
            foreach (var foo in api_username) { ApiUser = foo.Value; }
            foreach (var foo in api_pass) { ApiPassword = foo.Value; }
            foreach (var foo in dhl_id) { XmlAccountnumber = foo.Value; }
            foreach (var foo in dhl_id_int) { XmlAccountnumberInt = foo.Value; }
            foreach (var foo in dhl_pass) { XmlPass = foo.Value; }
            foreach (var foo in dhl_username) { XmlUser = foo.Value; }
            foreach (var foo in insertshipmenttodb) { SqlShipmentnumber = foo.Value; }
            foreach (var foo in insertcarriertodb) { SqlCarrierShipmentnumber = foo.Value; }
            foreach (var foo in insertnewmemotodb) { SqlInsertNewMemo = foo.Value; }
            foreach (var foo in insertnewtermin) { SqlInsertNewTermin = foo.Value; }
        }
    }
}
