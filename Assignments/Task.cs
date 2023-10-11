using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloorSimulation
{
    internal class Task
    {
        public DanishTrolley Trolley;
        public Hub TargetHub;
        private Distributer DButer;

        public bool InTask = false;
        private bool Travelling = false;
        public string Goal; // "TakeFullTrolley", "DistributePlants" 


        public Task(Hub TargetHub_,  Distributer DButer_, string Goal_, DanishTrolley trolley_ = default)
        {
            Trolley = trolley_;
            TargetHub = TargetHub_;
            DButer = DButer_;
            Goal = Goal_;
        }

        public void PerformTask()
        {
            if (!InTask && Goal == "TakeFullTrolley")
            {
                Trolley = DButer.floor.FirstStartHub.PeekFirstTrolley();
                DButer.TravelToTrolley(Trolley);
                InTask = true;
                Travelling = true;
            }

            if (InTask && Travelling)
                DButer.TickWalk();
            else if(InTask) 
                DButer.TickDistribute();
            
        }

        public void RouteCompleted()
        {
            if (Goal == "TakeFullTrolley") //Old goal
            {
                //TODO: Check to see to which trolley you should deliver to
                DButer.trolley = TargetHub.GiveTrolley();
                TargetHub = DButer.trolley.PeekFirstPlant().DestinationHub;
                Trolley = TargetHub.PeekFirstTrolley();
                DButer.TravelToTrolley(Trolley);
                Goal = "DistributePlants"; //New goal
                InTask = true;
                Travelling = true;
            }

            else if (Goal == "DistributePlants")
            {
                Travelling = false;
                InTask = true;
            }

            else if (Goal == "DeliveringEmptyTrolley") //Old goal
            {
                TargetHub.TakeVTrolleyIn(Trolley, DButer.RDPoint);
                DButer.trolley = null;
                Goal = "TakeFullTrolley"; //New goal
                Travelling = false;
                InTask = false;
            }
        }

        public void DistributionCompleted()
        {
            if (Goal == "DistributePlants")
            {
                plant p = DButer.trolley.GiveFirstPlant();
                Trolley.TakePlantIn(p);
                p = DButer.trolley.PeekFirstPlant();
                if (p == null) //Distributer trolley is empty. So move this trolley to the Empty trolley Hub.
                {
                    TargetHub = DButer.floor.BuffHub;
                    Trolley = DButer.trolley;
                    DButer.TravelToClosestTile(TargetHub.VOpenSpots(DButer));
                    Goal = "DeliveringEmptyTrolley";
                    InTask = true;
                    Travelling = true;
                }
                else if (!(p.DestinationHub == TargetHub)) // Next plant is not for this hub. So travel to new trolley
                {
                    TargetHub = p.DestinationHub;
                    Trolley = p.DestinationHub.PeekFirstTrolley();
                    DButer.TravelToTrolley(Trolley);
                    Travelling = true;
                }
            }
        }
    }
}
