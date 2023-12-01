using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Collections.Generic;
using System.Net.Mail;
using FloorSimulation.PathFinding;
using FloorSimulation;

namespace FloorSimulation
{
    /// <summary>
    /// Main distibuter class.
    /// The agent which walks trough the floor and distributes the plants for the trolley.
    /// </summary>
    internal class Distributer
    {
        private Image HDistributerIMG;
        private Image VDistributerIMG;
        public Point RDPoint; // Real distributer point
        public Point DPoint; // Sim distributer point
        public Point SavePoint; //A Point where the distributer can always return to.
        public Size RDistributerSize; //Is the real size in cm.
        private Size DistributerSize;
        public Size HRDistributerSize;
        public Size VRDistributerSize;
        public int id;
        public Floor floor;

        public const float OddsOfBord = 0.065f;
        public const float OddsOfLaag = 0.15f;
        public const float OddsOfHer = 0.28f;
        public const int BordTime = 34000; //ms
        public const int LaagTime = 30000; //ms
        public const int HerTime = 20000; //ms
        public int MaxWaitedTicks;

        public List<WalkTile> route;
        public const float WALKSPEED = 85f; // cm/s
        private float TravelSpeed = WALKSPEED;
        private float travel_dist_per_tick;
        public int distributionms_per_tick; // plant distribution per tick in ms
        public int SideActivityMsLeft; // Amount of ticks left of performing the side activity
        private float ticktravel = 0f; //The distance that has been traveled, but not registered to walkway yet
        private int distributionms = 0; // How many ms have you been distributing
        public Task MainTask;
        public DanishTrolley trolley;
        public LangeHarry Harry;
        private bool IsVertical;
        public bool TrolleyOnTopLeft; //True when the trolley is to the left or on top of the distributer
        public bool IsOnHarry;
        public string SideActivity;

        private AstarWalkWays AWW;
        public WalkWay WW;

        
        public Distributer(int id_, Floor floor_, WalkWay WW_, Point Rpoint_ = default, bool IsVertical_ = true, int MaxWaitedTicks_ = 100)
        {
            id = id_;
            floor = floor_;
            RDPoint = Rpoint_;
            if (Rpoint_ != null)
                SavePoint = Rpoint_;
            WW = WW_;
            IsVertical = IsVertical_;
            MaxWaitedTicks = MaxWaitedTicks_;
            IsOnHarry = false;

            VDistributerIMG = Image.FromFile(Program.rootfolder + @"\SimImages\Distributer_vertical.png");
            VRDistributerSize = new Size(VDistributerIMG.Width, VDistributerIMG.Height);
            HDistributerIMG = Image.FromFile(Program.rootfolder + @"\SimImages\Distributer_horizontal.png");
            HRDistributerSize = new Size(HDistributerIMG.Width, HDistributerIMG.Height);
            if (IsVertical)
                RDistributerSize = VRDistributerSize;
            else
                RDistributerSize = HRDistributerSize;

            DistributerSize = floor.ConvertToSimSize(RDistributerSize);
            DPoint = floor.ConvertToSimPoint(RDPoint);
            if(id == -1)
                return;

            travel_dist_per_tick = TravelSpeed / Program.TICKS_PER_SECOND;
            distributionms_per_tick = (int)(1000f / Program.TICKS_PER_SECOND);
            if (floor.layout.NLowpads == 0)
                MainTask = new DistributerTask(this, "TakeFullTrolley", floor.FinishedD);
            else
                MainTask = new RegionDistributerTask(this);
            trolley = null;

            AWW = new AstarWalkWays(WW, this);
            WW.fill_tiles(RDPoint, RDistributerSize, this);
        }

        public void Tick()
        {
            MainTask.PerformTask();
        }

        public void DrawObject(Graphics g,  Point p = default)
        {
            Point DrawPoint;
            if (p != default)
                DrawPoint = p;
            else
                DrawPoint = DPoint;

            if (trolley != null)
                trolley.DrawObject(g);
            if(IsVertical)
                g.DrawImage(VDistributerIMG, new Rectangle(DrawPoint, DistributerSize));
            else
                g.DrawImage(HDistributerIMG, new Rectangle(DrawPoint, DistributerSize));
        }

        /// <summary>
        /// Makes the distributer walk towards the target tile using a shortest path algorithm.
        /// </summary>
        /// <param name="target_tile"></param>
        public void TravelToTile(WalkTile target_tile)
        {
            if (RDPoint == target_tile.Rpoint)
                return;
            route = AWW.RunAlgoTile(WW.GetTile(RDPoint), target_tile);
        }

