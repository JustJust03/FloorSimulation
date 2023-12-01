using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.CodeDom.Compiler;

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
        public bool IsStatic; //A tile that is occupied and inaccessible For ever (walls and columns)
        public Distributer occupied_by;
        public bool inaccessible_by_static; //Is this tile unavailable because of a static object.

        private WalkWay WW;

        public Point Simpoint;
        public Point Rpoint;
        public Size SimSize;

        //Used by the dijkstra algo.
        public int TravelCost = int.MaxValue;
        public bool visited = false;
        public WalkTile Parent = null;  //From which tile did you get to here. (start tiles and unreachable tiles are null)
        public int visits;

        public const int Rwidth = 10;
        public const int Rheight = 10;

        public int AverageVisits = 0;
        private int Startx;
        private int Starty;
        private int Endx;
        private int Endy;

        public WalkTile(int tileX_, int tileY_, Point Simpoint_, Point Rpoint_, Size SimSize_, bool occupied_, WalkWay ww_)
        {
            TileX = tileX_;
            TileY = tileY_;
            Simpoint = Simpoint_;
            Rpoint = Rpoint_;
            occupied = occupied_;
            accessible = !occupied_;
            inaccessible_by_static = accessible;
            IsAgentsTile = false;
            WW = ww_;

            SimSize = SimSize_;
        }
        
        public void DrawOccupiance(Graphics g)
        {
            if (occupied_by != null && inaccessible_by_static)
            {
                Pen p = new Pen(Color.Purple);
                g.DrawRectangle(p, new Rectangle(Simpoint, SimSize));
                return;
            }
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

        public int UpdateAverageVisits()
        {
            int Neighbours = 10;
            float deltax = 0;
            float deltay = 0;
            float temp = 0;
            AverageVisits = 0;
            Startx = Math.Max(TileX - Neighbours, 0);
            Starty = Math.Max(TileY - Neighbours, 0);
            Endx = Math.Min(TileX + Neighbours + 1, WW.WalkTileListWidth);
            Endy = Math.Min(TileY + Neighbours + 1, WW.WalkTileListHeight);

            for (int x = Startx; x < Endx; x++)
                for (int y = Starty; y < Endy; y++)
                {
                    deltax = 1 - (Math.Abs(TileX - x) / (float)Neighbours);
                    deltay = 1 - (Math.Abs(TileY - y) / (float)Neighbours);

                    temp = deltax * deltay;
                    AverageVisits += (int)(WW.WalkTileList[x][y].visits * temp);
                }
            AverageVisits = AverageVisits / ((Neighbours * 2 + 1) * (Neighbours * 2 + 1));
            return AverageVisits;
        }

        /// <summary>
        /// You need to run UpdateAverageVisits first...
        /// </summary>
        public void DrawHeatmap(Graphics g, int max)
        {
            double x = Math.Min((double)AverageVisits / (double)max, 1.0);
            double y = Math.Sqrt(1 - (1 - x) * (1 - x));
            int Red = (int)(y * 255);
            if (AverageVisits > 10 && AverageVisits < 90)
                ;
            int Blue = 255 - Red;

            Brush b = new SolidBrush(Color.FromArgb(Red, Red, 0, Blue));
            g.FillRectangle(b, new Rectangle(Simpoint, SimSize));
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
