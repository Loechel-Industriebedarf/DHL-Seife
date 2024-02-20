using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHL_Seife.prog.json.details
{
    public class DHLDetailsWeightJson
    {
        public string uom;
        public string value;

        public DHLDetailsWeightJson(string details_weight_uom, string details_weight_value)
        {
            this.uom = details_weight_uom;
            this.value = details_weight_value;
        }
    }
}
