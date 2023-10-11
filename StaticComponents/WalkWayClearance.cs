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

        public WalkWayClearance(WalkWay WW_)
        {
            WW = WW_;
        }

        public void UpdateClearances(Distributer DButer, Size ObjSize)
        {
            foreach (List<WalkTile> TileCol in WW.WalkTileList)
                foreach (WalkTile t in TileCol)
                {
                    t.accessible = true;
                    t.IsAgentsTile = false;
                }

            Clear(DButer, ObjSize);

            foreach (List<WalkTile> TileCol in WW.WalkTileList)
                foreach (WalkTile t in TileCol)
                    if (t.occupied && !t.IsAgentsTile) 
                        //If the tile is occupied and the agent is not standing on this tile, update it's accessibility
                        UpdateTileClearance(t, ObjSize);
        }
        
        /// <summary>
        /// Updates the accessibility of a tile based on the object size that needs to traverse the walkway
        /// </summary>
        private void UpdateTileClearance(WalkTile t, Size ObjSize)
        {
            int leftx = Math.Min(ObjSize.Width, t.TileX);
            int topy = Math.Min(ObjSize.Height, t.TileY);

            for(int x = t.TileX; x > t.TileX - leftx; x--) 
                for (int y = t.TileY; y > t.TileY - topy; y--)
                    WW.WalkTileList[x][y].accessible = false;
        }

        /// <summary>
        /// Make all tiles around the distributer accessible.
        /// Used so the object can in fact move trough itself.
        /// </summary>
        /// <param name="DButer"></param>
        /// <param name="ObjSize">Size of the distributer in tiles</param>
        private void Clear(Distributer DButer, Size ObjSize)
        {
            int[] dindices = WW.TileListIndices(DButer.RDPoint, DButer.RDistributerSize);
            int dx = dindices[0]; int dy = dindices[1]; int dwidth = dindices[2]; int dheight = dindices[3];

            int StartX = Math.Max(dx - ObjSize.Width, 0);
            int EndX = Math.Min(dx + ObjSize.Width, WW.WalkTileListWidth - 1);

            int StartY = Math.Max(dy - ObjSize.Height, 0);
            int EndY = Math.Min(dy + ObjSize.Height, WW.WalkTileListHeight - 1);

            for(int x = StartX; x < EndX; x++)
                for (int y = StartY; y < EndY; y++)
                {
                    WW.WalkTileList[x][y].accessible = true;
                    if(x >= dx && y >= dy)
                        WW.WalkTileList[x][y].IsAgentsTile = true;
                }
        }
    }
}
