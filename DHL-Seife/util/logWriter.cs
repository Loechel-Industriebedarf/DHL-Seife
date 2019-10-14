using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHL_Seife
{
    class logWriter
    {
        private static void Main(String log)
        {
            writeLog(log, false, false);
        }
        private static void Main(String log, Boolean nl)
        {
            writeLog(log, nl, false);
        }
        private static void Main(String log, Boolean nl, Boolean termin)
        {
            writeLog(log, nl, termin);
        }



        private static void writeLog(String log, Boolean nl, Boolean termin)
        {

        }

    }
}
