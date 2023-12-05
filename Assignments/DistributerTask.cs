using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloorSimulation
{
    internal class DistributerTask : Task
    {
        public LangeHarry Harry;
        public bool WasOnTopLeft;

        private DanishTrolley OldTrolley; //Is only used to save which trolley you were working on when delivering a full trolley.
        private Hub OldTargetHub; //Is only used to save which Shop you were working on when delivering a full trolley.
        private WalkTile OldWalkTile; //Is only used to save on which spot you picked up a finished trolley.
        private WalkTile TilesDownTile; //Is only used as a target by the MoveEmptyTrolley
        private bool MovingToClose; //Is only used when distributing plants and the shop hub was occupied
        private Distributer DButer;
        private FinishedDistribution FinishedD;
        int WaitedTicks = 0;



        public DistributerTask(Distributer DButer_, string Goal_, FinishedDistribution FinishedD_, DanishTrolley trolley_ = default):
            base(Goal_, trolley_)
        {
            DButer = DButer_;
            Harry = DButer.floor.FirstHarry;
            FinishedD = FinishedD_;

            AInfo = new AnalyzeInfo(DButer, this, DButer.distributionms_per_tick);
        }

        public override void PerformTask()
        {
            if (Goal == "PushTrolleyAway" && DButer.trolley == null)
            {
                DButer.trolley = OldTrolley;
                DButer.TravelTrolley();
            }
            AInfo.TickAnalyzeInfo(DButer.floor.SpeedMultiplier);
            if (!InTask && Goal == "TakeFullTrolley")
            {
                TargetHub = DButer.floor.GetStartHub(DButer);
                Trolley = TargetHub.PeekFirstTrolley();
                if (Trolley != null)
                {
                    DButer.TravelToTrolley(Trolley);
                    if (DButer.route == null) //Route was not possible at this point. Try again later.
                        return;
                    InTask = true;
                    Travelling = true;
                }
                else //When the distribution is finished, travel to your savepoint.
                {
                    DButer.TravelToTile(DButer.WW.GetTile(DButer.SavePoint));
                    InTask = true;
                    Travelling = true;
                    TargetWasSaveTile = true;
                }
            }

            if (Waiting)
            {
                FailRoute();
                if (DButer.route != null)
                    WaitedTicks = 0;

                WaitedTicks++;

                if (WaitedTicks > 100)
                {
                    WaitedTicks = 0;
                    DButer.TravelToTile(DButer.WW.GetTile(DButer.SavePoint));
                    Waiting = false;
                    TargetWasSaveTile = true;
                }
                return;
            }

            if (TargetWasSaveTile && Goal == "DistributePlants")
            {
                FailRoute();
                DButer.TickWalk();
            }

            if (InTask && Travelling)
                DButer.TickWalk();
            else if (InTask)
                DButer.TickDistribute();
        }

        /// <summary>
        /// When the route is finished look at the task it was performing and run that code to determine the new goal.
        /// </summary>
        public override void RouteCompleted()
        {
            if (TargetWasSaveTile)
            {
                TargetWasSaveTile = false;
                FailRoute();
                return;
            }

            AInfo.UpdateFreq(Goal);

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
            else if (Goal == "LHTakeEmptyTrolley")
                LHTakeEmptyTrolley();

            //Targethub.FilledSpots(DButer)
            else if (Goal == "LHTakeFinishedTrolley")
                LHTakeFinishedTrolley();

            //Targethub.OpenSpots(DButer)
            else if (Goal == "LHDeliverFinishedTrolleys")
                LHDeliverFinishedTrolleys();

            //Targethub.OpenSpots(DButer)
            else if (Goal == "LHDeliverEmptyTrolleys")
                LHDeliverEmptyTrolleys();

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

        public override void FailRoute()
        {
            TargetWasSaveTile = false;
            if (TargetIsHubGoals.Contains(Goal)) //If the shophub is blocked by something, try to walk 10 tiles to the right and down of this.
            {
                DButer.TravelToClosestTile(TargetHub.OpenSpots(DButer));
                if (DButer.route == null)
                {
                    Point targetp = TargetHub.OpenSpots(DButer)[0].Rpoint;
                    Point p;
                    if (TargetHub.HasLeftAccess)
                    {
                        p = new Point(Math.Max(targetp.X - 180, 0), targetp.Y);
                        DButer.TravelToTile(DButer.WW.GetTile(p));
                    }
                    else
                    {
                        int maxwidth = DButer.floor.FirstWW.RSizeWW.Width;
                        p = new Point(Math.Min(targetp.X + 180, maxwidth), targetp.Y);
                        DButer.TravelToTile(DButer.WW.GetTile(p));
                    }
                    if (DButer.route != null)
                        MovingToClose = true;
                    else if (Floor.NDistributers > 30) //Let distributers walk to save tile when the target hub is not reachable in busy street
                    {
                        if (DButer.WW.GetTile(p) != DButer.WW.GetTile(DButer.RPoint))
                        {
                            DButer.TravelToTile(DButer.WW.GetTile(DButer.SavePoint));
                            TargetWasSaveTile = true;
                        }
                    }
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
            else if (TargetIsStartHubs.Contains(Goal))
            {
                TargetHub = DButer.floor.GetStartHub(DButer);
                Trolley = TargetHub.PeekFirstTrolley();

                if (Goal == "TakeFullTrolley" && Trolley == null)
                {
                    InTask = false;
                    Travelling = false;
                    return;
                }

                DButer.TravelToTrolley(Trolley);
            }
            else if (TargetIsFullBuffHub.Contains(Goal))
            {
                TargetHub = DButer.floor.GetBuffHubFull(DButer);
                DButer.TravelToClosestTile(TargetHub.FilledSpots(DButer));
            }
            else if (TargetIsOpenBuffHub.Contains(Goal))
            {
                TargetHub = DButer.floor.GetBuffHubOpen(DButer);

                Trolley = DButer.trolley;
                if (TargetHub.VerticalTrolleys != Trolley.IsVertical)
                    DButer.RotateDistributerAndTrolley();

                DButer.TravelToClosestTile(TargetHub.OpenSpots(DButer));
            }
            else
            {
                if (Trolley == null || Trolley.IsInTransport || Trolley.IsFull()) //The trolley you want to reach was taken by someone else. Look for another trolley in the targethub.
                    Trolley = TargetHub.PeekFirstTrolley();
                DButer.TravelToTrolley(Trolley);
            }

            //Check if there is a new route available now
            if (DButer.route == null)
            {
                Waiting = true;
            }
            else if (DButer.route.Count == 0)
            {
                RouteCompleted();
            }
            else
            {
                if (Waiting)
                    AInfo.UpdateWachtFreq();
                DButer.TickWalk();
                Waiting = false;
            }
        }

        public override void DistributionCompleted()
        {
            AInfo.UpdateFreq(Goal, true);
            if (Goal == "DistributePlants")
            {
                Trolley = TargetHub.GetRandomTrolley();
                if (Trolley == null) //If someone took the trolley, wait for a new one to return.
                    return;
                if (DButer.trolley.PeekFirstPlant() == null) //Distributer trolley is empty. So move this trolley to the Empty trolley Hub.
                {
                    DistributerTrolleyBecameEmpty();
                    return;
                }

                plant p = DButer.trolley.GiveFirstPlant();
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

            if (TargetHub.PeekFirstTrolley() != Trolley) // If the targeted trolley isn't in the hub anymore chose another trolley to target.
            {
                InTask = false;
                Travelling = false;
                return;
            }
            Goal = "DistributePlants"; //New goal

            DanishTrolley t = TargetHub.GiveTrolley();
            DButer.TakeTrolleyIn(t);
            if (DButer.RPoint.Y > t.RPoint.Y)
                DButer.SwitchDistributerTrolley();
            TargetHub = DButer.trolley.PeekFirstPlant().DestinationHub;
            Trolley = TargetHub.PeekFirstTrolley();
            DButer.TravelToClosestTile(TargetHub.OpenSpots(DButer));
            if (DButer.route == null)//Route was not possible at this point. Try again later.
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
            if (TargetHub.PeekFirstTrolley() == null)
            {
                return;
            }
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
            DButer.WW.unoccupie_by_tiles(DButer.trolley.RPoint, DButer.trolley.GetRSize()); // drop the trolley of from the distributer
            DButer.GiveTrolley();
            if (Trolley.IsVertical)
                TargetHub.TakeVTrolleyIn(Trolley, DButer.RPoint);
            else
                TargetHub.TakeHTrolleyIn(Trolley, DButer.RPoint);

            //Check if you need to empty a full trolley hub.
            FullTrolleyHub fh = DButer.floor.HasFullTrolleyHubFull(8);
            if (fh != null && !Harry.IsTargeted)
            {
                TargetHub = fh;
                DButer.TravelToHarry(Harry);
                Harry.IsTargeted = true;
                Goal = "TakeLangeHarry";
                return;
            }

            //Check if you need to transport some empty trolleys to the big buffer hub.
            BufferHub bh = DButer.floor.HasFullSmallBufferHub(7);
            if (bh != null && !Harry.IsTargeted)
            {
                TargetHub = bh;
                DButer.TravelToHarry(Harry);
                Harry.IsTargeted = true;
                Goal = "TakeLangeHarry";
                return;
            }

            TargetHub = DButer.floor.GetStartHub(DButer);
            Trolley = TargetHub.PeekFirstTrolley();

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
            TargetHub.TakeHTrolleyIn(Trolley, DButer.RPoint);
            DButer.WW.unoccupie_by_tiles(DButer.trolley.RPoint, DButer.trolley.GetRSize()); // drop the trolley of from the distributer
            DButer.GiveTrolley();

            TargetHub = DButer.floor.GetBuffHubFull(DButer);
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
                TargetHub = DButer.floor.GetStartHub(DButer);
                Trolley = TargetHub.PeekFirstTrolley();

                Goal = "TakeFullTrolley"; //New goal
                InTask = false;
                Travelling = false;
                return;
            }

            DButer.MountHarry(Harry);

            if (TargetHub is FullTrolleyHub)
                Goal = "LHTakeFinishedTrolley";
            else if (TargetHub is BufferHub)
                Goal = "LHTakeEmptyTrolley";
            else
                throw new NotImplementedException("Lange Harry can't target this hub.");

            //Target hub stays the same as it was
            if (Harry.IsVertical)
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

            InTask = true;
            Travelling = true;
        }

        private void LHTakeEmptyTrolley()
        {
            Trolley = TargetHub.GiveTrolleyToHarry(DButer.RPoint);
            if (Trolley == null)
            {
                FailRoute();
                return;
            }
            Harry.TakeTrolleyIn(Trolley);

            if (TargetHub.AmountOfTrolleys() > 0 && Harry.TrolleyList.Count < LangeHarry.MaxTrolleysPerHarry)
            {
                DButer.TravelToClosestTile(TargetHub.FilledSpots(DButer));
                if (DButer.route == null) //Route was not possible at this point
                    FailRoute();
            }
            else
            {
                TargetHub = DButer.floor.BuffHubs[DButer.floor.BuffHubs.Count - 1];
                DButer.TravelToClosestTile(TargetHub.OpenSpots(DButer)); //Automaticaly checks if dbuter is on harry, which it is.
                Goal = "LHDeliverEmptyTrolleys";
                if (DButer.route == null) //Route was not possible
                    FailRoute();
            }

            InTask = true;
            Travelling = true;
        }

        /// <summary>
        /// Uses Target hub (FullTrolleyHub) and FilledSpots(DButer)
        /// </summary>
        private void LHTakeFinishedTrolley()
        {
            Harry.TakeTrolleyIn(TargetHub.GiveTrolley(DButer.RPoint));

            if (TargetHub.AmountOfTrolleys() > 0 && Harry.TrolleyList.Count < LangeHarry.MaxTrolleysPerHarry)
            {
                DButer.TravelToClosestTile(TargetHub.FilledSpots(DButer));
                if (DButer.route == null) //Route was not possible at this point
                    FailRoute();
            }
            else
            {
                TargetHub = DButer.floor.TrHub;
                DButer.TravelToClosestTile(TargetHub.OpenSpots(DButer));
                Goal = "LHDeliverFinishedTrolleys";
                if (DButer.route == null) //Route was not possible
                    FailRoute();
            }

            InTask = true;
            Travelling = true;
        }

        private void LHDeliverEmptyTrolleys()
        {
            if (Harry.IsVertical == TargetHub.VerticalTrolleys)
                DButer.RotateDistributerAndHarry();
            DanishTrolley t = Harry.DropTrolley();
            TargetHub.LHTakeVTrolleyIn(t, DButer.RPoint);

            if (Harry.TrolleyList.Count < 1)
            {
                BufferHub bh = DButer.floor.HasFullSmallBufferHub(6);
                if (bh != null)
                {
                    TargetHub = bh;
                    Goal = "LHTakeEmptyTrolley";
                    DButer.RotateDistributerAndHarry();
                    DButer.TravelToClosestTile(TargetHub.FilledSpots(DButer));
                    return;
                }

                FullTrolleyHub fh = DButer.floor.HasFullTrolleyHubFull(4);
                if (fh != null)
                {
                    TargetHub = fh;
                    Goal = "LHTakeFinishedTrolley";
                    DButer.RotateDistributerAndHarry();
                    DButer.TravelToClosestTile(TargetHub.FilledSpots(DButer));
                    return;
                }

                DButer.DisMountHarry();
                TargetHub = DButer.floor.GetStartHub(DButer);
                Trolley = TargetHub.PeekFirstTrolley();

                Goal = "TakeFullTrolley"; //New goal
                InTask = false;
                Travelling = false;
                FinishedD.CheckFinishedDistribution();
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
        /// Uses target hub and OpenSpots as travel tile
        /// </summary>
        private void LHDeliverFinishedTrolleys()
        {
            if (Harry.IsVertical == TargetHub.VerticalTrolleys)
                DButer.RotateDistributerAndHarry();
            DanishTrolley t = Harry.DropTrolley();
            TargetHub.TakeVTrolleyIn(t, DButer.RPoint);

            if (Harry.TrolleyList.Count < 1)
            {
                FullTrolleyHub fh = DButer.floor.HasFullTrolleyHubFull(4);
                if (fh != null)
                {
                    TargetHub = fh;
                    Goal = "LHTakeFinishedTrolley";
                    DButer.RotateDistributerAndHarry();
                    DButer.TravelToClosestTile(TargetHub.FilledSpots(DButer));
                    return;
                }

                BufferHub bh = DButer.floor.HasFullSmallBufferHub(6);
                if (bh != null)
                {
                    TargetHub = bh;
                    Goal = "LHTakeEmptyTrolley";
                    DButer.RotateDistributerAndHarry();
                    DButer.TravelToClosestTile(TargetHub.FilledSpots(DButer));
                    return;
                }

                DButer.DisMountHarry();
                TargetHub = DButer.floor.GetStartHub(DButer);
                Trolley = TargetHub.PeekFirstTrolley();

                Goal = "TakeFullTrolley"; //New goal
                InTask = false;
                Travelling = false;
                FinishedD.CheckFinishedDistribution();
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


            if (Trolley.IsVertical)
            {
                TilesDownTile = DButer.WW.GetTile(new Point(DButer.RPoint.X, TargetHub.RFloorPoint.Y + TargetHub.RHubSize.Height));
                DButer.TravelToTile(TilesDownTile);
                Goal = "MoveEmptyTrolleyDown"; //New goal
            }
            else
            {
                if (TargetHub.VerticalTrolleys != Trolley.IsVertical)
                    DButer.RotateDistributerAndTrolley(); //Rotate the distributer and the trolley to fit into the shop.
                if (WasOnTopLeft)
                    OldWalkTile = DButer.WW.GetTile(new Point(OldWalkTile.Rpoint.X - DButer.GetRSize().Width + 10, OldWalkTile.Rpoint.Y)); //Because dbuter is on the left of the trolley.
                DButer.TravelToTile(OldWalkTile);
                TargetHub = OldTargetHub;

                Goal = "DeliverEmptyTrolleyToShop"; //New goal
            }
            InTask = true;
            Travelling = true;
        }

        private void MoveEmptyTrolleyDown()
        {
            DButer.RotateDistributerAndTrolley(); //Rotate the distributer and the trolley to fit into the shop.
            if (WasOnTopLeft)
                OldWalkTile = DButer.WW.GetTile(new Point(OldWalkTile.Rpoint.X - DButer.GetRSize().Width + 10, OldWalkTile.Rpoint.Y)); //Because dbuter is on the left of the trolley.
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
            Goal = "TakeOldTrolley"; //New goal
            if (DButer.route == null) //Route was not possible at this point. Try again later.
                FailRoute();

            InTask = true;
            Travelling = true;
        }

        /// <summary>
        /// Uses Trolley as target
        /// </summary>
        private void TakeOldTrolley()
        {
            if (Trolley != OldTrolley)
            {
                Trolley = OldTrolley;
                Console.WriteLine("This took the wrong trolley");
            }

            DButer.TakeTrolleyIn(Trolley);
            DButer.floor.TrolleyList.Remove(Trolley);
            if (DButer.trolley.PeekFirstPlant() == null) //This dropped off trolley was actually empty, so deliver it to the buffer hub
            {
                TargetHub = DButer.floor.GetBuffHubOpen(DButer);

                Trolley = DButer.trolley;
                if (TargetHub.VerticalTrolleys != Trolley.IsVertical)
                    DButer.RotateDistributerAndTrolley();

                DButer.TravelToClosestTile(TargetHub.OpenSpots(DButer));

                Goal = "DeliveringEmptyTrolley"; //New goal
                InTask = true;
                Travelling = true;
                return;
            }
            TargetHub = DButer.trolley.PeekFirstPlant().DestinationHub;
            Trolley = TargetHub.PeekFirstTrolley();
            DButer.TravelToClosestTile(TargetHub.OpenSpots(DButer));

            Goal = "DistributePlants"; //New goal
            InTask = true;
            Travelling = true;
        }

        /// <summary>
        /// While distributing the trolley became full.
        /// </summary>
        private void ShopTrolleyBecameFull()
        {
            TargetHub.SwapIfOtherTrolley(Trolley);

            OldWalkTile = DButer.WW.GetTile(new Point(DButer.RPoint.X, DButer.RPoint.Y + 40));
            DButer.TravelToTile(OldWalkTile);

            Goal = "PushTrolleyAway";
            InTask = true;
            Travelling = true;
        }

        /// <summary>
        /// New Bord:
        /// New side activity
        /// </summary>
        private void StickerBordBecameFull()
        {
            DButer.SideActivityMsLeft += Distributer.BordTime;
            DButer.SideActivity = "Bord";
            AInfo.UpdateFreq(Goal, true);
            Trolley.NStickers = 0;
            ;
        }

        /// <summary>
        /// After distributing, the trolley became empty. 
        /// </summary>
        private void DistributerTrolleyBecameEmpty()
        {
            TargetHub = DButer.floor.GetBuffHubOpen(DButer);
            Trolley = DButer.trolley;
            if (TargetHub.VerticalTrolleys != Trolley.IsVertical)
                DButer.RotateDistributerAndTrolley();

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
            DButer.TravelToClosestTile(TargetHub.OpenSpots(DButer));
            if (DButer.route == null) //Route was not possible at this point. Try again later.
            {
                FailRoute();
                return;
            }
        }
    }
}
