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
    public class DHLReturnJson
    {
        private SettingsReader Sett;

        [JsonProperty]
        public String receiverId = "deu"; //id
        [JsonProperty]
        public String customerReference = ""; //customer
        [JsonProperty]
        public DHLShipperJson shipper;

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


        public DHLReturnJson(SettingsReader sr)
        {
            Sett = sr;
            receiverId = Sett.ReceiverId;
        }

        public void GenerateJson(DHLJson dJson)
        {
            shipper = new DHLShipperJson(
                dJson.consignee_name1, dJson.consignee_name2, dJson.consignee_name3,
                dJson.consignee_addressStreet, " ", dJson.consignee_postalCode,
                dJson.consignee_city, dJson.consignee_country,
                dJson.consignee_email, dJson.consignee_phone
            );

            customerReference = dJson.refNo;
        }
    }
}
