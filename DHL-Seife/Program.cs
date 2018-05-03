using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Net;
using System.IO;
using System.Threading;

namespace DHLSoapTest
{
    class Program
    {

        /// <summary>
        /// Execute a Soap WebService call
        /// </summary>
        public static void Execute()
        {
            /* ID in xml thingy */
            /* 00 */
            string xmluser = "2222222222_01";
            /* 10 */
            string xmlpass = "pass";
            /* 11 */
            string xmlaccountnumber = "22222222220101";
            /* 12 */
            string xmlournumber = "12345";
            /* 01 */
            string xmlshippmentdate = "2018-05-04"; //YYYY-MM-DD
                                                    /* 02 */
            string xmlweight = "1"; //In kg
                                    /* 03 */
            string xmlmail = "info@loechel-industriebedarf.de"; //recipient mail
                                                                /* 04 */
            string xmlrecipient = "Max Mustermann"; //recipient name
                                                    /* 05 */
            string xmlstreet = "Musterstraße"; //recipient street
                                               /* 06 */
            string xmlstreetnumber = "3"; //recipient streetnumber
                                          /* 07 */
            string xmlplz = "27254"; //recipient plz
                                     /* 08 */
            string xmlcity = "Siedenburg"; //recipient city
                                           /* 09 */
            string xmlcountry = "Deutschland"; //recipient country







            HttpWebRequest request = CreateWebRequest();
            XmlDocument soapEnvelopeXml = new XmlDocument();
            String xml = String.Format(@"<?xml version=""1.0"" encoding=""utf-8""?>
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
            <sequenceNumber>01</sequenceNumber>
            <Shipment>
               <ShipmentDetails>
                  <product>V01PAK</product>
                  <cis:accountNumber>{11}</cis:accountNumber>
                  <customerReference>{12}</customerReference>
                  <shipmentDate>{1}</shipmentDate>
                  <ShipmentItem>
                     <weightInKG>{2}</weightInKG>
                  </ShipmentItem>
                  <Notification>
                     <recipientEmailAddress>{3}</recipientEmailAddress>
                  </Notification>
               </ShipmentDetails>
               <Shipper>
                  <Name>
                     <cis:name1>Löchel Industriebedarf</cis:name1>
                  </Name>
                  <Address>
                     <cis:streetName>Hans-Hermann-Meyer-Strasse</cis:streetName>
                     <cis:streetNumber>2</cis:streetNumber>
                     <cis:addressAddition>?</cis:addressAddition>
                     <cis:zip>27232</cis:zip>
                     <cis:city>Sulingen</cis:city>    
                     <cis:Origin>
                        <cis:country>Deutschland</cis:country>
                     </cis:Origin>
                  </Address>
                  <Communication>
                  <cis:phone>+49 4271 5727</cis:phone>
                  </Communication>
               </Shipper>
               <Receiver>
                  <cis:name1>{4}</cis:name1>
                  <Address>
                     <cis:streetName>{5}</cis:streetName>
                     <cis:streetNumber>{6}</cis:streetNumber>
                     <cis:addressAddition>?</cis:addressAddition>
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
         </ShipmentOrder>
      </bus:CreateShipmentOrderRequest>
   </soapenv:Body>
</soapenv:Envelope>", xmluser, xmlshippmentdate, xmlweight, xmlmail, xmlrecipient, xmlstreet, xmlstreetnumber, xmlplz, xmlcity, xmlcountry, xmlpass, xmlaccountnumber, xmlournumber);
            soapEnvelopeXml.LoadXml(xml);

            using (Stream stream = request.GetRequestStream())
            {
                soapEnvelopeXml.Save(stream);
            }


            // Get a soap response
            using (WebResponse response = request.GetResponse())
            {
                using (StreamReader rd = new StreamReader(response.GetResponseStream()))
                {
                    string soapResult = rd.ReadToEnd();



                    XmlDocument xmldoc = new XmlDocument();
                    xmldoc.LoadXml(soapResult);

                    XmlNodeList xnList = xmldoc.GetElementsByTagName("labelUrl");
                    foreach (XmlNode xn in xnList)
                    {
                        string labelUrl = xn.InnerText;
                        System.Diagnostics.Process.Start(labelUrl);
                    }
                }
            }
        }


        /// <summary>
        /// Create a soap webrequest to [Url]
        /// </summary>
        /// <returns></returns>
        public static HttpWebRequest CreateWebRequest()
        {
            //Basic http authentication
            String username = "USERNAME";
            String password = "PASSWORD";
            String encoded = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(username + ":" + password));

            //SOAP webrequest
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(@"https://cig.dhl.de/services/sandbox/soap");
            webRequest.Headers.Add("Authorization", "Basic " + encoded);
            webRequest.Headers.Add(@"SOAP:Action");
            webRequest.ContentType = "text/xml;charset=\"utf-8\"";
            webRequest.Accept = "text/xml";
            webRequest.Method = "POST";
            return webRequest;
        }

        static void Main(string[] args)
        {
            Execute();
        }
    }
}
