﻿using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace FloorSimulation
{

    /// <summary>
    /// The place where the distributers will be travelling trough the floor
    /// Is used for path finding and collission detection.
    /// </summary>
    internal class WalkWay
    {
        private Floor floor;
        public Point RPointWW;
        public Point PointWW;
        public Size RSizeWW;
        public Size SizeWW;

        private Brush WWBrush;
        public Pen WWTilePen;
        public List<Distributer> DistrList;
        public List<List<WalkTile>> WalkTileList; //call by WalkTileList[x][y]
        public int WalkTileListWidth;
        public int WalkTileListHeight;

        public const int WALK_TILE_WIDTH = 10; //cm
        public const int WALK_TILE_HEIGHT = 10; //cm

        /// <summary>
        /// 
        /// </summary>
        /// <param name="RP"></param>
        /// <param name="RS">Should always be devisable by Walk tile width and height</param>
        /// <param name="floor_"></param>
        public WalkWay(Point RP, Size RS, Floor floor_)  
        {
            floor = floor_;
            RPointWW = RP;
            PointWW = floor.ConvertToSimPoint(RP);
            RSizeWW = RS;
            SizeWW = floor.ConvertToSimSize(RS);

            WalkTileListWidth = RSizeWW.Width / WALK_TILE_WIDTH;
            WalkTileListHeight = RSizeWW.Height / WALK_TILE_HEIGHT;

            WWBrush = new SolidBrush(Color.Gray);
            WWTilePen = new Pen(Color.Red);
            InitWalkTiles();
            ;
        }

        /// <summary>
        /// Draws the gray walkway first, then the occupied tiles are drawn red.
        /// </summary>
        /// <param name="g"></param>
        public void DrawObject(Graphics g, bool DrawOccupiance = false)
        {
            g.FillRectangle(WWBrush, new Rectangle(PointWW, SizeWW));
            if (DrawOccupiance)
            {
                foreach(List<WalkTile> l in WalkTileList)
                    foreach(WalkTile t in l)
                        t.DrawOccupiance(g);
            }
        }

        /// <summary>
        /// Initializes the walktiles and assigns the default values to them.
        /// Should only be run on initiation.
        /// </summary>
        public void InitWalkTiles()
        {
            WalkTileList = new List<List<WalkTile>>();
            Size SimSize = floor.ConvertToSimSize(new Size(WalkTile.Rwidth, WalkTile.Rheight));

            for (int x = 0; x < WalkTileListWidth; x++)
            {
                List<WalkTile> col = new List<WalkTile>(WalkTileListHeight); 

                for (int y = 0; y < WalkTileListHeight; y++)
                {
                    Point Rp = new Point(RPointWW.X + x * WALK_TILE_WIDTH,
                                         RPointWW.Y + y * WALK_TILE_HEIGHT);
                    Point p = floor.ConvertToSimPoint(Rp);

                    col.Add(new WalkTile(x, y, p, Rp, SimSize, false, this));
                }
                WalkTileList.Add(col);
            }

            UpdateClearances();
        }

        /// <summary>
        /// Updates all the clearances of all the tiles in the walktile list
        /// </summary>
        public void UpdateClearances()
        {
            // TODO: Make this smarter and faster.
            foreach(List<WalkTile> col in WalkTileList)
            {
                foreach(WalkTile tile in col)
                {
                    tile.UpdateClearance();
                }
            }
        }

        /// <summary>
        /// Creates occupied tiles for in the walktilelist.
        /// Real points and Real sizes. it takes up more space then the real list.
        /// </summary>
        /// <param name="Rp"></param>
        /// <param name="Rs"></param>
        public void fill_tiles(Point Rp, Size Rs)
        {
            int[] indices = TileListIndices(Rp, Rs);
            int x = indices[0]; int y = indices[1]; int width = indices[2]; int height = indices[3];

            for (int xi = x; xi < x + width; xi++) 
                for (int yi = y; yi < y + height; yi++)
                    WalkTileList[xi][yi].occupied = true;
        }

        /// <summary>
        /// Removes occupied tiles for in the walktilelist.
        /// Real points and Real sizes. it takes up more space then the real list.
        /// </summary>
        /// <param name="Rp"></param>
        /// <param name="Rs"></param>
        public void unfill_tiles(Point Rp, Size Rs)
        {
            int[] indices = TileListIndices(Rp, Rs);
            int x = indices[0]; int y = indices[1]; int width = indices[2]; int height = indices[3];

            for (int xi = x; xi < x + width; xi++) 
                for (int yi = y; yi < y + height; yi++)
                    WalkTileList[xi][yi].occupied = false;
        }

        /// <summary>
        /// Uses real point and Real size to find the x, y, width, height in the walktile list
        /// </summary>
        /// <returns>Int array of size 4: [x, y, width, height]</returns>
        public int[] TileListIndices(Point Rp, Size Rs)
        {
            int x = Rp.X / WalkTile.Rwidth;
            int y = Rp.Y / WalkTile.Rheight;

            int width = (int)Math.Ceiling((double)Rs.Width / WalkTile.Rwidth);
            int height = (int)Math.Ceiling((double)Rs.Height / WalkTile.Rheight);
            int[] result = { x, y, width, height };

            return result;

        }

        /// <summary>
        /// Get tiles by the Real Point of an object (top left tile).
        /// </summary>
        /// <param name="Rp"></param>
        /// <returns></returns>
        public WalkTile GetTile(Point Rp)
        {
            int x = Rp.X / WalkTile.Rwidth; int y = Rp.Y / WalkTile.Rheight;
            return WalkTileList[x][y];
        }
    }
}
