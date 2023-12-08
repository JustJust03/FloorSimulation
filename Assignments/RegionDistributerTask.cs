using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloorSimulation
{
    internal class RegionDistributerTask : Task
    {
        private Distributer DButer;
        private FinishedDistribution FinishedD;
        private LowPadAccessHub RegionHub;

        private readonly List<string> TargetIsOpenSpotsRegionDb = new List<string>
        {
            "DistributePlants",
            "DeliverEmptyTrolley",
            "DeliverFullTrolley"
        };

        public RegionDistributerTask(Distributer DButer_, string Goal_, FinishedDistribution FinishedD_, LowPadAccessHub RHub_, DanishTrolley trolley_ = default) :
            base(Goal_, trolley_)
        {
            DButer = DButer_;
            FinishedD = FinishedD_;
            RegionHub = RHub_;
            AInfo = new AnalyzeInfo(DButer, this, DButer.distributionms_per_tick);
            Waiting = true;
        }

        public override void PerformTask()
        {
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
                    DButer.TravelToTrolley(RegionHub.HubTrolleys[0]);
                    Goal = "TakeRegionHubTrolley";
                    if(DButer.route == null)
                    {
                        FailRoute();
                        return;
                    }
                }
                else if(DButer.route.Count == 0)
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

            if (InTask && Travelling)
                DButer.TickWalk();
            else if (InTask)
                DButer.TickDistribute();
        }

        public override void RouteCompleted()
        {
            if (Goal == "TravelToLP")
                TravelToLP();
            else if (Goal == "DistributePlants")
                DistributePlants();
            else if (Goal == "TakeRegionHubTrolley")
                TakeRegionHubTrolley();
            else if (Goal == "DeliverEmptyTrolley")
                DeliverEmptyTrolley();
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
                DButer.TravelToTrolley(RegionHub.HubTrolleys[0]);
            else if (Goal == "TravelToStartTile")
                DButer.TravelToTile(DButer.WW.GetTile(DButer.SavePoint));
            else if (TargetIsOpenSpotsRegionDb.Contains(Goal))
                DButer.TravelToClosestTile(TargetHub.OpenSpots(DButer));
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
                Goal = "TravelToStartTile"; //New goal
                DButer.TravelToTile(DButer.WW.GetTile(DButer.SavePoint));
                if(DButer.route == null)
                {
                    FailRoute();
                    return;
                }

                RegionHub.HubTrolleys[0].ContinueDistribution = true;
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
            if (TargetHub.VerticalTrolleys != Trolley.IsVertical)
            {
                DButer.WW.unfill_tiles(DButer.RPoint, DButer.GetRSize());
                DButer.WW.unfill_tiles(Trolley.RPoint, Trolley.GetRSize());
                DButer.RotateDistributerAndTrolley();
            }

            DButer.TravelToClosestTile(TargetHub.OpenSpots(DButer));

            Goal = "DeliverEmptyTrolley"; //New goal
        }

        private void DeliverEmptyTrolley()
        {
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


        public override void DistributionCompleted()
        {
            //TODO: UPDATE AINFO
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

        private void ShopTrolleyBecameFull()
        {
            TargetHub.SwapIfOtherTrolley(Trolley);

            DButer.TakeTrolleyIn(TargetHub.GiveTrolley());
            WasOnTopLeft = DButer.TrolleyOnTopLeft;
            Trolley = DButer.trolley;
            TargetHub = DButer.floor.ClosestFTHub(DButer);
            DButer.TravelToClosestTile(TargetHub.OpenSpots(DButer));

            Goal = "DeliverFullTrolley"; //New goal
            InTask = true;
            Travelling = true;
        }
    }
}
