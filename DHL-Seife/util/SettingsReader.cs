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
		public string PrinterName2 { get; set; } //Name of the printer to print on later
        public string printLabels { get; set; } //If false: don't print; if true: print the labels after download
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
        public string LogfileCsv { get; set; } //Log file
        public string ProgramUser { get; set; } //User that executed the program

		public string OrderNumber { get; set; } //The number of the order

		public string senderName { get; set; }
		public string senderName2 { get; set; }
		public string senderName3 { get; set; }
		public string senderStreetName { get; set; }
		public string senderStreetNumber { get; set; }
		public string senderZip { get; set; }
		public string senderCity { get; set; }
		public string senderNumber { get; set; }
		public string senderMail { get; set; }
		public string newxmlmail { get; set; }

		public string OrderType { get; set; } //DHL or DPD?

		//DPD specific
		public string DPDId { get; set; } //DPD id
		public string DPDPassword { get; set; } //DPD password
		public string DPDCustomerNumber { get; set; } //DPD customer number
		public string DPDSoapAuth { get; set; } //Path to authservice
		public string DPDSoapLabel { get; set; } //Path to storeOrders / label printing service
		public string DPDAuthToken { get; set; } //The number of the order
		public string DPDDepotNumber { get; set; } //The number of the order

        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset LabelTime { get; set; }




        public SettingsReader()
		{
			ReadSettings();
		}




		/// <summary>
		/// Reads all settings from the xml settings-file and saves them to variables.
		/// </summary>
		private void ReadSettings()
		{
			XDocument doc = XDocument.Load("var/settings.xml");

			var logfile = doc.Descendants("logfile");
            var logfilecsv = doc.Descendants("csvlogfile");
            var dbconnection = doc.Descendants("dbconnection");
			var dbrowidshipment = doc.Descendants("rowidshipment");
			var dbrowidcarrier = doc.Descendants("rowidcarrier");
			var printer = doc.Descendants("printer");
			var printer2 = doc.Descendants("printer2");
			var printL = doc.Descendants("printLabels");
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
			var dpd_id = doc.Descendants("dpd_id");
			var dpd_password = doc.Descendants("dpd_password");
			var dpd_customer_number = doc.Descendants("dpd_customer_number");
			var dpd_soap_auth = doc.Descendants("dpd_soap_auth");
			var dpd_soap_label = doc.Descendants("dpd_soap_label");
			foreach (var foo in logfile) { Logfile = foo.Value; }
            foreach (var foo in logfilecsv) { LogfileCsv = foo.Value; }
            foreach (var foo in dbconnection) { ConnectionString = foo.Value; }
			foreach (var foo in dbrowidshipment) { RowIdShipmentnumber = foo.Value; }
			foreach (var foo in dbrowidcarrier) { RowIdCarrier = foo.Value; }
			foreach (var foo in printer) { PrinterName = foo.Value; }
			foreach (var foo in printer2) { PrinterName2 = foo.Value; }
			foreach (var foo in printL) { printLabels = foo.Value; }
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
			foreach (var foo in dpd_id) { DPDId = foo.Value; }
			foreach (var foo in dpd_password) { DPDPassword = foo.Value; }
			foreach (var foo in dpd_customer_number) { DPDCustomerNumber = foo.Value; }
			foreach (var foo in dpd_soap_auth) { DPDSoapAuth = foo.Value; }
			foreach (var foo in dpd_soap_label) { DPDSoapLabel = foo.Value; }
		}
	}
}
