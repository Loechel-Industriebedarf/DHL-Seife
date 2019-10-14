using DHL_Seife.util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DHL_Seife.prog
{
    class SQLHelper
    {
        private SettingsReader sett;
        private LogWriter log;

        public string xmlweight = "1"; //In kg
        public string xmlpscount = "1"; //Number of shipments in a package
        public string xmlordertype = "1"; //Parcel type (Germany only or international)
        public string xmlrecipient = ""; //recipient name
        public string xmlrecipient02 = ""; //recipient name (second line)
        public string xmlrecipient03 = ""; //recipient name (third line)
        public string xmlstreet = ""; //recipient street
        public string xmlstreetnumber = ""; //recipient streetnumber
        public string xmlplz = ""; //recipient plz
        public string xmlcity = ""; //recipient city
        public string xmlcountry = "Deutschland"; //recipient country
        public string xmlmail = ""; //recipient mail
        public string xmlcommunicationmail = ""; //Mail that gets used for postfilals
        public string xmlournumber = "";
        public ArrayList xmlweightarray = new ArrayList(); //Number of shipments in a package
        public string xmlparceltype = "V01PAK"; //Parcel type (Germany only or international)
        public string xmlshippmentdate = DateTime.Now.ToString("yyyy-MM-dd"); //YYYY-MM-DD
        public string rowid = ""; //Row ID for insert 

        public SQLHelper(SettingsReader settingsBuffer, LogWriter lw)
        {
            sett = settingsBuffer;
            log = lw;     
        }


        public void doSQLMagic()
        {
            xmlournumber = sett.orderNumber;

            string sql = "SELECT dbo.AUFTRAGSKOPF.FSROWID, dbo.AUFTRAGSKOPF.BELEGART, LFIRMA1, LFIRMA2, RFIRMA1, RFIRMA2, DCOMPANY3, ICOMPANY3, LSTRASSE, RSTRASSE, LPLZ, RPLZ, LORT, RORT, LLAND, RLAND, " +
                "dbo.AUFTRAGSKOPF.CODE1, dbo.AUFTRAGSKOPF.BELEGNR, NetWeightPerSalesUnit, MENGE_BESTELLT, dbo.AUFTRAGSPOS.STATUS, dbo.AUFTRAGSPOS.FARTIKELNR, dbo.AUFTRAGSPOS.ARTIKELNR, " +
                "GEWICHT, (select count(*) from dbo.VERSANDGUT m2 where m2.BELEGNR = '" + sett.orderNumber + "') as PSCount " +
                "FROM dbo.AUFTRAGSKOPF, dbo.AUFTRAGSPOS " +
                "LEFT JOIN dbo.VERSANDGUT ON dbo.VERSANDGUT.BELEGNR = dbo.AUFTRAGSPOS.BELEGNR " +
                "WHERE dbo.AUFTRAGSKOPF.BELEGNR = '" + sett.orderNumber + "' AND dbo.AUFTRAGSPOS.BELEGNR = '" + sett.orderNumber + "'";

            OdbcDataReader dr = null;
            try
            {
                OdbcConnection conn = new OdbcConnection(sett.connectionString);
                conn.Open();
                OdbcCommand comm = new OdbcCommand(sql, conn);
                dr = comm.ExecuteReader();
            }
            catch (Exception ex)
            {
                log.writeLog(ex.ToString());
                log.writeLog(ex.Message.ToString(), true);
            }

            xmlweight = "0";
            String xmlweightTemp = "0";
            Boolean addPackaging = true; //If the weight wasn't added manually: add extra weight for packaging later
            try { 
                while (dr.Read())
                {
                    rowid = dr["FSROWID"].ToString();
                    if (!String.IsNullOrEmpty(dr["PSCount"].ToString()) && dr["PSCount"].ToString() != "0")
                    {
                        xmlpscount = dr["PSCount"].ToString();
                    }


                    xmlordertype = dr["BELEGART"].ToString();

                    if (String.IsNullOrEmpty(dr["LFIRMA1"].ToString())) { xmlrecipient = removeSpecialCharacters(dr["RFIRMA1"].ToString()); }
                    else { xmlrecipient = removeSpecialCharacters(dr["LFIRMA1"].ToString()); }

                    if (String.IsNullOrEmpty(dr["LFIRMA2"].ToString())) { xmlrecipient02 = removeSpecialCharacters(dr["RFIRMA2"].ToString()); }
                    else { xmlrecipient02 = removeSpecialCharacters(dr["LFIRMA2"].ToString()); }

                    if (String.IsNullOrEmpty(dr["DCOMPANY3"].ToString())) { xmlrecipient03 = removeSpecialCharacters(dr["ICOMPANY3"].ToString()); }
                    else { xmlrecipient03 = removeSpecialCharacters(dr["DCOMPANY3"].ToString()); }

                    if (String.IsNullOrEmpty(dr["LSTRASSE"].ToString()))
                    {
                        getStreetAndStreetnumber(dr, "RSTRASSE");
                    }
                    else
                    {
                        getStreetAndStreetnumber(dr, "LSTRASSE");
                    }

                    if (String.IsNullOrEmpty(dr["LPLZ"].ToString())) { xmlplz = dr["RPLZ"].ToString().Trim(); }
                    else { xmlplz = dr["LPLZ"].ToString().Trim(); }
                    //Check, if zip code contains letters
                    if (Regex.Matches(xmlplz, @"[a-zA-Z]").Count > 0)
                    {
                        //For zips like 5051DV
                        if (!xmlplz.ToLower().Contains(' '))
                        {
                            int i = 0;
                            //Check how many chars there are at the end of the zip
                            for (i = xmlplz.Length - 1; i >= 0; i--)
                            {
                                if (!char.IsDigit(xmlplz[i]))
                                {
                                    break;
                                }
                            }
                            //Substring starts at 1...; isDigit starts at 0
                            i--;

                            //DHL wants zips with numbers splitted: 5051DV -> 5051 DV
                            xmlplz = xmlplz.Substring(0, i) + " " + xmlplz.Substring(i);
                        }
                    }

                    if (String.IsNullOrEmpty(dr["LORT"].ToString())) { xmlcity = removeSpecialCharacters(dr["RORT"].ToString().Trim()); }
                    else { xmlcity = removeSpecialCharacters(dr["LORT"].ToString().Trim()); }

                    //Read delivery country; If it is emty, set it to "Deutschland"
                    if (!String.IsNullOrEmpty(dr["LLAND"].ToString())) { xmlcountry = dr["LLAND"].ToString().Trim(); }
                    else if (!String.IsNullOrEmpty(dr["RLAND"].ToString().Trim()))
                    {
                        xmlcountry = dr["RLAND"].ToString().Trim();
                    }
                    else { xmlcountry = "Deutschland"; }
                    if (String.IsNullOrEmpty(xmlcountry))
                    {
                        xmlcountry = "Deutschland";
                    }

                    //If the "CODE1" field contains an @, it is an e-mail adress.
                    //If the "CODE1" field contains an amazon adress, ignore it; Amazon blocks DHL mails
                    if (dr["CODE1"].ToString().Contains('@') && !dr["CODE1"].ToString().Contains("amazon"))
                    {
                        xmlmail = dr["CODE1"].ToString().Trim();
                    }
                    xmlcommunicationmail = dr["CODE1"].ToString().Trim();


                    xmlournumber = dr["BELEGNR"].ToString();
                    String netWeight = dr["NetWeightPerSalesUnit"].ToString();
                    String orderAmount = dr["MENGE_BESTELLT"].ToString();

                    try
                    {
                        if (dr["GEWICHT"].ToString() == null || dr["GEWICHT"].ToString() == "")
                        {
                            if (dr["STATUS"].ToString() == "2")
                            {
                                xmlweight = (Convert.ToDouble(xmlweight) + Convert.ToDouble(netWeight) * Convert.ToDouble(orderAmount)).ToString();
                            }
                            else
                            {
                                //If there are no positions with status 2, just take the weight of all positions
                                xmlweightTemp = (Convert.ToDouble(xmlweightTemp) + Convert.ToDouble(netWeight) * Convert.ToDouble(orderAmount)).ToString();
                            }
                        }
                        else
                        {
                            xmlweightarray.Add(dr["GEWICHT"].ToString());
                            xmlweight = dr["GEWICHT"].ToString();
                            addPackaging = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        //logTextToFile("> Article weight for "+ dr["ARTIKELNR"] + " missing!");
                        log.writeLog("> Artikelgewicht für " + dr["ARTIKELNR"] + " fehlt!");
                        log.writeLog(ex.ToString());
                        log.writeLog(ex.Message.ToString(), true);
                    }

                }
            }
            catch(Exception ex)
            {
                log.writeLog("> Unbekannter Fehler!");
                log.writeLog(ex.ToString(), true);
            }

            //If the weight is to small, set it to zero
            if (String.IsNullOrEmpty(xmlweight) || Convert.ToDouble(xmlweight) <= 0.001)
            {
                //If temporary weight was set (no position with status 2): use that one.
                if (String.IsNullOrEmpty(xmlweightTemp) || Convert.ToDouble(xmlweightTemp) <= 0.001)
                {
                    xmlweight = "0";
                }
                else
                {
                    xmlweight = (Convert.ToDouble(xmlweightTemp) + 0.3).ToString();
                }
            }
            //If the weight is fine and extra weight for packaging should be added, add 300 grams for packaging
            else
            {
                if (addPackaging)
                {
                    xmlweight = (Convert.ToDouble(xmlweight) + 0.3).ToString();
                }
            }
        }

        /// <summary>
        /// Figures out, if the input street contains a street number and returns the street name and number seperatly
        /// </summary>
        private void getStreetAndStreetnumber(OdbcDataReader dr, string streetDef)
        {
            string streetDefinition = dr[streetDef].ToString().Trim();
            xmlstreetnumber = "";
            xmlstreet = "";
            int lastindex = streetDefinition.LastIndexOf(" ");
            int lastindexdot = streetDefinition.LastIndexOf(".");
            int indexlength = streetDefinition.Length;

            try
            {
                //If there is no number in the string, write eveything into the street and set the street number to 0
                if (!streetDefinition.Any(char.IsDigit))
                {
                    xmlstreet = streetDefinition;
                    xmlstreetnumber = "0";
                }
                //The user didn't put a space before the street number (Teststr.123)
                //AND user didn't put a dot at the end of the adress line (Teststreet 1. | for weird people that write their adress like that...)
                else if (lastindexdot > lastindex && streetDefinition.Length - lastindexdot != 1)
                {
                    xmlstreet = streetDefinition.Substring(0, lastindexdot + 1).ToString();
                    xmlstreetnumber = streetDefinition.Substring(lastindexdot + 1).ToString();
                }
                //If the last degit of the adress is not a number (Teststreet; Teststreet 123B; Teststreet 123 B)
                else if (!char.IsDigit(streetDefinition[streetDefinition.Length - 1]))
                {
                    //There are no spaces and no numbers at the street number (Teststreet)
                    if (lastindex == -1)
                    {
                        xmlstreet = removeSpecialCharacters(streetDefinition);
                        xmlstreetnumber = "0";
                    }
                    //Last char is a letter (Teststreet 123 B)
                    else if (streetDefinition[lastindex].Equals(' ') && char.IsLetter(streetDefinition[lastindex + 1]))
                    {
                        xmlstreet = streetDefinition.Substring(0, lastindex).ToString();
                        int lastindexnew = xmlstreet.LastIndexOf(" ");
                        xmlstreet = streetDefinition.Substring(0, lastindexnew + 1).ToString();
                        xmlstreetnumber = streetDefinition.Substring(lastindexnew + 1).ToString();
                    }
                    //Last char is a letter and no spaces between streetnumber and letter (Teststreet 123B)
                    else
                    {
                        xmlstreet = streetDefinition.Substring(0, lastindex + 1).ToString();
                        xmlstreetnumber = streetDefinition.Substring(lastindex + 1).ToString();
                    }
                }
                //"Correct" street adress (Test street 123; Teststreet 123)
                //OR adress without spaces in the end (Teststreet123; Test street123)
                else
                {
                    int i = 1;
                    //The street number cannot contain more than 5 numbers
                    for (i = 1; i <= 5; i++)
                    {
                        //Fix for street numbers like 25-26
                        if (char.IsDigit(streetDefinition[streetDefinition.Length - i]) || streetDefinition[streetDefinition.Length - i].Equals('-'))
                        {
                            //Add the last digit to the end of the street number
                            xmlstreetnumber = streetDefinition[streetDefinition.Length - i].ToString() + xmlstreetnumber;
                        }
                        else
                        {
                            //If last number is actually a letter, just set the streetnumber to 0
                            if (i == 1)
                            {
                                xmlstreetnumber = "0";
                            }
                            //If there is no more number, break the loop
                            break;
                        }
                    }

                    xmlstreet = streetDefinition.Substring(0, streetDefinition.Length - i + 1);
                }

                xmlstreet = xmlstreet.Trim();
                xmlstreet = removeSpecialCharacters(xmlstreet);
                if (String.IsNullOrEmpty(xmlstreetnumber))
                {
                    xmlstreetnumber = "0";
                }
            }
            catch (Exception ex)
            {
                xmlstreet = removeSpecialCharacters(streetDefinition);
                xmlstreetnumber = "0";
                log.writeLog(ex.ToString());
                log.writeLog(ex.Message.ToString(), true);
            }

            //People don't like to write the word "street" completely
            // xmlstreet = xmlstreet.Replace("str.", "straße");
            // xmlstreet = xmlstreet.Replace("Str.", "Straße");
        }

        /// <summary>
        /// This function removes all special characters from a string.
        /// </summary>
        public string removeSpecialCharacters(string str)
        {
            return Regex.Replace(str, @"[^a-zA-Z0-9äÄöÖüÜß\/\-_.]+", " ", RegexOptions.Compiled);
        }
    }
}
