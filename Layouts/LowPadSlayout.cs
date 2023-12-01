using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloorSimulation
{
    internal class LowPadSlayoutBuffhub : SLayout
    {
        public LowPadSlayoutBuffhub(Floor floor_, ReadData rData) : base(floor_, rData)
        {
            NLowpads = 1;
        }
        public override string ToString()
        {
            return "S-Layout grouped by an even distribution per distributer, With more small buffhubs in the street";
        }

        public override void PlaceShops(List<ShopHub> Shops, int UpperY, int LowerY)
        {
            List<List<ShopHub>> regions = CreateDistributionRegions(Shops);

            Shops = regions.SelectMany(obj => obj).ToList();
            base.PlaceShops(Shops, UpperY, LowerY);
        }

        public override void PlaceDistributers(int amount, Point StartPoint)
        {
            Distributer db;
            int y = StartPoint.Y;
            int x = StartPoint.X;

            for(int i  = 0; i < amount; i++)
            {
                db = new Distributer(i, floor, floor.FirstWW, Rpoint_: new Point(x, y), MaxWaitedTicks_: 100 - i);
                floor.TotalDistrList.Add(db);
                x += 200;
                if (x > floor.FirstWW.RSizeWW.Width - 250)
                {
                    x = StartPoint.X;
                    y += 200;
                    if(y > floor.FirstWW.RSizeWW.Height - 250)
                        break;
                }
            }
        }

        private List<List<ShopHub>> CreateDistributionRegions(List<ShopHub> Shops)
        {
            int nDbuters = floor.TotalDistrList.Count;
            Shops = Shops.OrderBy(obj => obj.StickersToReceive).ToList();

            int[] dbIndexes = new int[nDbuters];
            for (int i = 0; i < nDbuters; i++)
                dbIndexes[i] = i;

            List<List<ShopHub>> DistributionRegions = new List<List<ShopHub>>();
            for(int i = 0; i < nDbuters; i++)
                DistributionRegions.Add(new List<ShopHub>());

            for (int ShopI = 0; ShopI < Shops.Count; ShopI++)
            {
                if(ShopI % nDbuters == 0) 
                    dbIndexes = dbIndexes.OrderBy(x => Guid.NewGuid()).ToArray();
                DistributionRegions[ShopI % nDbuters].Add(Shops[ShopI]);
            }

            int[] StickersPerDistributer = new int[nDbuters];
            for(int ShopI = 0; ShopI < DistributionRegions.Count; ShopI++)
                StickersPerDistributer[ShopI] = DistributionRegions[ShopI].Sum(obj => obj.StickersToReceive);

            return DistributionRegions;
        }
    }
}
