using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Collections.Generic;

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
        public Size RDistributerSize; //Is the real size in cm.
        private Size DistributerSize;
        public Size HRDistributerSize;
        public Size VRDistributerSize;
        public int id;
        public Floor floor;

        public List<WalkTile> route;
        private const float WALKSPEED = 142f; // cm/s
        private float travel_dist_per_tick;
        private int distributionms_per_tick; // plant distribution per tick in ms
        private float ticktravel = 0f; //The distance that has been traveled, but not registered to walkway yet
        private int distributionms = 0; // How many ms have you been distributing
        public Task MainTask;
        public DanishTrolley trolley;
        public LangeHarry Harry;
        private bool IsVertical;
        public bool TrolleyOnTopLeft; //True when the trolley is to the left or on top of the distributer
        public bool IsOnHarry;

        private DijkstraWalkWays DWW;
        public WalkWay WW;

        
        public Distributer(int id_, Floor floor_, WalkWay WW_, Point Rpoint_ = default, bool IsVertical_ = true)
        {
            id = id_;
            floor = floor_;
            RDPoint = Rpoint_;
            WW = WW_;
            IsVertical = IsVertical_;
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

            travel_dist_per_tick = WALKSPEED / Program.TICKS_PER_SECOND;
            distributionms_per_tick = (int)(1000f / Program.TICKS_PER_SECOND);
            MainTask = new Task(floor.FirstStartHub, this, "TakeFullTrolley", floor.FinishedD);
            trolley = null;

            DWW = new DijkstraWalkWays(WW, this);
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
            route = DWW.RunAlgoTile(WW.GetTile(RDPoint), target_tile);
        }

        /// <summary>
        /// Makes a distributer walk towards the closest available target tile using a shortest path algorithm.
        /// </summary>
        /// <param name="target_tile"></param>
        public void TravelToClosestTile(List<WalkTile> target_tiles)
        {
            route = DWW.RunAlgoTiles(WW.GetTile(RDPoint), target_tiles);
        }

        /// <summary>
        /// Makes the distributer walk towards the target tile using a shortest path algorithm.
        /// </summary>
        public void TravelToTrolley(DanishTrolley target_trolley)
        {
            route = DWW.RunAlgoDistrToTrolley(target_trolley);
        }

        /// <summary>
        /// Makes the distributer walk towards LangeHarry using it's accesspoints.
        /// </summary>
        public void TravelToHarry(LangeHarry Harry)
        {
            route = DWW.RunAlgoDistrToHarry(Harry);
        }
        
        /// <summary>
        /// Ticks the walking distance. 
        /// If the walking distance is bigger than the width of a tile, move the distributer.
        /// </summary>
        /// <returns>true if there de distributer is walking, false if route has been completed</returns>
        public void TickWalk()
        {
            if (route == null)
                return;

            // TODO: develop non square tile tickwalks.
            if (WalkWay.WALK_TILE_WIDTH != WalkWay.WALK_TILE_HEIGHT)
                throw new ArgumentException("Distributer walk has not yet been developed for non square tiles.");

            if (route.Count > 0)
            {
                ticktravel += travel_dist_per_tick * floor.SpeedMultiplier;
                while(ticktravel > WalkWay.WALK_TILE_WIDTH)
                {
                    if(route.Count == 0)
                    {
                        ticktravel = 0;
                        break;
                    }

                    WalkTile destination = route[0];
                  
                    WW.WWC.UpdateLocalClearances(this, GetDButerTileSize(), destination);

                    if (!DWW.IsTileAccessible(destination)) //Route failed, there was something occupying the calculated route
                    {
                        ticktravel = 0;
                        MainTask.FailRoute();
                        return;
                    }

                    WW.unfill_tiles(RDPoint, RDistributerSize);
                    DPoint = route[0].Simpoint;
                    RDPoint = floor.ConvertToRealPoint(DPoint);
                    WW.fill_tiles(RDPoint, RDistributerSize, this);

                    if (IsOnHarry) //If you are on LangeHarry travel Harry too.
                        Harry.TravelHarry();
                    else if (trolley != null) //If you have a trolley, drag it with you.
                        TravelTrolley();

                    ticktravel -= WalkWay.WALK_TILE_WIDTH;
                    route.RemoveAt(0);
                }
            }
            else // Route is empty, thus target has been reached.
                MainTask.RouteCompleted(); 
        }

        private void TravelTrolley()
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
            distributionms += distributionms_per_tick * floor.SpeedMultiplier;
            if (distributionms >= trolley.PlantList[0].ReorderTime)
            {
                distributionms = 0;
                MainTask.DistributionCompleted();
            }
        }

        /// <summary>
        /// Takes a trolley in.
        /// Changes the occupied_by to this distributer.
        /// </summary>
        /// <param name="t">Trolley to take in</param>
        public void TakeTrolleyIn(DanishTrolley t)
        {
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
            WW.fill_tiles(trolley.RPoint, trolley.GetRSize(), this);
        }

        public void SwitchDistributerTrolley()
        {
            if (RDPoint.X < trolley.RPoint.X)
                SwitchDistrToRightOfTrolley();
            else
                SwitchDistrToLeftOfTrolley();
        }

        private void SwitchDistrToLeftOfTrolley()
        {
            WW.unfill_tiles(trolley.RPoint, trolley.GetRSize());
            WW.unfill_tiles(RDPoint, GetRDbuterSize());

            TrolleyOnTopLeft = false;
            RDPoint = trolley.RPoint;
            if (IsVertical)
                trolley.RPoint.Y = RDPoint.Y + VRDistributerSize.Height; //TODO: This doesn't work and is never run
            else
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
            if (IsVertical)
                trolley.RPoint.Y = RDPoint.Y - VRDistributerSize.Height; //TODO: This doesn't work and is never run
            else
                RDPoint.X = trolley.RPoint.X + trolley.GetRSize().Width + 10;
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
            WW.unfill_tiles(RDPoint, GetRDbuterSize());

            Harry.DButer = null;
            Harry.IsInUse = false;
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
    }
}
