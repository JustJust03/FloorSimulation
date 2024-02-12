﻿using System;
using System.IO;
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
        readonly bool UseDumbRegions = false;
        readonly bool UseSemiDumbRegions = true;
        int NShops = 0;

        //int[] NshopsPerDbuter = new int[] { 10, 10, 5, 4, 9, 9, 4, 5, 5, 4, 9, 9, 4, 5, 5, 4, 9, 9, 4, 5, 5 };
        public int[] NshopsPerDbuter = new int[] { 7, 7, 6, 6, 6, 7, 7, 6, 6, 6, 6, 7, 7, 6, 6, 6, 6, 7, 7, 7, 6 }; //21
        //int[] NshopsPerDbuter = new int[] {10, 10, 9, 10, 10, 9, 9, 10, 10, 9, 9, 10, 9, 9 }; // 14
        public int[] RegionsPLine = new int[] { 6, 6, 6, 3 };


        public LowPadSlayoutBuffhub(Floor floor_, ReadData rData) : base(floor_, rData)
        {
            NLowpads = 50;
            ShopStartX = 70;
            RealFloorWidth = 5200;
            StreetWidth += 120;
            RealFloorHeight = 5200;

            UseStickersForFull = false;
            CombineTrolleys = true;
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
            NShops = Shops.Count;
            regions = CreateDistributionRegions(Shops);

            Shops = regions.SelectMany(obj => obj).ToList();
            foreach(ShopHub s in Shops)
                s.DrawRegions = true;

            if(UseSemiDumbRegions)
                LPPlaceShopsSemiDumb(Shops, UpperY, LowerY);
            else
                LPPlaceShops(Shops, UpperY, LowerY);

            foreach(List<ShopHub> region in regions) 
            {
                region[0].RegionStartOrEnd = true; //First Shop
                region[region.Count - 1].RegionStartOrEnd = true; //Last Shop
            }

            PlaceLPAccessHubs();
            CreateDriveLines();
        }

        public virtual void LPPlaceShopsSemiDumb(List<ShopHub> Shops, int UpperY_, int LowerY)
        {
            throw new NotImplementedException("This has not yet been implemented for the old layout.");
        }

        public virtual void LPPlaceShops(List<ShopHub> Shops, int UpperY_, int LowerY)
        {
            UpperY = UpperY_;
            int y = LowerY;
            int x = ShopStartX;
            int two_per_row = 1; //Keeps track of how many cols are placed without space between them
            int placed_shops_in_a_row = 0;

            bool FirstColFinished = false;
            for (int i = 0; i < Shops.Count; i++)
            {
                ShopHub Shop = Shops[i];
                if (two_per_row == 2)
                    Shop.HasLeftAccess = true;
                Shop.TeleportHub(new Point(x, y));

                //Add corners when you get to a new col, or if this was the last col on the right
                if (y == UpperY || (i == Shops.Count - 1 && two_per_row == 1))
                {
                    if (Shop.HasLeftAccess)
                        ShopCornersX.Add(Shop.RFloorPoint.X);
                    else
                        ShopCornersX.Add(Shop.RFloorPoint.X + Shop.RHubSize.Width);
                    if (i == Shops.Count - 1)
                        ShopCornersX.Add(Shop.RFloorPoint.X + StreetWidth);
                }

                placed_shops_in_a_row++;
                if (FirstColFinished && placed_shops_in_a_row == HalfShopsInRow && !CheckForSkip135Shops(Shops.Count, i))
                {
                    placed_shops_in_a_row = 0;
                    if (two_per_row == 1)
                        y -= ShopHeight;
                    else
                        y += ShopHeight;
                }

                if (two_per_row == 1 && y > UpperY)
                    y -= ShopHeight;
                else if (two_per_row == 2 && y < LowerY)
                    y += ShopHeight;
                else
                {
                    if (FirstColFinished && i < Shops.Count - 1 && y > LowerY)
                    {
                        floor.HubList.Add(Shop);
                        i++;
                        Shop = Shops[i];
                        if (two_per_row == 2)
                            Shop.HasLeftAccess = true;
                        Shop.TeleportHub(new Point(x, y));
                    }

                    if (!FirstColFinished)
                        HalfShopsInRow = (placed_shops_in_a_row - 1) / 3;
                    FirstColFinished = true;
                    placed_shops_in_a_row = 0;
                    two_per_row++;
                    if (two_per_row <= 2)
                    {
                        x += StreetWidth;
                        y = UpperY;
                    }
                    else
                    {
                        LowestY = y + ShopHeight;
                        if (two_per_row == 3)
                            Shop.HasLeftAccess = true;
                        x += 160;
                        two_per_row = 1;
                        placed_shops_in_a_row--;
                        if (Shops.Count - i == 19)
                        {
                            y -= ShopHeight;
                            placed_shops_in_a_row++;
                        }
                    }
                }

                floor.HubList.Add(Shop);
            }
        }

        public int[] MaxShopsPerRow()
        {
            Dictionary<int, int[]> RowToIndices = new Dictionary<int, int[]>();
            RowToIndices.Add(0, new int[] { 0, 5 });
            RowToIndices.Add(1, new int[] { 1, 4 });
            RowToIndices.Add(2, new int[] { 2, 3 });

            int TussenPadHeight = 1;
            int[] MaxPerRow = new int[3];
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < NshopsPerDbuter.Length; j++)
                {
                    if (RowToIndices[i].Contains(j % 6) && MaxPerRow[i] < NshopsPerDbuter[j])
                        MaxPerRow[i] = NshopsPerDbuter[j];
                    if (j == NshopsPerDbuter.Length - 1)
                        MaxPerRow[i] += TussenPadHeight;
                }

            return MaxPerRow;
        }

        public bool CheckForSkip135Shops(int Nshops, int i)
        {
            if (Nshops != 135)
                return false;
            else
                return i == 127;
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
            List<LowPadAccessHub>[] ShopsPLine = CalculateShopsPLine();

            LPDriveLines = new LowPadDriveLines(ShopCornersX[ShopCornersX.Count - 1], UpperY - 300, RealFloorHeight - 160);

            LPDriveLines.AddHorizontalLine(RealFloorHeight - 210, 0, RealFloorWidth, -1); //Lowest line, Used to pick up a new full trolley
            LPDriveLines.AddHorizontalLine(LowestY + 180, 360, 850, -1, true); //Normal loop again. Used to push the lp's with the new trolleys to the first vertical shopline
            LPDriveLines.AddHorizontalLine(LowestY + 10, 0, 360, 1, true); //Also normal loop. Also Pushed the lp's to the first vertical shopline
            LPDriveLines.AddHorizontalLine(LowestY + 180, 850, RealFloorWidth, -1, true); //If a lp finished the loop, but still carries a trolley, put it on this line.

            LPDriveLines.AddHorizontalLine(UpperY - 180, 0, RealFloorWidth, 1); //Backup Line
            LPDriveLines.AddHorizontalLine(LowestY + 10, 500, 3310, 1, true); //lower horizontal line below the shops.

            LPDriveLines.AddVerticalLine(ShopCornersX[0] + 100, LowestY, LowestY + 310, -1); //If the first loop is skipped, up to the normal move right height

            LPDriveLines.AddVerticalLine(ShopCornersX[0] + 200, UpperY - 20, LowestY + 310, -1, ShopsInLine: ShopsPLine[0]);
            int ShopsPLineI = 1;
            for(int cornerI = 2; cornerI < ShopCornersX.Count; cornerI += 2) //Shop hub lines...
            {
                if(ShopsPLineI < 4)
                    LPDriveLines.AddVerticalLine(ShopCornersX[cornerI] + 200, UpperY - 20, LowestY + 20, -1, ShopsInLine: ShopsPLine[ShopsPLineI]);
                else
                    LPDriveLines.AddVerticalLine(ShopCornersX[cornerI] + 200, UpperY - 20, LowestY + 20, -1);
                ShopsPLineI++;
            }
            for(int cornerI = 1; cornerI < ShopCornersX.Count; cornerI += 2)
                LPDriveLines.AddVerticalLine(ShopCornersX[cornerI] - 260, UpperY - 200, LowestY, 1);
            LPDriveLines.AddVerticalLine(ShopCornersX[ShopCornersX.Count - 1] + 400, UpperY - 200, LowestY, 1);


            for (int EvenI = 0; EvenI < ShopCornersX.Count; EvenI += 2)
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

        public List<LowPadAccessHub>[] CalculateShopsPLine()
        {
            List<LowPadAccessHub>[] ShopsPLine = new List<LowPadAccessHub>[4];

            int ShopsAdded = 0;
            int RegionsAdded = 0;
            for (int i = 0; i < RegionsPLine.Length; i++)
            {

                int ShopsInLine = NshopsPerDbuter.Skip(RegionsAdded).Take(RegionsPLine[i]).Sum();
                RegionsAdded += RegionsPLine[i];
                ShopsPLine[i] = floor.LPHubs.GetRange(ShopsAdded, ShopsInLine);
                ShopsAdded += ShopsInLine;
            }

            return ShopsPLine;
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
                x += 150;
                if (x > floor.FirstWW.RSizeWW.Width - 250)
                {
                    x = StartPoint.X;
                    y += 150;
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
            .Where(obj => obj.name != "Buffer hub")
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
            List<List<ShopHub>> DistributionRegions = new List<List<ShopHub>>();
            for(int i = 0; i < NDbuters; i++)
                DistributionRegions.Add(new List<ShopHub>());
            if(UseDumbRegions)
            {
                AssignDumbDBregions(Shops, DistributionRegions);
                return DistributionRegions;
            }    
            else if (UseSemiDumbRegions)
            {
                AssignDBregionsFixedOrder(Shops, DistributionRegions);
                return DistributionRegions;
            }

            Shops = Shops.OrderBy(obj => obj.StickersToReceive).ToList();

            try
            {
                AssignDBregions(Shops, DistributionRegions);
                Console.WriteLine("Used the NEW assign regions");
            }
            catch (GRBException)
            {
                OldAssignDBregions(Shops, DistributionRegions);
                Console.WriteLine("Used the OLD assign regions");
            }


            //Sort the shops within the regions again.
            for (int regioni = 0; regioni < DistributionRegions.Count; regioni++)
            {
                DistributionRegions[regioni] = DistributionRegions[regioni]
                    .OrderBy(shop => shop.day)
                    .ThenBy(shop => shop.id).ToList();
            }

            return DistributionRegions;
        }
        private List<List<ShopHub>> AssignDumbDBregions(List<ShopHub> Shops, List<List<ShopHub>> DistributionRegions)
        {
            int[] NshopsPerDbuter = new int[] { 7, 7, 6, 6, 6, 7, 7, 6, 6, 6, 6, 7, 7, 6, 6, 6, 6, 7, 6, 6, 6 };

            int shopindex = 0;
            for(int regioni = 0; regioni < NshopsPerDbuter.Length; regioni++)
            {
                int shopi = 0;
                while(shopi < NshopsPerDbuter[regioni])
                {
                    DistributionRegions[regioni].Add(Shops[shopindex + shopi]);
                    shopi++;
                }
                shopindex += shopi;
            }

            int[] StickersPerDButer = RegionConstants.StickerPerDButerNew(DistributionRegions);


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

            int[] StickersPerDButer = RegionConstants.StickerPerDButerNew(DistributionRegions);

            return DistributionRegions;
        }

        private List<List<ShopHub>> AssignDBregionsFixedOrder(List<ShopHub> Shops, List<List<ShopHub>> DistributionRegions)
        {
            int[] StickersPerDButer;
            string file = Program.rootfolder + @"\Data\BestShopDistribution\" +
                floor.Display.date + "_" + floor.layout.NLowpads + "Lowpads_" + Floor.NDistributers + "Distributers" + ".json";
            if (File.Exists(file))
            {
                ReadSolution RS = new ReadSolution();
                RS.Read(Shops, DistributionRegions, file);

                StickersPerDButer = RegionConstants.StickerPerDButerNew(DistributionRegions);

                NshopsPerDbuter = DistributionRegions.Select(lst => lst.Count).ToArray();
                return DistributionRegions;
            }
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
                    GRBLinExpr expr = new GRBLinExpr();
                    expr.AddConstant(1.0);

                    // Condition: ShopsPerDistributer[dbi * Shops.Count + shopi] == 0
                    if(dbi > 0 || shopi > 0)
                        expr.AddTerm(-1.0, ShopsPerDistributer[dbi * Shops.Count + shopi]);

                    // Condition: ShopsPerDistributer[dbi * Shops.Count + shopi - 1] == 1 (if shopi > 0)
                    if (shopi > 0)
                        expr.AddTerm(1.0, ShopsPerDistributer[dbi * Shops.Count + shopi - 1]);

                    // Condition: ShopsPerDistributer[(dbi - 1) * Shops.Count + shopi - 1] == 1 (if dbi > 0 and shopi > 0)
                    if (dbi > 0 && shopi > 0)
                        expr.AddTerm(1.0, ShopsPerDistributer[(dbi - 1) * Shops.Count + shopi - 1]);

                    model.AddConstr(expr, GRB.GREATER_EQUAL, 1, "CombinedConstraint");

                    StickersPerDbuterExpr[dbi] += Shops[shopi].StickersToReceive * ShopsPerDistributer[dbi * Shops.Count + shopi];
                }
                model.AddConstr(MaxVar >= StickersPerDbuterExpr[dbi], $"maxVarConstraint{dbi}");
                model.AddConstr(MinVar <= StickersPerDbuterExpr[dbi], $"minVarConstraint{dbi}");
            }

            model.SetObjective(MaxVar - MinVar, GRB.MINIMIZE);
            model.Set("TimeLimit", "100.0");
            model.Optimize();

            //Assign the regions.
            for (int shopi = 0; shopi < Shops.Count; shopi++)
                for (int dbi = 0; dbi < NDbuters; dbi++)
                    if (ShopsPerDistributer[dbi * Shops.Count + shopi].X == 1)
                        DistributionRegions[dbi].Add(Shops[shopi]);

            WriteSolution WS = new WriteSolution(floor);
            WS.Write(DistributionRegions);

            NshopsPerDbuter = DistributionRegions.Select(lst => lst.Count).ToArray();

            return DistributionRegions;
        }

        private List<List<ShopHub>> OldAssignDBregions(List<ShopHub> Shops, List<List<ShopHub>> DistributionRegions)
        {
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
            else if(dlp.RPoint.X <= 10 && dlp.MainTask.LowpadDeltaX == -1) //Hit the left wall delete self if no more trolleys left.
            {
                if(dlp.trolley == null && dlp.floor.STHubs[0].HubTrolleys.Count == 0)
                {
                    dlp.floor.FirstWW.unfill_tiles(dlp.RPoint, dlp.GetRSize());
                    dlp.floor.DLPList[dlp.floor.DLPList.IndexOf(dlp)] = null;
                    return;
                }
                dlp.MainTask.LowpadDeltaX = 0;
                dlp.MainTask.LowpadDeltaY = -1;
                return;
            }

            foreach(VerticalLine line in VerticalLines)
            {
                if (line.RX != dlp.RPoint.X)
                    continue;
                if (line.LowerRY < dlp.RPoint.Y && dlp.RPoint.Y < line.UpperRY &&
                   (line.ShopsInLine == null || dlp.trolley == null || dlp.RPoint.Y < LowestY - 420 || line.ShopsInLine.Intersect(dlp.trolley.TargetRegions).Any()))
                {
                    if (line.EnterLPAHubWhenHit && dlp.LPAHub == null)
                        return; //The lp got stuck and couldn't move out of regionhub.
                    if(line.ShopsInLine != null && line.ShopsInLine.Count == 0)
                    {
                        ;
                    }

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
                double odds = Math.Min((1.0 / dlp.trolley.TargetRegions.Count) + (1.0 / Math.Pow(1.9, LPA.dbuter.MainTask.NTrolleysStanding() + 1.0)), 1.0);
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
