using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHL_Seife.prog.json.details
{
    public class DHLDetailsDimJson
    {
        public string uom;
        public string height;
        public string length;
        public string width;

        public DHLDetailsDimJson(string details_dim_uom, string details_dim_height, string details_dim_length, string details_dim_width)
        {
            this.uom = details_dim_uom;
            this.height = details_dim_height;
            this.length = details_dim_length;
            this.width = details_dim_width;
        }
    }
}
