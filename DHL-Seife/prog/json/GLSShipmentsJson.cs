using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DHL_Seife.prog.json
{
    public class GLSShipmentsJson
    {
        [JsonProperty]
        public string Product { get; set; } = "PARCEL";

        [JsonProperty]
        public List<String> ShipmentReference { get; set; }

        [JsonProperty]
        public GLSConsigneeJson Consignee { get; set; }

        [JsonProperty]
        public GLSShipperJson Shipper { get; set; }

        [JsonProperty]
        public List<Dictionary<String, Double>> ShipmentUnit { get; set; }
    }
}
