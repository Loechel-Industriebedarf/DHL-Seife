using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DHL_Seife.prog.json.details;
using Newtonsoft.Json;

namespace DHL_Seife.prog.json
{
    public class DHLServicesJson
    {
        public string parcelOutletRouting = "info@loechel-industriebedarf.de";

        public DHLServicesJson(string parcelOutletRouting)
        {
            //Only change value, if we have a customer mail address
            if (!String.IsNullOrEmpty(parcelOutletRouting))
            {
                this.parcelOutletRouting = parcelOutletRouting;
            }     
        }
    }
}
