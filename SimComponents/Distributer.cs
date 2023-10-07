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
        private Point RDPoint; // Real distributer point
        private Point DPoint; // Sim distributer point
        private Size RDistributerSize; //Is the real size in cm.
        private Size DistributerSize; 
        public int id;
        private Floor floor;

        private List<WalkTile> route;
        private const float WALKSPEED = 500f; // cm/s
        private float travel_dist_per_tick;
        private float ticktravel = 0f; //The distance that has been traveled, but not registered to walkway yet.

        private DijkstraWalkWays DWW;
        public WalkWay WW;

        
        public Distributer(int id_, Floor floor_, WalkWay WW_, Point Rpoint_ = default)
        {
            id = id_;
            floor = floor_;
            RDPoint = Rpoint_;
            WW = WW_;
            DWW = new DijkstraWalkWays(WW);

            DistributerIMG = Image.FromFile(Program.rootfolder + @"\SimImages\Distributer.png");
            RDistributerSize = new Size(DistributerIMG.Width, DistributerIMG.Height);

            DistributerSize = floor.ConvertToSimSize(RDistributerSize);
            DPoint = floor.ConvertToSimPoint(RDPoint);

            travel_dist_per_tick = WALKSPEED / Program.TICKS_PER_SECOND;
        }

        public void Tick()
        {
            TickWalk();
        }

        public void DrawObject(Graphics g)
        {
            g.DrawImage(DistributerIMG, new Rectangle(DPoint, DistributerSize));
        }

        /// <summary>
        /// Makes the distributer walk towards the target tile using a shortest path algorithm.
        /// </summary>
        /// <param name="target_tile"></param>
        public void TravelTo(WalkTile target_tile)
        {
            route = DWW.RunAlgo(WW.WalkTileList[0][7], WW.WalkTileList[76][178]);
        }

        /// <summary>
        /// Ticks the walking distance. 
        /// If the walking distance is bigger than the width of a tile, move the distributer.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public void TickWalk()
        {
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
                    DPoint = route[0].Simpoint;
                    ticktravel -= WalkWay.WALK_TILE_WIDTH;
                    route.RemoveAt(0);
                }
            }
        }
    }
}
