﻿using System;
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

        public WalkWayClearance(WalkWay WW_)
        {
            WW = WW_;

            foreach (List<WalkTile> TileCol in WW.WalkTileList)
                foreach (WalkTile t in TileCol)
                {
                    t.accessible = true;
                    t.IsAgentsTile = false;
                    t.inaccessible_by_static = false;
                    t.occupied_by = null;
                    if (t.occupied_by != null && !t.occupied) //Makes the inaccessible tiles around the distributer occupied by nothing again.
                        t.occupied_by = null;
                    if (t.TileY == WW.WalkTileListHeight - 1 || t.TileX == WW.WalkTileListWidth - 1) //Occupie the borders of the walkway
                        t.occupied = true;
                }
        }

        /// <summary>
        /// Updates the clearances using tile obj size
        /// </summary>
        /// <param name="DButer"></param>
        /// <param name="ObjSize"></param>
        public void UpdateClearances(Distributer DButer, Size ObjSize)
        {
            foreach (List<WalkTile> TileCol in WW.WalkTileList)
                foreach (WalkTile t in TileCol)
                {
                    if (t.accessible)
                        continue;
                    t.accessible = true;
                    t.IsAgentsTile = false;
                    t.inaccessible_by_static = false;
                    t.occupied_by = null;
                    if (t.occupied_by != null && !t.occupied) //Makes the inaccessible tiles around the distributer occupied by nothing again.
                        t.occupied_by = null;
                    if(t.TileY == WW.WalkTileListHeight - 1|| t.TileX == WW.WalkTileListWidth - 1) //Occupie the borders of the walkway
                        UpdateTileClearance(t, ObjSize);
                }

            int[] indices = WW.TileListIndices(DButer.RDPoint, DButer.GetRDbuterSize());
            int x = indices[0]; int y = indices[1]; int width = indices[2]; int height = indices[3];
            foreach (List<WalkTile> TileCol in WW.WalkTileList)
                foreach (WalkTile t in TileCol)
                {
                    if(t.TileX >= x && t.TileX < x + width && t.TileY >= y && t.TileY < y + height)
                        UpdateTileClearance(t, ObjSize, DButer);
                    else if (t.occupied && t.occupied_by != DButer) 
                        UpdateTileClearance(t, ObjSize);
                }
        }
        
        /// <summary>
        /// Updates the accessibility of a tile based on the object size that needs to traverse the walkway
        /// Creates a space above and to the left of this tile which will become inaccessible
        /// </summary>
        private void UpdateTileClearance(WalkTile t, Size ObjSize, Distributer DButer = null)
        {
            int leftx = Math.Min(ObjSize.Width, t.TileX + 1);
            int topy = Math.Min(ObjSize.Height, t.TileY + 1);

            WalkTile targett;
            for (int x = t.TileX; x > t.TileX - leftx; x--)
                for (int y = t.TileY; y > t.TileY - topy; y--)
                {
                    targett = WW.WalkTileList[x][y];
                    if (targett == t && DButer == null)
                        continue;
                    if(targett.occupied && DButer == null)
                        topy = t.TileY - targett.TileY;

                    targett.accessible = false;

                    if (DButer != null)
                        targett.occupied_by = DButer;
                    else
                        targett.inaccessible_by_static = true;
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
