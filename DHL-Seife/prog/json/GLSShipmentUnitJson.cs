using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHL_Seife.prog.json
{
    public class GLSShipmentUnitJson
    {
        public List<string> ShipmentUnit = new List<string>();


        public GLSShipmentUnitJson(string Weight)
        {
            ShipmentUnit.Add(Weight);
        }
    }
}
