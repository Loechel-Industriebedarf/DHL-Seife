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
    public class DHLJson
    {
        private SettingsReader Sett;

        [JsonProperty]
        public String profile = "STANDARD_GRUPPENPROFIL"; //???
        [JsonProperty]
        public List<DHLShipmentsJson> shipments = new List<DHLShipmentsJson>();

        public DHLShipperJson shipper;
        public DHLConsigneeJson consignee;
        public DHLDetailsJson details;

        public String product = "V01PAK"; //default value for package in germany
        public String billingNumber = "";
        public String refNo = null; //Our order number
        public String creationSoftware = "DHL-Seife 2.0 - Jetzt weniger seifig, dafür mehr restig"; //default value
        public String shipDate = DateTime.Now.ToString("yyyy-MM-dd"); //Current date

        public String shipper_name1 = "Löchel Industriebedarf"; //default shipper name
        public String shipper_name2 = null;
        public String shipper_name3 = null;
        public String shipper_addressStreet = "Hans-Hermann-Meyer Str."; //default address
        public String shipper_addressHouse = "2"; //default house number
        public String shipper_postalCode = "27232"; //default postal code
        public String shipper_city = "Sulingen"; //default city
        public String shipper_country = "DEU"; //default country
        public String shipper_email = "info@loechel-industriebedarf.de"; //default mail
        public String shipper_phone = "+49 042715727"; //default phone

        public String consignee_name1 = null;
        public String consignee_name2 = null;
        public String consignee_name3 = null;
        public String consignee_dispatchingInformation = null;
        public String consignee_addressStreet = null;
        public String consignee_addressHouse = null;
        public String consignee_additionalAddressInformation1 = null;
        public String consignee_additionalAddressInformation2 = null;
        public String consignee_postalCode = null;
        public String consignee_city = null;
        public String consignee_country = "DEU";
        public String consignee_contactName = null;
        public String consignee_email = null;
        public String consignee_phone = null;

        public String consignee_name = null;
        public String consignee_lockerID = null;
        public String consignee_postNumber = null;
        public String consignee_retailID = null;

        public String details_dim_uom = "cm"; //size unit
        public String details_dim_height = "10"; //default height
        public String details_dim_length = "10"; //default length
        public String details_dim_width = "10"; //default width

        public String details_weight_uom = "kg"; //weight unit
        public String details_weight_value = "3"; //default weight
        

        public DHLJson(SettingsReader sr)
        {
            Sett = sr;
            billingNumber = Sett.XmlAccountnumber; //default value; TODO: International id
        }

        public void GenerateJson()
        {
            shipper = new DHLShipperJson(
                shipper_name1, shipper_name2, shipper_name3,
                shipper_addressStreet, shipper_addressHouse, shipper_postalCode, 
                shipper_city, shipper_country,
                shipper_email, shipper_phone
            );

            consignee = new DHLConsigneeJson(
                consignee_name1, consignee_name2, consignee_name3,
                consignee_dispatchingInformation, consignee_addressStreet, consignee_addressHouse,
                consignee_additionalAddressInformation1, consignee_additionalAddressInformation2,
                consignee_postalCode, consignee_city, consignee_country,
                consignee_contactName, consignee_email, consignee_phone,
                consignee_lockerID, consignee_postNumber, consignee_name,
                consignee_retailID
            );

            //Height, length, width currently gets ignored
            details = new DHLDetailsJson(
                details_dim_uom, details_dim_height, details_dim_length, details_dim_width, details_weight_uom, details_weight_value
            );

            shipments.Add(new DHLShipmentsJson(
                product, billingNumber, refNo, creationSoftware, shipDate, shipper, consignee, details
            ));
        }
    }
}
