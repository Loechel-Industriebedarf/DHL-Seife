using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHL_Seife.prog.json
{
    public class DHLShipmentsJson
    {
        public string product;
        public string billingNumber;
        public string refNo;
        public string creationSoftware;
        public string shipDate;
        public DHLShipperJson shipper;
        public DHLConsigneeJson consignee;
        public DHLDetailsJson details;

        public DHLShipmentsJson(string product, string billingNumber, string refNo, string creationSoftware, string shipDate, DHLShipperJson shipper, DHLConsigneeJson consignee, DHLDetailsJson details)
        {
            this.product = product;
            this.billingNumber = billingNumber;
            this.refNo = refNo;
            this.creationSoftware = creationSoftware;
            this.shipDate = shipDate;
            this.shipper = shipper;
            this.consignee = consignee;
            this.details = details;
        }
    }
}
