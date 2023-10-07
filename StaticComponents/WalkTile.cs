using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace FloorSimulation
{
    /// <summary>
    /// Tiles are 10x10cm 
    /// Occupied means there is something in the way.
    /// </summary>
    internal class WalkTile
    {
        public int TileX;
        public int TileY;
        public bool occupied;
        private WalkWay WW;
        public int ClearanceBot; //How many free tiles to the bottom
        public int ClearanceRight; //How many free tile to the right

        public Point Simpoint;
        public Point Rpoint;

        //Used by the dijkstra algo.
        public int TravelCost = int.MaxValue;
        public bool visited = false;
        public WalkTile Parent = null;  //From which tile did you get to here. (start tiles and unreachable tiles are null)

        public WalkTile(int tileX_, int tileY_, Point Simpoint_, Point Rpoint_, bool occupied_, WalkWay ww_)
        {
            TileX = tileX_;
            TileY = tileY_;
            Simpoint = Simpoint_;
            Rpoint = Rpoint_;
            occupied = occupied_;
            WW = ww_;
        }
        
        /// <summary>
        /// Updates Clearance right and clearance left of this tile.
        /// </summary>
        public void UpdateClearance()
        {
            // Clearance on the right
            ClearanceRight = 0;
            for (int x = TileX; x < WW.WalkTileListWidth; x++)
            {
                if (WW.WalkTileList[x][TileY].occupied)
                    break;
                ClearanceRight++;
            }

            // Clearance on the bottom
            ClearanceBot = 0;
            for (int y = TileY; y < WW.WalkTileListHeight; y++)
            {
                if (WW.WalkTileList[TileX][y].occupied)
                    break;
                ClearanceBot++;
            }
        }

        /// <summary>
        /// returns the tile above this tile.
        /// If this is the highest tile, return null.
        /// </summary>
        public WalkTile TileBeneath()
        {
            if (TileY - 1 >= 0)
                return WW.WalkTileList[TileX][TileY - 1];
            else
                return null;
        }

        /// <summary>
        /// returns the tile beneath this tile.
        /// If this is the lowest tile, return null.
        /// </summary>
        public WalkTile TileAbove()
        {
            if (TileY + 1 < WW.WalkTileListHeight)
                return WW.WalkTileList[TileX][TileY + 1];
            else
                return null;
        }

        /// <summary>
        /// returns the tile to the left of this tile.
        /// If this tile is the tile most left, return null.
        /// </summary>
        public WalkTile TileLeft()
        {
            if (TileX - 1 >= 0)
                return WW.WalkTileList[TileX - 1][TileY];
            else
                return null;
        }

        /// <summary>
        /// returns the tile to the right of this tile.
        /// If this tile is the tile most right, return null.
        /// </summary>
        public WalkTile TileRight()
        {
            if (TileX + 1 < WW.WalkTileListWidth)
                return WW.WalkTileList[TileX + 1][TileY];
            else
                return null;
        }

        public override string ToString()
        {
            return "X: " + TileX + " Y: " + TileY + " Occupied: " + occupied;
        }
    }
}