        /// <summary>
        /// Makes a distributer walk towards the closest available target tile using a shortest path algorithm.
        /// </summary>
        /// <param name="target_tile"></param>
        public void TravelToClosestTile(List<WalkTile> target_tiles)
        {
            route = AWW.RunAlgoTiles(WW.GetTile(RDPoint), target_tiles);
        }

        /// <summary>
        /// Makes the distributer walk towards the target tile using a shortest path algorithm.
        /// </summary>
        public void TravelToTrolley(DanishTrolley target_trolley)
        {
            route = AWW.RunAlgoDistrToTrolley(target_trolley);
        }

        /// <summary>
        /// Makes the distributer walk towards LangeHarry using it's accesspoints.
        /// </summary>
        public void TravelToHarry(LangeHarry Harry)
        {
            route = AWW.RunAlgoDistrToHarry(Harry);
        }
        
        /// <summary>
        /// Ticks the walking distance. 
        /// If the walking distance is bigger than the width of a tile, move the distributer.
        /// </summary>
        /// <returns>true if there de distributer is walking, false if route has been completed</returns>
        public void TickWalk()
        {
            if (route == null)
            {
                MainTask.FailRoute();
                return;
            }

            // TODO: develop non square tile tickwalks.
            if (WalkWay.WALK_TILE_WIDTH != WalkWay.WALK_TILE_HEIGHT)
                throw new ArgumentException("Distributer walk has not yet been developed for non square tiles.");

            if (route.Count > 0)
            {
                ticktravel += travel_dist_per_tick * floor.SpeedMultiplier;
                while (ticktravel > WalkWay.WALK_TILE_WIDTH)
                {
                    WalkTile destination = route[0];

                    WW.WWC.UpdateLocalClearances(this, GetDButerTileSize(), destination);

                    if (!AWW.IsTileAccessible(destination)) //Route failed, there was something occupying the calculated route
                    {
                        ticktravel = 0;
                        MainTask.FailRoute();
                        return;
                    }

                    WW.unfill_tiles(RDPoint, RDistributerSize);
                    RDPoint = route[0].Rpoint;
                    DPoint = floor.ConvertToSimPoint(RDPoint);
                    WW.fill_tiles(RDPoint, RDistributerSize, this);

                    if (IsOnHarry) //If you are on LangeHarry travel Harry too.
                        Harry.TravelHarry();
                    else if (trolley != null) //If you have a trolley, drag it with you.
                        TravelTrolley();

                    ticktravel -= WalkWay.WALK_TILE_WIDTH;
                    route.RemoveAt(0);

                    if (route.Count == 0)
                    {
                        ticktravel = 0;
                        MainTask.RouteCompleted();
                        break;
                    }
                }
            }
            else
                MainTask.FailRoute();
        }

        public void TravelTrolley()
        {
            WW.unfill_tiles(trolley.RPoint, trolley.GetRSize());

            if (IsVertical)
                trolley.RPoint = new Point(RDPoint.X, RDPoint.Y + RDistributerSize.Height);

            else
                trolley.RPoint = new Point(RDPoint.X + RDistributerSize.Width, RDPoint.Y);
            trolley.SimPoint = floor.ConvertToSimPoint(trolley.RPoint);

            WW.fill_tiles(trolley.RPoint, trolley.GetRSize(), this);
        }

        public void TickDistribute()
        {
            if(SideActivityMsLeft > 0)
            {
                TickSideActivity();
                return;
            }

            distributionms += distributionms_per_tick * floor.SpeedMultiplier;
            if (distributionms >= plant.ReorderTime)
            {
                distributionms = 0;
                if (RollForSideActivity())
                    return;
                MainTask.DistributionCompleted();
            }
        }

        public void TickSideActivity()
        {
            SideActivityMsLeft -= distributionms_per_tick * floor.SpeedMultiplier;
            if (SideActivityMsLeft <= 0)
            {
                SideActivity = null;
                MainTask.DistributionCompleted();
            }
        }

