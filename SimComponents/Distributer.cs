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
        private Image DistributerIMG;
        public Point RDPoint; // Real distributer point
        private Point DPoint; // Sim distributer point
        public Size RDistributerSize; //Is the real size in cm.
        private Size DistributerSize; 
        public int id;
        public Floor floor;

        private List<WalkTile> route;
        private const float WALKSPEED = 1000f; // cm/s
        private float travel_dist_per_tick;
        private int distributionms_per_tick; // plant distribution per tick in ms
        private float ticktravel = 0f; //The distance that has been traveled, but not registered to walkway yet
        private int distributionms = 0; // How many ms have you been distributing
        private Task MainTask;
        public DanishTrolley trolley;

        private DijkstraWalkWays DWW;
        public WalkWay WW;

        
        public Distributer(int id_, Floor floor_, WalkWay WW_, Point Rpoint_ = default)
        {
            id = id_;
            floor = floor_;
            RDPoint = Rpoint_;
            WW = WW_;

            DistributerIMG = Image.FromFile(Program.rootfolder + @"\SimImages\Distributer.png");
            RDistributerSize = new Size(DistributerIMG.Width, DistributerIMG.Height);

            DistributerSize = floor.ConvertToSimSize(RDistributerSize);
            DPoint = floor.ConvertToSimPoint(RDPoint);

            travel_dist_per_tick = WALKSPEED / Program.TICKS_PER_SECOND;
            distributionms_per_tick = (int)(1000f / Program.TICKS_PER_SECOND);
            MainTask = new Task(floor.FirstStartHub, this, "TakeFullTrolley");
            trolley = null;

            DWW = new DijkstraWalkWays(WW, this);
            WW.fill_tiles(RDPoint, RDistributerSize);
        }

        public void Tick()
        {
            MainTask.PerformTask();
        }

        public void DrawObject(Graphics g)
        {
            if (trolley != null)
                trolley.DrawObject(g);
            g.DrawImage(DistributerIMG, new Rectangle(DPoint, DistributerSize));
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
        /// <param name="target_tile"></param>
        public void TravelToTrolley(DanishTrolley target_trolley)
        {
            route = DWW.RunAlgoDistrToTrolley(target_trolley);
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
                ticktravel += travel_dist_per_tick;
                while(ticktravel > WalkWay.WALK_TILE_WIDTH)
                {
                    if(route.Count == 0)
                    {
                        ticktravel = 0;
                        break;
                    }
                    //TODO: Update occupiance of WalkWay tiles when moving tiles. Also check if tile is occupied before traveling.
                    WW.unfill_tiles(RDPoint, RDistributerSize);

                    DPoint = route[0].Simpoint;
                    RDPoint = floor.ConvertToRealPoint(DPoint);

                    if (trolley != null) //If you have a trolley, drag it with you.
                    {
                        WW.unfill_tiles(trolley.RPoint, trolley.GetSize());

                        trolley.RPoint = new Point(RDPoint.X, RDPoint.Y + RDistributerSize.Height);
                        trolley.SimPoint = floor.ConvertToSimPoint(trolley.RPoint);

                        WW.fill_tiles(trolley.RPoint, trolley.GetSize());
                    }

                    ticktravel -= WalkWay.WALK_TILE_WIDTH;
                    route.RemoveAt(0);
                    
                    WW.fill_tiles(RDPoint, RDistributerSize); ;
                    //TODO: Update the clearance, and make this clearance update faster.
                    //WW.UpdateClearances();
                }
            }
            else // Route is empty, thus target has been reached.
                MainTask.RouteCompleted(); 
        }

        public void TickDistribute()
        {
            distributionms += distributionms_per_tick ;
            if (distributionms >= trolley.PlantList[0].ReorderTime)
            {
                distributionms -= trolley.PlantList[0].ReorderTime;
                MainTask.DistributionCompleted();
            }
        }
    }
}
