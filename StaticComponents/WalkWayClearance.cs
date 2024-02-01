using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Data;

namespace FloorSimulation
{
    /// <summary>
    /// Class that is used to update clearance
    /// </summary>
    internal class WalkWayClearance
    {
        private WalkWay WW;
        private int TilesChecked = 0;
        private int TilesChanged = 0;
        private int TilesReset = 0;
        bool log;


        public WalkWayClearance(WalkWay WW_, bool log_ = false)
        {
            WW = WW_;
            log = log_;

            foreach (List<WalkTile> TileCol in WW.WalkTileList)
                foreach (WalkTile t in TileCol)
                {
                    t.accessible = true;
                    t.IsAgentsTile = false;
                    t.inaccessible_by_static = false;
                    t.occupied_by = null;
                    if (t.occupied_by != null && !t.occupied) //Makes the inaccessible tiles around the distributer occupied by nothing again.
                        t.occupied_by = null;
                    if (t.TileY == WW.WalkTileListHeight - 1 || t.TileX == WW.WalkTileListWidth - 1 ||
                        t.TileY == 0 || t.TileX == 0) //Occupie the borders of the walkway
                    {
                        t.occupied = true;
                        t.accessible = false;
                        t.inaccessible_by_static = true;
                        t.IsStatic = true;
                    }
                }
        }

        /// <summary>
        /// Updates the clearances using tile obj size
        /// </summary>
        /// <param name="DButer"></param>
        /// <param name="ObjSize"></param>
        public void UpdateClearances(Agent agent, Size ObjSize)
        {
            TilesChecked = 0;
            TilesChanged = 0;
            TilesReset = 0;

            ClearAccessibility(ObjSize);

            int[] indices = WW.TileListIndices(agent.RPoint, agent.GetRSize());
            int x = indices[0]; int y = indices[1]; int width = indices[2]; int height = indices[3];
            foreach (List<WalkTile> TileCol in WW.WalkTileList)
                foreach (WalkTile t in TileCol)
                {
                    TilesChecked++;
                    if(t.TileX >= x && t.TileX < x + width && t.TileY >= y && t.TileY < y + height)
                        UpdateTileClearance(t, ObjSize, agent);
                    else if (t.occupied && t.occupied_by != agent) 
                        UpdateTileClearance(t, ObjSize);
                }
                    
            if (log)
                Console.WriteLine("Tiles Reset: " + TilesReset + " Tiles Checked: " + TilesChecked
                                + " Tiles Changed: " + TilesChanged);
        }

        /// <summary>
        /// Only updates the necessary tiles for an object to walk to 1 tile
        /// Size is in tiles
        /// </summary>
        public void UpdateLocalClearances(Agent agent, Size ObjSize, WalkTile TargetTile)
        {
            TilesChecked = 0;
            TilesChanged = 0;
            TilesReset = 0;

            //ClearAccessibility(new Size (ObjSize.Width + WalkWay.WALK_TILE_WIDTH, ObjSize.Height + WalkWay.WALK_TILE_HEIGHT));
            ClearAccessibility(ObjSize);

            int[] indices = WW.TileListIndices(agent.RPoint, agent.GetRSize());
            int dx = indices[0]; int dy = indices[1]; int dwidth = indices[2]; int dheight = indices[3];

            int maxx = Math.Min(TargetTile.TileX + ObjSize.Width, WW.WalkTileListWidth);
            int maxy = Math.Min(TargetTile.TileY + ObjSize.Height, WW.WalkTileListHeight);
            int minx = Math.Max(0, TargetTile.TileX);
            int miny = Math.Max(0, TargetTile.TileY);

            WalkTile t;
            for (int x = minx; x < maxx; x++)
                for (int y = miny; y < maxy; y++)
                {
                    t = WW.WalkTileList[x][y];
                    TilesChecked++;
                    if(t.TileX >= dx && t.TileX < dx + dwidth && t.TileY >= dy && t.TileY < dy + dheight)
                        UpdateTileClearance(t, ObjSize, agent);
                    else if (t.occupied && t.occupied_by != agent) 
                        UpdateTileClearance(t, ObjSize);
                }

            if (log)
                Console.WriteLine("Tiles Reset: " + TilesReset + " Tiles Checked: " + TilesChecked
                                + " Tiles Changed: " + TilesChanged);
        }

