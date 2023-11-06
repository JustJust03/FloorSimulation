﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Data;

namespace FloorSimulation
{
    internal class Task
    {
        public DanishTrolley Trolley;
        public LangeHarry Harry;
        private DanishTrolley OldTrolley; //Is only used to save which trolley you were working on when delivering a full trolley.
        public Hub TargetHub;
        private Hub OldTargetHub; //Is only used to save which Shop you were working on when delivering a full trolley.
        private WalkTile OldWalkTile; //Is only used to save on which spot you picked up a finished trolley.
        private WalkTile TilesDownTile; //Is only used as a target by the MoveEmptyTrolley
        public bool WasOnTopLeft;
        private bool MovingToClose; //Is only used when distributing plants and the shop hub was occupied
        public Hub StartHub;
        private Distributer DButer;
        private FinishedDistribution FinishedD;

        public readonly List<string> VerspillingTasks = new List<string>
        {
            "DeliveringEmptyTrolley",
            "TakeEmptyTrolley",
            "MoveEmptyTrolleyDown",
            "DeliverEmptyTrolleyToShop"
        };

        public bool InTask = false;
        private bool Travelling = false;
        private bool Waiting = false; // true when an agent is waiting for a possible route to it's destination.
        public bool InSideActivity = false;
        public string Goal; // "TakeFullTrolley", "DistributePlants", "DeliveringEmptyTrolley", "PushTrolleyAway", "TakeFinishedTrolley", "DeliverFullTrolley", "TakeLangeHarry", "TakeEmptyTrolley", "DeliverEmptyTrolleyToShop", "TakeOldTrolley"

        public readonly List<string> TargetIsHubGoals = new List<string> //Only distributeplants should be in this.
        {
            "DistributePlants"
        };
        public readonly List<string> TargetIsOldWalktile = new List<string>
        {
            "DeliverEmptyTrolleyToShop",
            "PushTrolleyAway"
        };
        public readonly List<string> TargetIsOpenSpots = new List<string>
        {
            "DeliveringEmptyTrolley",
            "DeliverFullTrolley",
            "LHDeliverFinishedTrolleys"
        };
        public readonly List<string> TargetIsFilledSpots = new List<string>
        {
            "LHTakeFinishedTrolley",
            "TakeEmptyTrolley"
        };
        public readonly List<string> TargetIsHarry = new List<string>
        {
            "TakeLangeHarry"
        };
        public readonly List<string> TargetIsTilesDownTile = new List<string>
        {
            "MoveEmptyTrolleyDown"
        };


        public Task(Hub TargetHub_,  Distributer DButer_, string Goal_, FinishedDistribution FinishedD_, DanishTrolley trolley_ = default)
        {
            Trolley = trolley_;
            StartHub = TargetHub_;
            DButer = DButer_;
            Goal = Goal_;
            Harry = DButer.floor.FirstHarry;
            FinishedD = FinishedD_;

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
                DButer.WachtTijd = DButer.WachtTijd.Add(TimeSpan.FromMilliseconds(DButer.distributionms_per_tick * DButer.floor.SpeedMultiplier));
                FailRoute();
                return;
            }

            if (InTask && Travelling)
                DButer.TickWalk();
            else if (InTask)
                DButer.TickDistribute();
        }

