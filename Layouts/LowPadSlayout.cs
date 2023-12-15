using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Common;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

namespace FloorSimulation
{
    internal class LowPadSlayoutBuffhub : SLayout
    {
        List<List<ShopHub>> regions;
        int NDbuters = Floor.NDistributers;
        int[] ShopsPerCol = new int[] {20, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 5};

        readonly bool UseDumbLowPads = true;

        public LowPadSlayoutBuffhub(Floor floor_, ReadData rData) : base(floor_, rData)
        {
            NLowpads = 10;
            ShopStartX = 50;
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
            CreateDriveLines();
        }

        private void PlaceLPAccessHubs()
        {
            int LPAhubId = 0;
            int HubX;
            List<LowPadAccessHub>[] lowPadAccessHubs = new List<LowPadAccessHub>[regions.Count];

            int i = 0;
            foreach(List<ShopHub> region in regions)
            {
                lowPadAccessHubs[i] = new List<LowPadAccessHub>();
                foreach(ShopHub sh in region)
                {
                    if (sh.HasLeftAccess)
                        HubX = sh.RFloorPoint.X - 130;
                    else
                        HubX = sh.RFloorPoint.X + sh.RHubSize.Width + 70;


                    LowPadAccessHub LPAHub = new LowPadAccessHub("LowPad Access Hub (X, Y): (" + HubX + ", " + sh.RFloorPoint.Y + ")",
                        LPAhubId, new Point(HubX, sh.RFloorPoint.Y), floor, new Size(50, 130), new List<ShopHub> { sh });
                    lowPadAccessHubs[i].Add(LPAHub);

                    floor.AccessPointPerRegion[LPAHub.OpenSpots(default)[0].Rpoint] = LPAHub;
                    floor.HubList.Add(LPAHub);
                    floor.LPHubs.Add(LPAHub);
                    floor.ShopHubPerRegion[sh] = LPAHub;

                    LPAhubId++;
                }

                i++;
            }

            //Assign the distributers
            for (int id = 0; id < Floor.NDistributers; id++)
            {
                ShopHub HighestShop = regions[id][0];
                ShopHub LowestShop = regions[id][regions[id].Count - 1];

                int MiddleY = (HighestShop.RFloorPoint.Y + LowestShop.RFloorPoint.Y + LowestShop.RHubSize.Height - 20) / 2;
                int DBX;
                if (regions[id][0].HasLeftAccess)
                    DBX = regions[id][0].RFloorPoint.X - 40;
                else
                    DBX = regions[id][0].RFloorPoint.X + regions[id][0].RHubSize.Width + 10;

                Distributer db = new Distributer(id, floor, floor.FirstWW, Rpoint_: new Point(DBX, MiddleY), MaxWaitedTicks_: 100 - id, IsVertical_: false, RHubs: lowPadAccessHubs[id].ToArray());
                floor.DistrList.Add(db);
            }
        }

        public void CreateDriveLines()
        {
            LPDriveLines = new LowPadDriveLines();

            LPDriveLines.AddVerticalLine(3520, UpperY - 200, LowestY + 20, 1);
            LPDriveLines.AddHorizontalLine(4790, 0, RealFloorWidth, -1);
            LPDriveLines.AddHorizontalLine(4590, 360, 850, -1);
            LPDriveLines.AddHorizontalLine(4590, 0, 360, 1);
            LPDriveLines.AddHorizontalLine(4590, 850, 4000, -1, true);

            LPDriveLines.AddHorizontalLine(UpperY - 180, 0, 3600, 1);
            LPDriveLines.AddHorizontalLine(LowestY + 20, 430, 3310, 1);

            LPDriveLines.AddVerticalLine(360, UpperY - 200, 4750, -1);
            LPDriveLines.AddVerticalLine(650, UpperY - 200, LowestY + 20, 1);
            LPDriveLines.AddVerticalLine(1320, UpperY - 200, LowestY + 40, -1);
            LPDriveLines.AddVerticalLine(1600, UpperY - 200, LowestY + 40, 1);
            LPDriveLines.AddVerticalLine(2280, UpperY - 200, LowestY + 40, -1);
            LPDriveLines.AddVerticalLine(2560, UpperY - 200, LowestY + 40, 1);
            LPDriveLines.AddVerticalLine(3240, UpperY - 200, LowestY + 40, -1);
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
            DumbLowPad dlp;
            int y = StartPoint.Y;
            int x = StartPoint.X;

            for(int i  = 0; i < NLowpads; i++)
            {
                if (UseDumbLowPads)
                {
                    dlp = new DumbLowPad(i, floor, floor.FirstWW, Rpoint_: new Point(x, y), MaxWaitedTicks_: 100 - i);
                    floor.TotalDLPList.Add(dlp);
                }
                else
                {
                    lp = new LowPad(i, floor, floor.FirstWW, Rpoint_: new Point(x, y), MaxWaitedTicks_: 100 - i);
                    floor.TotalLPList.Add(lp);
                }
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
            Shops = Shops.OrderBy(obj => obj.StickersToReceive).ToList();

            List<List<ShopHub>> DistributionRegions = new List<List<ShopHub>>();
            for(int i = 0; i < NDbuters; i++)
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
            int[] NshopsPerDbuter = new int[] { 10, 10, 5, 4, 9, 9, 4, 5, 5, 4, 9, 9, 4, 5, 5, 4, 9, 9, 4, 5, 5 };
            int[] NShopsPerRegion = NshopsPerDbuter.Distinct().OrderBy(x => 9999 - x).ToArray();

            for (int Nshopsi = 0; Nshopsi < NShopsPerRegion.Count(); Nshopsi++)
            {
                int[] IndexesOfShopsWithShopCount = Enumerable
                                                    .Range(0, NshopsPerDbuter.Length)
                                                    .Where(i => NshopsPerDbuter[i] == NShopsPerRegion[Nshopsi])
                                                    .ToArray();
                List<ShopHub> SlicedShops = Shops.GetRange(0, NShopsPerRegion[Nshopsi] * IndexesOfShopsWithShopCount.Length);
                Shops.RemoveRange(0, NShopsPerRegion[Nshopsi] * IndexesOfShopsWithShopCount.Length);

                for (int ShopI = 0; ShopI < SlicedShops.Count; ShopI++)
                    DistributionRegions[IndexesOfShopsWithShopCount[ShopI % IndexesOfShopsWithShopCount.Length]].Add(SlicedShops[ShopI]);

                ;
            }

            int[] StickersPerDButer = RegionConstants.StickerPerDButerNew(DistributionRegions);


            return DistributionRegions;
        }
    }

