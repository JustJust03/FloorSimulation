using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using CsvHelper;
using CsvHelper.Configuration;

namespace FloorSimulation
{
    internal class SimpleCalculation
    {
        static string FullPath;

        public static void GetPlantsPerTrolley(string date)
        {
            List<NPlants> PList = new List<NPlants>();
            CsvConfiguration CsvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ";"
            };
            FullPath = Program.rootfolder + @"\Data\" + date + " Export.csv";

            using (var reader = new StreamReader(FullPath))
            using (var csv = new CsvReader(reader, CsvConfig))
            {
                csv.Context.RegisterClassMap<NplantMap>();
                while(csv.Read())
                {
                    var records = csv.GetRecord<NPlants>();
                    NPlants p = records.InitPlant();
                    PList.Add(p);
                }
            }

            Dictionary<int, BoxActivity> alist = ReadImportData(date);
            CalculatePlantsPerT(PList, alist);
        }

        private static Dictionary<int, BoxActivity> ReadImportData(string date)
        {
            CsvConfiguration CsvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ";"
            };

            Dictionary<int, BoxActivity> ActivityList = new Dictionary<int, BoxActivity>();

            FullPath = Program.rootfolder + @"\Data\" + @"\Verkoop_verdeel_" + date + ".csv";

            using (var reader = new StreamReader(FullPath))
            using (var csv = new CsvReader(reader, CsvConfig))
            {
                csv.Context.RegisterClassMap<BoxActivityMap>();
                while(csv.Read())
                {
                    var records = csv.GetRecord<BoxActivity>();
                    ActivityList[records.Entry] = records;
                }
            }

            return ActivityList;
        }

        public static void CalculatePlantsPerT(List<NPlants> PList, Dictionary<int, BoxActivity> alist)
        {
            Dictionary<string, List<NPlants>> BestemmingToPlants = new Dictionary<string, List<NPlants>>();
            foreach(NPlants p in PList)
            {
                if (!BestemmingToPlants.Keys.Contains(p.Bestemmingscode))
                    BestemmingToPlants[p.Bestemmingscode] = new List<NPlants>();

                BestemmingToPlants[p.Bestemmingscode].Add(p);
            }

            List<NPlants> NonLagerTrolleys = new List<NPlants>();
            foreach(List<NPlants> pslist in BestemmingToPlants.Values)
            {
                if (pslist.Select(p => alist[int.Parse(p.BoxActiviteitEntry)].Transactieid).Distinct().ToList().Count <= 1)
                {
                    ;
                }
            }

            Dictionary<string, int> BestemmingToNumberOfPlants = new Dictionary<string, int>();
            foreach(string key in BestemmingToPlants.Keys)
            {
                BestemmingToNumberOfPlants[key] = BestemmingToPlants[key].Select(o => o.N).ToList().Sum();
            }

            BestemmingToNumberOfPlants = BestemmingToNumberOfPlants.OrderBy(o => o.Value).ToDictionary(o => o.Key, o => o.Value);
            List<int> l = BestemmingToNumberOfPlants.Values.ToList();
            List<int> StickersPerTrolley = BestemmingToPlants.Values
                .Select(t => t.Count)
                .Where(count => count > 1)
                .ToList();
            double MeanPlantsPerTrolley = l.Average();
            double MeanStickersPerTrolley = StickersPerTrolley.Average();

            double VarPlantsPerTrolley = l.Select(x => Math.Pow(x - MeanPlantsPerTrolley, 2)).Average();
            double VarStickersPerTrolley = StickersPerTrolley.Select(x => Math.Pow(x - MeanStickersPerTrolley, 2)).Average();

            int PlantsMinimum = l.Min();
            int StickerMinimum = StickersPerTrolley.Min();
            int PlantsMaximum = l.Max();
            int StickerMaximum = StickersPerTrolley.Max();

            double Plantssd = Math.Sqrt(VarPlantsPerTrolley);
            double Stickersd = Math.Sqrt(VarStickersPerTrolley);

            ;
        }



    }

    internal class NPlants
    {
        public string NString { get; set; }
        public string Bestemmingscode {get; set;}
        public int N { get; set; }
        public string BoxActiviteitEntry {  get; set; }

        public NPlants InitPlant()
        {
            N = int.Parse(NString.Split('.')[0]);
            return this;
        }
    }
    internal class NplantMap : ClassMap<NPlants>
    {
        public NplantMap()
        {
            Map(m => m.NString).Index(4);
            Map(m => m.Bestemmingscode).Index(2);
            Map(m => m.BoxActiviteitEntry).Index(9);
        }
    }
}
