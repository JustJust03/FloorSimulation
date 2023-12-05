using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace FloorSimulation
{
    internal abstract class Agent
    {
        protected Image HAgentIMG;
        protected Image VAgentIMG;

        public Point RPoint;
        public Point SimPoint;
        public Point SavePoint;
        protected Point DrawPoint;
        protected Size HRAgentSize;
        protected Size VRAgentSize;
        protected Size SimAgentSize;

        public int id;
        public Floor floor;
        public int MaxWaitedTicks;

        public List<WalkTile> route;
        public float TravelSpeed = 85f; // cm/s

        protected float travel_dist_per_tick;
        protected float ticktravel = 0f; //The distance that has been traveled, but not registered to walkway yet

        public Task MainTask;
        public DanishTrolley trolley;

        protected bool IsVertical;
        public bool IsOnHarry = false;

        protected AstarWalkWays AWW;
        public WalkWay WW;

        public Agent(int id_, Floor floor_, WalkWay WW_, string BaseImgFileName, Point Rpoint_ = default, bool IsVertical_ = true, int MaxWaitedTicks_ = 100) 
        {
            id = id_;
            floor = floor_;
            RPoint = Rpoint_;
            if (Rpoint_ != null)
                SavePoint = Rpoint_;
            WW = WW_;
            IsVertical = IsVertical_;
            MaxWaitedTicks = MaxWaitedTicks_;

            VAgentIMG = Image.FromFile(Program.rootfolder + @"\SimImages\" + BaseImgFileName + "_vertical.png");
            VRAgentSize = new Size(VAgentIMG.Width, VAgentIMG.Height);
            HAgentIMG = Image.FromFile(Program.rootfolder + @"\SimImages\" + BaseImgFileName + "_horizontal.png");
            HRAgentSize = new Size(HAgentIMG.Width, HAgentIMG.Height);

            SimAgentSize = floor.ConvertToSimSize(GetRSize());
            SimPoint = floor.ConvertToSimPoint(RPoint);

            travel_dist_per_tick = TravelSpeed / Program.TICKS_PER_SECOND;
            trolley = null;

            AWW = new AstarWalkWays(WW, this);
            WW.fill_tiles(RPoint, GetRSize(), this);
        }
        public void Tick()
        {
            MainTask.PerformTask();
        }

        public virtual Size GetRSize(bool OnlyAgentSize = false)
        {
            if (IsVertical)
                return VRAgentSize;
            else
                return HRAgentSize;
        }

        public virtual void DrawObject(Graphics g,  Point p = default)
        {
            if (p != default)
                DrawPoint = p;
            else
                DrawPoint = SimPoint;

            if (trolley != null)
                trolley.DrawObject(g);
            if(IsVertical)
                g.DrawImage(VAgentIMG, new Rectangle(DrawPoint, SimAgentSize));
            else
                g.DrawImage(HAgentIMG, new Rectangle(DrawPoint, SimAgentSize));
        }

        /// <summary>
        /// Makes the distributer walk towards the target tile using a shortest path algorithm.
        /// </summary>
        /// <param name="target_tile"></param>
        public void TravelToTile(WalkTile target_tile)
        {
            if (RPoint == target_tile.Rpoint)
                return;
            route = AWW.RunAlgoTile(WW.GetTile(RPoint), target_tile);
        }

        /// <summary>
        /// Makes a distributer walk towards the closest available target tile using a shortest path algorithm.
        /// </summary>
        /// <param name="target_tile"></param>
        public void TravelToClosestTile(List<WalkTile> target_tiles)
        {
            route = AWW.RunAlgoTiles(WW.GetTile(RPoint), target_tiles);
        }

        /// <summary>
        /// Makes the distributer walk towards the target tile using a shortest path algorithm.
        /// </summary>
        public virtual void TravelToTrolley(DanishTrolley target_trolley)
        {
            route = AWW.RunAlgoDistrToTrolley(target_trolley);
        }

        /// <summary>
        /// Makes the distributer walk towards LangeHarry using it's accesspoints.
        /// </summary>
        public virtual void TravelToHarry(LangeHarry Harry)
        {
            throw new NotImplementedException();
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

                    WW.WWC.UpdateLocalClearances(this, GetTileSize(), destination);

                    if (!AWW.IsTileAccessible(destination)) //Route failed, there was something occupying the calculated route
                    {
                        ticktravel = 0;
                        MainTask.FailRoute();
                        return;
                    }

                    WW.unfill_tiles(RPoint, GetRSize());
                    RPoint = route[0].Rpoint;
                    SimPoint = floor.ConvertToSimPoint(RPoint);
                    WW.fill_tiles(RPoint, GetRSize(), this);

                    if (IsOnHarry) //If you are on LangeHarry travel Harry too.
                        TravelHarry();
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


        public virtual void TravelHarry()
        {
            throw new NotImplementedException();
        }

        public virtual void TravelTrolley()
        {
            if (IsVertical)
                trolley.RPoint = new Point(RPoint.X, RPoint.Y + GetRSize(OnlyAgentSize: true).Height);
            else
                trolley.RPoint = new Point(RPoint.X + GetRSize(OnlyAgentSize: true).Width, RPoint.Y);
            trolley.SimPoint = floor.ConvertToSimPoint(trolley.RPoint);
        }
        public abstract void TakeTrolleyIn(DanishTrolley t);

        public virtual DanishTrolley GiveTrolley()
        {
            WW.unfill_tiles(RPoint, GetRSize());
            trolley.IsInTransport = false;

            WW.WWC.ClearOccupiedBy();
            int teleport = 10;

            if(IsVertical)
                RPoint.Y -= teleport;
            else
                RPoint.X -= teleport;
            SimPoint = floor.ConvertToSimPoint(RPoint);
            WW.fill_tiles(RPoint, GetTileSize(), this);

            //This is done so the tiles of this trolley are no longer occupied by the distributer.
            WW.unfill_tiles(trolley.RPoint, trolley.GetRSize());
            WW.fill_tiles(trolley.RPoint, trolley.GetRSize());

            DanishTrolley t = trolley;
            trolley = null;
            return t;
        }

        protected virtual void RotateAgentOnly()
        {
            WW.unfill_tiles(RPoint, GetRSize());
            IsVertical = !IsVertical;

            SimAgentSize = floor.ConvertToSimSize(GetRSize());
            WW.fill_tiles(RPoint, GetRSize(), this);
        }

        /// <summary>
        /// Returns the size of the distributer in tiles.
        /// This includes the trolley if the distributer has a trolley.
        /// </summary>
        /// <returns></returns>
        public Size GetTileSize()
        {
            int[] dindices = WW.TileListIndices(RPoint, GetRSize());
            Size dbsize = new Size(dindices[2], dindices[3]);

            return dbsize;
        }

        protected void ChangeTravelSpeed(float speed)
        {
            TravelSpeed = speed;
            travel_dist_per_tick = TravelSpeed / Program.TICKS_PER_SECOND;
        }
    }
}
