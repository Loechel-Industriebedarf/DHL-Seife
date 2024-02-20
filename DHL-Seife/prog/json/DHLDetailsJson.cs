using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DHL_Seife.prog.json.details;
using Newtonsoft.Json;

namespace DHL_Seife.prog.json
{
    [JsonObject(MemberSerialization.OptIn)]
    public class DHLDetailsJson
    {
        [JsonProperty]
        public DHLDetailsDimJson dim;
        [JsonProperty]
        public DHLDetailsWeightJson weight;

        public string details_dim_uom;
        public string details_dim_height;
        public string details_dim_length;
        public string details_dim_width;
        public string details_weight_uom;
        public string details_weight_value;

        public DHLDetailsJson(string details_dim_uom, string details_dim_height, string details_dim_length, string details_dim_width, string details_weight_uom, string details_weight_value)
        {
            this.details_dim_uom = details_dim_uom;
            this.details_dim_height = details_dim_height;
            this.details_dim_length = details_dim_length;
            this.details_dim_width = details_dim_width;
            this.details_weight_uom = details_weight_uom;
            this.details_weight_value = details_weight_value;

            //Currently not used
            //dim = new DHLDetailsDimJson(details_dim_uom, details_dim_height, details_dim_length, details_dim_width);
            weight = new DHLDetailsWeightJson(details_weight_uom, details_weight_value);
        }
    }
}
