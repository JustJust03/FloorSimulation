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

        public RegionDistributerTask(Distributer DButer_, string Goal_, FinishedDistribution FinishedD_, LowPadAccessHub RHub_, DanishTrolley trolley_ = default): 
            base(Goal_, trolley_)
        {
            DButer = DButer_;
            FinishedD = FinishedD_;
            RegionHub = RHub_;
        }

        public override void PerformTask()
        {
            if(!InTask && RegionHub.HubTrolleys.Count > 0)
            {
                Goal = "TravelToLP";
                DButer.TravelToTile(RegionHub.DbOpenSpots());
                if(DButer.route == null)
                {
                    FailRoute();
                    return;
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
            if(Goal == "TravelToLP")
                TravelToLP();
            else if(Goal == "DistributePlants")
                DistributePlants();
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
        }

        private void DistributePlants()
        {
            Travelling = false;
            InTask = true;
        }


        public override void FailRoute()
        {
            return;
        }

        public override void DistributionCompleted()
        {
            //TODO: UPDATE AINFO
            //AInfo.UpdateFreq(Goal, true);
            if (Goal == "DistributePlants")
            {
                Trolley = TargetHub.GetRandomTrolley();

                plant p = DButer.PlantInHand;
                /*
                if (Trolley.TakePlantIn(p)) //Transports plant from the distributer's trolley to the shop trolley. True when the shop trolley became full.
                {
                    ShopTrolleyBecameFull();
                    return;
                };
                if (Trolley.NStickers == Trolley.MaxStickers) //Sticker bord became full. Add side activity
                {
                    StickerBordBecameFull();
                    return;
                }
                */
                Trolley.TakePlantIn(p);

                if(RegionHub.HubTrolleys.Count > 0) 
                {
                    Goal = "TravelToLP";
                    DButer.TravelToTile(RegionHub.DbOpenSpots());
                    if(DButer.route == null)
                    {
                        FailRoute();
                        return;
                    }

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
    }
}
