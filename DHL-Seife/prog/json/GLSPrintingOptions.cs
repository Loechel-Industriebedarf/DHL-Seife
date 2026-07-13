using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHL_Seife.prog.json
{
    public class GLSPrintingOptions
    {
        public Dictionary<String, String> ReturnLabels = new Dictionary<String, String>();


        public GLSPrintingOptions(string TemplateSet, string LabelFormat)
        {
            ReturnLabels.Add("TemplateSet", TemplateSet);
            ReturnLabels.Add("LabelFormat", LabelFormat);
        }
    }
}
