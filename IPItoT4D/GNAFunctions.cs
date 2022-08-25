using System;
using System.Runtime;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPItoT4D
{
    class GNAFunctions
    {

        public string formatDataLine(string strDataLine, string strNullDataString)
        {

            //Strip leading and training blanks
            strDataLine = strDataLine.Trim();
            // Replace all missing data ",," with the null data string
            // This is not really safe but the temperature will also show zero
            string strOld = ",,";
            string strNew = "," + strNullDataString + ",";
            strDataLine = strDataLine.Replace(strOld, strNew);
            strOld = ",,";
            strNew = ",";
            strDataLine = strDataLine.Replace(strOld, strNew);

            //strip the trailing commas from the line
            string strSubString = strDataLine.Substring(strDataLine.Length - 1, 1);
            if (strSubString == ",")
            {
                strDataLine = strDataLine.Substring(0, strDataLine.Length - 1);
            }

            return strDataLine;

        }

    }
}
