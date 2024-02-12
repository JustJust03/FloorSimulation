using System;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloorSimulation
{
    internal class ReadSolution
    {
        public ReadSolution()
        {

        }

        public int[] Read(List<ShopHub> Shops, List<List<ShopHub>> DistributionRegions, string FilePath)
        {
            string jsonText = File.ReadAllText(FilePath);
            int[] intArray = JsonConvert.DeserializeObject<int[]>(jsonText);
            int i = 0;

            for (int dbi = 0; dbi < intArray.Length; dbi++)
            {
                DistributionRegions[dbi] = Shops.GetRange(i, intArray[dbi]);
                i += intArray[dbi];
            }

            return intArray;
        }

    }
}
