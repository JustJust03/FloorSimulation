using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;

namespace FloorSimulation
{
    internal class ReadData
    {
        public readonly string DirectoryPath = Program.rootfolder + @"\Data";
        public Dictionary<string, DanishTrolley> TransactieIdToTrolley;
        CsvConfiguration CsvConfig;
        public Dictionary<string, ShopHub> DestPlusDayToHub;
        public List<ShopHub> UsedShopHubs;
        public List<string> days = new List<string> { "DI", "WO" };

        public ReadData()
        {
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
        public List<DanishTrolley> ReadBoxHistoryToTrolleys(string date, Floor floor, string length = "")
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
                    UsedShopHubs.Add(s);
                    ActivityList.Add(records);
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
                if (t.PlantList.Count == 0)
                    continue;
                dtList.Add(t);
            }

            return dtList;
        }

        public void AddToTrolley(BoxActivity b, Floor floor)
        {
            if (!TransactieIdToTrolley.Keys.Contains(b.Transactieid))
                TransactieIdToTrolley[b.Transactieid] = new DanishTrolley(-1, floor, transactieId_: b.Transactieid);

            DanishTrolley t = TransactieIdToTrolley[b.Transactieid];
            if(days.Contains(b.Destination.day))
            {
                plant p = new plant(b.Destination, b.GetUnits(), name_: b.Product_omschrijving_1);
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

                    data.Add(recordsma);
                    data.Add(recordsdi);
                    data.Add(recordswo);
                    data.Add(recordsdo);
                    data.Add(recordsvr);
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
                    s = new ShopHub(d.Search_Name, d.Zoeknaam2, default, floor, HubSize, initial_trolleys: ntrolleys, ColliPlusDay_: d.ColliPlusDay);
                }
                else
                {
                    HubSize = new Size(160, 160);
                    s = new ShopHub(d.Search_Name, d.Zoeknaam2, default, floor, HubSize, initial_trolleys: ntrolleys, d.ColliPlusDay);
                }

                shops.Add(s);
                DestPlusDayToHub[s.ColliPlusDay] = s;
            }

            return shops;
        }
    }
}
