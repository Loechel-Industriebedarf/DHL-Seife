using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHL_Seife.prog.json
{
    public class GLSConsigneeJson
    {
        public string ConsigneeID;
        public Dictionary<string, string> Address = new Dictionary<string, string>();


        public GLSConsigneeJson(string ConsigneeID, string Name1, string Name2, string Name3, string Street, string ZIPCode, string City, string CountryCode, string ContactPerson, string eMail, string MobilePhoneNumber)
        {
            this.ConsigneeID = ConsigneeID;
            Address.Add("Name1", Name1);
            Address.Add("Name2", Name2);
            Address.Add("Name3", Name3);
            Address.Add("Street", Street);
            Address.Add("ZIPCode", ZIPCode);
            Address.Add("City", City);
            Address.Add("CountryCode", CountryCode);
            Address.Add("ContactPerson", ContactPerson);
            Address.Add("eMail", eMail);
            Address.Add("MobilePhoneNumber", MobilePhoneNumber);
        }
    }
}
