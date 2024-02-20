using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHL_Seife.prog.json
{
    public class DHLConsigneeJson
    {
        public string name1;
        public string name2;
        public string name3;
        public string dispatchingInformation;
        public string addressStreet;
        public string addressHouse;
        public string additionalAddressInformation1;
        public string additionalAddressInformation2;
        public string postalCode;
        public string city;
        public string country;
        public string contactName;
        public string email;
        public string phone;
        public string lockerID;
        public string postNumber;
        public string name;
        public string retailID;

        public DHLConsigneeJson(string consignee_name1, string consignee_name2, string consignee_name3, string consignee_dispatchingInformation, string consignee_addressStreet, string consignee_addressHouse, string consignee_additionalAddressInformation1, string consignee_additionalAddressInformation2, string consignee_postalCode, string consignee_city, string consignee_country, string consignee_contactName, string consignee_email, string consignee_phone, string lockerID, string postNumber, string consignee_name, string consignee_retailID)
        {
            this.name1 = consignee_name1;
            this.name2 = consignee_name2;
            this.name3 = consignee_name3;
            this.dispatchingInformation = consignee_dispatchingInformation;
            this.addressStreet = consignee_addressStreet;
            this.addressHouse = consignee_addressHouse;
            this.additionalAddressInformation1 = consignee_additionalAddressInformation1;
            this.additionalAddressInformation2 = consignee_additionalAddressInformation2;
            this.postalCode = consignee_postalCode;
            this.city = consignee_city;
            this.country = consignee_country;
            this.contactName = consignee_contactName;
            this.email = consignee_email;
            this.phone = consignee_phone;
            this.lockerID = lockerID;
            this.postNumber = postNumber;
            this.name = consignee_name;
            this.retailID = consignee_retailID;
        }
    }
}