        /// <summary>
        /// When the route is finished look at the task it was performing and run that code to determine the new goal.
        /// </summary>
        public void RouteCompleted()
        {
            //Trolley
            if (Goal == "TakeFullTrolley")
                TakeFullTrolley();

            //Trolley
            else if (Goal == "DistributePlants")
            {
                if (MovingToClose)
                {
                    MovingToClose = false;
                    FailRoute();
                }
                else
                    DistributePlants();
            }

            //TargetHub.OpenSpots(DButer)
            else if (Goal == "DeliveringEmptyTrolley")
                DeliveringEmptyTrolley();

            //OldWalkTile
            else if (Goal == "PushTrolleyAway")
                PushTrolleyAway();

            //Trolley
            else if (Goal == "TakeFinishedTrolley")
                TakeFinishedTrolley();

            //TargetHub.OpenSpots(DButer)
            else if (Goal == "DeliverFullTrolley")
                DeliverFullTrolley();

            //Harry
            else if (Goal == "TakeLangeHarry")
                TakeLangeHarry();

            //Targethub.FilledSpots(DButer)
            else if (Goal == "LHTakeFinishedTrolley")
                LHTakeFinishedTrolley();

            //Targethub.OpenSpots(DButer)
            else if (Goal == "LHDeliverFinishedTrolleys")
                LHDeliverFinishedTrolleys();

            //Trolley
            else if (Goal == "TakeEmptyTrolley")
                TakeEmptyTrolley();

            //TenTilesDownTile
            else if (Goal == "MoveEmptyTrolleyDown")
                MoveEmptyTrolleyDown();

            //OldWalkTile
            else if (Goal == "DeliverEmptyTrolleyToShop")
                DeliverEmptyTrolleyToShop();

            //Trolley
            else if (Goal == "TakeOldTrolley")
                TakeOldTrolley();

            else
                throw new Exception("Task was not recognised!");
        }

        public void FailRoute()
        {
            if (TargetIsHubGoals.Contains(Goal)) //If the shophub is block by something, try to walk 10 tiles to the right and down of this.
            {
                DButer.TravelToTrolley(TargetHub.PeekFirstTrolley());
                if (DButer.route == null)
                {
                    Point targetp = TargetHub.PeekFirstTrolley().RPoint;
                    int maxwidth = DButer.floor.FirstWW.RSizeWW.Width;

                    Point p;
                    p = new Point(Math.Min(targetp.X + 270, maxwidth), targetp.Y);
                    DButer.TravelToTile(DButer.WW.GetTile(p));
                    if (DButer.route == null)
                    {
                        p = new Point(Math.Max(targetp.X - 200, 0), targetp.Y);
                        DButer.TravelToTile(DButer.WW.GetTile(p));
                        if (DButer.route != null)
                            MovingToClose = true;
                    }
                    else
                        MovingToClose = true;
                }
                else
                    MovingToClose = false;
            }
            else if (TargetIsOldWalktile.Contains(Goal))
                DButer.TravelToTile(OldWalkTile);
            else if (TargetIsOpenSpots.Contains(Goal))
                DButer.TravelToClosestTile(TargetHub.OpenSpots(DButer));
            else if (TargetIsFilledSpots.Contains(Goal))
                DButer.TravelToClosestTile(TargetHub.FilledSpots(DButer));
            else if (TargetIsHarry.Contains(Goal))
                DButer.TravelToHarry(Harry);
            else if (TargetIsTilesDownTile.Contains(Goal))
                DButer.TravelToTile(TilesDownTile);
            else
            {
                if (Trolley == null || Trolley.IsInTransport || Trolley.IsFull()) //The trolley you want to reach was taken by someone else. Look for another trolley in the targethub.
                    Trolley = TargetHub.PeekFirstTrolley();
                DButer.TravelToTrolley(Trolley);
            }

            if (DButer.route == null || DButer.route.Count == 0)
                Waiting = true;
            else
            {
                DButer.TickWalk();
                Waiting = false;
            }
        }

        public void DistributionCompleted()
        {
            if (Goal == "DistributePlants")
            {
                plant p = DButer.trolley.GiveFirstPlant();
                if (Trolley.TakePlantIn(p)) //Transports plant from the distributer's trolley to the shop trolley. True when the shop trolley became full.
                {
                    ShopTrolleyBecameFull();
                    return;
                };
                p = DButer.trolley.PeekFirstPlant();
                if (p == null) //Distributer trolley is empty. So move this trolley to the Empty trolley Hub.
                    DistributerTrolleyBecameEmpty();
                else if (!(p.DestinationHub == TargetHub)) // Next plant is not for this hub. So travel to new trolley
                    ContinueDistributing(p);
            }
        }