        /// <summary>
        /// Uses random to determine if you need to perform a side activity.
        /// Adds the amount of time to SideActivityMsLeft.
        /// </summary>
        private bool RollForSideActivity()
        {
            //Nieuwe laag bijzetten
            int r = floor.rand.Next(0, 1000);
            if (r <= OddsOfLaag * 1000)
            {
                SideActivityMsLeft += LaagTime;
                SideActivity = "Laag";
            }
            //De kar herindelen
            r = floor.rand.Next(0, 1000);
            if (r <= OddsOfHer * 1000)
            {
                SideActivityMsLeft += HerTime;
                SideActivity = "Her";
            }

            if (SideActivityMsLeft > 0)
            {
                MainTask.AInfo.UpdateFreq(MainTask.Goal, true);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Takes a trolley in.
        /// Changes the occupied_by to this distributer.
        /// </summary>
        /// <param name="t">Trolley to take in</param>
        public void TakeTrolleyIn(DanishTrolley t)
        {
            ChangeTravelSpeed(DanishTrolley.TrolleyTravelSpeed);
            trolley = t;

            //Rotate the distributer if the trolley to take in is not his orientation
            if (IsVertical != trolley.IsVertical)
            {
                RotateDistributerOnly();
                if (RDPoint.X < trolley.RPoint.X)
                    TrolleyOnTopLeft = true;
                else //Switches the trolley and the distributer
                {
                    //TODO: Rebuild your system so trolleys can also be transported from the bottom right.
                    SwitchDistributerTrolley();
                }
            }
            trolley.IsInTransport = true;

            WW.unfill_tiles(trolley.RPoint, trolley.GetRSize());
            TravelTrolley();
            WW.fill_tiles(trolley.RPoint, trolley.GetRSize(), this);
        }

        public void SwitchDistributerTrolley()
        {
            if (trolley.RPoint.X - RDPoint.X > 10) //Distributer is on the left of the trolley
                SwitchDistrToRightOfTrolley();
            else if(RDPoint.X - trolley.RPoint.X > 10) //Distributer is on the right of the trolley
                SwitchDistrToLeftOfTrolley();
            else if(RDPoint.Y - trolley.RPoint.Y > 10) //Distributer is on the bottom of the trolley
                SwitchDistrToTopOfTrolley();
        }

        private void SwitchDistrToLeftOfTrolley()
        {
            WW.unfill_tiles(trolley.RPoint, trolley.GetRSize());
            WW.unfill_tiles(RDPoint, GetRDbuterSize());

            TrolleyOnTopLeft = false;
            RDPoint = trolley.RPoint;

            trolley.RPoint.X = RDPoint.X + HRDistributerSize.Width;
            DPoint = floor.ConvertToSimPoint(RDPoint);
            trolley.SimPoint = floor.ConvertToSimPoint(trolley.RPoint);

            WW.fill_tiles(RDPoint, GetDButerTileSize());
        }

        private void SwitchDistrToRightOfTrolley()
        {
            WW.unfill_tiles(trolley.RPoint, trolley.GetRSize());
            WW.unfill_tiles(RDPoint, GetRDbuterSize());

            TrolleyOnTopLeft = true;
            trolley.RPoint.X = RDPoint.X;

            RDPoint.X = trolley.RPoint.X + trolley.GetRSize().Width + 10;
            DPoint = floor.ConvertToSimPoint(RDPoint);
            trolley.SimPoint = floor.ConvertToSimPoint(trolley.RPoint);

            WW.fill_tiles(RDPoint, GetDButerTileSize());
        }

        private void SwitchDistrToTopOfTrolley()
        {
            WW.unfill_tiles(trolley.RPoint, trolley.GetRSize());
            WW.unfill_tiles(RDPoint, new Size(HRDistributerSize.Width + 10, VRDistributerSize.Height));

            TrolleyOnTopLeft = false;
            RDPoint = trolley.RPoint;
            
            trolley.RPoint.Y = RDPoint.Y + 10;
            DPoint = floor.ConvertToSimPoint(RDPoint);
            trolley.SimPoint = floor.ConvertToSimPoint(trolley.RPoint);

            WW.fill_tiles(RDPoint, GetDButerTileSize());
        }

        /// <summary>
        /// Leaves the trolley behind at the current point
        /// And teleports the distributer up
        /// </summary>
        /// <returns></returns>
        public DanishTrolley GiveTrolley()
        {
            ChangeTravelSpeed(WALKSPEED);
            WW.unfill_tiles(RDPoint, GetDButerTileSize());
            trolley.IsInTransport = false;

            WW.WWC.ClearOccupiedBy();
            int teleport = 10;

            if(IsVertical)
                RDPoint.Y -= teleport;
            else
                RDPoint.X -= teleport;
            DPoint = floor.ConvertToSimPoint(RDPoint);
            WW.fill_tiles(RDPoint, GetDButerTileSize(), this);

            //This is done so the tiles of this trolley are no longer occupied by the distributer.
            WW.unfill_tiles(trolley.RPoint, trolley.GetRSize());
            WW.fill_tiles(trolley.RPoint, trolley.GetRSize());

            DanishTrolley t = trolley;
            trolley = null;
            return t;
        }
        
        /// <summary>
        /// Returns the size of the distributer in tiles.
        /// This includes the trolley if the distributer has a trolley.
        /// </summary>
        /// <returns></returns>
        public Size GetDButerTileSize()
        {
            int[] dindices = WW.TileListIndices(RDPoint, GetRDbuterSize());
            Size dbsize = new Size(dindices[2], dindices[3]);

            return dbsize;
        }

        /// <summary>
        /// Including The trolley if it has a trolley
        /// </summary>
        /// <returns>The Real distributer Size</returns>
        public Size GetRDbuterSize()
        {
            if (IsOnHarry)
                return Harry.GetRSize();
            if (trolley == null)
                return RDistributerSize;
            if(trolley.IsVertical)
                return new Size(Math.Max(RDistributerSize.Width, trolley.GetRSize().Width), RDistributerSize.Height + trolley.GetRSize().Height);
            
            return new Size(RDistributerSize.Width + trolley.GetRSize().Width, Math.Max(RDistributerSize.Height, trolley.GetRSize().Height));

        }

        private void RotateDistributerOnly()
        {
            WW.unfill_tiles(RDPoint, RDistributerSize);
            IsVertical = !IsVertical;
            if (IsVertical)
                RDistributerSize = VRDistributerSize;
            else 
                RDistributerSize = HRDistributerSize;
            DistributerSize = floor.ConvertToSimSize(RDistributerSize);
            WW.fill_tiles(RDPoint, RDistributerSize, this);
        }
        public void RotateDistributerAndTrolley()
        {
            WW.unfill_tiles(RDPoint, RDistributerSize);
            WW.unfill_tiles(trolley.RPoint, trolley.GetRSize());

            IsVertical = !IsVertical;
            trolley.IsVertical = !trolley.IsVertical;
            if (IsVertical)
                RDistributerSize = VRDistributerSize;
            else 
                RDistributerSize = HRDistributerSize;

            trolley.RPoint.X = RDPoint.X + RDistributerSize.Width;
            trolley.RPoint.Y = RDPoint.Y;
            trolley.SimPoint = floor.ConvertToSimPoint(trolley.RPoint);

            DistributerSize = floor.ConvertToSimSize(RDistributerSize);
            WW.fill_tiles(RDPoint, RDistributerSize, this);
            WW.fill_tiles(trolley.RPoint, trolley.GetRSize(), this);
        }

        public void RotateDistributerAndHarry()
        {
            WW.unfill_tiles(RDPoint, GetRDbuterSize());
            IsVertical = !IsVertical;
            Harry.IsVertical = !Harry.IsVertical;
            if (IsVertical)
                RDistributerSize = VRDistributerSize;
            else 
                RDistributerSize = HRDistributerSize;
            DistributerSize = floor.ConvertToSimSize(RDistributerSize);

            Harry.RotateTrolleys();

            WW.fill_tiles(RDPoint, GetRDbuterSize(), this);
        }

        public void MountHarry(LangeHarry Harry_)
        {
            ChangeTravelSpeed(LangeHarry.HarryTravelSpeed);
            if (IsVertical != Harry_.IsVertical)
                RotateDistributerOnly();
            Harry = Harry_;

            WW.unfill_tiles(RDPoint, GetRDbuterSize());
            WW.unfill_tiles(Harry.RPoint, Harry.GetRSize());
            IsOnHarry = true;
            Harry.IsInUse = true;
            Harry.DButer = this;
            RDPoint = Harry.RPoint;

            WW.fill_tiles(RDPoint, GetRDbuterSize(), this);
        }
        
        public void DisMountHarry()
        {
            ChangeTravelSpeed(WALKSPEED);
            WW.unfill_tiles(RDPoint, GetRDbuterSize());

            Harry.DButer = null;
            Harry.IsInUse = false;
            Harry.IsTargeted = false;
            if (IsVertical)
                RDPoint = new Point(Harry.RPoint.X - VRDistributerSize.Width, Harry.RPoint.Y + 57 * 3);
            else
                RDPoint = new Point(Harry.RPoint.X + 20, Harry.RPoint.Y - HRDistributerSize.Height);
            DPoint = floor.ConvertToSimPoint(RDPoint);

            WW.fill_tiles(Harry.RPoint, Harry.GetRSize());
            Harry = null;
            IsOnHarry = false;
            WW.fill_tiles(RDPoint, GetRDbuterSize(), this);
        }

        private void ChangeTravelSpeed(float speed)
        {
            TravelSpeed = speed;
            travel_dist_per_tick = TravelSpeed / Program.TICKS_PER_SECOND;
        }

        /// <summary>
        /// Reshuffles the plant list of the trolley to make sure the next plant has a targethub with a trolley.
        /// switches the first plant in the list with the next first plant that has a targethub with a trolley.
        /// Returns true when a reshuffle was possible, false if not.
        /// </summary>
        public bool ReshufflePlants()
        {
            for (int i = 1; i < trolley.PlantList.Count; i++)
                if (trolley.PlantList[i].DestinationHub.PeekFirstTrolley() != null)
                {
                    trolley.SwitchPlants(0, i);
                    return true;
                }
            return false;

        }
    }
}
