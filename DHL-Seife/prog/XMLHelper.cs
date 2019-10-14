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
        private SettingsReader sett;
        private LogWriter log;
        private SQLHelper sqlh;
        

        public string Xml = ""; //XML to send to dhl
        public XmlDocument SoapEnvelopeXml = new XmlDocument();

        public XMLHelper(SettingsReader settingsBuffer, LogWriter lw, SQLHelper sql)
        {
            sett = settingsBuffer;
            log = lw;
            sqlh = sql;
        }

        /// <summary>
        /// Create a xml-string from the inputs the user made erlier.
        /// This xml will be sent as soap request to the dhl server.
        /// </summary>
        public void DoXMLMagic()
        {
            //E-Mail is not a needed thing for the dhl-xml
            String newxmlmail = "";
            String newxmlmailopen = "";
            String newxmlmailclose = "";
            if (!String.IsNullOrEmpty(sqlh.XmlMail) && sqlh.XmlMail.Contains("@") && !sqlh.XmlMail.Contains("amazon"))
            {
                newxmlmailopen = "<recipientEmailAddress>";
                newxmlmailclose = "</recipientEmailAddress>";
                newxmlmail = "<recipientEmailAddress>" + sqlh.XmlMail + "</recipientEmailAddress>";
            }

            //DHL wants decimal values with dots, not commas
            if (sqlh.XmlWeight.Contains(','))
            {
                sqlh.XmlWeight = sqlh.XmlWeight.Replace(",", ".");
            }

            //If the country is not Germany, send an international parcel
            if (!sqlh.XmlCountry.ToLower().Equals("deutschland") && !sqlh.XmlCountry.ToLower().Equals("de"))
            {
                sqlh.XmlParcelType = "V53WPAK";  //international parcel
                sett.XmlAccountnumber = sett.XmlAccountnumberInt; //international account number
            }

            //If the street name contains "Packstation", we deliver to a packing station
            string packstationStart = "";
            string packstationEnd = "";
            string packstationNumber = "";
            string postFiliale = "";
            if (sqlh.XmlStreet.ToLower().Contains("dhl-packstation"))
            {
                sqlh.XmlStreet = sqlh.XmlStreet.Replace("dhl-", "");
            }
            else if (sqlh.XmlStreet.ToLower().Contains("dhl packstation"))
            {
                sqlh.XmlStreet = sqlh.XmlStreet.Replace("dhl ", "");
            }
            if (sqlh.XmlStreet.ToLower().Contains("packstation"))
            {
                packstationStart = "<Packstation>" +
                    "<cis:postNumber>";
                packstationEnd = "</cis:postNumber>" +
                  "</Packstation>";
                if (!String.IsNullOrEmpty(sqlh.XmlRecipient02))
                {
                    packstationNumber = Regex.Replace(sqlh.XmlRecipient02, @"[^0-9]", "").Trim(); //For people who write additional words in the packstation number field; Only allows numbers
                }
                else
                {
                    packstationNumber = Regex.Replace(sqlh.XmlRecipient03, @"[^0-9]", "").Trim(); //For people who write additional words in the packstation number field; Only allows numbers
                }
            }
            if (sqlh.XmlStreet.ToLower().Contains("postfiliale"))
            {
                postFiliale = "<Communication>" +
                    "<cis:email>" + sqlh.XmlCommunicationMail + "</cis:email>" +
                    "</Communication>" +
                    "<Postfiliale>" +
                    "<cis:postfilialNumber>" + sqlh.XmlStreetnumber +
                    "</cis:postfilialNumber>" +
                  "</Postfiliale>";
            }
            sqlh.XmlRecipient = sqlh.XmlRecipient + " " + sqlh.XmlRecipient02 + " " + sqlh.XmlRecipient03; //Combines the recipients for unneccessary use of multiple fields


            //These values have a max length; Cut them, if they are too long
            //If recipient(01) is too long, write the rest of it to recipient02. If recipient02 is too long, write the rest to recipient03
            if (sqlh.XmlRecipient.Length > 35) { sqlh.XmlRecipient02 = sqlh.XmlRecipient.Substring(35, sqlh.XmlRecipient.Length - 35) + " " + sqlh.XmlRecipient02; sqlh.XmlRecipient = sqlh.XmlRecipient.Substring(0, 35); }
            if (sqlh.XmlRecipient02.Length > 35) { sqlh.XmlRecipient03 = sqlh.XmlRecipient02.Substring(35, sqlh.XmlRecipient02.Length - 35) + " " + sqlh.XmlRecipient03; sqlh.XmlRecipient02 = sqlh.XmlRecipient02.Substring(0, 35); }
            if (sqlh.XmlRecipient03.Length > 35) { sqlh.XmlRecipient03 = sqlh.XmlRecipient03.Substring(0, 35); }
            if (sqlh.XmlStreet.Length > 35) { sqlh.XmlStreet = sqlh.XmlStreet.Substring(0, 35); }
            if (sqlh.XmlStreetnumber.Length > 5) { sqlh.XmlStreetnumber = sqlh.XmlStreetnumber.Substring(0, 5); }
            if (sqlh.XmlPlz.Length > 10) { sqlh.XmlPlz = sqlh.XmlPlz.Substring(0, 10); }
            if (sqlh.XmlCity.Length > 35) { sqlh.XmlCity = sqlh.XmlCity.Substring(0, 35); }
            if (sqlh.XmlCountry.Length > 30) { sqlh.XmlCountry = sqlh.XmlCountry.Substring(0, 30); }
            if (newxmlmail.Length > 70) { newxmlmail = newxmlmail.Substring(0, 70); }
            try
            {
                double weight = Convert.ToDouble(sqlh.XmlWeight.Replace(".", ","));
                if (weight > 30) { sqlh.XmlWeight = "30"; }
                else if (weight <= 0.01) { sqlh.XmlWeight = "4"; }
                else if (weight < 0.1) { sqlh.XmlWeight = "0.1"; }
            }
            catch (Exception ex)
            {
                log.writeLog(ex.ToString());
                log.writeLog(ex.Message.ToString(), true);
            }

  
            try
            {
                string senderName = "";
                string senderStreetName = "";
                string senderStreetNumber = "";
                string senderZip = "";
                string senderCity = "";
                string senderNumber = "";
                if (sqlh.XmlOrderType.Equals("10"))
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
                if (Convert.ToDouble(sqlh.XmlPsCount) > 1)
                {
                    sqlh.XmlWeight = sqlh.XmlWeightArray[0].ToString().Replace(",", ".");

                    for (int i = 1; i < Convert.ToDouble(sqlh.XmlPsCount); i++)
                    {
                        String weightbuffer = sqlh.XmlWeightArray[i].ToString().Replace(",", ".");
                        String ournumberbuffer = sqlh.XmlOurNumber + " - Paket " + i + " von " + Convert.ToDouble(sqlh.XmlPsCount);

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
                     </ShipmentOrder>", sett.XmlUser, sqlh.XmlShippmentDate, weightbuffer, sqlh.XmlMail, sqlh.XmlRecipient, sqlh.XmlStreet,
sqlh.XmlStreetnumber, sqlh.XmlPlz, sqlh.XmlCity, sqlh.XmlCountry, sett.XmlPass,
sett.XmlAccountnumber, ournumberbuffer, sqlh.XmlParcelType, newxmlmailopen, newxmlmailclose,
sqlh.XmlRecipient02, sqlh.XmlRecipient03, packstationStart, packstationEnd, packstationNumber,
senderName, senderStreetName, senderStreetNumber, senderZip, senderCity,
senderNumber, postFiliale, sqlh.XmlPsCount);
                    }

                    sqlh.XmlOurNumber = sqlh.XmlOurNumber + " - Paket " + Convert.ToDouble(sqlh.XmlPsCount) + " von " + Convert.ToDouble(sqlh.XmlPsCount);
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
</soapenv:Envelope>", sett.XmlUser, sqlh.XmlShippmentDate, sqlh.XmlWeight, sqlh.XmlMail, sqlh.XmlRecipient, sqlh.XmlStreet,
sqlh.XmlStreetnumber, sqlh.XmlPlz, sqlh.XmlCity, sqlh.XmlCountry, sett.XmlPass,
sett.XmlAccountnumber, sqlh.XmlOurNumber, sqlh.XmlParcelType, newxmlmailopen, newxmlmailclose,
sqlh.XmlRecipient02, sqlh.XmlRecipient03, packstationStart, packstationEnd, packstationNumber,
senderName, senderStreetName, senderStreetNumber, senderZip, senderCity,
senderNumber, postFiliale, sqlh.XmlPsCount, xmlmultiple);

                SoapEnvelopeXml.LoadXml(Xml);
            }
            catch (Exception ex)
            {
                //logTextToFile(" > XML error!");
                log.writeLog("> XML Fehler!" + ex.ToString(), true, true);
            }

        }

    }
}
