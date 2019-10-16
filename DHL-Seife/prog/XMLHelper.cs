using DHL_Seife.util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace DHL_Seife.prog
{
    class XMLHelper
    {
        private SettingsReader Sett;
        private LogWriter Log;
        private SQLHelper SqlH;
        

        public string Xml = ""; //XML to send to dhl
        public XmlDocument SoapEnvelopeXml = new XmlDocument();

        /// <summary>
        /// This class creates an xml string from the sql inputs, that got read earlier.
        /// </summary>
        /// <param name="settingsBuffer">An SettingsReader object, that contains all settings.</param>
        /// <param name="lw">An LogWriter object, to write logs, if exceptions occur.</param>
        /// <param name="sql">An SQLHelper object, to write logs, with all the data we need (name, street, weight etc.).</param>
        public XMLHelper(SettingsReader settingsBuffer, LogWriter lw, SQLHelper sql)
        {
            Sett = settingsBuffer;
            Log = lw;
            SqlH = sql;
        }

        /// <summary>
        /// Create a xml-string from the inputs the user made erlier.
        /// This xml will be sent as soap request to the dhl server.
        /// </summary>
        /// 
        /// TODO: Do tons of refactoring...
        public void DoXMLMagic()
        {
            //E-Mail is not a needed thing for the dhl-xml
            String newxmlmail = "";
            String newxmlmailopen = "";
            String newxmlmailclose = "";
            if (!String.IsNullOrEmpty(SqlH.XmlMail) && SqlH.XmlMail.Contains("@") && !SqlH.XmlMail.Contains("amazon"))
            {
                newxmlmailopen = "<recipientEmailAddress>";
                newxmlmailclose = "</recipientEmailAddress>";
                newxmlmail = "<recipientEmailAddress>" + SqlH.XmlMail + "</recipientEmailAddress>";
            }

            //DHL wants decimal values with dots, not commas
            if (SqlH.XmlWeight.Contains(','))
            {
                SqlH.XmlWeight = SqlH.XmlWeight.Replace(",", ".");
            }

            //If the country is not Germany, send an international parcel
            if (!SqlH.XmlCountry.ToLower().Equals("deutschland") && !SqlH.XmlCountry.ToLower().Equals("de"))
            {
                SqlH.XmlParcelType = "V53WPAK";  //international parcel
                Sett.XmlAccountnumber = Sett.XmlAccountnumberInt; //international account number
            }

            //If the street name contains "Packstation", we deliver to a packing station
            string packstationStart = "";
            string packstationEnd = "";
            string packstationNumber = "";
            string postFiliale = "";
            if (SqlH.XmlStreet.ToLower().Contains("dhl-packstation"))
            {
                SqlH.XmlStreet = SqlH.XmlStreet.Replace("dhl-", "");
            }
            else if (SqlH.XmlStreet.ToLower().Contains("dhl packstation"))
            {
                SqlH.XmlStreet = SqlH.XmlStreet.Replace("dhl ", "");
            }
            if (SqlH.XmlStreet.ToLower().Contains("packstation"))
            {
                packstationStart = "<Packstation>" +
                    "<cis:postNumber>";
                packstationEnd = "</cis:postNumber>" +
                  "</Packstation>";
                if (!String.IsNullOrEmpty(SqlH.XmlRecipient02))
                {
                    packstationNumber = Regex.Replace(SqlH.XmlRecipient02, @"[^0-9]", "").Trim(); //For people who write additional words in the packstation number field; Only allows numbers
                }
                else
                {
                    packstationNumber = Regex.Replace(SqlH.XmlRecipient03, @"[^0-9]", "").Trim(); //For people who write additional words in the packstation number field; Only allows numbers
                }
            }
            if (SqlH.XmlStreet.ToLower().Contains("postfiliale"))
            {
                postFiliale = "<Communication>" +
                    "<cis:email>" + SqlH.XmlCommunicationMail + "</cis:email>" +
                    "</Communication>" +
                    "<Postfiliale>" +
                    "<cis:postfilialNumber>" + SqlH.XmlStreetnumber +
                    "</cis:postfilialNumber>" +
                  "</Postfiliale>";
            }
            SqlH.XmlRecipient = SqlH.XmlRecipient + " " + SqlH.XmlRecipient02 + " " + SqlH.XmlRecipient03; //Combines the recipients for unneccessary use of multiple fields


            //These values have a max length; Cut them, if they are too long
            //If recipient(01) is too long, write the rest of it to recipient02. If recipient02 is too long, write the rest to recipient03
            if (SqlH.XmlRecipient.Length > 35) { SqlH.XmlRecipient02 = SqlH.XmlRecipient.Substring(35, SqlH.XmlRecipient.Length - 35) + " " + SqlH.XmlRecipient02; SqlH.XmlRecipient = SqlH.XmlRecipient.Substring(0, 35); }
            if (SqlH.XmlRecipient02.Length > 35) { SqlH.XmlRecipient03 = SqlH.XmlRecipient02.Substring(35, SqlH.XmlRecipient02.Length - 35) + " " + SqlH.XmlRecipient03; SqlH.XmlRecipient02 = SqlH.XmlRecipient02.Substring(0, 35); }
            if (SqlH.XmlRecipient03.Length > 35) { SqlH.XmlRecipient03 = SqlH.XmlRecipient03.Substring(0, 35); }
            if (SqlH.XmlStreet.Length > 35) { SqlH.XmlStreet = SqlH.XmlStreet.Substring(0, 35); }
            if (SqlH.XmlStreetnumber.Length > 5) { SqlH.XmlStreetnumber = SqlH.XmlStreetnumber.Substring(0, 5); }
            if (SqlH.XmlPlz.Length > 10) { SqlH.XmlPlz = SqlH.XmlPlz.Substring(0, 10); }
            if (SqlH.XmlCity.Length > 35) { SqlH.XmlCity = SqlH.XmlCity.Substring(0, 35); }
            if (SqlH.XmlCountry.Length > 30) { SqlH.XmlCountry = SqlH.XmlCountry.Substring(0, 30); }
            if (newxmlmail.Length > 70) { newxmlmail = newxmlmail.Substring(0, 70); }
            try
            {
                double weight = Convert.ToDouble(SqlH.XmlWeight.Replace(".", ","));
                if (weight > 30) { SqlH.XmlWeight = "30"; }
                else if (weight <= 0.01) { SqlH.XmlWeight = "4"; }
                else if (weight < 0.1) { SqlH.XmlWeight = "0.1"; }
            }
            catch (Exception ex)
            {
                Log.writeLog(ex.ToString(), true);
            }

  
            try
            {
                string senderName = "";
                string senderStreetName = "";
                string senderStreetNumber = "";
                string senderZip = "";
                string senderCity = "";
                string senderNumber = "";
                if (SqlH.XmlOrderType.Equals("10"))
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

                //I need to clean this up. blargh.
                String xmlmultiple = "";
                if (Convert.ToDouble(SqlH.XmlPsCount) > 1)
                {
                    SqlH.XmlWeight = SqlH.XmlWeightArray[0].ToString().Replace(",", ".");

                    for (int i = 1; i < Convert.ToDouble(SqlH.XmlPsCount); i++)
                    {
                        String weightbuffer = SqlH.XmlWeightArray[i].ToString();
                        if (Convert.ToDouble(weightbuffer) > 30) { weightbuffer = "30"; }
                        weightbuffer = weightbuffer.Replace(",", ".");

                        String ournumberbuffer = SqlH.XmlOurNumber + " - Paket " + i + " von " + Convert.ToDouble(SqlH.XmlPsCount);

                        xmlmultiple = xmlmultiple + String.Format(@"<ShipmentOrder>
                        <sequenceNumber>{28}</sequenceNumber>
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
                     </ShipmentOrder>", Sett.XmlUser, SqlH.XmlShippmentDate, weightbuffer, SqlH.XmlMail, SqlH.XmlRecipient, SqlH.XmlStreet,
SqlH.XmlStreetnumber, SqlH.XmlPlz, SqlH.XmlCity, SqlH.XmlCountry, Sett.XmlPass,
Sett.XmlAccountnumber, ournumberbuffer, SqlH.XmlParcelType, newxmlmailopen, newxmlmailclose,
SqlH.XmlRecipient02, SqlH.XmlRecipient03, packstationStart, packstationEnd, packstationNumber,
senderName, senderStreetName, senderStreetNumber, senderZip, senderCity,
senderNumber, postFiliale, SqlH.XmlPsCount);
                    }

                    SqlH.XmlOurNumber = SqlH.XmlOurNumber + " - Paket " + Convert.ToDouble(SqlH.XmlPsCount) + " von " + Convert.ToDouble(SqlH.XmlPsCount);
                }


                Xml = String.Format(@"<?xml version=""1.0"" encoding=""utf-8""?>
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
            <sequenceNumber>{28}</sequenceNumber>
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
         </ShipmentOrder>{29}
      </bus:CreateShipmentOrderRequest>
   </soapenv:Body>
</soapenv:Envelope>", Sett.XmlUser, SqlH.XmlShippmentDate, SqlH.XmlWeight, SqlH.XmlMail, SqlH.XmlRecipient, SqlH.XmlStreet,
SqlH.XmlStreetnumber, SqlH.XmlPlz, SqlH.XmlCity, SqlH.XmlCountry, Sett.XmlPass,
Sett.XmlAccountnumber, SqlH.XmlOurNumber, SqlH.XmlParcelType, newxmlmailopen, newxmlmailclose,
SqlH.XmlRecipient02, SqlH.XmlRecipient03, packstationStart, packstationEnd, packstationNumber,
senderName, senderStreetName, senderStreetNumber, senderZip, senderCity,
senderNumber, postFiliale, SqlH.XmlPsCount, xmlmultiple);

                SoapEnvelopeXml.LoadXml(Xml);
            }
            catch (Exception ex)
            {
                //logTextToFile(" > XML error!");
                Log.writeLog("> XML Fehler!" + ex.ToString(), true, true);
            }

        }

    }
}
