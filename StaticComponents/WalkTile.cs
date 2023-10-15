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
        public bool accessible; //The square is accessible by the agent taking into account its dimensions. 
        public bool IsAgentsTile; //The agent is standing on this tile.
        public Distributer occupied_by;

        private WalkWay WW;

        public Point Simpoint;
        public Point Rpoint;
        public Size SimSize;

        //Used by the dijkstra algo.
        public int TravelCost = int.MaxValue;
        public bool visited = false;
        public WalkTile Parent = null;  //From which tile did you get to here. (start tiles and unreachable tiles are null)

        public const int Rwidth = 10;
        public const int Rheight = 10;

        public WalkTile(int tileX_, int tileY_, Point Simpoint_, Point Rpoint_, Size SimSize_, bool occupied_, WalkWay ww_)
        {
            TileX = tileX_;
            TileY = tileY_;
            Simpoint = Simpoint_;
            Rpoint = Rpoint_;
            occupied = occupied_;
            accessible = !occupied_;
            IsAgentsTile = false;
            WW = ww_;

            SimSize = SimSize_;
        }
        
        public void DrawOccupiance(Graphics g)
        {
            if (occupied_by != null)
            {
                Pen p = new Pen(Color.Green);
                g.DrawRectangle(p, new Rectangle(Simpoint, SimSize));
                return;
            }
            if (occupied)
                g.DrawRectangle(WW.WWTilePen, new Rectangle(Simpoint, SimSize));
            else if (!accessible)
            {
                Pen p = new Pen(Color.Orange);
                g.DrawRectangle(p, new Rectangle(Simpoint, SimSize));
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
