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

        public List<WalkTile> RunAlgoDistrToTrolley(Distributer DButer , DanishTrolley TargetTrolley)
        {
            WalkTile StartTile = WW.GetTile(DButer.RDPoint);
            int[] tindices = WW.TileListIndices(TargetTrolley.RPoint, TargetTrolley.GetSize());
            int tx = tindices[0]; int ty = tindices[1]; int twidth = tindices[2]; int theight = tindices[3];
            int[] dindices = WW.TileListIndices(DButer.RDPoint, DButer.RDistributerSize);
            int dx = dindices[0]; int dy = dindices[1]; int dwidth = dindices[2]; int dheight = dindices[3];

            List<WalkTile> TargetTiles = new List<WalkTile>();
            if (TargetTrolley.IsVertical) //Vertical target trolley's
            {
                int toppoint = ty - 1 - dheight;
                int botpoint = ty + 1 + theight + dheight;

                if (toppoint > 0)
                    TargetTiles.Add(WW.WalkTileList[tx][toppoint]); //top accesspoint to trolley
                if (botpoint < WW.WalkTileList[0].Count)
                    TargetTiles.Add(WW.WalkTileList[tx][botpoint]); //bot accesspoint to trolley
            }
            else //Horizontal target trolley
            {
                int rightpoint = tx + 1 + twidth;
                int leftpoint = tx - 1 - dwidth;

                if (rightpoint < WW.WalkTileList.Count)
                    TargetTiles.Add(WW.WalkTileList[rightpoint][ty]); //bot accesspoint to trolley
                if (leftpoint > 0)
                    TargetTiles.Add(WW.WalkTileList[leftpoint][ty]); //top accesspoint to trolley
            }

            return RunAlgo(StartTile, TargetTiles);
        }

        public List<WalkTile> RunAlgoTile(WalkTile Start_tile, WalkTile target_tile)
        {
            return RunAlgo(Start_tile, new List<WalkTile>() {target_tile});
        }

        /// <summary>
        /// Runs the dijkstra algorithm.
        /// Returns a list of the shortest path. 0 = start tile, last item = target tile, with the route within it.
        /// </summary>
        /// <param name="start_tile"></param>
        /// <param name="target_tile"></param>
        /// <returns>The route to take (List of WalkTiles)</returns>
        private List<WalkTile> RunAlgo(WalkTile start_tile, List<WalkTile> target_tiles) 
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
            WW.fill_tiles(distributer.RDPoint, distributer.RDistributerSize);

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

        private bool IsTileAccessible(WalkTile tile)
        {
            return tile != null && !tile.occupied && tile.ClearanceRight >= clearance_right && tile.ClearanceBot >= clearance_down;
        }

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
