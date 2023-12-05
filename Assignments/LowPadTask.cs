using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloorSimulation
{
    internal class LowPadTask : Task
    {
        Floor floor;
        LowPad LP;

        public LowPadTask(LowPad LP_, Floor floor_, string Goal_, DanishTrolley trolley_ = default): 
            base(Goal_, trolley_)
        {
            LP = LP_;
            floor = floor_;
        }

        public override void PerformTask()
        {
            if (!InTask && Goal == "TakeFullTrolley")
            {
                TargetHub = floor.GetStartHub(LP);
                Trolley = TargetHub.PeekFirstTrolley();
                if (Trolley != null)
                {
                    LP.TravelToTrolley(Trolley);
                    if (LP.route == null) //Route was not possible at this point. Try again later.
                        return;
                    InTask = true;
                    Travelling = true;
                }
                else //When the distribution is finished, travel to your savepoint.
                {
                    LP.TravelToTile(LP.WW.GetTile(LP.SavePoint));
                    InTask = true;
                    Travelling = true;
                    TargetWasSaveTile = true;
                }
            }
            else if(!InTask && Goal == "TravelToLPAccessHub" && LP.trolley.FinishedRegion((LowPadAccessHub)TargetHub))
            {
                //TODO: Remove this one from the hub
                TargetHub = LP.ClosestRegion(LP.trolley.TargetRegions);
                LP.TravelToClosestTile(TargetHub.OpenSpots(LP));
                if(LP.route == null)
                {
                    FailRoute();
                    return;
                }

                InTask = true;
                Travelling = true;
            }

            if (InTask && Travelling)
                LP.TickWalk();
        }

        public override void RouteCompleted()
        {
            if (Goal == "TakeFullTrolley")
                TakeFullTrolley();
            else if (Goal == "TravelToLPAccessHub")
                TravelToLPAccessHub();
        }

        public override void FailRoute()
        {
            return;
            //throw new NotImplementedException();
        }

        public override void DistributionCompleted()
        {
            throw new NotImplementedException();
        }

        private void TakeFullTrolley()
        {

            if (TargetHub.PeekFirstTrolley() != Trolley) // If the targeted trolley isn't in the hub anymore chose another trolley to target.
            {
                InTask = false;
                Travelling = false;
                return;
            }

            Goal = "TravelToLPAccessHub";   

            DanishTrolley t = TargetHub.GiveTrolley();
            LP.TakeTrolleyIn(t);

            TargetHub = LP.ClosestRegion(LP.trolley.TargetRegions);
            LP.TravelToClosestTile(TargetHub.OpenSpots(LP));
            if(LP.route == null)
            {
                FailRoute();
                return;
            }

            InTask = true;
            Travelling = true;
        }

        private void TravelToLPAccessHub()
        {
            TargetHub.TakeVTrolleyIn(LP.trolley);

            if(LP.trolley.TargetRegions.Count == 1) //Change the goal.
                throw new NotImplementedException();
            else
            {
                InTask = false;
                Travelling = false;
            }
        }
    }
}
