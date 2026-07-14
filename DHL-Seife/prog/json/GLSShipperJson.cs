using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHL_Seife.prog.json
{
    public class GLSShipperJson
    {
        public string ContactID;
        public Dictionary<String, String> AlternativeShipperAddress = new Dictionary<String, String>();


        public GLSShipperJson(string ContactID, string AlternativeShipperAddressName1, string AlternativeShipperAddressName2, string AlternativeShipperAddressName3, string AlternativeShipperAddressCountryCode, string AlternativeShipperAddressCity, string AlternativeShipperAddressStreet, string AlternativeShipperAddressZIPCode)
        {
            this.ContactID = ContactID;
            //Only if we have an alternate shipper address
            if (!String.IsNullOrEmpty(AlternativeShipperAddressName1))
            {
                AlternativeShipperAddress.Add("Name1", AlternativeShipperAddressName1);
                AlternativeShipperAddress.Add("Name2", AlternativeShipperAddressName2);
                AlternativeShipperAddress.Add("Name3", AlternativeShipperAddressName3);
                AlternativeShipperAddress.Add("CountryCode", AlternativeShipperAddressCountryCode);
                AlternativeShipperAddress.Add("City", AlternativeShipperAddressCity);
                AlternativeShipperAddress.Add("Street", AlternativeShipperAddressStreet);
                AlternativeShipperAddress.Add("ZIPCode", AlternativeShipperAddressZIPCode);
            }
            
        }
    }
}
