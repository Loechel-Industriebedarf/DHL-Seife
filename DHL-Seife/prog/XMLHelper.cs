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
		public void DoDHLXMLMagic()
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

			if (newxmlmail.Length > 70) { newxmlmail = newxmlmail.Substring(0, 70); }
			RefactorInputs();

			try
			{

				//Starts at 0, the string variables on the bottom are groups of five

				//I need to clean this up. blargh.
				String xmlmultiple = "";
				if (Convert.ToDouble(SqlH.XmlPsCount) > 1)
				{
					if (Convert.ToDouble(SqlH.XmlWeightArray[0].ToString()) > 30) { SqlH.XmlWeight = "30"; }
					else { SqlH.XmlWeight = SqlH.XmlWeightArray[0].ToString().Replace(",", "."); }

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
								 <cis:name2>{30}</cis:name2>
								 <cis:name3>{31}</cis:name3>
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
								<cis:email>{3}</cis:email>
								<cis:phone>{26}</cis:phone>
                              </Communication>
                           </Shipper>
                           <Receiver>
                              <cis:name1>{4}</cis:name1>
                                {18}{20}{19}{27}
                              <Address>
								 <cis:name2>{16}</cis:name2>
								 <cis:name3>{17}</cis:name3>
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
Sett.senderName, Sett.senderStreetName, Sett.senderStreetNumber, Sett.senderZip, Sett.senderCity,
Sett.senderNumber, postFiliale, SqlH.XmlPsCount, xmlmultiple, Sett.senderName2,
Sett.senderName3);
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
            <majorRelease>3</majorRelease>
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
					 <cis:name2>{30}</cis:name2>
					 <cis:name3>{31}</cis:name3>
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
					<cis:email>{3}</cis:email>
					<cis:phone>{26}</cis:phone>
                  </Communication>
               </Shipper>
               <Receiver>
                  <cis:name1>{4}</cis:name1>       
                  <Address>
					 <cis:name2>{16}</cis:name2>
					 <cis:name3>{17}</cis:name3>
                     <cis:streetName>{5}</cis:streetName>
                     <cis:streetNumber>{6}</cis:streetNumber>
                     <cis:zip>{7}</cis:zip>
                     <cis:city>{8}</cis:city>
                     <cis:Origin>
                        <cis:country>{9}</cis:country>
                     </cis:Origin>
                  </Address>
				  {18}{20}{19}{27}
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
Sett.senderName, Sett.senderStreetName, Sett.senderStreetNumber, Sett.senderZip, Sett.senderCity,
Sett.senderNumber, postFiliale, SqlH.XmlPsCount, xmlmultiple, Sett.senderName2,
Sett.senderName3);

				SoapEnvelopeXml.LoadXml(Xml);

			}
			catch (Exception ex)
			{
				//logTextToFile(" > XML error!");
				Log.writeLog("> XML Fehler!" + ex.ToString(), true, true);
			}

		}




		/// <summary>
		/// Create a xml-string from the inputs the user made erlier.
		/// This xml will be sent as soap request to the dpd server.
		/// </summary>
		public void DoDPDXMLMagic()
		{
			try
			{
				RefactorInputs();

				//DPD wants numbers with 11 chars
				String ourNumber11 = Sett.OrderNumber.PadLeft(11, '0');
				// DPD wants weight in grams*10 - If you input "1", DPD thinks it is 10 grams. 
				double dpdWeight = Math.Round(Convert.ToDouble(SqlH.XmlWeight) * 1000 / 10);

				//Does the order contain multiple parcels?
				String multipleParcels = "";
				if (Convert.ToDouble(SqlH.XmlPsCount) > 1)
				{
					dpdWeight = Math.Round(Convert.ToDouble(SqlH.XmlWeightArray[0]) * 1000 / 10);

					for (int i = 1; i < Convert.ToDouble(SqlH.XmlPsCount); i++)
					{
						String weightbuffer = Math.Round(Convert.ToDouble(SqlH.XmlWeightArray[i]) * 1000 / 10).ToString();
						if (Convert.ToDouble(weightbuffer) > 3000) { weightbuffer = "3000"; }
						weightbuffer = weightbuffer.Replace(",", ".");

						multipleParcels = multipleParcels + String.Format(@"<parcels>
                           <parcelLabelNumber>{0}</parcelLabelNumber>
                           <customerReferenceNumber1>{1}</customerReferenceNumber1>
                           <customerReferenceNumber2>{2}</customerReferenceNumber2>
                           <weight>{3}</weight>
                        </parcels>", ourNumber11, Sett.OrderNumber, SqlH.XmlMail, weightbuffer);
					}
				}

				//Should a notification be sent?
				//Currently not working...
				String dpdNotification = "";
				if (!String.IsNullOrEmpty(SqlH.XmlMail) && SqlH.XmlMail.Contains("@") && !SqlH.XmlMail.Contains("amazon"))
				{
					dpdNotification = String.Format(@"<proactiveNotification>
                        <channel>1</channel>
                        <value>{0}</value>
                        <rule>16</rule>
                        <language>DE</language>
                    </proactiveNotification>", SqlH.XmlMail);
				}
				dpdNotification = ""; //The code doesn't work correctly at the moment.


				Xml = String.Format(@"<?xml version=""1.0"" encoding=""UTF-8""?>
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ns=""http://dpd.com/common/service/types/Authentication/2.0"" 
xmlns:ns1=""http://dpd.com/common/service/types/ShipmentService/3.2"">   
<soapenv:Header>
    <ns:authentication>
        <delisId>{0}</delisId>
        <authToken>{1}</authToken>
        <messageLanguage>de_DE</messageLanguage>
    </ns:authentication>
</soapenv:Header>
<soapenv:Body>
    <ns1:storeOrders>
        <printOptions>
            <printerLanguage>PDF</printerLanguage>
            <paperFormat>A6</paperFormat>
        </printOptions>
        <order>
            <generalShipmentData>
                <identificationNumber>{4}</identificationNumber>
                <sendingDepot>{2}</sendingDepot>
                <product>CL</product>
                <mpsCompleteDelivery>0</mpsCompleteDelivery>
                <sender>
                    <name1>{5}</name1>
					<name2>{25}</name2>
                    <street>{6}</street>
                    <houseNo>{17}</houseNo>
                    <country>DE</country>
                    <zipCode>{7}</zipCode>
                    <city>{8}</city>
                    <customerNumber>{9}</customerNumber>
                    <phone>{20}</phone>
                    <email>{19}</email>
                </sender>
                <recipient>
                    <name1>{10}</name1>
					<name2>{26}</name2>
                    <street>{11}</street>
                    <houseNo>{18}</houseNo>
                    <country>{12}</country>
                    <zipCode>{13}</zipCode>
                    <city>{14}</city>
                    <phone>{22}</phone>
                    <email>{21}</email>
                </recipient>
            </generalShipmentData>
            
            <parcels>
                <parcelLabelNumber>{3}</parcelLabelNumber>
                <customerReferenceNumber1>{15}</customerReferenceNumber1>
                <customerReferenceNumber2>{21}</customerReferenceNumber2>
                <weight>{16}</weight>
            </parcels>
            {23}
            <productAndServiceData>
                <orderType>consignment</orderType>
                {24}
            </productAndServiceData>
        </order>       
    </ns1:storeOrders>    
</soapenv:Body> 
</soapenv:Envelope>", Sett.DPDId, Sett.DPDAuthToken, Sett.DPDDepotNumber, ourNumber11, Sett.OrderNumber, Sett.senderName,
Sett.senderStreetName, Sett.senderZip, Sett.senderCity, Sett.DPDCustomerNumber, SqlH.XmlRecipient,
SqlH.XmlStreet, SqlH.XmlCountryCode, SqlH.XmlPlz, SqlH.XmlCity, Sett.OrderNumber,
dpdWeight.ToString(), Sett.senderStreetNumber, SqlH.XmlStreetnumber, Sett.senderMail, Sett.senderNumber,
SqlH.XmlMail, "", multipleParcels, dpdNotification, Sett.senderName3,
SqlH.XmlRecipient02);

				SoapEnvelopeXml.LoadXml(@Xml);


			}
			catch (Exception ex)
			{
				Log.writeLog("> DPD-XML Fehler!\r\n" + Xml + "\r\n" + ex.ToString(), true, true);
			}

		}




		/// <summary>
		/// Refactors some of the inputs, so we can use it correctly.
		/// </summary>
		private void RefactorInputs()
		{
			//These values have a max length; Cut them, if they are too long
			//If recipient(01) is too long, write the rest of it to recipient02. If recipient02 is too long, write the rest to recipient03
			int recLen = 35; //Max chars for Recipient 1, 2, 3, streetname and cityname
			try
			{
				int cutindex = 0;
				while (SqlH.XmlRecipient.Length > recLen)
				{
					cutindex = SqlH.XmlRecipient.LastIndexOf(" ");
					SqlH.XmlRecipient02 = SqlH.XmlRecipient.Substring(cutindex).Trim() + " " + SqlH.XmlRecipient02.Trim();
					SqlH.XmlRecipient = SqlH.XmlRecipient.Substring(0, cutindex).Trim();
				}
				while (SqlH.XmlRecipient02.Length > recLen)
				{
					cutindex = SqlH.XmlRecipient02.LastIndexOf(" ");
					SqlH.XmlRecipient03 = SqlH.XmlRecipient02.Substring(cutindex).Trim() + " " + SqlH.XmlRecipient03.Trim();
					SqlH.XmlRecipient02 = SqlH.XmlRecipient02.Substring(0, cutindex).Trim();
				}
				if (SqlH.XmlRecipient03.Length > recLen)
				{
					SqlH.XmlRecipient03 = SqlH.XmlRecipient03.Substring(0, recLen).Trim();
				}

				Log.writeLog(SqlH.XmlRecipient);
			}
			catch (Exception ex)
			{
				Log.writeLog("> Fehler beim Input-Refactoring der Empfängeradresse!\r\n" + ex.ToString(), true, true);
			}


			if (SqlH.XmlStreet.Length > recLen) { SqlH.XmlStreet = SqlH.XmlStreet.Substring(0, recLen); }
			if (SqlH.XmlStreetnumber.Length > 10) { SqlH.XmlStreetnumber = SqlH.XmlStreetnumber.Substring(0, 10); }
			if (SqlH.XmlPlz.Length > 10) { SqlH.XmlPlz = SqlH.XmlPlz.Substring(0, 10); }
			if (SqlH.XmlCity.Length > recLen) { SqlH.XmlCity = SqlH.XmlCity.Substring(0, recLen); }
			if (SqlH.XmlCountry.Length > 30) { SqlH.XmlCountry = SqlH.XmlCountry.Substring(0, 30); }

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

			Sett.senderName = "Löchel Industriebedarf";
			Sett.senderStreetName = "Hans-Hermann-Meyer-Strasse";
			Sett.senderStreetNumber = "2";
			Sett.senderZip = "27232";
			Sett.senderCity = "Sulingen";
			Sett.senderMail = "info@loechel-industriebedarf.de";
			Sett.senderNumber = "+49 4271 5727";
			if (SqlH.XmlOrderType.Equals("10"))
			{
				Sett.senderName = "Mercateo Deutschland AG";
				Sett.senderName2 = "c/o Auslieferungslager";
				Sett.senderName3 = "Löchel Industriebedarf";
			}
		}

	}
}
