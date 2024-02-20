using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHL_Seife.prog.json
{
    public class DHLShipperJson
    {
        public string name1;
        public string name2;
        public string name3;
        public string addressStreet;
        public string addressHouse;
        public string postalCode;
        public string city;
        public string country;
        public string email;
        public string phone;

        public DHLShipperJson(string shipper_name1, string shipper_name2, string shipper_name3, string shipper_addressStreet, string shipper_addressHouse, string shipper_postalCode, string shipper_city, string shipper_country, string shipper_email, string shipper_phone)
        {
            this.name1 = shipper_name1;
            this.name2 = shipper_name2;
            this.name3 = shipper_name3;
            this.addressStreet = shipper_addressStreet;
            this.addressHouse = shipper_addressHouse;
            this.postalCode = shipper_postalCode;
            this.city = shipper_city;
            this.country = shipper_country;
            this.email = shipper_email;
            this.phone = shipper_phone;
        }
    }
}