    internal class RegionConstants
    {
        public static int[] StickerPerDButerNew(List<List<ShopHub>> DistributionRegions)
        {
            int NDbuters = DistributionRegions.Count;   
            int[] StickersPerDButer = new int[NDbuters];

            for (int i = 0; i < NDbuters; i++)
                StickersPerDButer[i] += DistributionRegions[i].Select(shop => shop.StickersToReceive).Sum();

            return StickersPerDButer;
        }
    }

    
    internal class LowPadDriveLines
    {
        List<VerticalLine> VerticalLines = new List<VerticalLine>();
        List<HorizontalLine> HorizontalLines = new List<HorizontalLine>();

        public void AddVerticalLine(int Rx, int LowerRy, int UpperRy, int DeltaY)
        {
            VerticalLines.Add(new VerticalLine(Rx, LowerRy, UpperRy, DeltaY));
        }

        public void AddHorizontalLine(int Ry, int LeftRx, int RightRx, int DeltaX, bool CarryTrolleyToEnterLine = false)
        {
            HorizontalLines.Add(new HorizontalLine(Ry, LeftRx, RightRx, DeltaX, CarryTrolleyToEnterLine));
        }

        public void HitDriveLine(DumbLowPad dlp)
        {
            if (dlp.MainTask.LowpadDeltaX != 0)
                HitVerticalDriveLine(dlp);
            else
                HitHorizontalDriveLine(dlp);
        }

        public void HitVerticalDriveLine(DumbLowPad dlp)
        {
            foreach(VerticalLine line in VerticalLines)
            {
                if (line.RX != dlp.RPoint.X)
                    continue;
                if(line.LowerRY < dlp.RPoint.Y && dlp.RPoint.Y < line.UpperRY)
                {
                    dlp.MainTask.LowpadDeltaX = 0;
                    dlp.MainTask.LowpadDeltaY = line.DeltaY;
                }
            }
        }

        public void HitHorizontalDriveLine(DumbLowPad dlp)
        {
            foreach(HorizontalLine line in HorizontalLines)
            {
                if (line.RY != dlp.RPoint.Y)
                    continue;
                if(line.LeftRX < dlp.RPoint.X && dlp.RPoint.X < line.RightRX &&
                   (!line.CarryTrolleyToEnterLine || line.MustHaveAtrolley(dlp)))
                {
                    dlp.MainTask.LowpadDeltaY = 0;
                    dlp.MainTask.LowpadDeltaX = line.DeltaX;
                }
            }
        }
    }

    internal class VerticalLine
    {
        public int RX;
        public int LowerRY;
        public int UpperRY;

        public int DeltaY;

        public VerticalLine(int Rx, int LowerRy, int UpperRy, int Deltay)
        {
            RX = Rx;
            LowerRY = LowerRy;  
            UpperRY = UpperRy;
            DeltaY = Deltay;
        }
    }

    internal class HorizontalLine
    {
        public int RY;
        public int LeftRX;
        public int RightRX;

        public int DeltaX;
        public bool CarryTrolleyToEnterLine;

        public HorizontalLine(int Ry, int LeftRx, int RightRx, int Deltax, bool CarryTrolleyToEnterLine_)
        {
            RY = Ry;
            LeftRX = LeftRx;
            RightRX = RightRx;
            DeltaX = Deltax;
            CarryTrolleyToEnterLine = CarryTrolleyToEnterLine_;
        }

        public bool MustHaveAtrolley(DumbLowPad dlp) 
        {
            return dlp.trolley != null;
        }
    }
}
