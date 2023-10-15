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
        private Distributer dummy_db;

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
                    if (t.occupied_by != null && !t.occupied) //Makes the inaccessible tiles around the distributer occupied by nothing again.
                        t.occupied_by = null;
                }

            foreach (List<WalkTile> TileCol in WW.WalkTileList)
                foreach (WalkTile t in TileCol)
                {
                    if (t.occupied && t.occupied_by != DButer) 
                        //If the tile is occupied and the agent is not standing on this tile, update it's accessibility
                        //TODO: Detect which object its occupied by
                        UpdateTileClearance(t, ObjSize);
                    else if (t.occupied_by == DButer)
                        //If the Agent is standing on this tile update the tiles around to be occupied by this agent.
                        UpdateTileClearance(t, ObjSize, DButer);
                }
                    
        }
        
        /// <summary>
        /// Updates the accessibility of a tile based on the object size that needs to traverse the walkway
        /// Creates a space above and to the left of this tile which will become inaccessible
        /// </summary>
        private void UpdateTileClearance(WalkTile t, Size ObjSize, Distributer DButer = null)
        {
            int leftx = Math.Min(ObjSize.Width, t.TileX);
            int topy = Math.Min(ObjSize.Height, t.TileY);

            for(int x = t.TileX; x > t.TileX - leftx; x--) 
                for (int y = t.TileY; y > t.TileY - topy; y--)
                {
                    WW.WalkTileList[x][y].accessible = false;
                    if (DButer != null)
                        WW.WalkTileList[x][y].occupied_by = DButer;
                }
        }
    }
}
