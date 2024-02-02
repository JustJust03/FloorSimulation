using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace FloorSimulation
{
    internal class RegionDistributerTask : Task
    {
        private Distributer DButer;
        private FinishedDistribution FinishedD;
        private LowPadAccessHub[] RegionHubs;
        private LowPadAccessHub RegionHub;

        public bool HasLowerAccessPoint;

        private WalkTile OldWalkTile; //Is only used to save on which spot you picked up a finished trolley.
        private ShopHub OldTargetHub;

        int WaitedTicks = 0;

        private readonly List<string> TargetIsOpenSpotsRegionDb = new List<string>
        {
            "DistributePlants",
            "DeliverEmptyTrolley",
            "DeliverFullTrolley"
        };

        public RegionDistributerTask(Distributer DButer_, string Goal_, FinishedDistribution FinishedD_, LowPadAccessHub[] RHubs_, DanishTrolley trolley_ = default) :
            base(Goal_, trolley_)
        {
            DButer = DButer_;
            FinishedD = FinishedD_;
            RegionHubs = RHubs_;
            AInfo = new AnalyzeInfo(DButer, this, DButer.distributionms_per_tick);
            Waiting = false;
            RegionHub = RegionHubs[0];
        }

        public override void PerformTask()
        {
            AInfo.TickAnalyzeInfo(DButer.floor.SpeedMultiplier);
            if (!InTask && RegionHub.HubTrolleys.Count > 0)
            {
                Goal = "TravelToLP";
                DButer.TravelToTile(RegionHub.DbOpenSpots());
                if (DButer.route == null)
                {
                    FailRoute();
                    return;
                }
                else if (RegionHub.HubTrolleys[0].PlantList.Count == 0)
                {
                    DButer.floor.FirstWW.unfill_tiles(new Point(RegionHub.RFloorPoint.X, RegionHub.RFloorPoint.Y - 60), new Size(RegionHub.RHubSize.Width + 10, 60));
                    DButer.TravelToTrolley(RegionHub.HubTrolleys[0], true);
                    Goal = "TakeRegionHubTrolley";
                    if (DButer.route == null)
                    {
                        FailRoute();
                        return;
                    }
                }
                else if (DButer.route.Count == 0)
                {
                    DButer.PlantInHand = RegionHub.HubTrolleys[0].GiveFirstPlantInRegion(RegionHub);
                    if (DButer.PlantInHand != null)
                    {
                        Goal = "DistributePlants";
                        TargetHub = DButer.PlantInHand.DestinationHub;
                        DButer.TravelToClosestTile(TargetHub.OpenSpots(DButer));
                        if (DButer.route == null) //Route was not possible at this point. Try again later.
                        {
                            FailRoute();
                            return;
                        }
                    }
                }

                InTask = true;
                Travelling = true;
            }
            else if (!InTask)
            {
                foreach(LowPadAccessHub RHub in RegionHubs)
                    if (RHub.HubTrolleys.Count > 0)
                    {
                        RegionHub = RHub;
                        break;
                    }
            }

            if (Waiting)
            {
                FailRoute();
                if (DButer.route != null)
                    WaitedTicks = 0;

                WaitedTicks++;

                if (WaitedTicks > DButer.MaxWaitedTicks)
                {
                    WaitedTicks = 0;
                    DButer.TravelToTile(DButer.WW.GetTile(DButer.SavePoint));
                    Waiting = false;
                    TargetWasSaveTile = true;
                    if(DButer.route == null)
                    {
                        Point p = new Point(DButer.RPoint.X - 20, DButer.RPoint.Y);
                        Size s = DButer.GetRSize();
                        s.Width += 40;
                        DButer.floor.FirstWW.unfill_tiles(p, s);
                        DButer.TravelToTile(DButer.WW.GetTile(DButer.SavePoint));
                    }
                }
                return;
            }

            if (InTask && Travelling)
                DButer.TickWalk();
            else if (InTask)
                DButer.TickDistribute();
        }

        public override void RouteCompleted()
        {
            if (TargetWasSaveTile)
            {
                TargetWasSaveTile = false;
                FailRoute();
                return;
            }

            AInfo.UpdateFreq(Goal);
            if (Goal == "TravelToLP")
                TravelToLP();
            else if (Goal == "DistributePlants")
                DistributePlants();
            else if (Goal == "TakeRegionHubTrolley")
                TakeRegionHubTrolley();
            else if (Goal == "DeliverEmptyTrolley")
                DeliverEmptyTrolley();
            else if (Goal == "TakeEmptyTrolley")
                TakeEmptyTrolley();
            else if (Goal == "DeliverEmptyTrolleyToShop")
                DeliverEmptyTrolleyToShop();
            else if (Goal == "TravelToStartTile")
                TravelToStartTile();
            else if (Goal == "DeliverFullTrolley")
                DeliverFullTrolley();
            else
                throw new NotImplementedException();
        }
        public override void FailRoute()
        {
            if (Goal == "TravelToLP")
                DButer.TravelToTile(RegionHub.DbOpenSpots());
            else if (Goal == "TakeRegionHubTrolley")
            {
                DButer.floor.FirstWW.unfill_tiles(new Point(RegionHub.RFloorPoint.X, RegionHub.RFloorPoint.Y - 60), new Size(RegionHub.RHubSize.Width + 10, 60));
                DButer.TravelToTrolley(RegionHub.HubTrolleys[0], true);
            }
            else if (Goal == "TravelToStartTile")
                DButer.TravelToTile(DButer.WW.GetTile(DButer.SavePoint));
            else if (TargetIsOpenSpotsRegionDb.Contains(Goal))
            {
                if (TargetHub == null)
                    return;
                DButer.TravelToClosestTile(TargetHub.OpenSpots(DButer));
            }
            else if (TargetIsFullBuffHub.Contains(Goal))
            {
                if (RegionHub.HubTrolleys.Count > 0)
                    DButer.floor.FirstWW.unfill_tiles(RegionHub.HubTrolleys[0].RPoint, RegionHub.HubTrolleys[0].GetRSize());
                TargetHub = DButer.floor.GetBuffHubFull(DButer);
                if (TargetHub == null)
                    return;
                DButer.TravelToClosestTile(TargetHub.FilledSpots(DButer));
            }
            else if (TargetIsOldWalktile.Contains(Goal))
            {
                if (RegionHub.HubTrolleys.Count > 0)
                    DButer.floor.FirstWW.unfill_tiles(RegionHub.HubTrolleys[0].RPoint, RegionHub.HubTrolleys[0].GetRSize());
                DButer.TravelToTile(OldWalkTile);
            }
            else if (Goal == "DeliverFullTrolley")
                DeliverFullTrolley();

            if (DButer.route == null)
                Waiting = true;
            else if (DButer.route.Count == 0)
                RouteCompleted();
            else
            {
                if (Waiting)
                    AInfo.UpdateWachtFreq();
                DButer.TickWalk();
                Waiting = false;
            }
        }

        private void TravelToLP()
        {
            DButer.PlantInHand = RegionHub.HubTrolleys[0].GiveFirstPlantInRegion(RegionHub);
            if (DButer.PlantInHand != null)
            {
                Goal = "DistributePlants";
                TargetHub = DButer.PlantInHand.DestinationHub;
                DButer.TravelToClosestTile(TargetHub.OpenSpots(DButer));
                if (DButer.route == null) //Route was not possible at this point. Try again later.
                {
                    FailRoute();
                    return;
                }
                InTask = true;
                Travelling = true;
            }
            else
            {
                RegionHub.HubTrolleys[0].ContinueDistribution = true;
                Goal = "TravelToStartTile"; //New goal
                DButer.TravelToTile(DButer.WW.GetTile(DButer.SavePoint));
                if(DButer.route == null)
                {
                    FailRoute();
                    return;
                }

                InTask = true;
                Travelling = true;
            }
        }

        private void DistributePlants()
        {
            Travelling = false;
            InTask = true;
        }

        private void TakeRegionHubTrolley()
        {
            DButer.TakeTrolleyIn(RegionHub.HubTrolleys[0]);
            RegionHub.GiveTrolley();
            RegionHub.Targeted = false;

            TargetHub = DButer.floor.GetBuffHubOpen(DButer);
            Trolley = DButer.trolley;
            Goal = "DeliverEmptyTrolley"; //New goal

            if (TargetHub != null)
                DButer.TravelToClosestTile(TargetHub.OpenSpots(DButer));
            else
                FailRoute();
        }

        private void DeliverEmptyTrolley()
        {
            DButer.RotateDistributerAndTrolley();

            DButer.WW.unoccupie_by_tiles(DButer.trolley.RPoint, DButer.trolley.GetRSize()); // drop the trolley of from the distributer
            DButer.GiveTrolley();
            if (Trolley.IsVertical)
                TargetHub.TakeVTrolleyIn(Trolley, DButer.RPoint);
            else
                TargetHub.TakeHTrolleyIn(Trolley, DButer.RPoint);

            Goal = "TravelToStartTile"; //New goal
            DButer.TravelToTile(DButer.WW.GetTile(DButer.SavePoint));
            if(DButer.route == null)
            {
                FailRoute();
                return;
            }

            InTask = true;
            Travelling = true;
        }

        private void TakeEmptyTrolley()
        {
            DanishTrolley t = TargetHub.GiveTrolley(DButer.RPoint);
            //The trolley in buffhub was already taken
            if (t == null)
            {
                TargetHub = DButer.floor.GetBuffHubFull(DButer);
                DButer.TravelToClosestTile(TargetHub.FilledSpots(DButer));
                if (DButer.route == null) //Route was not possible at this point. Try again later.
                    FailRoute();
                return;
            }
            Trolley = t;
            DButer.TakeTrolleyIn(t);


            if (!WasOnTopLeft)
                OldWalkTile = DButer.WW.GetTile(new Point(OldTargetHub.HubTrolleys[1].RPoint.X, OldWalkTile.Rpoint.Y)); //Because dbuter is on the left of the trolley.
            else 
                OldWalkTile = DButer.WW.GetTile(new Point(OldWalkTile.Rpoint.X + 30, OldWalkTile.Rpoint.Y)); //Because dbuter is on the left of the trolley.
            DButer.TravelToTile(OldWalkTile);
            TargetHub = OldTargetHub;

            Goal = "DeliverEmptyTrolleyToShop"; //New goal
            InTask = true;
            Travelling = true;
        }

        private void DeliverEmptyTrolleyToShop()
        {
            if (!WasOnTopLeft)
                DButer.SwitchDistributerTrolley();
            TargetHub.TakeHTrolleyIn(Trolley);
            DButer.GiveTrolley();

            if (RegionHub.HubTrolleys.Count > 0)
                DButer.floor.FirstWW.fill_tiles(RegionHub.HubTrolleys[0].RPoint, RegionHub.HubTrolleys[0].GetRSize());

            if(RegionHub.HubTrolleys.Count > 0) 
            {
                Goal = "TravelToLP";
                DButer.TravelToTile(RegionHub.DbOpenSpots());

                InTask = true;
                Travelling = true;
            }
            else
            {
                InTask = false;
                Travelling = false;
            }
        }

        private void TravelToStartTile()
        {
            Goal = "TravelToLP";
            InTask = false;
            Travelling = false;
        }

        private void DeliverFullTrolley()
        {
            TargetHub.TakeHTrolleyIn(Trolley, DButer.RPoint);
            DButer.WW.unoccupie_by_tiles(DButer.trolley.RPoint, DButer.trolley.GetRSize()); // drop the trolley of from the distributer
            DButer.GiveTrolley();

            TargetHub = DButer.floor.GetBuffHubFull(DButer);
            if (TargetHub == null)
                return;
            DButer.TravelToClosestTile(TargetHub.FilledSpots(DButer));
            Goal = "TakeEmptyTrolley"; //New goal
            if (DButer.route == null) //Route was not possible at this point. Try again later.
            {
                FailRoute();
                return;
            }

            InTask = true;
            Travelling = true;
        }


        public override void DistributionCompleted()
        {
            //AInfo.UpdateFreq(Goal, true);
            if (Goal == "DistributePlants")
            {
                Trolley = TargetHub.GetRandomTrolley();

                plant p = DButer.PlantInHand;
                if (Trolley.TakePlantIn(p))
                {
                    ShopTrolleyBecameFull();
                    return;
                }
                if (Trolley.NStickers == Trolley.MaxStickers) //Sticker bord became full. Add side activity
                {
                    StickerBordBecameFull();
                    return;
                }

                if(RegionHub.HubTrolleys.Count > 0) 
                {
                    Goal = "TravelToLP";
                    DButer.TravelToTile(RegionHub.DbOpenSpots());

                    InTask = true;
                    Travelling = true;
                }
                else
                {
                    InTask = false;
                    Travelling = false;
                }
            }
        }

        private void StickerBordBecameFull()
        {
            DButer.SideActivityMsLeft += Distributer.BordTime;
            DButer.SideActivity = "Bord";
            AInfo.UpdateFreq(Goal, true);
            Trolley.NStickers = 0;
        }

        private void ShopTrolleyBecameFull()
        {
            TargetHub.SwapIfOtherTrolley(Trolley);
            OldTargetHub = (ShopHub)TargetHub;

            OldWalkTile = DButer.WW.GetTile(DButer.RPoint);

            DButer.TakeTrolleyIn(TargetHub.GiveTrolley());
            WasOnTopLeft = DButer.TrolleyOnTopLeft;
            Trolley = DButer.trolley;
            TargetHub = DButer.floor.ClosestFTHub(DButer);
            DButer.TravelToClosestTile(TargetHub.OpenSpots(DButer));

            if (RegionHub.HubTrolleys.Count > 0)
                DButer.floor.FirstWW.unfill_tiles(RegionHub.HubTrolleys[0].RPoint, RegionHub.HubTrolleys[0].GetRSize());

            Goal = "DeliverFullTrolley"; //New goal
            InTask = true;
            Travelling = true;
        }

        public override int NTrolleysStanding()
        {
            return RegionHubs.Count(r => r.Targeted || r.HubTrolleys.Count > 0);
        }
    }
}
