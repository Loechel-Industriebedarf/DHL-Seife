﻿using DHL_Seife.util;
using Spire.Pdf;
using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DHL_Seife.prog
{
    class SOAPHelper
    {
        private SettingsReader Sett;
        private LogWriter Log;
        private SQLHelper SqlH;
        private XMLHelper XmlH;
        private static HttpWebRequest WebRequest;

        static int apiConnectTries = 0; //If the connection to the api fails, it should try again and increment this counter.


        /// <summary>
        /// This class creates an xml string from the sql inputs, that got read earlier.
        /// </summary>
        /// <param name="settingsBuffer">An SettingsReader object, that contains all settings.</param>
        /// <param name="lw">An LogWriter object, to write logs, if exceptions occur.</param>
        /// <param name="sql">An SQLHelper object, to write logs, with all the data we need (name, street, weight etc.).</param>
        /// <param name="xml">An XMLHelper object, with the xml data, that should be sent to the server.</param>
        public SOAPHelper(SettingsReader settingsBuffer, LogWriter lw, SQLHelper sql, XMLHelper xml)
        {
            Sett = settingsBuffer;
            Log = lw;
            SqlH = sql;
            XmlH = xml;
        }



		/// <summary>
		/// If we can't get a connection to the dhl api, log the error message and try again.
		/// </summary>
		/// <param name="ex">The WebException that should be logged, before the program tries to connect again.</param>
		private void AnotherApiConnectionTryHttp(WebException ex)
		{
			AnotherApiConnectionTryHttp(ex, "DHL");
		}
		private void AnotherApiConnectionTryHttp(WebException ex, String orderType)
		{
			try
			{
				using (WebResponse response = ex.Response)
				{

					apiConnectTries++;
					//If there is an error while connecting to the api, try again 3 times
					if (apiConnectTries <= 3)
					{
						HttpWebResponse httpResponse = (HttpWebResponse)response;
						using (Stream data = response.GetResponseStream())
						using (var reader = new StreamReader(data))
						{
							string text = reader.ReadToEnd();
							//logTextToFile("> Error while connecting to DHL-API!");
							Log.writeLog("> Fehlerhafte Datenübermittlung an DPD  - neuer Versuch in 5 Sekunden!!\r\n" +
								text + "\r\n\r\n" +
								XmlH.Xml + "\r\n\r\n" +
								ex.ToString(), true, false);
						}

						System.Threading.Thread.Sleep(5000);

						switch (orderType)
						{
                            case "DPD":
								XmlH.DoDPDXMLMagic();
								SendDPDSoapRequest();
								break;
						}
					}
					else
					{
						Log.writeLog("> Fehlerhafte Datenübermittlung an DPD!\r\n" + ex.ToString() + "\r\n\r\n" + XmlH.Xml, true, true);
					}
				}
			}
			catch (Exception ex1)
			{
				AnotherApiConnectionTry(ex1, orderType);
			}
		}


		/// <summary>
		/// If we can't get a connection to the dhl api, try again.
		/// </summary>
		/// <param name="ex">The Exception that should be logged, before the program tries to connect again.</param>
		private void AnotherApiConnectionTry(Exception ex)
		{
			AnotherApiConnectionTry(ex, "DHL");
		}
		private void AnotherApiConnectionTry(Exception ex, String orderType)
		{
			apiConnectTries++;
			//If there is an error while connecting to the api, try again 10 times
			if (apiConnectTries <= 10)
			{
				//logTextToFile("> Error while connecting to DHL-API!");
				Log.writeLog("> Fehler bei der Verbindung mit der DPD-API - neuer Versuch in 3 Sekunden!\r\n" + ex.ToString(), true, false);
				System.Threading.Thread.Sleep(3000);
				switch (orderType)
				{
                    case "DPD":
						XmlH.DoDPDXMLMagic();
						SendDPDSoapRequest();
						break;
				}
			}
			else
			{
				Log.writeLog("> Fehler bei der Verbindung mit der DPD-API!\r\n" + ex.ToString(), true, true);
			}
		}







		/// <summary>
		/// Writes shipment-number to the database.
		/// </summary>
		/// <param name="shipmentnumber">The shipment number as a string, that should be inserted into the database.</param>
		private void WriteShipmentNumber(string shipmentnumber, Boolean isReturn)
		{
			string sql = Sett.SqlShipmentnumber;
            if (isReturn)
            {
                sql = sql.Replace("%rowidshipmentnumber%", Sett.RowIdReturnnumber);
            }
            else
            {
                sql = sql.Replace("%rowidshipmentnumber%", Sett.RowIdShipmentnumber);
            }
			sql = sql.Replace("%rowid%", SqlH.RowId);
			sql = sql.Replace("%shipmentnumber%", shipmentnumber);
			string sql_carrier = Sett.SqlCarrierShipmentnumber;
			sql_carrier = sql_carrier.Replace("%rowidcarrier%", Sett.RowIdCarrier);
			sql_carrier = sql_carrier.Replace("%rowid%", SqlH.RowId);
			if (Sett.OrderType.Contains("DPD"))
			{
				sql_carrier = sql_carrier.Replace("DHL", "DPD");
			}

			try
			{
				OdbcConnection conn = new OdbcConnection(Sett.ConnectionString);
				conn.Open();
				OdbcCommand comm = new OdbcCommand(sql, conn);
				OdbcCommand comm_carrier = new OdbcCommand(sql_carrier, conn);
				comm.ExecuteNonQuery();
				comm_carrier.ExecuteNonQuery();
			}
			catch (Exception ex)
			{
				Log.writeLog(ex.ToString(), true);
			}

		}





		/// <summary>
		/// Gets an auth token from DPD. We'll need an auth token to send further requests.
		/// </summary>
		public void DPDAuth()
		{
			try
			{
                //Use TSL 1.2
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                HttpWebRequest request = CreateDPDWebRequest(Sett.DPDSoapAuth);
				XmlDocument soapEnvelopeXml = new XmlDocument();
				String xml = String.Format(@"<?xml version=""1.0"" encoding=""UTF-8""?>
            <SOAP-ENV:Envelope xmlns:SOAP-ENV=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ns1=""http://dpd.com/common/service/types/LoginService/2.0"">   
                <SOAP-ENV:Body>       
                    <ns1:getAuth>           
                        <delisId>{0}</delisId>           
                        <password>{1}</password>           
                        <messageLanguage>de_DE</messageLanguage>       
                    </ns1:getAuth>   
                </SOAP-ENV:Body>
            </SOAP-ENV:Envelope>", Sett.DPDId, Sett.DPDPassword);
				soapEnvelopeXml.LoadXml(@xml);

                using (Stream stream = request.GetRequestStream())
				{
					soapEnvelopeXml.Save(stream);
				}

				using (WebResponse response = request.GetResponse())
				{
					using (StreamReader rd = new StreamReader(response.GetResponseStream()))
					{
						String soapResult = rd.ReadToEnd();
                        XmlDocument soapResultXml = new XmlDocument();
						soapResultXml.LoadXml(soapResult);

						Sett.DPDAuthToken = soapResultXml.GetElementsByTagName("authToken")[0].InnerXml;
						Sett.DPDDepotNumber = soapResultXml.GetElementsByTagName("depot")[0].InnerXml;
					}
				}
			}
			catch (WebException ex)
			{
                Log.writeLog(ex.ToString().ToString(), true, false);

                //Log the error message of the WebException
                AnotherApiConnectionTryHttp(ex, "DPD");
			}
			catch (Exception ex)
			{
                Log.writeLog("Auth exception: " + ex.ToString().ToString(), true, false);

                //If there is no WebException, log the "normal" exception
                AnotherApiConnectionTry(ex, "DPD");
			}
		}





		/// <summary>
		/// The webrequest to get the authtoken.
		/// </summary>
		/// <param name="endPoint">The endpoint that should be used for the request (Auth, label creation etc.)</param>
		private HttpWebRequest CreateDPDWebRequest(String endPoint)
		{
			try
			{
				HttpWebRequest webRequest = (HttpWebRequest)System.Net.WebRequest.Create(@endPoint);
				webRequest.Headers.Add(@"SOAP:Action");
				webRequest.ContentType = "text/xml;charset=\"utf-8\"";
				webRequest.Accept = "text/xml";
				webRequest.Method = "POST";
				return webRequest;
			}
			catch (Exception ex)
			{
				Log.writeLog("> Fehler beim Verbindungsaufbau mit DPD!\r\n" + ex.ToString(), true, true);
				return null;
			}
		}





		/// <summary>
		/// Sends the soap request to the dpd server.
		/// </summary>
		public void SendDPDSoapRequest()
		{
			try
			{
                //Use TSL 1.2
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                HttpWebRequest request = CreateDPDWebRequest(Sett.DPDSoapLabel);

				using (Stream stream = request.GetRequestStream())
				{
					XmlH.SoapEnvelopeXml.Save(stream);
				}

				using (WebResponse response = request.GetResponse())
				{
					using (StreamReader rd = new StreamReader(response.GetResponseStream()))
					{
						String soapResult = rd.ReadToEnd();

						if (soapResult.Contains("faults"))
						{
							Log.writeLog("> Kritischer Adressfehler!\r\n" + soapResult, true, true);
						}
						else
						{
							try
							{
								XmlDocument soapResultXml = new XmlDocument();
                                soapResultXml.LoadXml(soapResult);

                                WriteShipmentNumber(soapResultXml.GetElementsByTagName("parcelLabelNumber")[0].InnerXml, false);
								foreach (XmlElement dpdLabel in soapResultXml.GetElementsByTagName("content"))
								{
									String labelName = SaveDPDLabel(dpdLabel.InnerText);
                                    Sett.LabelTime = DateTimeOffset.Now;
                                    Log.writeLog("> " + Sett.LabelTime.ToString("dd.MM.yyyy HH:mm:ss:fff") + " - " + labelName + " wurde erfolgreich heruntergeladen!", false);

                                    if(Sett.printLabels.Equals("true") && !Sett.PrinterName.Equals("false"))
                                    {
                                        PrintHelper print = new PrintHelper(Sett, Log, labelName);
                                    }                   
								}
							}
							catch (Exception ex)
							{
								Log.writeLog(soapResult + ex.ToString(), true, true);
							}
						}


					}
				}
			}
			catch (WebException ex)
			{
                //Log the error message of the WebException
                AnotherApiConnectionTryHttp(ex, "DPD");
			}
			catch (Exception ex)
			{
                //If there is no WebException, log the "normal" exception
                AnotherApiConnectionTry(ex, "DPD");
			}
		}




		/// <summary>
		/// Saves the DPD label to file.
		/// </summary>
		private String SaveDPDLabel(String base64BinaryStr)
		{
			try
			{
				String labelName = "labels/" + DateTime.Now.ToString("ddMMyyyy-HHmmssfff") + "-" + Sett.ProgramUser + "-DPD-" + SqlH.XmlRecipient.Replace(" ", string.Empty).Replace("/", string.Empty).Replace("\\", string.Empty) + ".pdf";

				byte[] bytes = Convert.FromBase64String(base64BinaryStr);

				System.IO.FileStream stream =
				new FileStream(@labelName, FileMode.Create);
				System.IO.BinaryWriter writer = new BinaryWriter(stream);
				writer.Write(bytes, 0, bytes.Length);
				writer.Close();

				return labelName;
			}
			catch (Exception ex)
			{
				Log.writeLog("> Fehler beim Abspeichern des DPD-Labels!\r\n" + ex.ToString(), true, true);

				return null;
			}
		}
	}
}
