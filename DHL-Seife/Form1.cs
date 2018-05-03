﻿using System;
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

namespace DHL_Seife
{
    public partial class Form1 : Form
    {
        private static HttpWebRequest request;
        private static string orderNumber = "18336473";
        private static string xmluser = "2222222222_01";
        private static string xmlpass = "pass";
        private static string xmlaccountnumber = "22222222220101";
        private static string xmlournumber = "12345";
        private static string xmlshippmentdate = "2018-05-04"; //YYYY-MM-DD
        private static string xmlweight = "1"; //In kg
        private static string xmlmail = "info@loechel-industriebedarf.de"; //recipient mail
        private static string xmlrecipient = "Max Mustermann"; //recipient name
        private static string xmlstreet = "Musterstraße"; //recipient street
        private static string xmlstreetnumber = "3"; //recipient streetnumber
        private static string xmlplz = "27254"; //recipient plz
        private static string xmlcity = "Siedenburg"; //recipient city
        private static string xmlcountry = "Deutschland"; //recipient country


        public Form1()
        {
            InitializeComponent();
            Execute();
        }

        /// <summary>
        /// Execute a Soap WebService call
        /// </summary>
        public static void Execute()
        {
            

            doSQLMagic(orderNumber, ref xmlournumber, ref xmlshippmentdate, ref xmlrecipient, ref xmlstreet, ref xmlstreetnumber, ref xmlplz, ref xmlcity, ref xmlcountry);

            request = doXMLMagic(xmluser, xmlpass, xmlaccountnumber, xmlournumber, xmlshippmentdate, xmlweight, xmlmail, xmlrecipient, xmlstreet, xmlstreetnumber, xmlplz, xmlcity, xmlcountry);

            //On Button press?
        }

        private static void doSQLMagic(string orderNumber, ref string xmlournumber, ref string xmlshippmentdate, ref string xmlrecipient, ref string xmlstreet, ref string xmlstreetnumber, ref string xmlplz, ref string xmlcity, ref string xmlcountry)
        {
            string connectionString = "DSN=eNVenta SQL Server;Server=server-03;Database=LOE99;User Id=sa;Password = sasasa;";
            string sql = "SELECT * FROM dbo.AUFTRAGSKOPF WHERE BELEGNR = '" + orderNumber + "'";
            OdbcConnection conn = new OdbcConnection(connectionString);
            conn.Open();
            OdbcCommand comm = new OdbcCommand(sql, conn);
            OdbcDataReader dr = comm.ExecuteReader();
            while (dr.Read())
            {
                if (String.IsNullOrEmpty(dr["LFIRMA1"].ToString())) { xmlrecipient = dr["RFIRMA1"].ToString(); }
                else { xmlrecipient = dr["LFIRMA1"].ToString(); }

                if (String.IsNullOrEmpty(dr["LSTRASSE"].ToString()))
                {
                    xmlstreet = dr["RSTRASSE"].ToString().Substring(0, dr["RSTRASSE"].ToString().LastIndexOf(" ") + 1).ToString();
                    xmlstreetnumber = dr["RSTRASSE"].ToString().Substring(dr["RSTRASSE"].ToString().LastIndexOf(" ") + 1).ToString();
                }
                else
                {
                    xmlstreet = dr["LSTRASSE"].ToString().Substring(0, dr["LSTRASSE"].ToString().LastIndexOf(" ") + 1).ToString();
                    xmlstreetnumber = dr["LSTRASSE"].ToString().Substring(dr["LSTRASSE"].ToString().LastIndexOf(" ") + 1).ToString();
                }

                if (String.IsNullOrEmpty(dr["LPLZ"].ToString())) { xmlplz = dr["RPLZ"].ToString(); }
                else { xmlplz = dr["LPLZ"].ToString(); }

                if (String.IsNullOrEmpty(dr["LORT"].ToString())) { xmlcity = dr["RORT"].ToString(); }
                else { xmlcity = dr["LORT"].ToString(); }

                if (String.IsNullOrEmpty(dr["LLAND"].ToString())) { xmlcountry = dr["RLAND"].ToString(); }
                else { xmlcountry = dr["LLAND"].ToString(); }

                xmlournumber = dr["BELEGNR"].ToString();

                xmlshippmentdate = DateTime.Now.AddDays(2).ToString("yyyy-MM-dd");
            }
        }

        private static HttpWebRequest doXMLMagic(string xmluser, string xmlpass, string xmlaccountnumber, string xmlournumber, string xmlshippmentdate, string xmlweight, string xmlmail, string xmlrecipient, string xmlstreet, string xmlstreetnumber, string xmlplz, string xmlcity, string xmlcountry)
        {
            HttpWebRequest request = CreateWebRequest();
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
                  <product>V01PAK</product>
                  <cis:accountNumber>{11}</cis:accountNumber>
                  <customerReference>{12}</customerReference>
                  <shipmentDate>{1}</shipmentDate>
                  <ShipmentItem>
                     <weightInKG>{2}</weightInKG>
                  </ShipmentItem>
                  <Notification>
                     <recipientEmailAddress>{3}</recipientEmailAddress>
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
</soapenv:Envelope>", xmluser, xmlshippmentdate, xmlweight, xmlmail, xmlrecipient, xmlstreet, xmlstreetnumber, xmlplz, xmlcity, xmlcountry, xmlpass, xmlaccountnumber, xmlournumber);
            soapEnvelopeXml.LoadXml(xml);

            Console.WriteLine(xml);

            using (Stream stream = request.GetRequestStream())
            {
                soapEnvelopeXml.Save(stream);
            }

            return request;
        }

        private static void sendSoapRequest()
        {
            // Get a soap response
            using (WebResponse response = request.GetResponse())
            {
                using (StreamReader rd = new StreamReader(response.GetResponseStream()))
                {
                    string soapResult = rd.ReadToEnd();

                    XmlDocument xmldoc = new XmlDocument();
                    xmldoc.LoadXml(soapResult);

                    Console.WriteLine(soapResult);

                    XmlNodeList xnList = xmldoc.GetElementsByTagName("labelUrl");
                    foreach (XmlNode xn in xnList)
                    {
                        string labelUrl = xn.InnerText;
                        System.Diagnostics.Process.Start(labelUrl);
                    }
                }
            }
        }


        /// <summary>
        /// Create a soap webrequest to [Url]
        /// </summary>
        /// <returns></returns>
        public static HttpWebRequest CreateWebRequest()
        {
            //Basic http authentication
            String username = "loechelindustriebedarf";
            String password = System.IO.File.ReadAllText(@"password.txt"); //Saves me from accidently pushing our password. Just input a normal string here.
            String encoded = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(username + ":" + password));

            //SOAP webrequest
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(@"https://cig.dhl.de/services/sandbox/soap");
            webRequest.Headers.Add("Authorization", "Basic " + encoded);
            webRequest.Headers.Add(@"SOAP:Action");
            webRequest.ContentType = "text/xml;charset=\"utf-8\"";
            webRequest.Accept = "text/xml";
            webRequest.Method = "POST";
            return webRequest;
        }

        private void Main_Load(object sender, EventArgs e)
        {

        }

        
        private void button1_Click(object sender, EventArgs e)
        {
            sendSoapRequest();
            Application.Exit();
        }
    }
}