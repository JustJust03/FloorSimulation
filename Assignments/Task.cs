using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace FloorSimulation
{
    internal class Task
    {
        public DanishTrolley Trolley;
        private DanishTrolley OldTrolley; //Is only used to save which trolley you were working on when delivering a full trolley.
        public Hub TargetHub;
        private Hub OldTargetHub; //Is only used to save which Shop you were working on when delivering a full trolley.
        private WalkTile OldWalkTile; //Is only used to save on which spot you picked up a finished trolley.
        public bool WasOnTopLeft;
        public Hub StartHub;
        private Distributer DButer;

        public bool InTask = false;
        private bool Travelling = false;
        private bool Waiting = false; // true when an agent is waiting for a possible route to it's destination.
        public string Goal; // "TakeFullTrolley", "DistributePlants", "DeliveringEmptyTrolley", "PushTrolleyAway", "TakeFinishedTrolley", "DeliverFullTrolley", "TakeEmptyTrolley", "DeliverEmptyTrolleyToShop", "TakeOldTrolley"
        public readonly List<string> TargetIsHubGoals = new List<string>
        {
            "DistributePlants",
        };
        public readonly List<string> TargetIsOldWalktile = new List<string>
        {
            "DeliverEmptyTrolleyToShop",
        };


        public Task(Hub TargetHub_,  Distributer DButer_, string Goal_, DanishTrolley trolley_ = default)
        {
            Trolley = trolley_;
            StartHub = TargetHub_;
            DButer = DButer_;
            Goal = Goal_;
        }

        public void PerformTask()
        {
            if (!InTask && Goal == "TakeFullTrolley")
            {
                TargetHub = StartHub;
                Trolley = TargetHub.PeekFirstTrolley();
                if (Trolley != null)
                {
                    DButer.TravelToTrolley(Trolley);
                    if (DButer.route == null) //Route was not possible at this point. Try again later.
                        return;
                    InTask = true;
                    Travelling = true;
                }
            }

            if (Waiting)
            {
                FailRoute();
                return;
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
                if(TargetHub.PeekFirstTrolley() != Trolley) // If the targeted trolley isn't in the hub anymore chose another trolley to target.
                {
                    InTask = false;
                    Travelling = false;
                    return;
                }
                DanishTrolley t = TargetHub.GiveTrolley();
                if (t == null) //If the start hub has no more trolleys stop.
                {
                    InTask = false;
                    Travelling = false;
                    return;
                }

                DButer.TakeTrolleyIn(t);
                TargetHub = DButer.trolley.PeekFirstPlant().DestinationHub;
                Trolley = TargetHub.PeekFirstTrolley();
                DButer.TravelToTrolley(Trolley);
                if(DButer.route == null) //Route was not possible at this point. Try again later.
                    return;

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
                DButer.WW.unoccupie_by_tiles(DButer.trolley.RPoint, DButer.trolley.GetRSize()); // drop the trolley of from the distributer
                DButer.GiveTrolley();

                Goal = "TakeFullTrolley"; //New goal
                InTask = false;
                Travelling = false;
            }

            else if (Goal == "PushTrolleyAway") //Old goal
            {
                DButer.floor.TrolleyList.Add(DButer.trolley);
                DButer.WW.unoccupie_by_tiles(DButer.trolley.RPoint, DButer.trolley.GetRSize()); // drop the trolley of from the distributer
                OldTrolley = DButer.GiveTrolley();
                OldTargetHub = TargetHub;
                DButer.TravelToTrolley(TargetHub.PeekFirstTrolley());
                if (DButer.route == null) //Route was not possible at this point. Try again later.
                    return;

                Goal = "TakeFinishedTrolley"; //New goal
                InTask = true;
                Travelling = true;
            }

            else if (Goal == "TakeFinishedTrolley") //Old goal
            {
                OldWalkTile = DButer.WW.GetTile(Trolley.RPoint);
                DButer.TakeTrolleyIn(TargetHub.GiveTrolley());
                WasOnTopLeft = DButer.TrolleyOnTopLeft;
                Trolley = DButer.trolley;
                TargetHub = DButer.floor.FTHub;
                DButer.TravelToClosestTile(TargetHub.OpenSpots(DButer));
                if(DButer.route == null) //Route was not possible at this point. Try again later. 
                    return;

                Goal = "DeliverFullTrolley"; //New goal
                InTask = true;
                Travelling = true;
            }

            //Uses the First trolley of target hub as target
            else if (Goal == "DeliverFullTrolley")
            {
                TargetHub.TakeHTrolleyIn(Trolley, DButer.RDPoint);
                DButer.WW.unoccupie_by_tiles(DButer.trolley.RPoint, DButer.trolley.GetRSize()); // drop the trolley of from the distributer
                DButer.GiveTrolley();

                TargetHub = DButer.floor.BuffHub;
                Trolley = TargetHub.PeekFirstTrolley();
                DButer.TravelToTrolley(Trolley);
                if (DButer.route == null) //Route was not possible at this point. Try again later.
                    return;

                Goal = "TakeEmptyTrolley"; //New goal
                InTask = true;
                Travelling = true;
            }

            //Uses OldTargetTile + the width of the distributer as target
            else if (Goal == "TakeEmptyTrolley")
            {
                DanishTrolley t = TargetHub.GiveTrolley(DButer.RDPoint);
                //The trolley in buffhub was already taken
                if (t == null)
                {
                    Trolley = TargetHub.PeekFirstTrolley();
                    DButer.TravelToTrolley(Trolley);
                    if (DButer.route == null) //Route was not possible at this point. Try again later.
                        return;
                    return;
                }
                Trolley = t;
                DButer.TakeTrolleyIn(t);
                DButer.RotateDistributerAndTrolley(); //Rotate the distributer and the trolley to fit into the shop.

                if(WasOnTopLeft)
                    OldWalkTile = DButer.WW.GetTile(new Point (OldWalkTile.Rpoint.X - DButer.RDistributerSize.Width + 10, OldWalkTile.Rpoint.Y)); //Because dbuter is on the left of the trolley.
                DButer.TravelToTile(OldWalkTile);
                TargetHub = OldTargetHub;

                Goal = "DeliverEmptyTrolleyToShop"; //New goal
                InTask = true;
                Travelling = true;
            }

            //Uses Trolley as target
            else if (Goal == "DeliverEmptyTrolleyToShop")
            {
                if(!WasOnTopLeft)
                    DButer.SwitchDistributerTrolley();
                else
                {
                    ;
                }
                TargetHub.TakeVTrolleyIn(Trolley);
                DButer.GiveTrolley();

                Trolley = OldTrolley;
                DButer.TravelToTrolley(Trolley);
                if (DButer.route == null) //Route was not possible at this point. Try again later.
                    return;

                Goal = "TakeOldTrolley"; //New goal
                InTask = true;
                Travelling = true;
            }

            else if (Goal == "TakeOldTrolley")
            {
                DButer.TakeTrolleyIn(Trolley);
                DButer.floor.TrolleyList.Remove(Trolley);
                if (DButer.trolley.PeekFirstPlant() == null) //This dropped off trolley was actually empty, so deliver it to the buffer hub
                {
                    TargetHub = DButer.floor.BuffHub;
                    Trolley = DButer.trolley;
                    DButer.TravelToClosestTile(TargetHub.OpenSpots(DButer));

                    Goal = "DeliveringEmptyTrolley"; //New goal
                    InTask = true;
                    Travelling = true;
                    return;
                }
                TargetHub = DButer.trolley.PeekFirstPlant().DestinationHub;
                Trolley = TargetHub.PeekFirstTrolley();
                DButer.TravelToTrolley(Trolley);
                if (DButer.route == null) //Route was not possible at this point. Try again later.
                    return;

                Goal = "DistributePlants"; //New goal
                InTask = true;
                Travelling = true;
            }
        }

        public void FailRoute()
        {
            if (TargetIsHubGoals.Contains(Goal))
            {
                DButer.TravelToClosestTile(TargetHub.OpenSpots(DButer));
            }
            else if (TargetIsOldWalktile.Contains(Goal))
            {
                DButer.TravelToTile(OldWalkTile);
            }
            else
            {
                if (Trolley.IsInTransport) //The trolley you want to reach was taken by someone else. Look for another trolley in the targethub.
                    Trolley = TargetHub.PeekFirstTrolley();
                DButer.TravelToTrolley(Trolley);
            }

            if (DButer.route == null || DButer.route.Count == 0)
                Waiting = true;
            else
                Waiting = false;
        }

        public void DistributionCompleted()
        {
            if (Goal == "DistributePlants")
            {
                plant p = DButer.trolley.GiveFirstPlant();
                if (Trolley.TakePlantIn(p)) //Transports plant from the distributer's trolley to the shop trolley. True when the shop trolley became full.
                {
                    DButer.TravelToTile(DButer.WW.GetTile(new Point(DButer.RDPoint.X, DButer.RDPoint.Y + 40)));

                    Goal = "PushTrolleyAway";
                    InTask = true;
                    Travelling = true;
                    return;
                };
                p = DButer.trolley.PeekFirstPlant();
                if (p == null) //Distributer trolley is empty. So move this trolley to the Empty trolley Hub.
                {
                    TargetHub = DButer.floor.BuffHub;
                    Trolley = DButer.trolley;
                    DButer.TravelToClosestTile(TargetHub.OpenSpots(DButer));

                    Goal = "DeliveringEmptyTrolley"; //New goal
                    InTask = true;
                    Travelling = true;
                }
                else if (!(p.DestinationHub == TargetHub)) // Next plant is not for this hub. So travel to new trolley
                {
                    TargetHub = p.DestinationHub;
                    Trolley = TargetHub.PeekFirstTrolley();
                    DButer.TravelToTrolley(Trolley);
                    if (DButer.route == null) //Route was not possible at this point. Try again later.
                        return;
                    Travelling = true;
                }
            }
        }
    }
}
