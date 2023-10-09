using System;
using System.Collections.Generic;
using System.Data;
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
        private Distributer distributer;
        int clearance_down;
        int clearance_right;

        public DijkstraWalkWays(WalkWay WW_, Distributer distributer_, int clearance_down_, int clearance_right_)
        {
            WW = WW_;
            distributer = distributer_; 
            clearance_down = clearance_down_;
            clearance_right = clearance_right_;
            TileQueue = new Queue<WalkTile>();
        }

        /// <summary>
        /// Runs the dijkstra algorithm.
        /// Returns a list of the shortest path. 0 = start tile, last item = target tile, with the route within it.
        /// </summary>
        /// <param name="start_tile"></param>
        /// <param name="target_tile"></param>
        /// <returns>The route to take (List of WalkTiles)</returns>
        public List<WalkTile> RunAlgo(WalkTile start_tile, WalkTile target_tile) 
        { 
            TileQueue.Clear();
            ResetTravelCosts();
            //TODO: update the clearances costs too much time right now.
            WW.unfill_tiles(distributer.RDPoint, distributer.RDistributerSize);
            WW.UpdateClearances();

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

            //No route was found
            if (target_tile.TravelCost == int.MaxValue)
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

            WW.fill_tiles(distributer.RDPoint, distributer.RDistributerSize);
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

        private bool IsTileAccessible(WalkTile tile)
        {
            return tile != null && !tile.occupied && tile.ClearanceRight >= clearance_right && tile.ClearanceBot >= clearance_down;
        }
    }
}