        public bool IsBlockedInDirection(Agent agent, WalkTile targettile)
        {
            int[] indices = WW.TileListIndices(agent.RPoint, agent.GetRSize());
            int dwidth = indices[2]; int dheight = indices[3];

            if (agent.RPoint.Y - targettile.Rpoint.Y < -8)//Moving Down
            {
                int y = targettile.TileY + dheight - 1;
                if (y >= WW.WalkTileListHeight)
                    return false;
                for (int x = targettile.TileX; x < targettile.TileX + dwidth; x += 4)
                    if (WW.WalkTileList[x][y].occupied)
                        return true;
            }
            else if (agent.RPoint.Y - targettile.Rpoint.Y > 8) //Moving Up
            {
                int y = targettile.TileY;
                for (int x = targettile.TileX; x < targettile.TileX + dwidth; x += 4)
                    if (WW.WalkTileList[x][y].occupied)
                        return true;
            }
            else if (agent.RPoint.X - targettile.Rpoint.X > 8) // Moving Left
            {
                int x = targettile.TileX;
                for (int y = targettile.TileY; y < targettile.TileY + dheight; y += 4)
                    if (WW.WalkTileList[x][y].occupied)
                        return true;
            }
            else if (agent.RPoint.X - targettile.Rpoint.X < -8) // Moving Right
            {
                int x = targettile.TileX + dwidth - 1;
                if (x >= WW.WalkTileListWidth)
                    return false;
                for (int y = targettile.TileY; y < targettile.TileY + dheight; y += 4)
                    if (WW.WalkTileList[x][y].occupied)
                        return true;
            }
            return false;
        }
        
        /// <summary>
        /// Updates the accessibility of a tile based on the object size that needs to traverse the walkway
        /// Creates a space above and to the left of this tile which will become inaccessible
        /// </summary>
        private void UpdateTileClearance(WalkTile t, Size ObjSize, Agent agent = null)
        {
            int leftx = Math.Min(ObjSize.Width, t.TileX);
            int topy = Math.Min(ObjSize.Height, t.TileY);

            WalkTile targett;
            for (int x = t.TileX; x > t.TileX - leftx; x--)
                for (int y = t.TileY; y > t.TileY - topy; y--)
                {
                    targett = WW.WalkTileList[x][y];
                    if (targett == t)
                    {
                        if (targett.occupied)
                        {
                            targett.accessible = false;
                            if (agent != null)
                                targett.occupied_by = agent;
                            else
                                targett.inaccessible_by_static = true;
                        }
                        continue;
                    }

                    if(targett.occupied && (targett.occupied_by == agent))
                        topy = t.TileY - targett.TileY;

                    TilesChanged++;
                    targett.accessible = false;

                    if (agent != null)
                        targett.occupied_by = agent;
                    else
                        targett.inaccessible_by_static = true;
                }
        }

        /// <summary>
        /// Resets all the accessibility variables
        /// Only the occupied tiles will stay occupied
        /// </summary>
        private void ClearAccessibility(Size ObjSize)
        {
            foreach (List<WalkTile> TileCol in WW.WalkTileList)
                foreach (WalkTile t in TileCol)
                {
                    if (t.accessible || t.IsStatic)
                        continue;
                    TilesReset++;
                    t.accessible = true;
                    t.IsAgentsTile = false;
                    t.inaccessible_by_static = false;
                    t.occupied_by = null;
                    if (t.occupied_by != null && !t.occupied) //Makes the inaccessible tiles around the distributer occupied by nothing again.
                        t.occupied_by = null;
                }

        }

        public void ClearOccupiedBy()
        {
            foreach (List<WalkTile> TileCol in WW.WalkTileList)
                foreach (WalkTile t in TileCol)
                    t.occupied_by = null;
        }
    }
}
