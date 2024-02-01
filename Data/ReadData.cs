using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using Newtonsoft.Json;

namespace FloorSimulation
{
    internal class ReadData
    {
        public readonly string DirectoryPath = Program.rootfolder + @"\Data";
        public Dictionary<string, DanishTrolley> TransactieIdToTrolley;
        CsvConfiguration CsvConfig;
        public Dictionary<string, ShopHub> DestPlusDayToHub;
        public List<ShopHub> UsedShopHubs;
        public List<string> days;

        int SplitTrolleyI = 0;

        public ReadData(List<string> days_)
        {
            days = days_;
            CsvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ";"
            };
        }

        /// <summary>
        /// Date is YYYY-MM-DD: 2023-07-18
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public List<DanishTrolley> ReadBoxHistoryToTrolleys(string date, Floor floor, string length = "", bool DistributeSecondDay = true)
        {
            List<BoxActivity> ActivityList = new List<BoxActivity>();
            UsedShopHubs = new List<ShopHub>(); 
            TransactieIdToTrolley = new Dictionary<string, DanishTrolley>();

            string FullPath;
            if(length.Count() > 0)
                FullPath = DirectoryPath + @"\Verkoop_verdeel_" + date + "_" + length + ".csv";
            else
                FullPath = DirectoryPath + @"\Verkoop_verdeel_" + date + ".csv";


            using (var reader = new StreamReader(FullPath))
            using (var csv = new CsvReader(reader, CsvConfig))
            {
                csv.Context.RegisterClassMap<BoxActivityMap>();
                while(csv.Read())
                {
                    var records = csv.GetRecord<BoxActivity>();
                    ShopHub s = records.InitActivity(DestPlusDayToHub);
                    if (s != null)
                    {
                        UsedShopHubs.Add(s);
                        ActivityList.Add(records);
                    }
                }
            }

            UsedShopHubs = UsedShopHubs.Distinct().ToList();
            UsedShopHubs = UsedShopHubs.Where(obj => days.Contains(obj.day)).ToList();
            UsedShopHubs = UsedShopHubs
                .OrderBy(obj => obj.day)
                .ThenBy(obj => obj.id).ToList();

            foreach(BoxActivity b in ActivityList)
                AddToTrolley(b, floor);

            floor.layout.SortPlantLists(TransactieIdToTrolley.Values.ToList());
           

            List<DanishTrolley> dtList = new List<DanishTrolley>();
            foreach(DanishTrolley t in TransactieIdToTrolley.Values.ToList())
            {
                if (t.PlantList.Select(obj => obj.DestinationHub).Distinct().ToList().Count <= 1)
                {
                    continue;
                }
                if (!DistributeSecondDay && !t.PlantList.Select(obj => obj.DestinationHub.day).Contains(days[0]))
                    continue;
                dtList.Add(t);
                
            }

            CalculateImportStickers(dtList);

            return dtList;
        }

        public void AddToTrolley(BoxActivity b, Floor floor)
        {
            if (!TransactieIdToTrolley.Keys.Contains(b.Transactieid + SplitTrolleyI))
            {
                SplitTrolleyI = 0;
                TransactieIdToTrolley[b.Transactieid + SplitTrolleyI] = new DanishTrolley(-1, floor, transactieId_: b.Transactieid + SplitTrolleyI);
                TransactieIdToTrolley[b.Transactieid + SplitTrolleyI].MaxUnitsPerTrolley = b.Lgstk_aantal_fust_op_sticker;
            }

            DanishTrolley t = TransactieIdToTrolley[b.Transactieid + SplitTrolleyI];
            if(days.Contains(b.Destination.day))
            {
                b.Destination.StickersToReceive++;
                plant p = new plant(b.Destination, b.GetUnits(), b.GetSingleUnits(), b.Lgstk_aantal_fust_op_sticker, name_: b.Product_omschrijving_1);
                if(t.SingleUnits + p.SingleUnits > t.MaxUnitsPerTrolley) 
                {
                    if (t.SingleUnits == 0)
                        ;
                    SplitTrolleyI++;
                    TransactieIdToTrolley[b.Transactieid + SplitTrolleyI] = new DanishTrolley(-1, floor, transactieId_: b.Transactieid + SplitTrolleyI);
                    TransactieIdToTrolley[b.Transactieid + SplitTrolleyI].MaxUnitsPerTrolley = b.Lgstk_aantal_fust_op_sticker;
                    t = TransactieIdToTrolley[b.Transactieid + SplitTrolleyI];
                }
                t.TakePlantIn(p);
            }
        }

