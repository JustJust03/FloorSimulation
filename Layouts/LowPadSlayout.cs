using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Common;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloorSimulation
{
    internal class LowPadSlayoutBuffhub : SLayout
    {
        List<List<ShopHub>> regions;
        RegionConstants RC;
        int NRegions = 29;
        int NDbuters = Floor.NDistributers;

        public LowPadSlayoutBuffhub(Floor floor_, ReadData rData) : base(floor_, rData)
        {
            NLowpads = 10;
            RC = new RegionConstants();
            if (NRegions == 0)
                NRegions = NDbuters;
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
                    id, new Point(HubX, HubY), floor, new Size(50, 130), region);

                floor.AccessPointPerRegion[LPAHub.OpenSpots(default)[0].Rpoint] = LPAHub;

                floor.HubList.Add(LPAHub);
                floor.LPHubs.Add(LPAHub);

                foreach(ShopHub shop in region) 
                    floor.ShopHubPerRegion[shop] = LPAHub;

                id++;
            }

            //Assign the distributers
            for (id = 0; id < Floor.NDistributers; id++)
            {
                int[] regionsPerDB = RC.RegionsPerDbuter[(NRegions, NDbuters)][id];
                int Nregions = regionsPerDB.Length;
                int NShops = regions[regionsPerDB[Nregions - 1]].Count;
                ShopHub HighestShop = regions[regionsPerDB[0]][0];
                ShopHub LowestShop = regions[regionsPerDB[Nregions - 1]][NShops - 1];

                int MiddleY = (HighestShop.RFloorPoint.Y + LowestShop.RFloorPoint.Y + LowestShop.RHubSize.Height - 20) / 2;
                int regiona = regionsPerDB[0];
                int DBX;
                if (regions[regiona][0].HasLeftAccess)
                    DBX = regions[regiona][0].RFloorPoint.X - 40;
                else
                    DBX = regions[regiona][0].RFloorPoint.X + regions[regiona][0].RHubSize.Width + 10;

                LowPadAccessHub[] LPAHubs = new LowPadAccessHub[Nregions];
                for (int regioni = 0; regioni < Nregions; regioni++)
                    LPAHubs[regioni] = floor.LPHubs[regionsPerDB[regioni]];

                Distributer db = new Distributer(id, floor, floor.FirstWW, Rpoint_: new Point(DBX, MiddleY), MaxWaitedTicks_: 100 - id, IsVertical_: false, RHubs: LPAHubs);
                floor.DistrList.Add(db);
            }
        }

        public override void PlaceDistributers(int amount, Point StartPoint)
        {
            if (NLowpads > 0)
                PlaceLowPads(new Point(floor.FirstWW.RSizeWW.Width - 1000, 2000));
            floor.LHDriver = new Distributer(-8, floor, floor.FirstWW, Rpoint_: floor.FirstHarry.RPoint);

        }

        private void PlaceLowPads(Point StartPoint)
        {
            LowPad lp;
            int y = StartPoint.Y;
            int x = StartPoint.X;

            for(int i  = 0; i < NLowpads; i++)
            {
                lp = new LowPad(i, floor, floor.FirstWW, Rpoint_: new Point(x, y), MaxWaitedTicks_: 100 - i);
                floor.TotalLPList.Add(lp);
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

        public override BufferHub GetBuffHubFull(Agent agent)
        {
            List<BufferHub> sortedList = floor.BuffHubs.OrderBy(obj =>
            {
                int deltaX = obj.RFloorPoint.X - agent.RPoint.X;
                int deltaY = obj.RFloorPoint.Y - agent.RPoint.Y;
                return deltaX * deltaX + deltaY * deltaY; // Return the squared distance
            })
            .Where(obj => obj.name != "Buffer hub")
            .ToList();

            foreach(BufferHub buffhub in sortedList) 
            {
                if (buffhub.FilledSpots(agent).Count > 0)
                    return buffhub;
            }

            return null;
        }

        public override BufferHub GetBuffHubOpen(Agent agent)
        {
            List<BufferHub> sortedList = floor.BuffHubs
            .OrderBy(obj =>
            {
                int deltaX = obj.RFloorPoint.X - agent.RPoint.X;
                int deltaY = obj.RFloorPoint.Y - agent.RPoint.Y;
                if (obj.name == "Buffer hub")
                    deltaX = 0;
                return deltaX * deltaX + deltaY * deltaY; // Return the squared distance
            })
            .ToList();

            foreach(BufferHub buffhub in sortedList) 
            {
                if (buffhub.OpenSpots(agent).Count > 0)
                    return buffhub;
            }

            return null;
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

        private List<List<ShopHub>> CreateDistributionRegions(List<ShopHub> Shops)
        {
            if(NRegions == 0)
                NRegions = NDbuters;

            Shops = Shops.OrderBy(obj => obj.StickersToReceive).ToList();

            List<List<ShopHub>> DistributionRegions = new List<List<ShopHub>>();
            for(int i = 0; i < NRegions; i++)
                DistributionRegions.Add(new List<ShopHub>());

            AssignDBregions(Shops, DistributionRegions);

            //Sort the shops within the regions again.
            for(int regioni = 0; regioni < DistributionRegions.Count; regioni++)
            {
                DistributionRegions[regioni] = DistributionRegions[regioni]
                    .OrderBy(shop => shop.day)
                    .ThenBy(shop => shop.id).ToList();
            }

            return DistributionRegions;
        }

        private List<List<ShopHub>> AssignDBregions(List<ShopHub> Shops, List<List<ShopHub>> DistributionRegions)
        {
            int[] NshopsPerRegion = RC.ShopsPerDButer[(NRegions, NDbuters)].Distinct().OrderBy(x => 999 - x).ToArray();
            for (int Nshopsi = 0; Nshopsi < NshopsPerRegion.Length; Nshopsi++)
            {
                int[][] RegionsPerNshops = RC.RegionsPerDbuter[(NRegions, NDbuters)]
                    .Where(x => 
                    {
                        int NShops = 0;
                        foreach(int regioni in x)
                            NShops += RC.ShopsPerRegion[NRegions][regioni];

                        return NShops == NshopsPerRegion[Nshopsi];
                    }).ToArray();
                ;
                List<ShopHub> SlicedShops = Shops.GetRange(0, NshopsPerRegion[Nshopsi] * RegionsPerNshops.Length);
                Shops.RemoveRange(0, NshopsPerRegion[Nshopsi] * RegionsPerNshops.Length);

                int[] Regions = RegionsPerNshops.SelectMany(a => a).ToArray();


                int ExtraI = 0;
                for (int ShopI = 0; ShopI < SlicedShops.Count; ShopI++)
                {
                    //If the max trolleys for this shop has already been reached, continue.
                    while (RC.ShopsPerRegion[NRegions][Regions[(ShopI + ExtraI) % Regions.Length]] == DistributionRegions[Regions[(ShopI + ExtraI) % Regions.Length]].Count)
                        ExtraI++;

                    DistributionRegions[Regions[(ShopI + ExtraI) % Regions.Length]].Add(SlicedShops[ShopI]);
                }
            }

            int[] StickersPerDButer = RC.StickersPerDistributer(DistributionRegions, NDbuters, NRegions);

            return DistributionRegions;
        }
    }

    internal class RegionConstants
    {
        // Key: (int, int) => (Nregions, NDbuters)
        public readonly Dictionary<(int, int), List<int[]>> RegionsPerDbuter = new Dictionary<(int, int), List<int[]>>();

        // Key: Nregions
        public readonly Dictionary<int, int[]> ShopsPerRegion = new Dictionary<int, int[]>();

        // Key: (int, int) => (Nregions, NDbuters)
        public Dictionary<(int, int), int[]> ShopsPerDButer = new Dictionary<(int, int), int[]>();
            
        public RegionConstants()
        {
            RegionsPerDbuter[(29, 21)] = 
            new List<int[]> {
                new int[] {0, 1},
                new int[] {2, 3},
                new int[] {4},
                new int[] {5},
                new int[] {6, 7},
                new int[] {8, 9},
                new int[] {10},
                new int[] {11},
                new int[] {12},
                new int[] {13},
                new int[] {14, 15},
                new int[] {16, 17},
                new int[] {18},
                new int[] {19},
                new int[] {20},
                new int[] {21},
                new int[] {22, 23},
                new int[] {24, 25},
                new int[] {26},
                new int[] {27},
                new int[] {28},
            };
            //ShopsPerRegion[29] = new int[] { 5, 5, 5, 5, 5, 4, 5, 4, 4, 5, 4, 5, 5, 4, 5, 4, 4, 5, 4, 5, 5, 4, 5, 4, 4, 5, 4, 5, 5 };
            ShopsPerRegion[29] = new int[] { 5, 5, 5, 5, 5, 4, 5, 4, 4, 5, 4, 5, 5, 4, 5, 4, 4, 5, 4, 5, 5, 4, 5, 4, 4, 5, 4, 5, 4 };
            AddShopsPerDButer(29, 21);
        }

        private void AddShopsPerDButer(int Nregions, int NDbuters)
        {
            int[] ShopsPerDB = new int[NDbuters];

            for (int i = 0; i < NDbuters; i++) 
                foreach(int shopi in RegionsPerDbuter[(Nregions, NDbuters)][i])
                    ShopsPerDB[i] += ShopsPerRegion[Nregions][shopi];

            ShopsPerDButer[(Nregions, NDbuters)] = ShopsPerDB;
        }

        public int[] StickersPerDistributer(List<List<ShopHub>> DistributionRegions, int NDbuters, int NRegions)
        {
            int[] StickersPerDButer = new int[NDbuters];

            for (int i = 0; i < NDbuters; i++)
                foreach (int Regioni in RegionsPerDbuter[(NRegions, NDbuters)][i])
                    StickersPerDButer[i] += DistributionRegions[Regioni].Select(shop => shop.StickersToReceive).Sum();

            return StickersPerDButer;
        }
    }
}
