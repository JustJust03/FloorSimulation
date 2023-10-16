using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloorSimulation
{
    /// <summary>
    /// A class which's actions should be performed by simulation components like distributers.
    /// Uses Walkway to find shortest paths.
    /// </summary>
    // TODO: Convert this to an A* algorithm
    internal class DijkstraWalkWays
    {
        private WalkWay WW;
        private Queue<WalkTile> TileQueue;
        private Distributer DButer;

        public DijkstraWalkWays(WalkWay WW_, Distributer distributer_)
        {
            WW = WW_;
            DButer = distributer_; 
            TileQueue = new Queue<WalkTile>();
        }

        /// <summary>
        /// returns shortest path to a trolley.
        /// Access points are at the top or bottom for vertical trolleys and at the right and left of horizontal trolleys
        /// </summary>
        public List<WalkTile> RunAlgoDistrToTrolley(DanishTrolley TargetTrolley)
        {
            WalkTile StartTile = WW.GetTile(DButer.RDPoint);
            int[] tindices = WW.TileListIndices(TargetTrolley.RPoint, TargetTrolley.GetRSize());
            int tx = tindices[0]; int ty = tindices[1]; int twidth = tindices[2]; int theight = tindices[3];
            int[] dindices = WW.TileListIndices(DButer.RDPoint, DButer.RDistributerSize);
            int dx = dindices[0]; int dy = dindices[1]; int dwidth = dindices[2]; int dheight = dindices[3];

            List<WalkTile> TargetTiles = new List<WalkTile>();
            if (TargetTrolley.IsVertical) //Vertical target trolley's
            {
                int toppoint = ty - dheight;
                int botpoint = ty + theight + dheight;

                if (toppoint > 0)
                    TargetTiles.Add(WW.WalkTileList[tx][toppoint]); //top accesspoint to trolley
                if (botpoint < WW.WalkTileList[0].Count)
                    TargetTiles.Add(WW.WalkTileList[tx][botpoint]); //bot accesspoint to trolley
            }
            else //Horizontal target trolley
            {
                int rightpoint = tx + twidth;
                int leftpoint = tx - 1 - dwidth;

                if (rightpoint < WW.WalkTileList.Count)
                    TargetTiles.Add(WW.WalkTileList[rightpoint][ty]); //bot accesspoint to trolley
                if (leftpoint > 0)
                    TargetTiles.Add(WW.WalkTileList[leftpoint][ty]); //top accesspoint to trolley
            }

            return RunAlgo(StartTile, TargetTiles);
        }

        /// <summary>
        /// Returns shortest path to the target tile
        /// </summary>
        public List<WalkTile> RunAlgoTile(WalkTile StartTile, WalkTile TargetTile)
        {
            List<WalkTile> TargetTiles = new List<WalkTile>() { TargetTile};
            return RunAlgo(StartTile, TargetTiles);
        }

        /// <summary>
        /// Returns the path to the closest target tile
        /// </summary>
        public List<WalkTile> RunAlgoTiles(WalkTile StartTile, List<WalkTile> TargetTiles)
        {
            return RunAlgo(StartTile, TargetTiles);
        }

        /// <summary>
        /// Runs the dijkstra algorithm.
        /// Returns a list of the shortest path. 0 = start tile, last item = target tile, with the route within it.
        /// </summary>
        /// <returns>The route to take (List of WalkTiles)</returns>
        private List<WalkTile> RunAlgo(WalkTile start_tile, List<WalkTile> target_tiles) 
        {
            TileQueue.Clear();
            ResetTravelCosts();
            //TODO: update the clearances costs too much time right now.
            WW.WWC.UpdateClearances(DButer, DButer.GetDButerTileSize());
 
            start_tile.TravelCost = 0;
            start_tile.visited = true;
            TileQueue.Enqueue(start_tile);

            while(TileQueue.Count > 0) 
            {
                WalkTile tile = TileQueue.Dequeue();
                tile.visited = true;

                WalkTile tileA = tile.TileAbove();
                WalkTile tileB = tile.TileBeneath();
                WalkTile tileL = tile.TileLeft();
                WalkTile tileR = tile.TileRight();

                //up
                if (IsTileAccessible(tileA) && tileA.TravelCost > tile.TravelCost + 1)
                {
                    tileA.TravelCost = tile.TravelCost + 1;
                    tileA.Parent = tile;
                    if (!tileA.visited)
                        TileQueue.Enqueue(tileA);
                }
                //down
                if (IsTileAccessible(tileB) && tileB.TravelCost > tile.TravelCost + 1)
                {
                    tileB.TravelCost = tile.TravelCost + 1;
                    tileB.Parent = tile;
                    if (!tileB.visited)
                        TileQueue.Enqueue(tileB);
                }
                //left
                if (IsTileAccessible(tileL) && tileL.TravelCost > tile.TravelCost + 1)
                {
                    tileL.TravelCost = tile.TravelCost + 1;
                    tileL.Parent = tile;
                    if (!tileL.visited)
                        TileQueue.Enqueue(tileL);
                }
                //right
                if (IsTileAccessible(tileR) && tileR.TravelCost > tile.TravelCost + 1)
                {
                    tileR.TravelCost = tile.TravelCost + 1;
                    tileR.Parent = tile;
                    if (!tileR.visited)
                        TileQueue.Enqueue(tileR);
                }
            }

            WalkTile target_tile = ClosestTargetTile(target_tiles);

            //No route was found
            if (target_tile == null || target_tile.TravelCost == int.MaxValue)
                return null;
            
            //Trace back from the target tile to it's parent to calculate the route.
            List<WalkTile> Route = new List<WalkTile>();
            WalkTile ptile = target_tile;

            Route.Insert(0, ptile);
            while(ptile.Parent != null)
            {
                ptile = ptile.Parent;
                Route.Insert(0, ptile);
            }

            return Route;
        }

        /// <summary>
        /// Sets all travelcosts of all tiles in this walkway to maximum allowd by int.
        /// </summary>
        private void ResetTravelCosts()
        {
            foreach (List<WalkTile> col in WW.WalkTileList)
                foreach(WalkTile tile in col)
                {
                    tile.TravelCost = int.MaxValue;
                    tile.visited = false;
                    tile.Parent = null;
                } 
        }

        /// <summary>
        /// Checks if tile is accessible: 
        /// Does the tile exist, 
        /// Is it unoccupied, 
        /// Is there enough clearance on the right and bottom.
        /// </summary>
        public bool IsTileAccessible(WalkTile tile)
        {
            return tile != null && (tile.accessible || tile.occupied_by == DButer) && !tile.inaccessible_by_static;
        }

        /// <summary>
        /// From a list of target tiles return the one with the lowest travel cost (thus the one that has the shortest path)
        /// </summary>
        private WalkTile ClosestTargetTile(List<WalkTile> target_tiles)
        {
            WalkTile ClosestTile = null;
            int shortest = int.MaxValue;
            foreach(WalkTile TargetTile in target_tiles)
            {
                if (TargetTile.TravelCost < shortest)
                {
                    ClosestTile = TargetTile;
                    shortest = TargetTile.TravelCost;
                }
            }

            return ClosestTile;
        }
    }
}