        //From here to the bottom all the individual tasks are programmed.
        
        /// <summary>
        /// Uses PeekFirst of Target hub to determine Trolley.
        /// Uses Trolley as target
        /// </summary>
        private void TakeFullTrolley()
        {
            //TODO: Check to see to which trolley you should deliver to
            if(TargetHub.PeekFirstTrolley() != Trolley) // If the targeted trolley isn't in the hub anymore chose another trolley to target.
            {
                InTask = false;
                Travelling = false;
                return;
            }
            Goal = "DistributePlants"; //New goal

            DanishTrolley t = TargetHub.GiveTrolley();
            DButer.TakeTrolleyIn(t);
            if (DButer.RDPoint.Y > t.RPoint.Y)
                DButer.SwitchDistributerTrolley();
            TargetHub = DButer.trolley.PeekFirstPlant().DestinationHub;
            Trolley = TargetHub.PeekFirstTrolley();
            DButer.TravelToTrolley(Trolley);
            if(DButer.route == null)//Route was not possible at this point. Try again later.
            {
                Goal = "DistributePlants";
                FailRoute();
                return;
            } 

            InTask = true;
            Travelling = true;

        }

        /// <summary>
        /// Uses PeekFirstPlant to determine Targethub
        /// Uses PeekFirst to determine Trolley
        /// Uses Trolley as target
        /// </summary>
        private void DistributePlants()
        {
            Travelling = false;
            InTask = true; 
        }

        /// <summary>
        /// Finished distributing this trolley and deliverd it to the bufferhub.
        /// Targethub is buffhub
        /// Uses OpenSpots to determine TargetTiles
        /// uses target tiles as target
        /// </summary>
        private void DeliveringEmptyTrolley()
        {
            TargetHub.TakeVTrolleyIn(Trolley, DButer.RDPoint);
            DButer.WW.unoccupie_by_tiles(DButer.trolley.RPoint, DButer.trolley.GetRSize()); // drop the trolley of from the distributer
            DButer.GiveTrolley();

            TargetHub = StartHub;


            Goal = "TakeFullTrolley"; //New goal
            InTask = false;
            Travelling = false;
            FinishedD.CheckFinishedDistribution();
        }

        /// <summary>
        /// Pushes the trolley 40 cm down.
        /// Uses OldwalkTile as target
        /// </summary>
        private void PushTrolleyAway()
        { 
            DButer.floor.TrolleyList.Add(DButer.trolley);
            DButer.WW.unoccupie_by_tiles(DButer.trolley.RPoint, DButer.trolley.GetRSize()); // drop the trolley of from the distributer
            OldTrolley = DButer.GiveTrolley();
            OldTargetHub = TargetHub;
            DButer.TravelToTrolley(TargetHub.PeekFirstTrolley());
            if (DButer.route == null) //Route was not possible at this point. Try again later.
            {
                FailRoute();
                return;
            }
            
            Goal = "TakeFinishedTrolley"; //New goal
            InTask = true;
            Travelling = true;
        }

        /// <summary>
        /// Uses PeekFirstTrolley to determine trolley
        /// Uses Trolley as target
        /// </summary>
        private void TakeFinishedTrolley()
        {
            OldWalkTile = DButer.WW.GetTile(Trolley.RPoint);
            DButer.TakeTrolleyIn(TargetHub.GiveTrolley());
            WasOnTopLeft = DButer.TrolleyOnTopLeft;
            Trolley = DButer.trolley;
            TargetHub = DButer.floor.ClosestFTHub(DButer);
            DButer.TravelToClosestTile(TargetHub.OpenSpots(DButer));
            if (DButer.route == null) //Route was not possible at this point. Try again later. 
                return;

            Goal = "DeliverFullTrolley"; //New goal
            InTask = true;
            Travelling = true;
        }

