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
        public string connectionString { get; set; } //Connection String for Database
        public string rowidshipmentnumber { get; set; } //Row ID for insert
        public string rowidcarrier { get; set; } //Row ID for insert
        public string printerName { get; set; } //Name of the printer to print on later
        public string dhlsoapconnection { get; set; } //Connection string for the soap request
        public string api_user { get; set; }//Username to connect to the api
        public string api_password { get; set; } //Password to connect to the api
        public string xmlaccountnumber { get; set; } //DHL customer id / dhl business id
        public string xmlaccountnumberint { get; set; } //DHL customer id / dhl business id international
        public string xmlpass { get; set; } //DHL api password  / dhl business password
        public string xmluser { get; set; } //DHL api username / dhl business username
        public string sqlshipmentnumber { get; set; } //Insert String to insert the shipment number to the database
        public string sql_carrier_shipmentnumber { get; set; } //Insert String to insert the carrier number to the database
        public string sqlinsertnewmemo { get; set; } //Insert String to insert memo to the database
        public string sqlinsertnewtermin { get; set; } //Insert String to insert termin to the database

        public SettingsReader()
        {
            readSettings();
        }

        private void readSettings()
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
            var insertnewtermin = doc.Descendants("insertnewtermin");
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
            foreach (var foo in insertnewtermin) { sqlinsertnewtermin = foo.Value; }
        }
    }
}