        public List<ShopHub> ReadHubData(Floor floor) 
        {
            List<ShopHub> shops = new List<ShopHub>();
            List<HubData> data = new List<HubData>();
            DestPlusDayToHub = new Dictionary<string, ShopHub>();
            string path = DirectoryPath + @"\CustomerData.csv";
            
            using (var reader = new StreamReader(path))
            using (var csv = new CsvReader(reader, CsvConfig))
            {
                csv.Context.RegisterClassMap<HubDataMap>();
                while(csv.Read())
                {
                    HubData recordsma = csv.GetRecord<HubData>();

                    recordsma.ColliPlusDay = recordsma.Code_collistikkers + "-" + "MA";
                    HubData recordsdi = recordsma.Clone();
                    recordsdi.ColliPlusDay = recordsdi.Code_collistikkers + "-" + "DI";
                    HubData recordswo = recordsma.Clone();
                    recordswo.ColliPlusDay = recordswo.Code_collistikkers + "-" + "WO";
                    HubData recordsdo = recordsma.Clone();
                    recordsdo.ColliPlusDay = recordsdo.Code_collistikkers + "-" + "DO";
                    HubData recordsvr = recordsma.Clone();
                    recordsvr.ColliPlusDay = recordsvr.Code_collistikkers + "-" + "VR";
                    HubData recordsza = recordsma.Clone();
                    recordsza.ColliPlusDay = recordsza.Code_collistikkers + "-" + "ZA";
                    HubData recordszo = recordsma.Clone();
                    recordszo.ColliPlusDay = recordszo.Code_collistikkers + "-" + "ZO";

                    data.Add(recordsma);
                    data.Add(recordsdi);
                    data.Add(recordswo);
                    data.Add(recordsdo);
                    data.Add(recordsvr);
                    data.Add(recordsza);
                    data.Add(recordszo);
                }
            }

            foreach(HubData d in data)
            {
                int ntrolleys = floor.layout.NTrolleysInShop;
                Size HubSize;
                ShopHub s;
                if (ntrolleys == 1) 
                {
                    HubSize = new Size(160, 80);
                    s = new ShopHub(d.Search_Name, d.Zoeknaam2, default, floor, HubSize, initial_trolleys: ntrolleys, ColliPlusDay_: d.ColliPlusDay, HorizontalTrolleys_: floor.layout.HorizontalShops );
                }
                else
                {
                    HubSize = new Size(floor.layout.ForcedShopWidth, floor.layout.ForcedShopHeight);
                    s = new ShopHub(d.Search_Name, d.Zoeknaam2, default, floor, HubSize, initial_trolleys: ntrolleys, d.ColliPlusDay, HorizontalTrolleys_: floor.layout.HorizontalShops);
                }

                shops.Add(s);
                DestPlusDayToHub[s.ColliPlusDay] = s;
            }

            return shops;
        }

        private void CalculateImportStickers(List<DanishTrolley> dtList)
        {
            List<int> StickersPerTrolley = dtList
                .Select(dt => dt.PlantList.Count)
                .Where(count => count > 1)
                .ToList();

            double MeanStickersPerTrolley = StickersPerTrolley.Average();

            double VarStickersPerTrolley = StickersPerTrolley.Select(x => Math.Pow(x - MeanStickersPerTrolley, 2)).Average();

            int StickerMinimum = StickersPerTrolley.Min();
            int StickerMaximum = StickersPerTrolley.Max();

            double Stickersd = Math.Sqrt(VarStickersPerTrolley);
            ;
        }
            
        public void LoadHeatMap(string FileName, WalkWay WW)
        {
            string FullPath = Program.rootfolder + @"\Results\HeatMap Results\" + FileName + ".json";
            string jsonContent = File.ReadAllText(FullPath);
            int[,] intArray;
            intArray = JsonConvert.DeserializeObject<int[,]>(jsonContent);

            for (int x = 0; x < intArray.GetLength(0); x++) 
                for(int y = 0; y < intArray.GetLength(1); y++)
                    WW.WalkTileList[x][y].visits = intArray[x, y];

            WW.DrawHeatMap = true;
        }
    }
}
