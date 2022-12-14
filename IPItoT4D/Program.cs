using System;
using System.IO;
using System.Configuration;
using System.Runtime;



namespace IPItoT4D
{
    class Program
    {
        static void Main(string[] args)
        {
            string strFileExtension, strDataloggerDataFolder, strT4DDataFolder, strConversionTrigger;
            string strT4DfileName = null;
            string strDataLine = null;
            string strSubString = null;
            string strFormat = "0.";
            string strDataloggerIDList = null;
            string strDataloggerID = null;
            string strTrashLineFlag = "Yes";
            string strHeaderLine;
            string strNullDataString = "oink";

            var T4Ddata = new string[100000];                   // 100000 = No of data lines that will appear in a datalogger file
            var Columns = new string[200];                      // Flags to indicate whether conversion must take place or not



            int i;
            int intT4DdataCounter = 0;
            int intDecimalPlaces;
            //int intNoOfColumns = 0;
            int intNoOfHeaderColumns;


            double dblConversionFactor;
            double dblDataElement;


            GNAFunctions gna = new GNAFunctions();


            // Welcome message & License check

 



            //declare constants
            strFileExtension = ConfigurationManager.AppSettings["configFileExtension"];
            strDataloggerDataFolder = ConfigurationManager.AppSettings["configDataloggerFilesFolder"];
            strT4DDataFolder = ConfigurationManager.AppSettings["configT4DFilesFolder"];
            strConversionTrigger = ConfigurationManager.AppSettings["configConversionTrigger"];
            dblConversionFactor = Convert.ToDouble(ConfigurationManager.AppSettings["configConversionFactor"]);
            intDecimalPlaces = Convert.ToInt16(ConfigurationManager.AppSettings["configDecimalPlaces"]);
            for (i = 0; i < intDecimalPlaces; i++)
            {
                strFormat = strFormat + "#";
            }
            strNullDataString = ConfigurationManager.AppSettings["configNullDataString"];
            strDataloggerIDList = ConfigurationManager.AppSettings["configDataloggerIDList"];


            // Extract the list of dataloggers whose files must be processed
            // split the strDataloggerIDList and assign it to the array strDataloggerIDArray

            string[] strDataloggerIDArray = strDataloggerIDList.Split(',');

            foreach (string strID in strDataloggerIDArray)
            {
                strDataloggerID = strID.Trim();

                Console.WriteLine(strDataloggerID);

                strT4DfileName = strT4DDataFolder + "\\" + strDataloggerID + "-converted-readings.csv";

                //Initialise variables;
                string strFileFilter = "*." + strFileExtension;

                //Read the datalogger file names that are ready for conversion
                string[] DataloggerFiles = Directory.GetFiles(strDataloggerDataFolder, strFileFilter);
                // Sort the files
                Array.Sort(DataloggerFiles);
                //The array DataloggerFiles now contains the names of each datalogger file to be processed, earliest one first
                //This is now the primary processing loop.
                string strFirstLoggerFile = "Yes";

                string strObservationLine = "No";

                foreach (string strDataloggerFileName in DataloggerFiles)
                {

                    strTrashLineFlag = "Yes";

                    //Console.WriteLine(strDataloggerFileName);
                    //Console.ReadLine();

                    //Process datalogger file provided it contains the datalogger ID in the file name

                    if (strDataloggerFileName.IndexOf(strDataloggerID) != -1)       //searching for the substring of the datalogger ID
                    {

                        var lines = File.ReadLines(strDataloggerFileName);
                        foreach (var line in lines)
                        {
                            strDataLine = line;

                            // step through the lines looking for the header line that starts with "Date-and-time"
                            // This line is the last header line and is followed by data lines.

                            string strTempString = strDataLine + "xxxxxxxxxxxxxxxxxxxx";

                            strSubString = strTempString.Substring(1, 13);

                            if (strSubString == "Date-and-time")
                            {
                                strHeaderLine = "Yes";
                                strTrashLineFlag = "No";                //Assumption that lines following the header are valid data lines
                                strObservationLine = "Yes";             // Set this flag so that the subsequent lines are treated as data lines

                                // Prepare the data line
                                strDataLine = gna.formatDataLine(strDataLine, strNullDataString);

                                // Now prepare the array that holds the conversion indicators for each column
                                // If the column header contains the conversion phrase (configConversionTrigger), then the data in that column must be converted
                                // Split the header line, look for the phrase, and if found, set the conversion indicator
                                // Now extract the elements of the header line and write to the array Headers[]

                                string[] Headers = strDataLine.Split(',');

                                // Check each header string for the conversion trigger and record the conversion status in the array Columns[]

                                i = -1;
                                foreach (var Header in Headers)
                                {
                                    i++;

                                    // Check for the conversion trigger
                                    if (Header.IndexOf(strConversionTrigger) != -1)
                                    {
                                        Columns[i] = "Yes";
                                    }
                                    else
                                    {
                                        Columns[i] = "No";
                                    }

                                }

                                intNoOfHeaderColumns = i;

                                // If this is the first data file, then write this header line to the array holding the converted data for this data logger

                                if (strFirstLoggerFile == "Yes")
                                {
                                    T4Ddata[0] = strDataLine;
                                    strFirstLoggerFile = "No";



                                }

                                // The header line has been written to T4Ddata[0] if it was the first data file
                                // The elements of the header have been extracted and written Headers[].
                                // The conversion flags have been written to Columns[].

                                // Work on the header line is complete
                                goto NextDataLine;
                            }


                            if ((strObservationLine == "Yes") && (strTrashLineFlag == "No"))
                            {
                                // Prepare the data line
                                strDataLine = gna.formatDataLine(strDataLine, strNullDataString);

                                //Split into components
                                string[] Components = strDataLine.Split(',');


                                //Now step through the components, identify which must be converted, and convert.
                                i = -1;
                                foreach (var Component in Components)
                                {
                                    i++;
                                    if (Columns[i] == "Yes")
                                    {
                                        if (Convert.ToString(Component) != strNullDataString)
                                        {
                                            dblDataElement = Convert.ToDouble(Component) * dblConversionFactor;
                                            Components[i] = dblDataElement.ToString(strFormat);
                                        }
                                    }
                                }

                                //Now create the text data line
                                strDataLine = "";
                                i = -1;
                                foreach (var Component in Components)
                                {
                                    i++;
                                    strDataLine = strDataLine + Components[i] + ",";
                                }
                                //strip the trailing comma from the line and write to the T4D data array
                                strDataLine = strDataLine.Substring(0, strDataLine.Length - 1);
                                intT4DdataCounter++;
                                T4Ddata[intT4DdataCounter] = strDataLine;

                            }

NextDataLine:
                            continue;

                        }


                        // Write the array elements to the T4D data file strT4DfileName.
                        // If the file exists then append data to existing file
                        // If the file does not exist then create a new file

                        int j = 0;

                        if (File.Exists(strT4DfileName))
                        {
                            j = 1;
                        }
                        else
                        {
                            j = 0;
                        }

                        using (StreamWriter writer = new StreamWriter(strT4DfileName, true))
                        {
                            for (i = j; i < intT4DdataCounter + 1; i++)
                            {
                                writer.WriteLine(T4Ddata[i]);
                            }
                        }

                        //Tag the datalogger file
                        File.Move(strDataloggerFileName, strDataloggerFileName + ".imported");
                        //Reset the flags & counters
                        //strHeaderFlag = "HeaderInserted";
                        //strFirstDataFile = "No";

                        strObservationLine = "No";
                        strTrashLineFlag = "Yes";
                        i = 0;
                        intT4DdataCounter = 0;

                    }
                }
            }
        }
    }
}
