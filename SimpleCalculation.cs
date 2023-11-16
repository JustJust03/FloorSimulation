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

        public static void GetPlantsPerTrolley(string file_name)
        {
            List<NPlants> PList = new List<NPlants>();
            CsvConfiguration CsvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ";"
            };
            string FullPath = Program.rootfolder + @"\Data\" + file_name + ".csv";

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
            CalculatePlantsPerT(PList);
        }

        public static void CalculatePlantsPerT(List<NPlants> PList)
        {
            Dictionary<string, List<NPlants>> BestemmingToPlants = new Dictionary<string, List<NPlants>>();
            foreach(NPlants p in PList)
            {
                if (!BestemmingToPlants.Keys.Contains(p.Bestemmingscode))
                    BestemmingToPlants[p.Bestemmingscode] = new List<NPlants>();

                BestemmingToPlants[p.Bestemmingscode].Add(p);
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
        }
    }
}