        /// <summary>
        /// Targethub is FullTrolleyHub
        /// Uses OpenSpots to determine TargetTiles
        /// uses target tiles as target
        /// 
        /// Takes LangeHarry when there are 3 or more finished trolleys in FullTrolleyHub
        /// </summary>
        private void DeliverFullTrolley()
        {
            TargetHub.TakeHTrolleyIn(Trolley, DButer.RDPoint);
            DButer.WW.unoccupie_by_tiles(DButer.trolley.RPoint, DButer.trolley.GetRSize()); // drop the trolley of from the distributer
            DButer.GiveTrolley();
            if (TargetHub.AmountOfTrolleys() >= 3 && !Harry.IsTargeted)
            {
                Harry.IsTargeted = true;
                DButer.TravelToHarry(Harry);
                if (DButer.route == null)
                {
                    FailRoute();
                    return;
                }

                Goal = "TakeLangeHarry";
                InTask = true;
                Travelling = true;
                return;
            }

            TargetHub = DButer.floor.BuffHub;
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

        /// <summary>
        /// Uses Harry as target
        /// </summary>
        private void TakeLangeHarry()
        {
            if (Harry.IsInUse)
            {
                TargetHub = DButer.floor.BuffHub;
                DButer.TravelToClosestTile(TargetHub.FilledSpots(DButer));
                if (DButer.route == null) //Route was not possible at this point. Try again later.
                    FailRoute();

                Goal = "TakeEmptyTrolley"; //New goal


                InTask = true;
                Travelling = true;

                return;
            }

            DButer.MountHarry(Harry);

            //Target hub stays the same as it was
            if(Harry.IsVertical)
            {
                DButer.TravelToClosestTile(TargetHub.FilledSpots(DButer));
                if (DButer.route == null) //Route was not possible at this point
                    FailRoute();
            }
            else
            {
                DButer.RotateDistributerAndHarry();
                DButer.TravelToClosestTile(TargetHub.FilledSpots(DButer));
                if (DButer.route == null)
                    FailRoute();
            }

            Goal = "LHTakeFinishedTrolley";
            InTask = true;
            Travelling = true;
        }

        /// <summary>
        /// Uses Target hub (FullTrolleyHub) and FilledSpots(DButer)
        /// </summary>
        private void LHTakeFinishedTrolley()
        {
            Harry.TakeTrolleyIn(TargetHub.GiveTrolley(DButer.RDPoint));

            if (TargetHub.AmountOfTrolleys() > 0 && Harry.TrolleyList.Count < 3)
            {
                DButer.TravelToClosestTile(TargetHub.FilledSpots(DButer));
                if (DButer.route == null) //Route was not possible at this point
                    FailRoute();
            }
            else
            {
                TargetHub = DButer.floor.TrHub;
                DButer.RotateDistributerAndHarry();
                DButer.TravelToClosestTile(TargetHub.OpenSpots(DButer));
                Goal = "LHDeliverFinishedTrolleys";
                if (DButer.route == null) //Route was not possible
                    FailRoute();
            }

            InTask = true;
            Travelling = true;
        }

        /// <summary>
        /// Uses target hub and OpenSpots as travel tile
        /// </summary>
        private void LHDeliverFinishedTrolleys()
        {
            DanishTrolley t = Harry.DropTrolley();
            TargetHub.TakeVTrolleyIn(t, DButer.RDPoint);

            if(Harry.TrolleyList.Count < 1)
            {
                DButer.DisMountHarry();
                TargetHub = DButer.floor.BuffHub;
                DButer.TravelToClosestTile(TargetHub.FilledSpots(DButer));
                if (DButer.route == null) //Route was not possible at this point. Try again later.
                    FailRoute();

                Goal = "TakeEmptyTrolley"; //New goal
            }
            else
            {
                DButer.TravelToClosestTile(TargetHub.OpenSpots(DButer));
                if (DButer.route == null) //Route was not possible
                    FailRoute();
            }

            InTask = true;
            Travelling = true;
        }

        /// <summary>
        /// Targethub is bufferhub
        /// Uses PeekFirstTrolley to determine trolley
        /// Uses trolley as target
        /// </summary>
        private void TakeEmptyTrolley()
        {
            DanishTrolley t = TargetHub.GiveTrolley(DButer.RDPoint);
            //The trolley in buffhub was already taken
            if (t == null)
            {
                DButer.TravelToClosestTile(TargetHub.FilledSpots(DButer));
                if (DButer.route == null) //Route was not possible at this point. Try again later.
                    FailRoute();
                return;
            }
            Trolley = t;
            DButer.TakeTrolleyIn(t);

            TilesDownTile = DButer.WW.GetTile(new Point(DButer.RDPoint.X, DButer.RDPoint.Y + 180));
            DButer.TravelToTile(TilesDownTile);

            Goal = "MoveEmptyTrolleyDown"; //New goal
            InTask = true;
            Travelling = true;
        }

        private void MoveEmptyTrolleyDown()
        {
            DButer.RotateDistributerAndTrolley(); //Rotate the distributer and the trolley to fit into the shop.
            if (WasOnTopLeft)
                OldWalkTile = DButer.WW.GetTile(new Point(OldWalkTile.Rpoint.X - DButer.RDistributerSize.Width + 10, OldWalkTile.Rpoint.Y)); //Because dbuter is on the left of the trolley.
            DButer.TravelToTile(OldWalkTile);
            TargetHub = OldTargetHub;

            Goal = "DeliverEmptyTrolleyToShop"; //New goal
            InTask = true;
            Travelling = true;
        }

        /// <summary>
        /// Uses OldWalktile as target
        /// Delivered a new empty trolley to the shop
        /// </summary>
        private void DeliverEmptyTrolleyToShop()
        { 
            if (!WasOnTopLeft)
                DButer.SwitchDistributerTrolley();
            TargetHub.TakeVTrolleyIn(Trolley);
            DButer.GiveTrolley();
            
            Trolley = OldTrolley;
            DButer.TravelToTrolley(Trolley);
            if (DButer.route == null) //Route was not possible at this point. Try again later.
                FailRoute();
            
            Goal = "TakeOldTrolley"; //New goal
            InTask = true;
            Travelling = true;
        }

        /// <summary>
        /// Uses Trolley as target
        /// </summary>
        private void TakeOldTrolley()
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
                FailRoute();

            Goal = "DistributePlants"; //New goal
            InTask = true;
            Travelling = true;
        }

        /// <summary>
        /// While distributing the trolley became full.
        /// </summary>
        private void ShopTrolleyBecameFull()
        {
            OldWalkTile = DButer.WW.GetTile(new Point(DButer.RDPoint.X, DButer.RDPoint.Y + 40));
            DButer.TravelToTile(OldWalkTile);

            Goal = "PushTrolleyAway";
            InTask = true;
            Travelling = true;
        }

        /// <summary>
        /// After distributing, the trolley became empty. 
        /// </summary>
        private void DistributerTrolleyBecameEmpty()
        {
            TargetHub = DButer.floor.BuffHub;
            Trolley = DButer.trolley;
            DButer.TravelToClosestTile(TargetHub.OpenSpots(DButer));

            Goal = "DeliveringEmptyTrolley"; //New goal
            InTask = true;
            Travelling = true;
        }

        /// <summary>
        /// Uses Trolley as target
        /// </summary>
        private void ContinueDistributing(plant p)
        {
            Travelling = true;
            TargetHub = p.DestinationHub;
            Trolley = TargetHub.PeekFirstTrolley();
            DButer.TravelToTrolley(Trolley);
            if (DButer.route == null) //Route was not possible at this point. Try again later.
            {
                FailRoute();
                return;
            }
        }
    }
}
