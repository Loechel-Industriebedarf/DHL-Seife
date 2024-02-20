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

                //DPD doesn't like long mail addresses
                if (SqlH.XmlMail.Length > 35)
                {
                    SqlH.XmlMail = "";
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

                if (String.IsNullOrEmpty(Sett.DPDDepotNumber))
                {
                    Sett.DPDDepotNumber = "0163";
                }

                String dpdMail = "";
                if (!String.IsNullOrEmpty(SqlH.XmlMail))
                {
                    dpdMail = "<email>" + SqlH.XmlMail + "</email>";
                }

                SqlH.GetStreetAndStreetnumber(SqlH.XmlStreet);
                if (SqlH.XmlStreetnumber.Length > 10) { SqlH.XmlStreetnumber = SqlH.XmlStreetnumber.Substring(0, 10); }




                Xml = String.Format(@"<?xml version=""1.0"" encoding=""UTF-8""?>
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ns=""http://dpd.com/common/service/types/Authentication/2.0"" 
xmlns:ns1=""http://dpd.com/common/service/types/ShipmentService/4.4"">   
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
            <printOption>
                <outputFormat>PDF</outputFormat>
                <paperFormat>A6</paperFormat>
            </printOption>
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
                    {21}
                </recipient>
            </generalShipmentData>
            
            <parcels>
                <parcelLabelNumber>{3}</parcelLabelNumber>
                <customerReferenceNumber1>{15}</customerReferenceNumber1>
                <customerReferenceNumber2>{27}</customerReferenceNumber2>
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
dpdMail, "", multipleParcels, dpdNotification, Sett.senderName3,
SqlH.XmlRecipient02, SqlH.XmlMail);

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
			if (SqlH.XmlPlz.Length > 10) { SqlH.XmlPlz = SqlH.XmlPlz.Substring(0, 10); }
			if (SqlH.XmlCity.Length > recLen) { SqlH.XmlCity = SqlH.XmlCity.Substring(0, recLen); }
			if (SqlH.XmlCountry.Length > 30) { SqlH.XmlCountry = SqlH.XmlCountry.Substring(0, 30); }

			try
			{
				double weight = Convert.ToDouble(SqlH.XmlWeight.Replace(".", ","));
				if (weight > 30) { SqlH.XmlWeight = "30"; }
				else if (weight <= 0.001) { SqlH.XmlWeight = "3"; }
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
