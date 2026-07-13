using DHL_Seife.prog.json;
using DHL_Seife.util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHL_Seife.prog
{
    [JsonObject(MemberSerialization.OptIn)]
    public class GLSJson
    {
        private SettingsReader Sett;
        private LogWriter Log;

        [JsonProperty(Order=1)]
        public GLSShipmentsJson Shipment { get; set; }
        [JsonProperty(Order=2)]
        public GLSPrintingOptions PrintingOptions { get; set; }

        //Headers
        public String Product = "PARCEL"; //default value for package in germany
        public List<String> ShipmentReference = new List<String>(); //Our order number
        public string ShipmentReferenceStr = null;

        public GLSConsigneeJson Consignee;
        public GLSShipperJson Shipper;
        public List<Dictionary<String, Double>> ShipmentUnit = new List<Dictionary<string, Double>>();


        // public String creationSoftware = "DHL-Seife 2.0 - Jetzt weniger seifig, dafür mehr restig"; //default value

        //Shipper - Only ContactID is needed
        public String ContactID = null;

        //Consignee
        public String ConsigneeID = null;
        //In Address
        public String Name1 = null;
        public String Name2 = null;
        public String Name3 = null;
        public String Street = null;
        public String ZIPCode = null;
        public String City = null;
        public String CountryCode = "DE";
        public String ContactPerson = null;
        public String eMail = null;
        public String MobilePhoneNumber = null;

        //ShipmentUnit - Only weight needed
        public Double Weight = 3; //default weight

        //PrintingOptions
        //ReturnLabels
        public String TemplateSet = "NONE";
        public String LabelFormat = "PDF";


        public GLSJson(SettingsReader sr)
        {
            Sett = sr;
        }

        /// <summary>
        /// 
        /// </summary>
        public void GenerateJson()
        {
            try
            {
                Consignee = new GLSConsigneeJson(
                    ConsigneeID,
                    Name1, Name2, Name3,
                    Street, ZIPCode, City, CountryCode,
                    ContactPerson, eMail, MobilePhoneNumber
                );

                Shipper = new GLSShipperJson(
                    ContactID
                );

                ShipmentUnit.Add(new Dictionary<string, Double>
                {
                    { "Weight", Weight }
                });

                ShipmentReference.Add(ShipmentReferenceStr);

                PrintingOptions = new GLSPrintingOptions(
                    TemplateSet, LabelFormat
                );

                Shipment = new GLSShipmentsJson
                {
                    Product = "PARCEL",
                    ShipmentReference = ShipmentReference,
                    Consignee = Consignee,
                    Shipper = Shipper,
                    ShipmentUnit = ShipmentUnit
                };
            }
            catch (Exception ex)
            {
                Log.writeLog(ex.ToString());
            }
            
        }

    }
}
