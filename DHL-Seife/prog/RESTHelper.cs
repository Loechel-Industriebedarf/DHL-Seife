using DHL_Seife.util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using Spire.Pdf;
using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DHL_Seife.prog
{
    class RESTHelper
    {
        private SettingsReader Sett;
        private LogWriter Log;
        private SQLHelper SqlH;
        private JSONHelper JsonH;
        private static HttpWebRequest WebRequest;

        static int apiConnectTries = 0; //If the connection to the api fails, it should try again and increment this counter.


        /// <summary>
        /// This class creates an xml string from the sql inputs, that got read earlier.
        /// </summary>
        /// <param name="settingsBuffer">An SettingsReader object, that contains all settings.</param>
        /// <param name="lw">An LogWriter object, to write logs, if exceptions occur.</param>
        /// <param name="sql">An SQLHelper object, to write logs, with all the data we need (name, street, weight etc.).</param>
        /// <param name="xml">An XMLHelper object, with the xml data, that should be sent to the server.</param>
        public RESTHelper(SettingsReader settingsBuffer, LogWriter lw, SQLHelper sql, JSONHelper json)
        {
            Sett = settingsBuffer;
            Log = lw;
            SqlH = sql;
            JsonH = json;
        }

        public void SendDHLRestRequest(Boolean isReturn)
        {
            try
            {
                //Use TSL 1.2
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                //Encode username + password to base64
                String encoded = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("iso-8859-1").GetBytes(Sett.DHLUser + ":" + Sett.DHLPass));

                var client = new RestClient(Sett.DHLSoapConnection);
                var request = new RestRequest(Method.POST);
                request.AddHeader("Content-Type", "application/json");
                request.AddHeader("Accept-Language", "de-DE");
                request.AddHeader("Authorization", "Basic " + encoded);
                request.AddHeader("Accept", "application/json");
                request.AddHeader("Allow", "GET, PUT, POST, DELETE");
                request.AddHeader("dhl-api-key", Sett.DHLApiKey);
                request.AddParameter("application/json", JsonH.Json, ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);

                try
                {
                    dynamic dhlResponse = JObject.Parse(response.Content);

                    String labelDetail = dhlResponse.status.detail;
                    String shipmentNo = "";

                    foreach (dynamic item in dhlResponse.items) {
                        String labelName = "labels/" + DateTimeOffset.Now.ToString("ddMMyyyy-HHmmssfff") + "-" + Sett.ProgramUser + "-DHL-" + SqlH.XmlRecipient.Replace(" ", string.Empty).Replace("/", string.Empty).Replace("\\", string.Empty) + ".pdf";
                        String b64Label = item.label.b64;
                        SaveBase64Label(b64Label, labelName);

                        Sett.LabelTime = DateTimeOffset.Now;
                        Log.writeLog("> " + Sett.LabelTime.ToString("dd.MM.yyyy HH:mm:ss:fff") + " - " + labelName + " wurde erfolgreich heruntergeladen!", false);
                        Log.writeLog(labelDetail);
                        if(item.validationMessages != null)
                        {
                            if (item.validationMessages.Count > 0)
                            {
                                foreach (dynamic valMsg in item.validationMessages)
                                {
                                    Log.writeLog("[" + valMsg.validationState + "] " + valMsg.property + " - " + valMsg.validationMessage);
                                }
                            }
                        }
                        

                        //Print label
                        if (Sett.printLabels.Equals("true") && !Sett.PrinterName.Equals("false"))
                        {
                            PrintHelper print = new PrintHelper(Sett, Log, labelName);
                        }

                        if(shipmentNo != "") { shipmentNo = shipmentNo + ";"; }
                        shipmentNo = shipmentNo + item.shipmentNo;
                    }
                    
                    WriteShipmentNumber(shipmentNo, isReturn);
                }
                catch (Exception ex)
                {
                    Log.writeLog("> Kritischer Adressfehler:", true);
                    Log.writeLog(response.Content, true);
                    Log.writeLog(JsonH.Json, true);
                    Log.writeLog(ex.ToString(), true);
                }  
            }
            catch(Exception ex)
            {
                Log.writeLog(ex.ToString());
                AnotherApiConnectionTry("DHL", isReturn);
            }

        }

        //TODO: SaveBase64label, WriteShipmentNumber and AnotherAPIConnection are duplicates from SOAPHelper

        /// <summary>
        /// Saves the base 64 label to file.
        /// </summary>
        private string SaveBase64Label(string labelB64, string labelName)
        {
            try
            {
                byte[] bytes = Convert.FromBase64String(labelB64);

                System.IO.FileStream stream =
                new FileStream(@labelName, FileMode.Create);
                System.IO.BinaryWriter writer = new BinaryWriter(stream);
                writer.Write(bytes, 0, bytes.Length);
                writer.Close();

                return labelName;
            }
            catch (Exception ex)
            {
                Log.writeLog("> Fehler beim Abspeichern des DHL-Labels!\r\n" + ex.ToString(), true, true);

                return null;
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
        /// If we can't get a connection to the dhl api, try again.
        /// </summary>
        /// <param name="ex">The Exception that should be logged, before the program tries to connect again.</param>
        private void AnotherApiConnectionTry()
        {
            AnotherApiConnectionTry("DHL", false);
        }
        private void AnotherApiConnectionTry(String orderType, Boolean isReturn)
        {
            apiConnectTries++;
            //If there is an error while connecting to the api, try again 10 times
            if (apiConnectTries <= 10)
            {
                //logTextToFile("> Error while connecting to DHL-API!");
                Log.writeLog("> Fehler bei der Verbindung mit der DHL-API - neuer Versuch in 3 Sekunden!\r\n", true, false);
                System.Threading.Thread.Sleep(3000);
                switch (orderType)
                {
                    default:
                        SendDHLRestRequest(isReturn);
                        break;
                }
            }
            else
            {
                Log.writeLog("> Fehler bei der Verbindung mit der DHL-API!\r\n", true, true);
            }
        }

    }
}
