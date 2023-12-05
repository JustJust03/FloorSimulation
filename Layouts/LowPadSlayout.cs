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
        List<List<ShopHub>> regions;

        public LowPadSlayoutBuffhub(Floor floor_, ReadData rData) : base(floor_, rData)
        {
            NLowpads = 1;
        }
        public override string ToString()
        {
            return "S-Layout grouped by an even distribution per distributer, With more small buffhubs in the street";
        }

        public override void AssignRegionsToTrolleys(List<DanishTrolley> dtlist)
        {
            foreach(DanishTrolley dt in dtlist)
            {
                dt.TargetRegions = dt.PlantList.Select(plantobj => floor.ShopHubPerRegion[plantobj.DestinationHub]).Distinct().ToList();
                dt.PlantList = dt.PlantList
                    .OrderBy(plist => floor.ShopHubPerRegion[plist.DestinationHub].id)
                    .ThenBy(plist => plist.DestinationHub.day)
                    .ThenBy(plist => plist.DestinationHub.id)
                    .ToList();
            }
        }

        public override void PlaceShops(List<ShopHub> Shops, int UpperY, int LowerY)
        {
            regions = CreateDistributionRegions(Shops);

            Shops = regions.SelectMany(obj => obj).ToList();
            foreach(ShopHub s in Shops)
                s.DrawRegions = true;

            base.PlaceShops(Shops, UpperY, LowerY);
            foreach(List<ShopHub> region in regions) 
            {
                region[0].RegionStartOrEnd = true; //First Shop
                region[region.Count - 1].RegionStartOrEnd = true; //Last Shop
            }

            PlaceLPAccessHubs();
        }

        private void PlaceLPAccessHubs()
        {
            ShopHub FirstHub;
            ShopHub LastHub;
            int id = 0;
            foreach(List<ShopHub> region in regions)
            {
                FirstHub = region[0];
                LastHub = region[region.Count - 1];

                int HighestY;
                int LowestY;
                int HubX;

                if (FirstHub.HasLeftAccess)
                {
                    HighestY = LastHub.RFloorPoint.Y + FirstHub.RHubSize.Height;
                    LowestY = FirstHub.RFloorPoint.Y;
                    HubX = FirstHub.RFloorPoint.X - 160;
                }
                else
                {
                    HighestY = FirstHub.RFloorPoint.Y + FirstHub.RHubSize.Height;
                    LowestY = LastHub.RFloorPoint.Y;
                    HubX = FirstHub.RFloorPoint.X + FirstHub.RHubSize.Width + 100;
                }

                int HubY = LowestY + ((HighestY - LowestY) / 2) - 70;

                LowPadAccessHub LPAHub = new LowPadAccessHub("LowPad Access Hub (X, Y): (" + HubX + ", " + HubY + ")",
                    id, new Point(HubX, HubY), floor, new Size(60, 150));
                floor.HubList.Add(LPAHub);

                foreach(ShopHub shop in region) 
                    floor.ShopHubPerRegion[shop] = LPAHub;

                id++;
            }
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
            if (NLowpads > 0)
                PlaceLowPads(new Point(50, 50));
        }

        private void PlaceLowPads(Point StartPoint)
        {
            LowPad lp;
            int y = StartPoint.Y;
            int x = StartPoint.X;

            for(int i  = 0; i < NLowpads; i++)
            {
                lp = new LowPad(i, floor, floor.FirstWW, Rpoint_: new Point(x, y), MaxWaitedTicks_: 100 - i);
                floor.LPList.Add(lp);
                x += 200;
                if (x > floor.FirstWW.RSizeWW.Width - 250)
                {
                    x = StartPoint.X;
                    y += 200;
                    if(y > floor.FirstWW.RSizeWW.Height - 250)
                        break;
                }
                floor.LPList.Add(lp);
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

            //Sort the shops within the regions again.
            for(int regioni = 0; regioni < DistributionRegions.Count; regioni++)
            {
                DistributionRegions[regioni] = DistributionRegions[regioni]
                    .OrderBy(shop => shop.day)
                    .ThenBy(shop => shop.id).ToList();
            }


            int[] StickersPerDistributer = new int[nDbuters];
            for(int ShopI = 0; ShopI < DistributionRegions.Count; ShopI++)
                StickersPerDistributer[ShopI] = DistributionRegions[ShopI].Sum(obj => obj.StickersToReceive);

            //Temporary
            int[] ShopisToBe5 = new int[] { 13, 16, 17, 20, 21, 24, 25, 28, 29 };
            int[] ShopisToBe4 = new int[] { 0, 1, 2, 3, 4, 6, 7, 10, 11 };

            for (int i = 0; i < ShopisToBe5.Length; i++)
            {
                List<ShopHub> r = DistributionRegions[ShopisToBe5[i]];
                DistributionRegions[ShopisToBe5[i]] = DistributionRegions[ShopisToBe4[i]];
                DistributionRegions[ShopisToBe4[i]] = r;
            }
            return DistributionRegions;
        }

        public override BufferHub GetBuffHubFull(Distributer db)
        {
            List<BufferHub> sortedList = floor.BuffHubs.OrderBy(obj =>
            {
                int deltaX = obj.RFloorPoint.X - db.RPoint.X;
                int deltaY = obj.RFloorPoint.Y - db.RPoint.Y;
                if (obj.name == "Buffer hub")
                    deltaX = 0;
                return deltaX * deltaX + deltaY * deltaY; // Return the squared distance
            })
            .ToList();

            foreach(BufferHub buffhub in sortedList) 
            {
                if (buffhub.FilledSpots(db).Count > 0)
                    return buffhub;
            }

            return base.GetBuffHubFull(db);
        }

        public override BufferHub GetBuffHubOpen(Distributer db)
        {
            List<BufferHub> sortedList = floor.BuffHubs
            .OrderBy(obj =>
            {
                int deltaX = obj.RFloorPoint.X - db.RPoint.X;
                int deltaY = obj.RFloorPoint.Y - db.RPoint.Y;
                if (obj.name == "Buffer hub")
                    deltaX = 0;
                return deltaX * deltaX + deltaY * deltaY; // Return the squared distance
            })
            .ToList();

            foreach(BufferHub buffhub in sortedList) 
            {
                if (buffhub.OpenSpots(db).Count > 0)
                    return buffhub;
            }

            return base.GetBuffHubOpen(db);
        }

        public override void PlaceFullTrolleyHubs()
        {
            UpperY += 880;
            LowestY -= 880;

            base.PlaceFullTrolleyHubs();
        }

        public override void PlaceBuffHubs()
        {
            int BuffHubWidth = 200;
            for (int i = 0; i < ShopCornersX.Count - 1; i += 2)
            {
                int x = ((ShopCornersX[i] + ShopCornersX[i + 1]) / 2) - (BuffHubWidth / 2);
                for (int y = UpperY; y < LowestY; y += LowestY - UpperY - 600)
                    floor.BuffHubs.Add(new BufferHub("Small buffer hub", 1 + i, new Point(x, y),new Size(BuffHubWidth, 600), floor, vertical_trolleys_: false));
            }

            floor.BuffHubs.Add(new BufferHub("Buffer hub", 1, new Point(300, 40), new Size(floor.FirstWW.RSizeWW.Width - 500, 600), floor));
            floor.HubList = floor.HubList.Concat(floor.BuffHubs).ToList();
        }
    }
}
