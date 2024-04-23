using DHL_Seife.util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DHL_Seife.prog
{
    class JSONHelper
    {
        private static SettingsReader Sett;
        private static LogWriter Log;
        private static SQLHelper SqlH;
        private static DHLJson dJson;
        private static DHLReturnJson dReJson;


        public string Json = ""; //Json to send to dhl

        /// <summary>
        /// This class creates an json string from the sql inputs, that got read earlier.
        /// </summary>
        /// <param name="settingsBuffer">An SettingsReader object, that contains all settings.</param>
        /// <param name="lw">An LogWriter object, to write logs, if exceptions occur.</param>
        /// <param name="sql">An SQLHelper object, to write logs, with all the data we need (name, street, weight etc.).</param>
        public JSONHelper(SettingsReader settingsBuffer, LogWriter lw, SQLHelper sql)
        {
            Sett = settingsBuffer;
            Log = lw;
            SqlH = sql;
            dJson =  new DHLJson(Sett);
            dReJson =  new DHLReturnJson(Sett);
        }


        public void DoDHLJsonMagic()
        {
            DoDHLJsonMagic(false);
        }
        public void DoDHLJsonMagic(Boolean isReturn)
        {
            try
            {
                dJson.details_weight_value = SqlH.XmlWeight;

                dJson.refNo = SqlH.XmlOurNumber;

                //Weight with comma?
                dJson.details_weight_value = SqlH.XmlWeight.Replace(",", ".");

                //If the country is not Germany, send an international parcel
                if (!SqlH.XmlCountry.ToLower().Equals("deutschland") && !SqlH.XmlCountry.ToLower().Equals("de"))
                {
                    dJson.product = "V53WPAK";  //international parcel
                    dJson.billingNumber = Sett.XmlAccountnumberInt; //international account number
                }


                //Length of email?
                if (SqlH.XmlMail.Length > 80) { dJson.consignee_email = SqlH.XmlMail.Substring(0, 80); }
                else { dJson.consignee_email = SqlH.XmlMail; }
                if (dJson.consignee_email == "") { dJson.consignee_email = null; }


                RefactorRecepientAddress();
                RefactorWeight();
                RefactorIfMercateo();
                SwitchCountryCode(); //DE => DEU etc.
                CheckForPackstation();
                CheckForRetail(); //Postfiliale
                CheckForPostNumber();

                //Do we ship multiple packages for the same person at once?
                if (Convert.ToDouble(SqlH.XmlPsCount) > 1)
                {
                    for (int i = 0; i < Convert.ToDouble(SqlH.XmlPsCount); i++)
                    {
                        String weightbuffer = SqlH.XmlWeightArray[i].ToString();
                        if (Convert.ToDouble(weightbuffer) > 30) { weightbuffer = "30"; }
                        dJson.details_weight_value = weightbuffer.Replace(",", ".");

                        int currentPackNum = i + 1;
                        dJson.refNo = SqlH.XmlOurNumber + " - Paket " + currentPackNum + " von " + Convert.ToDouble(SqlH.XmlPsCount);

                        if (isReturn) { dReJson.GenerateJson(dJson); }
                        else { dJson.GenerateJson(); }
                        
                    }
                }
                else
                {
                    if (isReturn) { dReJson.GenerateJson(dJson); }
                    else { dJson.GenerateJson(); }
                }

                if (isReturn) { SerializeReturnJson(); }
                else { SerializeJson(); }
            }
            catch (Exception ex)
            {
                Log.writeLog(ex.ToString());
            }
            
        }

            private void SerializeJson()
        {
            Json = JsonConvert.SerializeObject(dJson, Formatting.Indented, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            //Debug write to file
            using (StreamWriter writer = new StreamWriter("json.json"))
            {
                writer.WriteLine(Json);
            }
        }

        private void SerializeReturnJson()
        {
            Json = JsonConvert.SerializeObject(dReJson, Formatting.Indented, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            //Debug write to file
            using (StreamWriter writer = new StreamWriter("json.json"))
            {
                writer.WriteLine(Json);
            }
        }

            private void CheckForRetail()
        {
            //Takes the first 3 digit number and defines it as retailid
            String regex = @"\d{3}$";

            String regexStr1 = Regex.Match(dJson.consignee_name1, regex).Value;
            if (regexStr1 != null && dJson.consignee_name1.ToLower().Contains("postfiliale"))
            {
                dJson.consignee_retailID = regexStr1;
            }
            if (dJson.consignee_name2 != null)
            {
                if (dJson.consignee_name1.ToLower().Contains("postfiliale"))
                {
                    String regexStr2 = Regex.Match(dJson.consignee_name2, regex).Value;
                    if (regexStr2 != null) dJson.consignee_retailID = regexStr2;
                }   
            }
            if (dJson.consignee_name3 != null)
            {
                if (dJson.consignee_name3.ToLower().Contains("postfiliale"))
                {
                    String regexStr3 = Regex.Match(dJson.consignee_name3, regex).Value;
                    if (regexStr3 != null) dJson.consignee_retailID = regexStr3;
                }         
            }

        }

        private void CheckForPackstation()
        {
            //Takes the first 3 digit number in street and defines it as lockerid
            if (dJson.consignee_addressStreet.ToLower().Contains("packstation"))
            {
                dJson.consignee_lockerID = Regex.Match(dJson.consignee_addressStreet, @"\d{3}").Value;
            }

               
        }

        private void CheckForPostNumber()
        {
            //Takes the first 6-10 digit number in name1-name3 and defines it as postnumber
            String regex = @"\d[0-9]{6,10}$";
            if (dJson.consignee_lockerID != null || dJson.consignee_retailID != null)
            {
                String regexStr1 = Regex.Match(dJson.consignee_name1, regex).Value;
                if (regexStr1 != null) dJson.consignee_postNumber = regexStr1;
                if (dJson.consignee_name2 != null)
                {
                    String regexStr2 = Regex.Match(dJson.consignee_name2, regex).Value;
                    if (regexStr2 != null) dJson.consignee_postNumber = regexStr2;
                }
                if (dJson.consignee_name3 != null)
                {
                    String regexStr3 = Regex.Match(dJson.consignee_name3, regex).Value;
                    if (regexStr3 != null) dJson.consignee_postNumber = regexStr3;
                }
            }
        }

        /// <summary>
        /// Refactors some of the inputs, so we can use it correctly.
        /// </summary>
        private void RefactorRecepientAddress()
        {
            //These values have a max length; Cut them, if they are too long
            //If recipient(01) is too long, write the rest of it to recipient02. If recipient02 is too long, write the rest to recipient03
            int recLen = 35; //Max chars for Recipient 1, 2, 3, streetname and cityname
            try
            {
                int cutindex = 0;
                dJson.consignee_name1 = SqlH.XmlRecipient;
                dJson.consignee_name2 = SqlH.XmlRecipient02;
                dJson.consignee_name3 = SqlH.XmlRecipient03;
                

                while (dJson.consignee_name1.Length > recLen)
                {
                    cutindex = dJson.consignee_name1.LastIndexOf(" ");
                    dJson.consignee_name2 = dJson.consignee_name1.Substring(cutindex).Trim() + " " + dJson.consignee_name2.Trim();
                    dJson.consignee_name1 = dJson.consignee_name1.Substring(0, cutindex).Trim();
                }
                while (dJson.consignee_name2.Length > recLen)
                {
                    cutindex = dJson.consignee_name2.LastIndexOf(" ");
                    dJson.consignee_name3 = dJson.consignee_name2.Substring(cutindex).Trim() + " " + dJson.consignee_name3.Trim();
                    dJson.consignee_name2 = dJson.consignee_name2.Substring(0, cutindex).Trim();
                }
                if (dJson.consignee_name3.Length > recLen)
                {
                    dJson.consignee_name3 = dJson.consignee_name3.Substring(0, recLen).Trim();
                }

                if (dJson.consignee_name2 == "") { dJson.consignee_name2 = null; }
                if (dJson.consignee_name3 == "") { dJson.consignee_name3 = null; }

                Log.writeLog(dJson.consignee_name1);
                dJson.consignee_name = dJson.consignee_name1;

                if (SqlH.XmlStreet.Length > recLen) { dJson.consignee_addressStreet = SqlH.XmlStreet.Substring(0, recLen); }
                else { dJson.consignee_addressStreet = SqlH.XmlStreet; }

                //Check if the house number is missing. If it's missing, addressHouse must not be null
                //The "nothing" is a FIGURE SPACE U+2007, because normal spaces are ignored. :)
                if (!Regex.IsMatch(dJson.consignee_addressStreet, @"\d"))
                {
                    dJson.consignee_addressHouse = " ";
                }

                if (SqlH.XmlPlz.Length > 10) { dJson.consignee_postalCode = SqlH.XmlPlz.Substring(0, 10); }
                else { dJson.consignee_postalCode = SqlH.XmlPlz; }

                if (SqlH.XmlCity.Length > recLen) { dJson.consignee_city = SqlH.XmlCity.Substring(0, recLen); }
                else { dJson.consignee_city = SqlH.XmlCity; }
            }
            catch (Exception ex)
            {
                Log.writeLog("> Fehler beim Input-Refactoring der Empfängeradresse!\r\n" + ex.ToString(), true, true);
            }
        }

        private static void SwitchCountryCode()
        {
            //The two most common codes are hard coded, so we don't have to deal with a csv file in most cases
            if(SqlH.XmlCountryCode == "DE")
            {
                dJson.consignee_country = "DEU";
            }
            else if (SqlH.XmlCountryCode == "AT")
            {
                dJson.consignee_country = "AUT";
            }
            //Read csv file with country codes
            else
            {
                List<string> lkz = new List<string>();
                List<string> isoAlpha3 = new List<string>();

                using (var reader = new StreamReader(@"countryCodes.csv"))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        var values = line.Split(';');

                        lkz.Add(values[0]);
                        isoAlpha3.Add(values[1]);
                    }
                }

                //Search original country string in list
                //DE => DEU
                for (int i = 0; i < lkz.Count; i++)
                {
                    if (lkz[i] == SqlH.XmlCountryCode)
                    {
                        dJson.consignee_country = isoAlpha3[i];
                    }
                }
            }  
        }

        /// <summary>
        /// Refactors some of the inputs, so we can use it correctly.
        /// </summary>
        private void RefactorWeight() {
            try
            {
                double weight = Convert.ToDouble(SqlH.XmlWeight.Replace(".", ","));
                if (weight > 30) { dJson.details_weight_value = "30"; }
                else if (weight <= 0.001) { dJson.details_weight_value = "3"; }
                else if (weight < 0.1) { dJson.details_weight_value = "0.1"; }
            }
            catch (Exception ex)
            {
                Log.writeLog(ex.ToString(), true);
            }
        }


        /// <summary>
		/// Refactors some of the inputs, so we can use it correctly.
		/// </summary>
		private void RefactorIfMercateo()
        {
            if (SqlH.XmlOrderType.Equals("10"))
            {
                dJson.shipper_name1 = "Mercateo Deutschland AG";
                dJson.shipper_name2 = "c/o Auslieferungslager";
                dJson.shipper_name3 = "Löchel Industriebedarf";
            }
        }

        /// <summary>
		/// 
		/// </summary>
        public void AddBlankStreetNumber()
        {
            dJson.AddBlankStreetNumberToShipments();
            SerializeJson();
        }

    }
}
