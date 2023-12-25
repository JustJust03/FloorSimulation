using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Gurobi;

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
            NLowpads = 50;
            ShopStartX = 70;
            RealFloorWidth = 5200;
            StreetWidth += 120;
            RealFloorHeight = 5200;
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

                foreach(LowPadAccessHub lpa in lowPadAccessHubs[id])
                    lpa.dbuter = db;

                floor.DistrList.Add(db);
            }
        }

        public void CreateDriveLines()
        {
            List<LowPadAccessHub>[] ShopsPLine = new List<LowPadAccessHub>[3];
            ShopsPLine[0] = floor.LPHubs.GetRange(39, 38);
            ShopsPLine[1] = floor.LPHubs.GetRange(77, 38);
            ShopsPLine[2] = floor.LPHubs.GetRange(115, 18);

            LPDriveLines = new LowPadDriveLines(ShopCornersX[ShopCornersX.Count - 1], UpperY - 300, RealFloorHeight - 160);

            LPDriveLines.AddHorizontalLine(RealFloorHeight - 210, 0, RealFloorWidth, -1); //Lowest line, Used to pick up a new full trolley
            LPDriveLines.AddHorizontalLine(RealFloorHeight - 410, 360, 850, -1, true); //Normal loop again. Used to push the lp's with the new trolleys to the first vertical shopline
            LPDriveLines.AddHorizontalLine(RealFloorHeight - 410, 0, 360, 1, true); //Also normal loop. Also Pushed the lp's to the first vertical shopline
            LPDriveLines.AddHorizontalLine(RealFloorHeight - 410, 850, RealFloorWidth, -1, true); //If a lp finished the loop, but still carries a trolley, put it on this line.

            LPDriveLines.AddHorizontalLine(UpperY - 180, 0, RealFloorWidth, 1); //Backup Line
            LPDriveLines.AddHorizontalLine(LowestY + 10, 500, 3310, 1, true); //lower horizontal line below the shops.

            LPDriveLines.AddVerticalLine(ShopCornersX[0] + 200, UpperY - 20, LowestY + 310, -1);
            int ShopsPLineI = 0;
            for(int cornerI = 2; cornerI < ShopCornersX.Count; cornerI += 2) //Shop hub lines...
            {
                if(ShopsPLineI < 3)
                    LPDriveLines.AddVerticalLine(ShopCornersX[cornerI] + 200, UpperY - 20, LowestY + 20, -1, ShopsInLine: ShopsPLine[ShopsPLineI]);
                else
                    LPDriveLines.AddVerticalLine(ShopCornersX[cornerI] + 200, UpperY - 20, LowestY + 20, -1);
                ShopsPLineI++;
            }
            for(int cornerI = 1; cornerI < ShopCornersX.Count; cornerI += 2)
                LPDriveLines.AddVerticalLine(ShopCornersX[cornerI] - 260, UpperY - 200, LowestY, 1);

            for(int EvenI = 0; EvenI < ShopCornersX.Count - 1; EvenI += 2)
                LPDriveLines.AddVerticalLine(ShopCornersX[EvenI] + 70, UpperY - 10, LowestY, 0, EnterLPAHubWhenHit: true);
            for(int UnevenI = 1; UnevenI < ShopCornersX.Count - 1; UnevenI += 2)
                LPDriveLines.AddVerticalLine(ShopCornersX[UnevenI] - 130, UpperY - 10, LowestY, 0, EnterLPAHubWhenHit: true);

            LPDriveLines.AddVerticalLine(ShopCornersX[ShopCornersX.Count - 1] - 130, UpperY - 10, floor.LPHubs[floor.LPHubs.Count - 1].RFloorPoint.Y + 50, 0, EnterLPAHubWhenHit: true);

            foreach(LowPadAccessHub LPAHub in floor.LPHubs)
            {
                if(LPAHub.HasLeftAccess)
                    LPDriveLines.AddHorizontalLine(LPAHub.RFloorPoint.Y, LPAHub.RFloorPoint.X - 170, LPAHub.RFloorPoint.X, 1, LPA: LPAHub);
                else
                    LPDriveLines.AddHorizontalLine(LPAHub.RFloorPoint.Y, LPAHub.RFloorPoint.X, LPAHub.RFloorPoint.X + 170, -1, LPA: LPAHub);
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
                    deltaX = 999;
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
            int[] NshopsPerDbuter = new int[] { 7, 7, 6, 6, 6, 7, 7, 6, 6, 6, 6, 7, 7, 6, 6, 6, 6, 7, 6, 6, 6 };

            GRBEnv env = new GRBEnv();
            env.Start();
            GRBModel model = new GRBModel(env);

            GRBVar[] ShopsPerDistributer = new GRBVar[Shops.Count * NDbuters];
            for (int i = 0; i < Shops.Count * NDbuters; i++)
                ShopsPerDistributer[i] = model.AddVar(0.0, 1.0, 0.0, GRB.BINARY, $"Shop_{i}_Shops");


            // Ensure each shop is assigned to exactly 1 distributor
            for (int s = 0; s < Shops.Count; s++)
            {
                GRBLinExpr distributorCountExpr = new GRBLinExpr();

                for (int d = 0; d < NDbuters; d++)
                    distributorCountExpr += ShopsPerDistributer[d * Shops.Count + s];

                model.AddConstr(distributorCountExpr == 1, $"Shop_{s}_DistributorCount");
            }

            GRBLinExpr[] ShopsPerDbuterExpr = new GRBLinExpr[NDbuters];
            GRBLinExpr[] StickersPerDbuterExpr = new GRBLinExpr[NDbuters];

            GRBVar MaxVar = model.AddVar(0, GRB.INFINITY, 0, GRB.INTEGER, "Maximum var");
            GRBVar MinVar = model.AddVar(0, GRB.INFINITY, 0, GRB.INTEGER, "Minimum var");

            //every dbuter distributes to 6 or 7 shops.
            //calculate the stickers per distributer and minimize the difference.
            for (int dbi = 0; dbi < DistributionRegions.Count; dbi++)
            {
                ShopsPerDbuterExpr[dbi] = new GRBLinExpr();
                StickersPerDbuterExpr[dbi] = new GRBLinExpr();

                for (int shopi = 0; shopi < Shops.Count; shopi++)
                {
                    ShopsPerDbuterExpr[dbi] += ShopsPerDistributer[dbi * Shops.Count + shopi];
                    StickersPerDbuterExpr[dbi] += Shops[shopi].StickersToReceive * ShopsPerDistributer[dbi * Shops.Count + shopi];
                }

                model.AddConstr(ShopsPerDbuterExpr[dbi] == NshopsPerDbuter[dbi], $"Distributor_{dbi}");

                model.AddConstr(MaxVar >= StickersPerDbuterExpr[dbi], $"maxVarConstraint{dbi}");
                model.AddConstr(MinVar <= StickersPerDbuterExpr[dbi], $"minVarConstraint{dbi}");
            }



            model.SetObjective(MaxVar - MinVar, GRB.MINIMIZE);
            model.Optimize();

            //Assign the regions.
            for(int shopi = 0; shopi < Shops.Count; shopi++)
                for(int dbi = 0; dbi < NDbuters; dbi++)
                    if (ShopsPerDistributer[dbi * Shops.Count + shopi].X == 1)
                        DistributionRegions[dbi].Add(Shops[shopi]);


            return DistributionRegions;
        }

        private List<List<ShopHub>> OldAssignDBregions(List<ShopHub> Shops, List<List<ShopHub>> DistributionRegions)
        {
            //int[] NshopsPerDbuter = new int[] { 10, 10, 5, 4, 9, 9, 4, 5, 5, 4, 9, 9, 4, 5, 5, 4, 9, 9, 4, 5, 5 };
            int[] NshopsPerDbuter = new int[] { 7, 7, 6, 6, 6, 7, 7, 6, 6, 6, 6, 7, 7, 6, 6, 6, 6, 7, 6, 6, 6 };
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

        int MostRightX;
        int UppestY;
        int LowestY;

        public LowPadDriveLines(int LastCornerX, int HighestY, int LowestY_)
        {
            MostRightX = LastCornerX + 300;
            UppestY = HighestY;
            LowestY = LowestY_;
        }

        public void AddVerticalLine(int Rx, int LowerRy, int UpperRy, int DeltaY, bool EnterLPAHubWhenHit = false, List<LowPadAccessHub> ShopsInLine = null)
        {
            VerticalLines.Add(new VerticalLine(Rx, LowerRy, UpperRy, DeltaY, EnterLPAHubWhenHit, ShopsInLine));
        }

        public void AddHorizontalLine(int Ry, int LeftRx, int RightRx, int DeltaX, bool CarryTrolleyToEnterLine = false, LowPadAccessHub LPA= default)
        {
            HorizontalLines.Add(new HorizontalLine(Ry, LeftRx, RightRx, DeltaX, CarryTrolleyToEnterLine, LPA));
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
            if(dlp.RPoint.X > MostRightX && dlp.MainTask.LowpadDeltaX == 1)
            {
                dlp.MainTask.LowpadDeltaX = 0;
                dlp.MainTask.LowpadDeltaY = 1;
                return;
            }
            else if(dlp.RPoint.X <= 10 && dlp.MainTask.LowpadDeltaX == -1)
            {
                dlp.MainTask.LowpadDeltaX = 0;
                dlp.MainTask.LowpadDeltaY = -1;
                return;
            }

            foreach(VerticalLine line in VerticalLines)
            {
                if (line.RX != dlp.RPoint.X)
                    continue;
                if (line.LowerRY < dlp.RPoint.Y && dlp.RPoint.Y < line.UpperRY &&
                   (line.ShopsInLine == null || dlp.trolley == null || LowestY - 420 != dlp.RPoint.Y || line.ShopsInLine.Intersect(dlp.trolley.TargetRegions).Any()))
                {
                    if (line.EnterLPAHubWhenHit && dlp.LPAHub == null)
                        return; //The lp got stuck and couldn't move out of regionhub.

                    dlp.MainTask.LowpadDeltaX = 0;
                    dlp.MainTask.LowpadDeltaY = line.DeltaY;
                    if(line.EnterLPAHubWhenHit)
                        dlp.HitAccessHub();
                    else
                        dlp.FinishedRegion();
                }
                ;
            }
            ;
        }

        public void HitHorizontalDriveLine(DumbLowPad dlp)
        {
            if(dlp.RPoint.Y > LowestY && dlp.MainTask.LowpadDeltaY == 1)
            {
                dlp.MainTask.LowpadDeltaX = 0;
                dlp.MainTask.LowpadDeltaY = -1;
                return;
            }
            else if (dlp.RPoint.Y < UppestY && dlp.MainTask.LowpadDeltaY == -1)
            {
                dlp.MainTask.LowpadDeltaX = 1;
                dlp.MainTask.LowpadDeltaY = 0;
                return;
            }
            
            foreach(HorizontalLine line in HorizontalLines)
            {
                if (line.RY != dlp.RPoint.Y)
                    continue;
                if (line.LeftRX < dlp.RPoint.X && dlp.RPoint.X < line.RightRX &&         //Hit line,
                   (!line.CarryTrolleyToEnterLine || line.MustHaveAtrolley(dlp)) &&     //Must have a trolley to enter this line (redo the loop).
                   (line.LPA == default || line.EnterRegion(dlp)))                      //Must have a plant to deliver to this region and it should not be used already.
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
        public bool EnterLPAHubWhenHit;
        public List<LowPadAccessHub> ShopsInLine;

        public VerticalLine(int Rx, int LowerRy, int UpperRy, int Deltay, bool EnterLPAHubWhenHit_, List<LowPadAccessHub> ShopsInLine_)
        {
            RX = Rx;
            LowerRY = LowerRy;  
            UpperRY = UpperRy;
            DeltaY = Deltay;

            EnterLPAHubWhenHit = EnterLPAHubWhenHit_;
            ShopsInLine = ShopsInLine_;
        }
    }

    internal class HorizontalLine
    {
        public int RY;
        public int LeftRX;
        public int RightRX;

        public int DeltaX;
        public bool CarryTrolleyToEnterLine;

        public LowPadAccessHub LPA;

        public HorizontalLine(int Ry, int LeftRx, int RightRx, int Deltax, bool CarryTrolleyToEnterLine_, LowPadAccessHub LPA_)
        {
            RY = Ry;
            LeftRX = LeftRx;
            RightRX = RightRx;
            DeltaX = Deltax;
            CarryTrolleyToEnterLine = CarryTrolleyToEnterLine_;
            LPA = LPA_;
        }

        public bool MustHaveAtrolley(DumbLowPad dlp) 
        {
            return dlp.trolley != null;
        }

        public bool EnterRegion(DumbLowPad dlp)
        {
            if (dlp.trolley != null && dlp.trolley.TargetRegions.Contains(LPA) && !LPA.Targeted && LPA.HubTrolleys.Count == 0)
            {
                double odds = Math.Min((1.0 / dlp.trolley.TargetRegions.Count) + (1.0 / Math.Pow(2, LPA.dbuter.MainTask.NTrolleysStanding() + 1.0)), 1.0);
                if (odds < 0.20)
                    return false;
                
                dlp.LPAHub = LPA;
                LPA.Targeted = true;
                return true;
            }
            return false;
        }
    }
}
