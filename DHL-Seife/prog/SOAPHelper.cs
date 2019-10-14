using DHL_Seife.util;
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
        public SOAPHelper(SettingsReader settingsBuffer, LogWriter lw, SQLHelper sql,  XMLHelper xml)
        {
            Sett = settingsBuffer;
            Log = lw;
            SqlH = sql;
            XmlH = xml;
        }


        /// <summary>
        /// Sends a soap request to the dhl-api and receives an answer in xml-format.
        /// Next, it reads the xml answer, downloads the label and prints it.
        /// </summary>
        /// 
        /// TODO: Do tons of refactoring...
        public void SendSoapRequest()
        {
            WebRequest = CreateWebRequest();

            try
            {
                using (Stream stream = WebRequest.GetRequestStream())
                {
                    XmlH.SoapEnvelopeXml.Save(stream);
                }
            }
            catch (Exception ex)
            {
                Log.writeLog(ex.ToString(), true);
            }



            try
            {
                // Get a soap response
                using (WebResponse response = WebRequest.GetResponse())
                {
                    using (StreamReader rd = new StreamReader(response.GetResponseStream()))
                    {
                        string soapResult = rd.ReadToEnd();

                        //Check, if a hard validation error occurs. If yes: log it.
                        //If a hard validation error occurs, no label is printed usually.
                        if (soapResult.Contains("Hard validation"))
                        {
                            //logTextToFile("Critical adress-error!");
                            Log.writeLog("> Kritischer Adressfehler!\r\n" + soapResult + "\r\n\r\n" + XmlH.Xml, true, true);
                        }
                        //Weak validation errors normally occur, when there is a "Leitcodenachentgelt" error from DHL
                        //Labels can be printed, but are a bit more expansive then.
                        else if (soapResult.Contains("Weak validation"))
                        {
                            //logTextToFile("You'll have to pay 'Leitcodenachentgelt' for this order!");
                            Log.writeLog("> Leitcodenachentgelt muss für diesen Auftrag bezahlt werden!");
                            Log.writeLog(soapResult, true);
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
                                labelName = "labels/" + DateTime.Now.ToString("ddMMyyyy-HHmmss") + "-" + SqlH.XmlRecipient.Replace(" ", string.Empty).Replace("/", string.Empty).Replace("\\", string.Empty) + ".pdf";
                                Client.DownloadFile(labelUrl, @labelName);
                            }
                            catch (Exception ex)
                            {
                                Log.writeLog(ex.ToString(), true);
                            }
                            //Print label
                            PrintHelper print = new PrintHelper(Sett, Log, labelName);
                        }

                        xnList = xmldoc.GetElementsByTagName("cis:shipmentNumber");
                        foreach (XmlNode xn in xnList)
                        {
                            string shipmentnumber = xn.InnerText;
                            WriteShipmentNumber(shipmentnumber);
                        }
                    }
                }
            }
            catch (WebException ex)
            {
                //Log the error message of the WebException
                AnotherApiConnectionTryHttp(ex);
            }
            catch (Exception ex)
            {
                //If there is no WebException, log the "normal" exception
                AnotherApiConnectionTry(ex);
            }

        }


        /// <summary>
        /// Create a soap webrequest to to the dhl-api. Also adds basic http-authentication.
        /// </summary>
        public HttpWebRequest CreateWebRequest()
        {
            String encoded = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("iso-8859-1").GetBytes(Sett.ApiUser + ":" + Sett.ApiPassword));

            //SOAP webrequest
            HttpWebRequest webRequest = null;
            try
            {
                webRequest = (HttpWebRequest)System.Net.WebRequest.Create(Sett.DHLSoapConnection);
                webRequest.Headers.Add("Authorization", "Basic " + encoded);
                webRequest.Headers.Add("SOAPAction: urn:createShipmentOrder");
                webRequest.ContentType = "text/xml;charset=\"utf-8\"";
                webRequest.Accept = "text/xml";
                webRequest.Method = "POST";
                webRequest.KeepAlive = true;
            }
            catch (Exception ex)
            {
                Log.writeLog(ex.ToString(), true);
            }
            return webRequest;
        }


        /// <summary>
        /// If we can't get a connection to the dhl api, log the error message and try again.
        /// </summary>
        /// <param name="ex">The WebException that should be logged, before the program tries to connect again.</param>
        private void AnotherApiConnectionTryHttp(WebException ex)
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
                            Log.writeLog("> Fehlerhafte Datenübermittlung an DHL!\r\n" + text + "\r\n\r\n" + XmlH.Xml, true, false);
                        }

                        //logTextToFile("> Error while connecting to DHL-API!");
                        Log.writeLog("> Fehler bei der Verbindung mit der DHL-API - neuer Versuch in 3 Sekunden!\r\n" + ex.ToString(), true, false);
                        System.Threading.Thread.Sleep(5000);
                        XmlH.DoXMLMagic();
                        SendSoapRequest();
                    }
                    else
                    {
                        Log.writeLog("> Fehlerhafte Datenübermittlung an DHL!\r\n" + ex.ToString() + "\r\n\r\n" + XmlH.Xml, true, true);
                    }
                }
            }
            catch (Exception ex1)
            {
                AnotherApiConnectionTry(ex1);
            }
        }


        /// <summary>
        /// If we can't get a connection to the dhl api, try again.
        /// </summary>
        /// <param name="ex">The Exception that should be logged, before the program tries to connect again.</param>
        private void AnotherApiConnectionTry(Exception ex)
        {
            apiConnectTries++;
            //If there is an error while connecting to the api, try again 10 times
            if (apiConnectTries <= 10)
            {
                //logTextToFile("> Error while connecting to DHL-API!");
                Log.writeLog("> Fehler bei der Verbindung mit der DHL-API - neuer Versuch in 3 Sekunden!\r\n" + ex.ToString(), true, false);
                System.Threading.Thread.Sleep(3000);
                XmlH.DoXMLMagic();
                SendSoapRequest();
            }
            else
            {
                Log.writeLog("> Fehler bei der Verbindung mit der DHL-API!\r\n" + ex.ToString(), true, true);
            }
        }







        /// <summary>
        /// Writes shipment-number to the database.
        /// </summary>
        /// <param name="shipmentnumber">The shipment number as a string, that should be inserted into the database.</param>
        private void WriteShipmentNumber(string shipmentnumber)
        {
            string sql = Sett.SqlShipmentnumber;
            sql = sql.Replace("%rowidshipmentnumber%", Sett.RowIdShipmentnumber);
            sql = sql.Replace("%rowid%", SqlH.RowId);
            sql = sql.Replace("%shipmentnumber%", shipmentnumber);
            string sql_carrier = Sett.SqlCarrierShipmentnumber;
            sql_carrier = sql_carrier.Replace("%rowidcarrier%", Sett.RowIdCarrier);
            sql_carrier = sql_carrier.Replace("%rowid%", SqlH.RowId);

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
        
    }
}
