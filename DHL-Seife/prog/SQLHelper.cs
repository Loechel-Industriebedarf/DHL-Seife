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
		private SettingsReader Sett;
		private LogWriter Log;

		public string XmlWeight = "1"; //In kg
		public string XmlPsCount = "1"; //Number of shipments in a package
		public string XmlOrderType = "1"; //Parcel type (Germany only or international)
		public string XmlRecipient = ""; //recipient name
		public string XmlRecipient02 = ""; //recipient name (second line)
		public string XmlRecipient03 = ""; //recipient name (third line)
		public string XmlStreet = ""; //recipient street
		public string XmlStreetnumber = ""; //recipient streetnumber
		public string XmlPlz = ""; //recipient plz
		public string XmlCity = ""; //recipient city
		public string XmlCountry = "Deutschland"; //recipient country
		public string XmlCountryCode = "DE"; //recipient countrycode
		public string XmlMail = ""; //recipient mail
		public string XmlPhone = ""; //recipient mail
        public string XmlCommunicationMail = ""; //Mail that gets used for postfilals
		public string XmlOurNumber = "";
		public ArrayList XmlWeightArray = new ArrayList(); //Number of shipments in a package
        public ArrayList XmlRowNumArray = new ArrayList(); //Only first row of every shipment should be considered
		public string XmlParcelType = "V01PAK"; //Parcel type (Germany only or international)
		public string XmlShippmentDate = DateTime.Now.ToString("yyyy-MM-dd"); //YYYY-MM-DD
		public string RowId = ""; //Row ID for insert 
		public string XmlStatus = ""; //Order Status - 3 = completed

        /// <summary>
        /// This class handles data from a sql database.
        /// </summary>
        /// <param name="settingsBuffer">An SettingsReader object, that contains all settings.</param>
        /// <param name="lw">An LogWriter object, to write logs, if exceptions occur.</param>
        public SQLHelper(SettingsReader settingsBuffer, LogWriter lw)
		{
			Sett = settingsBuffer;
			Log = lw;
		}



		/// <summary>
		/// Reads all relevant values (name, street, country, packaging weight etc.) from a sql database and saves them to variables.
		/// </summary>
		public void DoSQLMagic()
		{
			XmlOurNumber = Sett.OrderNumber;

            string sql = "SELECT dbo.AUFTRAGSKOPF.FSROWID, dbo.AUFTRAGSKOPF.STATUS as KOPFSTATUS, dbo.AUFTRAGSKOPF.BELEGART, LFIRMA1, LFIRMA2, " +
                "RFIRMA1, RFIRMA2, DCOMPANY3, ICOMPANY3, " +
                "LSTRASSE, RSTRASSE, LPLZ, RPLZ, " +
                "LORT, RORT, LLAND, RLAND, " +
                "LLAENDERKZ, RLAENDERKZ, dbo.AUFTRAGSKOPF.CODE1, dbo.AUFTRAGSKOPF.BELEGNR, " +
                "NetWeightPerSalesUnit, MENGE_BESTELLT, dbo.AUFTRAGSPOS.STATUS, dbo.AUFTRAGSPOS.FARTIKELNR, dbo.AUFTRAGSKOPF.CODE4, " +
                "dbo.AUFTRAGSPOS.ARTIKELNR, GEWICHT, versanddatum, dbo.POSPACKSTUECKE.VERSANDGUTNR, " +
                "ROW_NUMBER() OVER(PARTITION BY dbo.POSPACKSTUECKE.VERSANDGUTNR ORDER BY dbo.POSPACKSTUECKE.VERSANDGUTNR DESC) rn, " +
                "(select count(*) from dbo.VERSANDGUT where BELEGNR = '" + Sett.OrderNumber + "' " +
                "AND dbo.VERSANDGUT.versanddatum > '" + Sett.StartTime.AddHours(-12).ToString("dd.MM.yyyy HH:mm:ss") + "') as PSCount " +
                "FROM dbo.AUFTRAGSKOPF, dbo.AUFTRAGSPOS " +
                "LEFT JOIN dbo.POSPACKSTUECKE ON dbo.POSPACKSTUECKE.FIXPOSNR = dbo.AUFTRAGSPOS.FIXPOSNR AND dbo.POSPACKSTUECKE.BELEGNR = dbo.AUFTRAGSPOS.BELEGNR " +
                "LEFT JOIN dbo.VERSANDGUT ON dbo.VERSANDGUT.VERSANDGUTNR = dbo.POSPACKSTUECKE.VERSANDGUTNR " +
                "WHERE dbo.AUFTRAGSKOPF.BELEGNR = '" + Sett.OrderNumber + "' AND dbo.AUFTRAGSPOS.BELEGNR = '" + Sett.OrderNumber + "' " +
                "ORDER BY versanddatum DESC, status";

			OdbcDataReader dr = null;
			try
			{
				OdbcConnection conn = new OdbcConnection(Sett.ConnectionString);
				conn.Open();
				OdbcCommand comm = new OdbcCommand(sql, conn);
				dr = comm.ExecuteReader();
			}
			catch (Exception ex)
			{
				Log.writeLog(ex.ToString(), true);
			}

			XmlWeight = "0";
			String xmlweightTemp = "0";
			Boolean addPackaging = true; //If the weight wasn't added manually: add extra weight for packaging later
			try
			{
				while (dr.Read())
				{
					RowId = dr["FSROWID"].ToString();
					if (!String.IsNullOrEmpty(dr["PSCount"].ToString()) && dr["PSCount"].ToString() != "0")
					{
						XmlPsCount = dr["PSCount"].ToString();
					}


					XmlOrderType = dr["BELEGART"].ToString();

					if (String.IsNullOrEmpty(dr["LFIRMA1"].ToString())) { XmlRecipient = RemoveSpecialCharacters(dr["RFIRMA1"].ToString()); }
					else { XmlRecipient = RemoveSpecialCharacters(dr["LFIRMA1"].ToString()); }

					if (String.IsNullOrEmpty(dr["LFIRMA2"].ToString())) { XmlRecipient02 = RemoveSpecialCharacters(dr["RFIRMA2"].ToString()); }
					else { XmlRecipient02 = RemoveSpecialCharacters(dr["LFIRMA2"].ToString()); }

					if (String.IsNullOrEmpty(dr["DCOMPANY3"].ToString())) { XmlRecipient03 = RemoveSpecialCharacters(dr["ICOMPANY3"].ToString()); }
					else { XmlRecipient03 = RemoveSpecialCharacters(dr["DCOMPANY3"].ToString()); }

					if (String.IsNullOrEmpty(dr["LSTRASSE"].ToString()))
					{
                        //Obsolete
                        //GetStreetAndStreetnumber(dr, "RSTRASSE");
                        XmlStreet = dr["RSTRASSE"].ToString().Trim();
                    }
					else
					{
                        //Obsolete
                        //GetStreetAndStreetnumber(dr, "LSTRASSE");
                        XmlStreet = dr["LSTRASSE"].ToString().Trim();
                    }
                    XmlStreetnumber = null;

                    if (String.IsNullOrEmpty(dr["LPLZ"].ToString())) { XmlPlz = dr["RPLZ"].ToString().Trim(); }
					else { XmlPlz = dr["LPLZ"].ToString().Trim(); }
					//Check, if zip code contains letters
					if (Regex.Matches(XmlPlz, @"[a-zA-Z]").Count > 0)
					{
						//For zips like 5051DV AND D-12345
						if (!XmlPlz.ToLower().Contains(' '))
						{
							int i = 0;
							//Check how many chars there are at the end of the zip
							for (i = -1; i >= 0; i--)
							{
								if (!char.IsDigit(XmlPlz[i]))
								{
									break;
								}
							}

							//User put a land code in front of the zip. For example D-12345
							if (i <= XmlPlz.Length / 2)
							{
								XmlPlz = Regex.Replace(XmlPlz, "[^0-9.]", "");
							}
							//For weird zips like 5051DV
							else
							{
								//Substring starts at 1...; isDigit starts at 0
								i--;

								//DHL wants zips with numbers splitted: 5051DV -> 5051 DV
								XmlPlz = XmlPlz.Substring(0, i) + " " + XmlPlz.Substring(i);
							}

						}
					}

					if (String.IsNullOrEmpty(dr["LORT"].ToString())) { XmlCity = RemoveSpecialCharacters(dr["RORT"].ToString().Trim()); }
					else { XmlCity = RemoveSpecialCharacters(dr["LORT"].ToString().Trim()); }

					//Read delivery country; If it is emty, set it to "Deutschland"
					if (!String.IsNullOrEmpty(dr["LLAND"].ToString())) { XmlCountry = dr["LLAND"].ToString().Trim(); }
					else if (!String.IsNullOrEmpty(dr["RLAND"].ToString().Trim()))
					{
						XmlCountry = dr["RLAND"].ToString().Trim();
					}
					else { XmlCountry = "Deutschland"; }
					if (String.IsNullOrEmpty(XmlCountry))
					{
						XmlCountry = "Deutschland";
					}

					//Read delivery countrycode; If it is emty, set it to "DE" (Germany)
					if (!String.IsNullOrEmpty(dr["LLAENDERKZ"].ToString())) { XmlCountryCode = dr["LLAENDERKZ"].ToString().Trim(); }
					else if (!String.IsNullOrEmpty(dr["RLAENDERKZ"].ToString().Trim()))
					{
						XmlCountryCode = dr["RLAENDERKZ"].ToString().Trim();
					}
					else { XmlCountryCode = "DE"; }
					if (String.IsNullOrEmpty(XmlCountry))
					{
						XmlCountryCode = "DE";
					}

					//If the "CODE1" field contains an @, it is an e-mail adress.
					//If the "CODE1" field contains an amazon adress, ignore it; Amazon blocks DHL mails
					if (dr["CODE1"].ToString().Contains('@') && !dr["CODE1"].ToString().Contains("amazon"))
					{
						XmlMail = dr["CODE1"].ToString().Trim();
					}
					XmlCommunicationMail = dr["CODE1"].ToString().Trim();


					XmlOurNumber = dr["BELEGNR"].ToString();
					String netWeight = dr["NetWeightPerSalesUnit"].ToString();
					String orderAmount = dr["MENGE_BESTELLT"].ToString();

                    XmlStatus = dr["KOPFSTATUS"].ToString();

                    XmlPhone = dr["CODE4"].ToString();

                    try
					{
                        //GEWICHT => Weight of VERSANDGUT
                        if (dr["GEWICHT"].ToString() == null || dr["GEWICHT"].ToString() == "" || dr["PSCount"].ToString() == "0")
						{
							if (dr["STATUS"].ToString() == "2")
							{
								XmlWeight = (Convert.ToDouble(XmlWeight) + Convert.ToDouble(netWeight) * Convert.ToDouble(orderAmount)).ToString();
							}
							else
							{
								//If there are no positions with status 2, just take the weight of all positions
								xmlweightTemp = (Convert.ToDouble(xmlweightTemp) + Convert.ToDouble(netWeight) * Convert.ToDouble(orderAmount)).ToString();
							}
						}
						else if (dr["versanddatum"].ToString() != null || dr["versanddatum"].ToString() != "")
						{
                            DateTimeOffset versandDatum = DateTime.Parse(dr["versanddatum"].ToString());
                            TimeSpan timeSinceLastPackage = versandDatum.Subtract(Sett.StartTime);

                            //Only add to array, if the package is younger than 12 hours
                            if(System.Math.Abs(timeSinceLastPackage.Hours) < 12)
                            {
                                XmlWeightArray.Add(dr["GEWICHT"].ToString());
                                XmlRowNumArray.Add(dr["rn"].ToString());
                                XmlWeight = dr["GEWICHT"].ToString();
                                addPackaging = false;
                            }         
						}
					}
					catch (Exception ex)
					{
						//logTextToFile("> Article weight for "+ dr["ARTIKELNR"] + " missing!");
						Log.writeLog("> Artikelgewicht für " + dr["ARTIKELNR"] + " fehlt!");
						Log.writeLog(ex.ToString(), true);
					}

				}
			}
			catch (Exception ex)
			{
				Log.writeLog("> Unbekannter Fehler!");
				Log.writeLog(ex.ToString(), true);
			}

			//If the weight is to small, set it to zero
			if (String.IsNullOrEmpty(XmlWeight) || Convert.ToDouble(XmlWeight) <= 0.001)
			{
				//If temporary weight was set (no position with status 2): use that one.
				if (String.IsNullOrEmpty(xmlweightTemp) || Convert.ToDouble(xmlweightTemp) <= 0.001)
				{
					XmlWeight = "0";
				}
				else
				{
					XmlWeight = (Convert.ToDouble(xmlweightTemp) + 0.3).ToString();
				}
			}
			//If the weight is fine and extra weight for packaging should be added, add 300 grams for packaging
			else
			{
				if (addPackaging)
				{
					XmlWeight = (Convert.ToDouble(XmlWeight) + 0.3).ToString();
				}
			}
		}

		/// <summary>
		/// Figures out, if the input street contains a street number and returns the street name and number seperatly
		/// </summary>
		/// <param name="dr">An OdbcDataReader object.</param>
		/// <param name="streetDef">A string, that tells the program if it should look for RSTREET or LSTREET.</param>
		public void GetStreetAndStreetnumber(string streetDef)
		{
			string streetDefinition = streetDef.Trim(); //String that contains the street name + street number
			XmlStreetnumber = "";
			XmlStreet = "";
			int lastindex = streetDefinition.LastIndexOf(" "); //Last space in the string
			int firstindex = streetDefinition.IndexOf(" "); //First space in the string
			int lastindexdot = streetDefinition.LastIndexOf("."); //Last dot in the string
			int indexlength = streetDefinition.Length;

			try
			{
				//If there is no number in the string, write eveything into the street and set the street number to "nothing"
                //The "nothing" is a FIGURE SPACE U+2007, because normal spaces are ignored.
				if (!streetDefinition.Any(char.IsDigit))
				{
					XmlStreet = streetDefinition;
					XmlStreetnumber = " ";
				}
				//The user puts his street number BEFORE the actual street (12a Teststreet)
				else if (char.IsDigit(streetDefinition[0]))
				{
					XmlStreet = streetDefinition.Substring(firstindex + 1).ToString();
					XmlStreetnumber = streetDefinition.Substring(0, firstindex + 1);
				}
				//The user didn't put a space before the street number (Teststr.123)
				//AND user didn't put a dot at the end of the adress line (Teststreet 1. | for weird people that write their adress like that...)
				else if (lastindexdot > lastindex && streetDefinition.Length - lastindexdot != 1)
				{
					XmlStreet = streetDefinition.Substring(0, lastindexdot + 1).ToString();
					XmlStreetnumber = streetDefinition.Substring(lastindexdot + 1).ToString();
				}
				//If the last degit of the adress is not a number (Teststreet; Teststreet 123B; Teststreet 123 B)
				else if (!char.IsDigit(streetDefinition[streetDefinition.Length - 1]))
				{
					//There are no spaces and no numbers at the street number (Teststreet)
					if (lastindex == -1)
					{
						XmlStreet = RemoveSpecialCharacters(streetDefinition);
						XmlStreetnumber = " ";
					}
					//Last char is a letter (Teststreet 123 B)
					else if (streetDefinition[lastindex].Equals(' ') && char.IsLetter(streetDefinition[lastindex + 1]))
					{
						XmlStreet = streetDefinition.Substring(0, lastindex).ToString();
						int lastindexnew = XmlStreet.LastIndexOf(" ");
						XmlStreet = streetDefinition.Substring(0, lastindexnew + 1).ToString();
						XmlStreetnumber = streetDefinition.Substring(lastindexnew + 1).ToString();
					}
					//Last char is a letter and no spaces between streetnumber and letter (Teststreet 123B)
					else
					{
						XmlStreet = streetDefinition.Substring(0, lastindex + 1).ToString();
						XmlStreetnumber = streetDefinition.Substring(lastindex + 1).ToString();
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
							XmlStreetnumber = streetDefinition[streetDefinition.Length - i].ToString() + XmlStreetnumber;
						}
						else
						{
							//If last number is actually a letter, just set the streetnumber to 0
							if (i == 1)
							{
								XmlStreetnumber = " ";
							}
							//If there is no more number, break the loop
							break;
						}
					}

					XmlStreet = streetDefinition.Substring(0, streetDefinition.Length - i + 1);
				}

				XmlStreet = XmlStreet.Trim();
				XmlStreet = RemoveSpecialCharacters(XmlStreet);
				if (String.IsNullOrEmpty(XmlStreetnumber))
				{
					XmlStreetnumber = " ";
				}
			}
			catch (Exception ex)
			{
				XmlStreet = RemoveSpecialCharacters(streetDefinition);
				XmlStreetnumber = " ";
				Log.writeLog(ex.ToString(), true);
			}

			//People don't like to write the word "street" completely
			//Didn't really make a diffence, so it is not used anymore
			// xmlstreet = xmlstreet.Replace("str.", "straße");
			// xmlstreet = xmlstreet.Replace("Str.", "Straße");
		}

		/// <summary>
		/// This function removes all special characters from a string.
		/// </summary>
		/// <param name="str">The string that should be edited.</param>
		/// <returns>
		/// The inputted string without special characters.
		/// </returns>
		public string RemoveSpecialCharacters(string str)
		{
			return Regex.Replace(str, @"[^a-zA-Z0-9äÄöÖüÜß\/\-_.]+", " ", RegexOptions.Compiled);
		}
	}
}
