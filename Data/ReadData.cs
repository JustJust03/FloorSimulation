using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;

namespace FloorSimulation
{
    internal class ReadData
    {
        public readonly string DirectoryPath = Program.rootfolder + @"\Data";

        public ReadData()
        {

        }

        /// <summary>
        /// Date is YYYY-MM-DD: 2023-07-18
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public List<DanishTrolley> ReadBoxHistoryToTrolleys(string date, bool Short = true) 
        {
            List<DanishTrolley> TrolleyList = new List<DanishTrolley>();
            string FullPath;
            if (Short)
                FullPath = DirectoryPath + @"\Verkoop_verdeel_" + date + "_short.csv";
            else
                FullPath = DirectoryPath + @"\Verkoop_verdeel_" + date + ".csv";
            using (var reader = new StreamReader(FullPath))
            {
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    var records = csv.GetRecords<Box>();
                    ;
                }
                
            }
            ;


            return TrolleyList;
        }

        public DanishTrolley FillTrolley()
        {
            return null;
        }
    }
}
