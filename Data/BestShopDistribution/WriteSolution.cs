using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;


namespace FloorSimulation
{
    internal class WriteSolution
    {
        private Floor floor;

        public WriteSolution(Floor floor_)
        {
            floor = floor_;
        }

        public void Write(List<List<ShopHub>> DistributionRegions)
        { 
            string json = JsonConvert.SerializeObject(DistributionRegions.Select(ShopsInRegion => ShopsInRegion.Count));

            string FilePath = Program.rootfolder + @"\Data\BestShopDistribution\" + 
                floor.Display.date + "_" + floor.layout.NLowpads + "Lowpads_" + Floor.NDistributers + "Distributers" + ".json";
            File.WriteAllText(FilePath, json);
        }
    }

}
