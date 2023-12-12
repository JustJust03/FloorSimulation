using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using Priority_Queue;

namespace FloorSimulation
{
    internal class AstarWalkWays
    { 
        private WalkWay WW;
        private SimplePriorityQueue<WalkTile> TileQueue;
        private Agent agent;
        private WalkTile ClosesTargetTile;

        public AstarWalkWays(WalkWay WW_, Agent agent_)
        {
            WW = WW_;
            agent = agent_; 
            TileQueue = new SimplePriorityQueue<WalkTile>();
        }

        private int SquaredHeuristic(WalkTile tile)
        {
            // You can adjust the heuristic based on your specific requirements.
            // Here's an example of the Euclidean distance heuristic:
            int x = tile.TileX - ClosesTargetTile.TileX;
            int y = tile.TileY - ClosesTargetTile.TileY;
            return x * x + y * y;
        }

        private void AssignClosestTargetTile(WalkTile tile, List<WalkTile> targetTiles)
        {
            // You can adjust the heuristic based on your specific requirements.
            // Here's an example of the Euclidean distance heuristic:
            int mincost = int.MaxValue;
            foreach(WalkTile t in targetTiles)
            {
                int cost = Math.Abs(tile.TileX - t.TileX) + Math.Abs(tile.TileY - t.TileY);
                if (cost < mincost)
                {
                    ClosesTargetTile = t;
                    mincost = cost;
                }
            }
        }
        /// <summary>
        /// returns shortest path to a trolley.
        /// Access points are at the top or bottom for vertical trolleys and at the right and left of horizontal trolleys
        /// </summary>
        public List<WalkTile> RunAlgoDistrToTrolley(DanishTrolley TargetTrolley, bool OnlyUpper)
        {
            if (TargetTrolley == null)
                return null;
            WalkTile StartTile = WW.GetTile(agent.RPoint);
            int[] tindices = WW.TileListIndices(TargetTrolley.RPoint, TargetTrolley.GetRSize());
            int tx = tindices[0]; int ty = tindices[1]; int twidth = tindices[2]; int theight = tindices[3];
            int[] dindices = WW.TileListIndices(agent.RPoint, agent.GetRSize());
            int dx = dindices[0]; int dy = dindices[1]; int dwidth = dindices[2]; int dheight = dindices[3];

            List<WalkTile> TargetTiles = new List<WalkTile>();
            if (TargetTrolley.IsVertical) //Vertical target trolley's
            {
                int toppoint = ty - dheight;
                int botpoint = ty + theight + dheight;

                if (toppoint > 0)
                    TargetTiles.Add(WW.WalkTileList[tx][toppoint]); //top accesspoint to trolley
                if (botpoint < WW.WalkTileList[0].Count && !OnlyUpper)
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

        public List<WalkTile> RunAlgoLowPadToTrolley(DanishTrolley TargetTrolley)
        {
            if (TargetTrolley == null)
                return null;
            WalkTile StartTile = WW.GetTile(agent.RPoint);
            int[] tindices = WW.TileListIndices(TargetTrolley.RPoint, TargetTrolley.GetRSize());
            int tx = tindices[0]; int ty = tindices[1]; int twidth = tindices[2]; int theight = tindices[3];
            int[] dindices = WW.TileListIndices(agent.RPoint, agent.GetRSize());
            int dx = dindices[0]; int dy = dindices[1]; int dwidth = dindices[2]; int dheight = dindices[3];

            List<WalkTile> TargetTiles = new List<WalkTile>();
            if (TargetTrolley.IsVertical) //Vertical target trolley's
            {
                int middlepoint = ty + ((theight - dheight) / 2);
                int rightpoint = tx + twidth;
                int leftpoint = tx - dwidth;

                if (leftpoint > 0)
                    TargetTiles.Add(WW.WalkTileList[leftpoint][middlepoint]); //top accesspoint to trolley
                if (rightpoint < WW.WalkTileList.Count)
                    TargetTiles.Add(WW.WalkTileList[rightpoint][middlepoint]); //bot accesspoint to trolley
            }
            else //Horizontal target trolley
            {
                int middlepoint = tx + ((twidth - dwidth) / 2);
                int toppoint = ty - dheight;
                int botpoint = ty + theight;

                if (toppoint > 0)
                    TargetTiles.Add(WW.WalkTileList[middlepoint][toppoint]); //top accesspoint to trolley
                if (botpoint < WW.WalkTileList[0].Count)
                    TargetTiles.Add(WW.WalkTileList[middlepoint][botpoint]); //bot accesspoint to trolley
            }

            return RunAlgo(StartTile, TargetTiles);
        }

        public List<WalkTile> RunAlgoDistrToHarry(LangeHarry Harry)
        {
            WalkTile StartTile = WW.GetTile(agent.RPoint);
            int[] hindices = WW.TileListIndices(Harry.RPoint, Harry.GetRSize());
            int hx = hindices[0]; int hy = hindices[1]; int hwidth = hindices[2]; int hheight = hindices[3];
            int[] dindices = WW.TileListIndices(agent.RPoint, agent.GetRSize());
            int dx = dindices[0]; int dy = dindices[1]; int dwidth = dindices[2]; int dheight = dindices[3];

            List<WalkTile> TargetTiles = new List<WalkTile>();
            if (Harry.IsVertical)
            {
                int leftpoint = hx - dwidth;
                int rightpoint = hx + hwidth;

                if (leftpoint > 0)
                    TargetTiles.Add(WW.WalkTileList[leftpoint][hy + 171 / WalkWay.WALK_TILE_HEIGHT]); //top accesspoint to trolley
                if (rightpoint < WW.WalkTileList[0].Count)
                    TargetTiles.Add(WW.WalkTileList[rightpoint][hy + 171 / WalkWay.WALK_TILE_HEIGHT]); //bot accesspoint to trolley
            }
            else
            {
                int toppoint = hy - dheight;
                int botpoint = hy + hheight;

                if (toppoint > 0)
                    TargetTiles.Add(WW.WalkTileList[hx + 20 / WalkWay.WALK_TILE_WIDTH][toppoint]); //top accesspoint to trolley
                if (botpoint < WW.WalkTileList[0].Count)
                    TargetTiles.Add(WW.WalkTileList[hx + 20 / WalkWay.WALK_TILE_WIDTH][botpoint]); //bot accesspoint to trolley
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
            TargetTiles.RemoveAll(i => i == null);
            return RunAlgo(StartTile, TargetTiles);
        }

        /// <summary>
        /// Runs the dijkstra algorithm.
        /// Returns a list of the shortest path. 0 = start tile, last item = target tile, with the route within it.
        /// </summary>
        /// <returns>The route to take (List of WalkTiles)</returns>
        private List<WalkTile> RunAlgo(WalkTile start_tile, List<WalkTile> target_tiles) 
        {
            //Remove inaccessible target tiles.
            List<int> tiles_to_remove = new List<int>();
            for (int i = 0; i < target_tiles.Count; i++)
            {
                WW.WWC.UpdateLocalClearances(agent, agent.GetTileSize(), target_tiles[i]);
                if (!IsTileAccessible(target_tiles[i]))
                    tiles_to_remove.Add(i);
            }
            for(int i = tiles_to_remove.Count - 1; i >= 0; i--)
                target_tiles.RemoveAt(tiles_to_remove[i]);
            if (target_tiles.Count == 0)
                return null;


            TileQueue.Clear();
            ResetTravelCosts();
            WW.WWC.UpdateClearances(agent, agent.GetTileSize());
 
            start_tile.TravelCost = 0;
            start_tile.visited = true;
            TileQueue.Enqueue(start_tile, 0);

            WalkTile tileA = null;
            WalkTile tileB = null;
            WalkTile tileL = null;
            WalkTile tileR = null;

            WalkTile[] neighbours = new WalkTile[]
            {
                tileA,
                tileB,
                tileL,
                tileR
            };

            AssignClosestTargetTile(start_tile, target_tiles); 

            int nodesSearched = 0;

            while(TileQueue.Count > 0) 
            {
                nodesSearched++;
                WalkTile tile = TileQueue.Dequeue();
                //Target tile was found:
                if (target_tiles.Contains(tile))
                {
                    //Trace back from the target tile to it's parent to calculate the route.
                    List<WalkTile> Route = new List<WalkTile>();
                    WalkTile ptile = tile;

                    Route.Insert(0, ptile);
                    while(ptile.Parent != null)
                    {
                        ptile = ptile.Parent;
                        Route.Insert(0, ptile);
                    }

                    Route.RemoveAt(0);
                    return Route;
                }


                tile.visited = true;

                neighbours[0] = tile.TileAbove();
                neighbours[1] = tile.TileBeneath();
                neighbours[2] = tile.TileLeft();
                neighbours[3] = tile.TileRight();

                int newCost;

                //Add neighbours to priority queue
                for (int i = 0; i < neighbours.Length; i++)
                {

                    WalkTile neighbour = neighbours[i];
                    if (neighbour == null)
                        continue;
                    newCost = tile.TravelCost + SquaredHeuristic(neighbour);
                    if (IsTileAccessible(neighbour) && neighbour.TravelCost > newCost)
                    {
                        neighbour.TravelCost = newCost;
                        neighbour.Parent = tile;
                        if (!neighbour.visited)
                            TileQueue.Enqueue(neighbour, newCost);
                    }
                }
            }

            //No route was found
            return null;
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
            return tile != null && (tile.accessible || tile.occupied_by == agent) && !tile.inaccessible_by_static;
        }
    }
}
