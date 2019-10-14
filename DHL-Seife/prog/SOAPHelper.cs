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
        private SettingsReader sett;
        private LogWriter log;
        private SQLHelper sqlh;
        private XMLHelper xmlh;
        private static HttpWebRequest webRequest;

        public SOAPHelper(SettingsReader settingsBuffer, LogWriter lw, SQLHelper sql,  XMLHelper xml)
        {
            sett = settingsBuffer;
            log = lw;
            sqlh = sql;
            xmlh = xml;
        }

        /// <summary>
        /// Sends a soap request to the dhl-api and receives an answer in xml-format.
        /// Next, it reads the xml answer and opens the labelUrl in the default web-browser.
        /// </summary>
        static int apiConnectTries = 0; //If the connection to the api fails, it should try again.
        public void sendSoapRequest()
        {
            webRequest = CreateWebRequest();

            try
            {
                using (Stream stream = webRequest.GetRequestStream())
                {
                    xmlh.soapEnvelopeXml.Save(stream);
                }
            }
            catch (Exception ex)
            {
                log.writeLog(ex.ToString());
                log.writeLog(ex.Message.ToString(), true);
            }



            try
            {
                // Get a soap response
                using (WebResponse response = webRequest.GetResponse())
                {
                    using (StreamReader rd = new StreamReader(response.GetResponseStream()))
                    {
                        string soapResult = rd.ReadToEnd();

                        //Check, if a hard validation error occurs. If yes: log it.
                        if (soapResult.Contains("Hard validation"))
                        {
                            //logTextToFile("Critical adress-error!");
                            log.writeLog("> Kritischer Adressfehler!\r\n" + soapResult + "\r\n\r\n" + xmlh.xml, true, true);
                        }
                        else if (soapResult.Contains("Weak validation"))
                        {
                            //logTextToFile("You'll have to pay 'Leitcodenachentgelt' for this order!");
                            log.writeLog("> Leitcodenachentgelt muss für diesen Auftrag bezahlt werden!");
                            log.writeLog(soapResult, true);
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
                                labelName = "labels/" + DateTime.Now.ToString("ddMMyyyy-HHmmss") + "-" + sqlh.xmlrecipient.Replace(" ", string.Empty).Replace("/", string.Empty).Replace("\\", string.Empty) + ".pdf";
                                Client.DownloadFile(labelUrl, @labelName);
                            }
                            catch (Exception ex)
                            {
                                log.writeLog(ex.ToString(), true);
                                log.writeLog(ex.Message.ToString(), true);
                            }
                            //Print label
                            PrintHelper print = new PrintHelper(sett, log, labelName);
                        }

                        xnList = xmldoc.GetElementsByTagName("cis:shipmentNumber");
                        foreach (XmlNode xn in xnList)
                        {
                            string shipmentnumber = xn.InnerText;
                            writeShipmentNumber(shipmentnumber);
                        }
                    }
                }
            }
            catch (WebException ex)
            {
                //Log the error message of the WebException
                anotherApiConnectionTryHttp(ex);
            }
            catch (Exception ex)
            {
                //If there is no WebException, log the "normal" exception
                anotherApiConnectionTry(ex);
            }

        }


        /// <summary>
        /// Create a soap webrequest to to the dhl-api. Also adds basic http-authentication.
        /// </summary>
        public HttpWebRequest CreateWebRequest()
        {
            String encoded = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("iso-8859-1").GetBytes(sett.api_user + ":" + sett.api_password));

            //SOAP webrequest
            HttpWebRequest webRequest = null;
            try
            {
                webRequest = (HttpWebRequest)WebRequest.Create(@sett.dhlsoapconnection);
                webRequest.Headers.Add("Authorization", "Basic " + encoded);
                webRequest.Headers.Add("SOAPAction: urn:createShipmentOrder");
                webRequest.ContentType = "text/xml;charset=\"utf-8\"";
                webRequest.Accept = "text/xml";
                webRequest.Method = "POST";
                webRequest.KeepAlive = true;
            }
            catch (Exception ex)
            {
                log.writeLog(ex.ToString(), true);
                log.writeLog(ex.Message.ToString(), true);
            }
            return webRequest;
        }


        /// <summary>
        /// If we can't get a connection to the dhl api, try again.
        /// </summary>
        private void anotherApiConnectionTryHttp(WebException ex)
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
                            log.writeLog("> Fehlerhafte Datenübermittlung an DHL!\r\n" + text + "\r\n\r\n" + xmlh.xml, true, false);
                        }

                        //logTextToFile("> Error while connecting to DHL-API!");
                        log.writeLog("> Fehler bei der Verbindung mit der DHL-API - neuer Versuch in 3 Sekunden!\r\n" + ex.ToString(), true, false);
                        System.Threading.Thread.Sleep(5000);
                        xmlh.doXMLMagic();
                        sendSoapRequest();
                    }
                    else
                    {
                        log.writeLog("> Fehlerhafte Datenübermittlung an DHL!\r\n" + ex.ToString() + "\r\n\r\n" + xmlh.xml, true, true);
                    }
                }
            }
            catch (Exception ex1)
            {
                anotherApiConnectionTry(ex1);
            }
        }


        /// <summary>
        /// If we can't get a connection to the dhl api, try again.
        /// </summary>
        private void anotherApiConnectionTry(Exception ex)
        {
            apiConnectTries++;
            //If there is an error while connecting to the api, try again 10 times
            if (apiConnectTries <= 10)
            {
                //logTextToFile("> Error while connecting to DHL-API!");
                log.writeLog("> Fehler bei der Verbindung mit der DHL-API - neuer Versuch in 3 Sekunden!\r\n" + ex.ToString(), true, false);
                System.Threading.Thread.Sleep(3000);
                xmlh.doXMLMagic();
                sendSoapRequest();
            }
            else
            {
                log.writeLog("> Fehler bei der Verbindung mit der DHL-API!\r\n" + ex.ToString(), true, true);
            }
        }


       




        /// <summary>
        /// Writes shipment-number to the database.
        /// </summary>
        private void writeShipmentNumber(string shipmentnumber)
        {
            string sql = sett.sqlshipmentnumber;
            sql = sql.Replace("%rowidshipmentnumber%", sett.rowidshipmentnumber);
            sql = sql.Replace("%rowid%", sqlh.rowid);
            sql = sql.Replace("%shipmentnumber%", shipmentnumber);
            string sql_carrier = sett.sql_carrier_shipmentnumber;
            sql_carrier = sql_carrier.Replace("%rowidcarrier%", sett.rowidcarrier);
            sql_carrier = sql_carrier.Replace("%rowid%", sqlh.rowid);

            try
            {
                OdbcConnection conn = new OdbcConnection(sett.connectionString);
                conn.Open();
                OdbcCommand comm = new OdbcCommand(sql, conn);
                OdbcCommand comm_carrier = new OdbcCommand(sql_carrier, conn);
                comm.ExecuteNonQuery();
                comm_carrier.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                log.writeLog(ex.ToString(), true);
                log.writeLog(ex.Message.ToString(), true);
            }

        }
        
    }
}
