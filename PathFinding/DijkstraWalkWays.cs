using System;
using System.Collections.Generic;
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
        private Stack<WalkTile> TileStack;

        public DijkstraWalkWays(WalkWay WW_)
        {
            WW = WW_;
            TileStack = new Stack<WalkTile>();
        }

        /// <summary>
        /// Runs the dijkstra algorithm.
        /// Returns a list of the shortest path. 0 = start tile, last item = target tile, with the route within it.
        /// </summary>
        /// <param name="start_tile"></param>
        /// <param name="target_tile"></param>
        /// <returns></returns>
        public List<WalkTile> RunAlgo(WalkTile start_tile, WalkTile target_tile) 
        { 
            TileStack.Clear();
            ResetTravelCosts();

            start_tile.TravelCost = 0;
            start_tile.visited = true;
            TileStack.Push(start_tile);

            while(TileStack.Count > 0) 
            { 
                WalkTile tile = TileStack.Pop();
                tile.visited = true;

                WalkTile tileA = tile.TileAbove();
                WalkTile tileB = tile.TileBeneath();
                WalkTile tileL = tile.TileLeft();
                WalkTile tileR = tile.TileRight();

                //up
                if (tileA != null && !tileA.occupied && tileA.TravelCost > tile.TravelCost + 1)
                {
                    tileA.TravelCost = tile.TravelCost + 1;
                    tileA.Parent = tile;
                    if (!tileA.visited)
                        TileStack.Push(tileA);
                }
                //down
                if (tileB != null && !tileB.occupied && tileB.TravelCost > tile.TravelCost + 1)
                {
                    tileB.TravelCost = tile.TravelCost + 1;
                    tileB.Parent = tile;
                    if (!tileB.visited)
                        TileStack.Push(tileB);
                }
                //left
                if (tileL != null && !tileL.occupied && tileL.TravelCost > tile.TravelCost + 1)
                {
                    tileL.TravelCost = tile.TravelCost + 1;
                    tileL.Parent = tile;
                    if (!tileL.visited)
                        TileStack.Push(tileL);
                }
                //right
                if (tileR != null && !tileR.occupied && tileR.TravelCost > tile.TravelCost + 1)
                {
                    tileR.TravelCost = tile.TravelCost + 1;
                    tileR.Parent = tile;
                    if (!tileR.visited)
                        TileStack.Push(tileR);
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
    }
}
